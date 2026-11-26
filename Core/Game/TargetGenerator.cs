using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Game
{
    public sealed class TargetInfo
    {
        public string Name;
        public int Level;
        public int Difficulty;      // 0 kolay, 1 normal, 2 zor
        public DefenseSetup Defense;
        public ResourceCost Pool;   // yağmalanabilir kaynak havuzu
        public int Seed;
    }

    /// <summary>
    /// Oyuncu seviyesine göre deterministik düşman köy üretir (aynı tohum = aynı köy). Kuralları oyuncununkilerle aynıdır
    /// (yol hakkı, giriş, kuleler yola komşu) ve Validate'ten geçer. Sayılar GEÇİCİDİR.
    /// </summary>
    public static class TargetGenerator
    {
        private static readonly string[] Names =
        {
            "Kuzey Kalesi", "Gölge Köyü", "Demirkaya", "Yeşilvadi", "Kızıltepe", "Buzdağı Karakolu", "Altınova", "Karaorman",
            "Tuzlayurt", "Rüzgârburnu", "Taşköprü", "Ejderpınarı", "Sisliada", "Kervansaray", "Bozkır Hanı", "Çamlıbel"
        };

        /// <summary>
        /// Seviye başına hedef gücü düzeltmesi (‰; kule hasarı/canı ve bina canı). Rastgele savaş matrisinde her seviyede
        /// ≈ %50 zafer için otomatik kalibre edilir (BalanceSimTests.CalibrateLevelPower → docs/power-calibration.txt). İndeks = seviye.
        /// </summary>
        public static int[] LevelPowerPermille = { 0, 0, 0, 4050, 8850, -300, -600, 150, -350, 0, -150, 0, -200, 600, 350, 400, 100, 1350, 1050, 400, 0, 250, 100, 250, 150, 550, 800, 1050, 1150, 1200, 1950 }; // kalibre 15.11.2026 (köy düzeni + asker yeri sonrası)

        /// <summary>Kule sayısı çarpanı (%): denge ayarı. Varsayılan 100.</summary>
        public static int TowerScalePct = 100;

        public static int LevelFor(int playerLevel, int difficulty)
        {
            int l = playerLevel + (difficulty - 1) * 2;
            return Math.Max(1, Math.Min(GameRules.MaxLevel, l));
        }

        public static TargetInfo Generate(int playerLevel, int difficulty, int seed)
        {
            int level = LevelFor(playerLevel, difficulty);
            var rng = new DeterministicRng(seed * 7 + level * 131 + difficulty * 17 + 5);
            var layout = new VillageLayout(11, RoadRules.RoadLimitForLevel(level), RoadRules.MaxEntrances);
            string e;

            // Girişler: Sv10'dan itibaren 2, Sv20'den itibaren 3
            var entranceXs = new List<int> { 5 };
            if (level >= 10) entranceXs.Add(2);
            if (level >= 20) entranceXs.Add(8);
            foreach (int x in entranceXs) layout.ToggleEntrance(new GridPos(x, 0), out e);

            // Yol adayları (sırayla, mevcut yola komşu olanlar eklenir)
            var cand = new List<GridPos>();
            for (int y = 1; y <= 9; y++) cand.Add(new GridPos(5, y));
            // Ek girişleri omurgaya bağla (önce): giriş sütunu y=1..5, sonra 5. satırda omurgaya doğru
            foreach (int ex in entranceXs)
            {
                if (ex == 5) continue;
                for (int y = 1; y <= 5; y++) cand.Add(new GridPos(ex, y));
                int step = ex < 5 ? 1 : -1;
                for (int x = ex + step; x != 5; x += step) cand.Add(new GridPos(x, 5));
            }
            foreach (int row in new[] { 9, 5, 7, 3 })
                for (int d = 1; d <= 5; d++) { cand.Add(new GridPos(5 - d, row)); cand.Add(new GridPos(5 + d, row)); }
            foreach (int col in new[] { 2, 8, 0, 10, 3, 7 })
                for (int y = 1; y <= 9; y++) cand.Add(new GridPos(col, y));
            bool added = true;
            while (added && layout.RoadCount < layout.RoadLimit)
            {
                added = false;
                foreach (var p in cand)
                {
                    if (layout.RoadCount >= layout.RoadLimit) break;
                    if (layout.Get(p) != CellKind.Empty || !layout.InBounds(p)) continue;
                    if (!RoadNeighbour(layout, p)) continue;
                    if (layout.TryPlace(p, CellKind.Road, out e)) added = true;
                }
            }

            // Boş hücreler, yola yakınlığa göre sıralı (aynı mesafede tohumlu karışım)
            var cells = new List<GridPos>();
            for (int y = 1; y < layout.Size; y++)
                for (int x = 0; x < layout.Size; x++)
                {
                    var p = new GridPos(x, y);
                    if (layout.Get(p) == CellKind.Empty) cells.Add(p);
                }
            var keyed = new List<KeyValuePair<int, GridPos>>();
            foreach (var p in cells) keyed.Add(new KeyValuePair<int, GridPos>(rng.Next(1000) + (AdjacentRoad(layout, p) ? 0 : 5000), p));
            keyed.Sort((a, b) => a.Key.CompareTo(b.Key));

            // Savunma araştırması: o seviyedeki tipik oyuncunun kule/bina bonusu kadar (oyuncu saldırırken kendi araştırmasını kullanır)
            int rp = Math.Max(-700, ExpectedDefenseResearchPermille(level) + LevelPowerPermille[Math.Max(1, Math.Min(30, level))]);
            var setup = new DefenseSetup(layout) { Mods = new CombatMods { TowerDamage = rp, TowerHp = rp, BuildingHp = rp } };
            // Kule sayısı: o seviyedeki oyuncunun kule hakkının bir oranı (Kolay %50, Normal %80, Zor %100).
            // Seviyeler: tipik oyuncunun kule seviyesi (GameRules.ExpectedTowerLevel), Kolay -1 / Zor +1.
            int countPct = difficulty == 0 ? 50 : (difficulty == 2 ? 100 : 80);
            int towerTarget = Math.Max(1, GameRules.ExpectedTowerSlots(level) * countPct / 100 * TowerScalePct / 100);
            int structLevel = Math.Max(1, Math.Min(GameRules.MaxStructureLevel(level), GameRules.ExpectedTowerLevel(level) + (difficulty == 0 ? -1 : difficulty == 2 ? (level >= 20 ? 3 : 2) : (level >= 20 ? 1 : 0))));
            var towerTypes = new List<TowerType>();
            foreach (TowerType t in Enum.GetValues(typeof(TowerType))) if (ContentCatalog.Tower(t).UnlockLevel <= level) towerTypes.Add(t);
            if (towerTypes.Count == 0) towerTypes.Add(TowerType.Archer); // Sv1 hedeflerde de en az bir okçu kulesi olsun
            var bldTypes = new List<BuildingType>();
            foreach (BuildingType t in Enum.GetValues(typeof(BuildingType))) if (ContentCatalog.Building(t).UnlockLevel <= level) bldTypes.Add(t);
            int bldTarget = 4 + level / 2;

            int towers = 0, blds = 0, ti = 0, bi = 0;
            // Önce yola komşu hücrelere kule, kalanlara bina
            foreach (var kv in keyed)
            {
                var p = kv.Value;
                if (towers < towerTarget && AdjacentRoad(layout, p))
                {
                    var tt = towerTypes[ti++ % towerTypes.Count];
                    if (layout.TryPlace(p, CellKind.Tower, out e)) { setup.Towers[p] = TargetTower(tt, structLevel, level, towers); towers++; }
                }
            }
            foreach (var kv in keyed)
            {
                var p = kv.Value;
                if (blds >= bldTarget) break;
                if (layout.Get(p) != CellKind.Empty || !AdjacentRoad(layout, p)) continue;
                var bt = bldTypes[bi++ % bldTypes.Count];
                if (layout.TryPlace(p, CellKind.Building, out e)) { setup.Buildings[p] = new BuildingSpec(bt, structLevel); blds++; }
            }

            // Tuzaklar (Sv3+): yol hücrelerine, girişe değil
            if (level >= 3)
            {
                var roads = layout.CellsOf(CellKind.Road);
                // tipik oyuncu: tuzak hakkı 4, Sv14+ araştırmayla 6; tuzak türleri seviyeyle açılır
                int trapCount = Math.Min(roads.Count / 3, Math.Min(level >= 14 ? GameRules.ResearchTrapSlots : GameRules.StartTrapSlots, level / 3 + 1));
                var kinds = new List<TrapType>();
                for (int k = 1; k <= TrapCatalog.Count; k++) if (TrapCatalog.Get((TrapType)k).UnlockLevel <= level) kinds.Add((TrapType)k);
                if (kinds.Count == 0) kinds.Add(TrapType.Spikes);
                for (int i = 0; i < trapCount && roads.Count > 0; i++)
                {
                    var p = roads[rng.Next(roads.Count)];
                    if (layout.IsEntrance(p) || setup.Traps.ContainsKey(p)) continue;
                    setup.Traps[p] = kinds[i % kinds.Count];
                }
            }

            int pct = difficulty == 0 ? 80 : (difficulty == 2 ? 130 : 100);
            var cfg = ResourceConfig.CreateDefault();
            var pool = GameRules.Scale(new ResourceCost(350 * level, 200 * level, 100 * level, 50 * level), cfg, level, pct);
            return new TargetInfo { Name = Names[rng.Next(Names.Length)], Level = level, Difficulty = difficulty, Defense = setup, Pool = pool, Seed = seed };
        }

        /// <summary>Tipik oyuncunun o seviyedeki savunma araştırma bonusu (‰). Sv10≈%3, Sv20≈%13, Sv30≈%30 (simülasyondan kaba tahmin).</summary>
        public static int ExpectedDefenseResearchPermille(int level) => level * level; // geç oyun zorluğu (Sv30 ≈ +%90; 25.11.2026 yeni araştırma kataloğuyla)

        /// <summary>Tipik oyuncunun saldırı araştırma bonusu (‰): baskıncı askerleri bunu alır.</summary>
        public static int ExpectedAttackResearchPermille(int level) => level * level / 3;

        private static bool RoadNeighbour(VillageLayout l, GridPos p)
        {
            return l.Get(new GridPos(p.X + 1, p.Y)) == CellKind.Road || l.Get(new GridPos(p.X - 1, p.Y)) == CellKind.Road ||
                   l.Get(new GridPos(p.X, p.Y + 1)) == CellKind.Road || l.Get(new GridPos(p.X, p.Y - 1)) == CellKind.Road;
        }

        /// <summary>Yola bağlı mı (sağ/sol/üst/alt; oyuncu köyüyle aynı kural).</summary>
        /// <summary>
        /// Hedef köy kulesi: tipik oyuncu gibi geliştirme dalları eşit, Okçu kulelerine seviyeyle açılan elementler sırayla
        /// (Sv6 Ok/Ateş, Sv9 Buz, Sv12 Zehir, Sv15 Büyü, Sv18 Şimşek; ilk kule elementsiz kalabilir).
        /// </summary>
        private static TowerSpec TargetTower(TowerType t, int structLevel, int playerLevel, int index)
        {
            var spec = new TowerSpec(t, structLevel, 0) { BranchA = (structLevel - 1) / 2 };
            spec.BranchB = structLevel - 1 - spec.BranchA;
            // savunma elementi yalnızca Duvarda: Ateş Sv5, Buz Sv11, Zehir Sv16 (tipik oyuncunun araştırdığı kadar), sırayla
            var defs = new List<TowerElement>();
            if (playerLevel >= 5) defs.Add(TowerElement.Fire);
            if (playerLevel >= 11) defs.Add(TowerElement.Ice);
            if (playerLevel >= 16) defs.Add(TowerElement.Poison);
            if (t == TowerType.Wall && defs.Count > 0) spec.DefElement = defs[index % defs.Count];
            if (t != TowerType.Archer || playerLevel < 6) return spec;
            var avail = new List<TowerElement> { TowerElement.None, TowerElement.Arrow, TowerElement.Fire };
            if (playerLevel >= 9) avail.Add(TowerElement.Ice);
            if (playerLevel >= 12) avail.Add(TowerElement.Poison);
            if (playerLevel >= 15) avail.Add(TowerElement.Magic);
            if (playerLevel >= 18) avail.Add(TowerElement.Lightning);
            if (playerLevel >= 12) avail.Remove(TowerElement.None);
            spec.Element = avail[index % avail.Count];
            return spec;
        }

        private static bool AdjacentRoad(VillageLayout l, GridPos p) => l.HasAdjacentRoad(p);
    }
}
