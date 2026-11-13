using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Tests
{
    /// <summary>
    /// Denge simülasyonu: kurallara uyan, açgözlü "aktif oyuncu" botu GameState'i gerçek API ile oynar
    /// (yol/bina/kule kurar, yükseltir, araştırır, eğitir, Normal hedefe saldırır, görev ödülü alır).
    /// Ölçülen: her seviyeye kaç günde ulaşıldığı ve o anki gerçek orduyla Kolay/Normal/Zor kazanma oranı.
    /// Çıktı: docs/balance-report.txt. Tüm sayılar GEÇİCİ dengeleme içindir.
    /// </summary>
    public sealed class PlayerSim
    {
        public readonly GameState G = GameState.NewGame();
        public readonly int CheckInHours;
        public int Day, Attacks, Wins, Stars, RaidsTotal, RaidsDefended, TargetSeed = 1;
        public long XpQuest, XpPlace, XpTime, XpBattle; // XP kaynak dökümü (XpTime = biten yükseltmeler)
        public readonly Dictionary<int, double> DayReached = new Dictionary<int, double> { { 1, 0 } };
        public readonly List<string> Snapshots = new List<string>();
        private static readonly int[] SnapshotLevels = { 3, 5, 8, 10, 15, 20, 25, 30 };
        private readonly HashSet<int> snapped = new HashSet<int>();

        public PlayerSim(int checkInHours) { CheckInHours = checkInHours; }

        private static readonly (BuildingType t, int want)[] BuildPlan =
        {
            (BuildingType.VillagerHouse, 5), (BuildingType.Barracks, 1), (BuildingType.University, 1),
            (BuildingType.Forest, 2), (BuildingType.LumberDepot, 2), (BuildingType.StoneQuarry, 2), (BuildingType.StoneDepot, 1),
            (BuildingType.GoldMine, 2), (BuildingType.Bank, 1), (BuildingType.IronMine, 2), (BuildingType.IronDepot, 1),
            (BuildingType.Barracks, 2), (BuildingType.Forest, 3), (BuildingType.StoneQuarry, 3), (BuildingType.Barracks, 3),
        };

        /// <summary>Gün gün oynatır; maxDays dolunca ya da Sv30'da durur.</summary>
        public void Run(int maxDays)
        {
            for (Day = 0; Day < maxDays && G.Level < 30; Day++)
            {
                var raidBefore = G.LastRaid;
                long x0 = G.TotalXp;
                G.Advance(8 * 3600, Day); // gece (çevrimdışı üretim + günlük baskın)
                XpTime += G.TotalXp - x0;
                if (G.LastRaid != null && G.LastRaid != raidBefore) { RaidsTotal++; if (!G.LastRaid.Result.Victory) RaidsDefended++; }
                Mark(0);
                for (int h = 0; h < 16; h += CheckInHours)
                {
                    Act(h);
                    long x1 = G.TotalXp;
                    G.Advance(CheckInHours * 3600, Day);
                    XpTime += G.TotalXp - x1;
                    Mark(h + CheckInHours);
                }
            }
        }

        private void Mark(int hourOfDay)
        {
            G.PendingLevelUps.Clear();
            for (int l = 2; l <= G.Level; l++) if (!DayReached.ContainsKey(l)) DayReached[l] = Day + hourOfDay / 24.0;
        }

        private int ArmyCount
        {
            get { int n = 0; for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) n += G.Inventory.Get((SoldierType)i); return n; }
        }

        private void Act(int hourOfDay)
        {
            long x = G.TotalXp;
            ClaimQuests();
            XpQuest += G.TotalXp - x; x = G.TotalXp;
            FillRoads();
            PlaceBuildings();
            PlaceTowers();
            FillRoads();
            XpPlace += G.TotalXp - x;
            StartUpgrades();
            StartResearch();
            Train();
            x = G.TotalXp;
            for (int k = 0; k < 2 && G.ArmyCapacity > 0 && ArmyCount >= Math.Max(1, G.ArmyCapacity * 6 / 10); k++) { Attack(); Train(); }
            XpBattle += G.TotalXp - x; x = G.TotalXp;
            ClaimQuests();
            XpQuest += G.TotalXp - x;
            Mark(hourOfDay);
            foreach (int s in SnapshotLevels) if (G.Level >= s && snapped.Add(s)) Snapshots.Add(Snapshot(s));
        }

        private void ClaimQuests()
        {
            bool any = true;
            int guard = 50;
            while (any && guard-- > 0)
            {
                any = false;
                foreach (var q in QuestCatalog.All)
                {
                    if (!G.Quests.IsActive(q, G.Level) || G.Quests.IsClaimed(q.Id) || !G.Quests.IsComplete(q)) continue;
                    if (G.TryClaimQuest(q.Id, out _, out _)) any = true;
                }
            }
        }

        private IEnumerable<GridPos> Cells()
        {
            for (int y = 1; y < G.Layout.Size; y++)
                for (int x = 0; x < G.Layout.Size; x++) yield return new GridPos(x, y);
        }

        private bool Road4(GridPos p)
        {
            var L = G.Layout;
            return L.Get(new GridPos(p.X + 1, p.Y)) == CellKind.Road || L.Get(new GridPos(p.X - 1, p.Y)) == CellKind.Road ||
                   L.Get(new GridPos(p.X, p.Y + 1)) == CellKind.Road || L.Get(new GridPos(p.X, p.Y - 1)) == CellKind.Road;
        }

        private bool Road8(GridPos p)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if ((dx != 0 || dy != 0) && G.Layout.Get(new GridPos(p.X + dx, p.Y + dy)) == CellKind.Road) return true;
            return false;
        }

        /// <summary>Yol hakkını doldurur: tek satırlarda yatay kollarla ağaç gibi büyür.</summary>
        private void FillRoads()
        {
            int guard = 200;
            while (G.Layout.RoadCount < G.Layout.RoadLimit && guard-- > 0)
            {
                GridPos best = new GridPos(-1, -1); int bestScore = int.MaxValue;
                foreach (var p in Cells())
                {
                    if (G.Layout.Get(p) != CellKind.Empty || !Road4(p)) continue;
                    int score = (p.Y % 2 == 1 ? 0 : 100) + Math.Abs(p.X - 5) * 3 + p.Y;
                    if (score < bestScore) { bestScore = score; best = p; }
                }
                if (bestScore == int.MaxValue || !G.TryPlaceRoad(best, out _)) break;
            }
        }

        private void PlaceBuildings()
        {
            foreach (var (t, want) in BuildPlan)
            {
                if (G.Level < ContentCatalog.Building(t).UnlockLevel) continue;
                if (G.CountBuildings(t) >= want) continue;
                if (!G.Wallet.CanAfford(GameRules.Scale(GameRules.BuildCostOf(t), G.Config, G.Level, 100), G.Level, SpendArea.Building, out _)) continue;
                bool placed = false;
                // bina: yola komşu olmayan hücreyi tercih et (kuleye yer kalsın)
                for (int pass = 0; pass < 2 && !placed; pass++)
                    foreach (var p in Cells())
                    {
                        if (G.Layout.Get(p) != CellKind.Empty) continue;
                        if (pass == 0 && Road8(p)) continue;
                        if (G.TryPlaceBuilding(p, t, out _)) placed = true;
                        break;
                    }
            }
        }

        private int towerCycle;
        private void PlaceTowers()
        {
            var types = new List<TowerType>();
            foreach (TowerType t in Enum.GetValues(typeof(TowerType))) if (ContentCatalog.Tower(t).UnlockLevel <= G.Level) types.Add(t);
            if (types.Count == 0) return;
            int guard = 40;
            while (G.Towers.Count < GameRules.MaxTowers(G.Level) && guard-- > 0)
            {
                var tt = types[towerCycle % types.Count];
                bool placed = false;
                foreach (var p in Cells())
                {
                    if (G.Layout.Get(p) != CellKind.Empty || !Road8(p)) continue;
                    if (G.TryPlaceTower(p, tt, out _)) { placed = true; towerCycle++; }
                    break;
                }
                if (!placed) break;
            }
        }

        private void StartUpgrades()
        {
            int guard = 12;
            while (G.FreeBuilders > 0 && guard-- > 0)
            {
                JobKind bk = JobKind.Building; GridPos bp = default; SoldierType bs = SoldierType.Swordsman;
                int bestKey = int.MaxValue;
                void Consider(JobKind k, GridPos p, SoldierType s, int prio)
                {
                    var info = G.PreviewUpgrade(k, p, s);
                    if (info.Block != null) return;
                    if (!G.Wallet.CanAfford(info.Cost, G.Level, SpendArea.Upgrade, out _)) return;
                    int key = info.TargetLevel * 10 + prio;
                    if (key < bestKey) { bestKey = key; bk = k; bp = p; bs = s; }
                }
                foreach (var kv in G.Buildings)
                {
                    var t = kv.Value.Type;
                    int prio = GameRules.ProducedBy(t).HasValue || ContentCatalog.Building(t).IsDepot ? 0 : (t == BuildingType.Barracks ? 1 : 3);
                    Consider(JobKind.Building, kv.Key, SoldierType.Swordsman, prio);
                }
                foreach (var kv in G.Towers) Consider(JobKind.Tower, kv.Key, SoldierType.Swordsman, 2);
                for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
                    if (ContentCatalog.Soldier((SoldierType)i).UnlockLevel <= G.Level) Consider(JobKind.Soldier, default, (SoldierType)i, 2);
                if (bestKey == int.MaxValue) break;
                if (!G.TryStartUpgrade(bk, bp, bs, out _)) break;
            }
        }

        private void StartResearch()
        {
            if (G.UniversityLevel <= 0 || G.Research.Active.Count >= G.Research.Slots) return;
            ResearchDef pick = null;
            foreach (var d in ResearchCatalog.All)
            {
                if (pick != null && d.Level >= pick.Level) continue;
                if (G.Research.LockReason(d, G.Level, G.UniversityLevel) != null) continue;
                if (!G.Wallet.CanAfford(G.Research.CostFor(d, G.Config, G.Level), G.Level, SpendArea.Research, out _)) continue;
                pick = d;
            }
            if (pick != null) G.TryResearch(pick.Id, out _);
        }

        private void Train()
        {
            var types = new List<SoldierType>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) if (ContentCatalog.Soldier((SoldierType)i).UnlockLevel <= G.Level) types.Add((SoldierType)i);
            if (types.Count == 0) return;
            int k = 0, guard = 400;
            while (G.ArmySize < G.ArmyCapacity && guard-- > 0)
                if (!G.TryTrain(types[k++ % types.Count], out _)) break;
        }

        private void Attack()
        {
            var tgt = TargetGenerator.Generate(G.Level, 1, TargetSeed++);
            var sim = G.StartAttack(tgt.Defense, TargetSeed * 13, out _);
            if (sim == null) return;
            Deploy(sim, tgt.Defense.Layout.Size);
            var res = sim.Result(tgt.Pool, G.Config, tgt.Level);
            G.FinishAttack(sim, res, tgt.Level);
            Attacks++; if (res.Victory) Wins++; Stars += res.Stars;
        }

        public static void Deploy(BattleSim sim, int size)
        {
            int i = 0;
            for (int k = 0; k < ContentCatalog.SoldierTypeCount; k++)
            {
                var t = (SoldierType)k;
                int n = sim.StockOf(t);
                for (int j = 0; j < n; j++, i++) sim.Deploy(t, (i * 3) % size, 1 + (i % VillageLayout.AttackZoneRows), out _);
            }
            int guard = BattleSim.TimeLimitTicks + 5;
            while (!sim.Finished && guard-- > 0) sim.Step();
        }

        /// <summary>O anki tam ordu (kapasite kadar, açık türler eşit, mevcut asker seviyeleri) ile 3 zorlukta 6'şar savaş.</summary>
        private string Snapshot(int lvl)
        {
            var types = new List<SoldierType>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) if (ContentCatalog.Soldier((SoldierType)i).UnlockLevel <= G.Level) types.Add((SoldierType)i);
            var levels = new Dictionary<SoldierType, int>();
            int lvSum = 0;
            foreach (var t in types) { levels[t] = G.SoldierLevel(t); lvSum += G.SoldierLevel(t); }
            int cap = G.ArmyCapacity;
            int towerLv = 0; foreach (var kv in G.Towers) towerLv += kv.Value.Level;
            var sb = new StringBuilder();
            sb.Append("Sv").Append(lvl).Append(" gün ").Append(Day).Append(" | ordu ").Append(cap)
              .Append(" | asker sv ").Append(types.Count > 0 ? (lvSum / (double)types.Count).ToString("0.0", CultureInfo.InvariantCulture) : "-")
              .Append(" | kule ").Append(G.Towers.Count).Append(" sv ").Append(G.Towers.Count > 0 ? (towerLv / (double)G.Towers.Count).ToString("0.0", CultureInfo.InvariantCulture) : "-")
              .Append(" | araşt ").Append(G.Research.Done.Count);
            if (cap <= 0 || types.Count == 0) return sb.Append(" | ordu yok").ToString();
            for (int diff = 0; diff < 3; diff++)
            {
                int wins = 0, stars = 0;
                for (int seed = 100; seed < 106; seed++)
                {
                    var stock = new Dictionary<SoldierType, int>();
                    for (int i = 0; i < cap; i++) { var t = types[i % types.Count]; stock[t] = (stock.TryGetValue(t, out var n) ? n : 0) + 1; }
                    var tgt = TargetGenerator.Generate(G.Level, diff, seed);
                    var sim = new BattleSim(tgt.Defense, stock, levels, seed) { AttackerMods = G.Research.Mods() };
                    Deploy(sim, tgt.Defense.Layout.Size);
                    var res = sim.Result(tgt.Pool, G.Config, tgt.Level);
                    if (res.Victory) wins++; stars += res.Stars;
                }
                sb.Append(" | ").Append(diff == 0 ? "Kolay " : diff == 1 ? "Normal " : "Zor ").Append(wins).Append("/6 ")
                  .Append((stars / 6.0).ToString("0.0", CultureInfo.InvariantCulture)).Append('*');
            }
            return sb.ToString();
        }
    }

    public class BalanceSimTests
    {
        public static string RunReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("DENGE RAPORU (otomatik, BalanceSimTests). Bot: kurallara uyan açgözlü oyuncu; günde 16 saat uyanık.");
            sb.AppendLine("Hedef (öneri): Sv30 ≈ 21-35 gün (2 saatte bir giren aktif oyuncu); Normal ~%60-80 zafer, Kolay ~%90+, Zor ~%25-50.");
            foreach (int every in new[] { 2, 4 })
            {
                var p = new PlayerSim(every);
                p.Run(90);
                sb.AppendLine();
                sb.AppendLine("=== Giriş aralığı: " + every + " saat ===");
                var days = new StringBuilder("Seviyeye ulaşılan gün: ");
                foreach (int l in new[] { 2, 3, 5, 8, 10, 15, 20, 25, 30 })
                    days.Append("Sv").Append(l).Append('=').Append(p.DayReached.TryGetValue(l, out var d) ? d.ToString("0.0", CultureInfo.InvariantCulture) : "-").Append("  ");
                sb.AppendLine(days.ToString());
                sb.AppendLine("Saldırı " + p.Attacks + ", zafer " + p.Wins + " (%" + (p.Attacks > 0 ? p.Wins * 100 / p.Attacks : 0) + "), ort yıldız "
                    + (p.Attacks > 0 ? (p.Stars / (double)p.Attacks).ToString("0.0", CultureInfo.InvariantCulture) : "-")
                    + " | XP: görev " + p.XpQuest + ", kurma " + p.XpPlace + ", yükseltme " + p.XpTime + ", savaş " + p.XpBattle
                    + " | baskın " + p.RaidsTotal + ", savunulan " + p.RaidsDefended + " | son seviye " + p.G.Level + " (gün " + p.Day + ")");
                foreach (var s in p.Snapshots) sb.AppendLine("  " + s);
            }
            return sb.ToString();
        }

        [Test]
        public void BalanceReport_ActivePlayerSimulation()
        {
            string text = RunReport();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "..", "docs", "balance-report.txt");
            try { File.WriteAllText(path, text); } catch { /* yazılamazsa sessiz geç */ }
            Assert.Pass(text);
        }
    }
}
