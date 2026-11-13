using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Grid;
using GameCore.Military;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Game
{
    public enum JobKind { Building, Tower, Soldier }

    public sealed class UpgradeJob
    {
        public JobKind Kind;
        public GridPos Pos;
        public SoldierType Soldier;
        public int TargetLevel;
        public int RemainingSeconds;
    }

    public sealed class LevelUpInfo
    {
        public int NewLevel;
        public ResourceCost Reward;
        public List<string> Unlocks = new List<string>();
    }

    public sealed class RaidReport
    {
        public int Day;
        public BattleResult Result;
        public ResourceCost LootLost;
        public int TowersDamaged;
    }

    public sealed class AttackOutcome
    {
        public List<LevelUpInfo> LevelUps = new List<LevelUpInfo>();
        public int XpGained;
        public ResourceCost LootGained;
    }

    /// <summary>
    /// Oyuncunun tüm oyun durumu (saf C#, sunucu aynı sınıfı kullanabilir). İstemci yalnızca bu sınıfın metotlarını çağırır;
    /// kaynak/XP doğrudan değiştirilmez. Tüm sayılar GEÇİCİDİR.
    /// </summary>
    public sealed class GameState
    {
        public const int MaxOfflineSeconds = 24 * 3600;
        public const int PlacementXpBuilding = 20, PlacementXpTower = 30, UpgradeXpPerLevel = 12;

        /// <summary>Bina/kule kurma veya yükseltme tamamlanınca kazanılan seviye atlamaları; arayüz okuyup boşaltır.</summary>
        public readonly List<LevelUpInfo> PendingLevelUps = new List<LevelUpInfo>();

        public readonly ResourceConfig Config;
        public long TotalXp;
        public Wallet Wallet;
        public VillageLayout Layout;
        public Dictionary<GridPos, TowerSpec> Towers = new Dictionary<GridPos, TowerSpec>();
        public Dictionary<GridPos, BuildingSpec> Buildings = new Dictionary<GridPos, BuildingSpec>();
        public Dictionary<GridPos, TrapType> Traps = new Dictionary<GridPos, TrapType>();
        public Dictionary<GridPos, TowerWear> Wears = new Dictionary<GridPos, TowerWear>();
        public SoldierInventory Inventory = new SoldierInventory();
        public Dictionary<SoldierType, int> SoldierLevels = new Dictionary<SoldierType, int>();
        public ProductionQueue Queue = new ProductionQueue();
        public ResearchState Research = new ResearchState();
        public QuestState Quests = new QuestState();
        public List<UpgradeJob> Jobs = new List<UpgradeJob>();
        public long[] ProdAccumMilli = new long[4];
        public int BattlesFought, BattlesWon, TotalStars;
        public int LastRaidDay = -1;
        public RaidReport LastRaid;
        public long LastUnixSeconds;

        public GameState(ResourceConfig cfg)
        {
            Config = cfg;
            Wallet = new Wallet(cfg);
        }

        public int Level
        {
            get { return ProgressionRules.LevelFromTotalXp(TotalXp, out _); }
        }

        public int XpIntoLevel
        {
            get { ProgressionRules.LevelFromTotalXp(TotalXp, out int x); return x; }
        }

        /// <summary>Yeni oyun: Sv1, 10 yol hakkı (giriş + yol), orman + kereste deposu + köylü evi.</summary>
        public static GameState NewGame(ResourceConfig cfg = null)
        {
            var g = new GameState(cfg ?? ResourceConfig.CreateDefault());
            g.Layout = new VillageLayout(11, RoadRules.RoadLimitForLevel(1), RoadRules.MaxEntrances);
            string e;
            g.Layout.ToggleEntrance(new GridPos(5, 0), out e);
            for (int y = 1; y <= 9; y++) g.Layout.TryPlace(new GridPos(5, y), CellKind.Road, out e);
            g.AddBuildingDirect(new GridPos(4, 3), BuildingType.Forest, 1);
            g.AddBuildingDirect(new GridPos(6, 3), BuildingType.LumberDepot, 1);
            g.AddBuildingDirect(new GridPos(4, 6), BuildingType.VillagerHouse, 1);
            g.Wallet.SetAmount(ResourceType.Wood, GameRules.StarterWood);
            g.RecomputeCapacity();
            return g;
        }

        public void AddBuildingDirect(GridPos p, BuildingType t, int level)
        {
            string e;
            if (Layout.TryPlace(p, CellKind.Building, out e)) Buildings[p] = new BuildingSpec(t, level);
        }

        public void AddTowerDirect(GridPos p, TowerType t, int level)
        {
            string e;
            if (Layout.TryPlace(p, CellKind.Tower, out e)) { Towers[p] = new TowerSpec(t, level, 0); Wears[p] = new TowerWear(); }
        }

        // ---------- Türetilmiş değerler ----------
        public int CountBuildings(BuildingType t)
        {
            int n = 0;
            foreach (var kv in Buildings) if (kv.Value.Type == t) n++;
            return n;
        }

        public int MaxBuildingLevelOf(BuildingType t)
        {
            int m = 0;
            foreach (var kv in Buildings) if (kv.Value.Type == t && kv.Value.Level > m) m = kv.Value.Level;
            return m;
        }

        public int BarracksLevelSum()
        {
            int s = 0;
            foreach (var kv in Buildings) if (kv.Value.Type == BuildingType.Barracks) s += kv.Value.Level;
            return s;
        }

        public int UniversityLevel => MaxBuildingLevelOf(BuildingType.University);
        public int ArmyCapacity => GameRules.ArmyCapacity(BarracksLevelSum());
        public int ArmySize
        {
            get
            {
                int n = Queue.Items.Count;
                for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) n += Inventory.Get((SoldierType)i);
                return n;
            }
        }
        public int Builders => GameRules.Builders(CountBuildings(BuildingType.VillagerHouse));
        public int FreeBuilders => Math.Max(0, Builders - Jobs.Count);
        public int SoldierLevel(SoldierType t) => SoldierLevels.TryGetValue(t, out var l) ? Math.Max(1, l) : 1;

        public int PerHour(ResourceType t)
        {
            long sum = 0;
            foreach (var kv in Buildings)
            {
                var prod = GameRules.ProducedBy(kv.Value.Type);
                if (prod.HasValue && prod.Value == t)
                    sum += (long)GameRules.BasePerHour(t) * ContentCatalog.ScalePct(kv.Value.Level) / 100;
            }
            return (int)(sum * (1000 + Research.Total(ResearchStat.ProductionPct)) / 1000);
        }

        public int CapacityOf(ResourceType t)
        {
            long cap = GameRules.BaseCapacity(t);
            foreach (var kv in Buildings)
            {
                var def = ContentCatalog.Building(kv.Value.Type);
                if (def.IsDepot && def.Stores == t) cap += GameRules.DepotCapacity(t, kv.Value.Level);
            }
            return (int)(cap * (1000 + Research.Total(ResearchStat.StoragePct)) / 1000);
        }

        public void RecomputeCapacity()
        {
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType))) Wallet.SetCapacity(t, CapacityOf(t));
        }

        public DefenseSetup MakeDefense()
        {
            var d = new DefenseSetup(Layout) { Mods = Research.Mods() };
            foreach (var kv in Towers)
            {
                int perm = Wears.TryGetValue(kv.Key, out var w) ? w.PermanentDamagePct : 0;
                d.Towers[kv.Key] = new TowerSpec(kv.Value.Type, kv.Value.Level, perm);
            }
            foreach (var kv in Buildings) d.Buildings[kv.Key] = kv.Value;
            foreach (var kv in Traps) d.Traps[kv.Key] = kv.Value;
            return d;
        }

        public bool VillageValid(out string error) => Layout.Validate(out error);

        public ResourceCost ResourcesNow() => new ResourceCost(Wallet.Get(ResourceType.Wood), Wallet.Get(ResourceType.Stone), Wallet.Get(ResourceType.Gold), Wallet.Get(ResourceType.Iron));

        // ---------- XP / seviye ----------
        public List<LevelUpInfo> AddXp(int xp)
        {
            var ups = new List<LevelUpInfo>();
            if (xp <= 0) return ups;
            int old = Level;
            TotalXp += xp;
            int now = Level;
            for (int l = old + 1; l <= now; l++)
            {
                var info = new LevelUpInfo { NewLevel = l };
                int w = 120 * l;
                info.Reward = GameRules.Scale(new ResourceCost(w, w / 2, w / 4, w / 6), Config, l, 100);
                foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
                    if (info.Reward.Get(t) > 0) Wallet.TryAdd(t, info.Reward.Get(t), l, out _, out _);
                Layout = Layout.WithLimits(RoadRules.RoadLimitForLevel(l), RoadRules.MaxEntrances);
                info.Unlocks.Add("Yol hakkı " + RoadRules.RoadLimitForLevel(l));
                foreach (ResourceType t in Enum.GetValues(typeof(ResourceType))) if (Config.Get(t).UnlockLevel == l) info.Unlocks.Add("Kaynak: " + ResName(t));
                for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) { var d = ContentCatalog.Soldier((SoldierType)i); if (d.UnlockLevel == l) info.Unlocks.Add("Asker: " + d.Name); }
                foreach (TowerType t in Enum.GetValues(typeof(TowerType))) { var d = ContentCatalog.Tower(t); if (d.UnlockLevel == l) info.Unlocks.Add("Kule: " + d.Name); }
                foreach (BuildingType t in Enum.GetValues(typeof(BuildingType))) { var d = ContentCatalog.Building(t); if (d.UnlockLevel == l && l > 1) info.Unlocks.Add("Bina: " + d.Name); }
                if (l == ResearchState.UniversityUnlockLevel) info.Unlocks.Add("Araştırma ağaçları");
                ups.Add(info);
            }
            if (ups.Count > 0) RecomputeCapacity();
            return ups;
        }

        public static string ResName(ResourceType t)
        {
            switch (t) { case ResourceType.Wood: return "Odun"; case ResourceType.Stone: return "Taş"; case ResourceType.Gold: return "Altın"; default: return "Demir"; }
        }

        // ---------- Zaman ----------
        /// <summary>Geçen süreyi uygular (çevrimdışı dahil, en çok 24 saat). Olayları metin listesi olarak döndürür.</summary>
        public List<string> Advance(int seconds, int dayIndex)
        {
            var events = new List<string>();
            if (seconds < 0) seconds = 0;
            if (seconds > MaxOfflineSeconds) seconds = MaxOfflineSeconds;
            int lvl = Level;
            RecomputeCapacity();

            // Üretim (kesirler kaybolmasın diye mili birim biriktirilir)
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            {
                if (!ResourceRules.CanProduce(Config, t, lvl)) continue;
                int ph = PerHour(t);
                if (ph <= 0) continue;
                ProdAccumMilli[(int)t] += (long)ph * seconds * 1000 / 3600;
                int whole = (int)(ProdAccumMilli[(int)t] / 1000);
                if (whole > 0)
                {
                    Wallet.TryAdd(t, whole, lvl, out int added, out _);
                    ProdAccumMilli[(int)t] %= 1000;
                    if (added < whole) ProdAccumMilli[(int)t] = 0;
                }
            }

            int trained = Queue.Advance(seconds, Inventory);
            if (trained > 0) { Quests.Report(QuestEvent.SoldiersTrained, trained, lvl); events.Add(trained + " asker eğitildi"); }

            foreach (var d in Research.Advance(seconds))
            {
                Quests.Report(QuestEvent.ResearchDone, 1, lvl);
                events.Add("Araştırma tamamlandı: " + d.Name);
            }
            RecomputeCapacity();

            for (int i = Jobs.Count - 1; i >= 0; i--)
            {
                var j = Jobs[i];
                j.RemainingSeconds -= seconds;
                if (j.RemainingSeconds > 0) continue;
                Jobs.RemoveAt(i);
                events.Add(CompleteJob(j));
                PendingLevelUps.AddRange(AddXp(UpgradeXpPerLevel * j.TargetLevel));
                Quests.Report(QuestEvent.BuildingsUpgraded, 1, lvl);
            }
            RecomputeCapacity();

            if (Quests.RollDaily(dayIndex, lvl)) events.Add("Yeni günlük görevler");
            if (dayIndex > LastRaidDay) // gün yalnızca ileri gider; saat geri alınınca ikinci baskın olmaz
            {
                LastRaidDay = dayIndex;
                var rep = RaidSimulator.TryRaid(this, dayIndex);
                if (rep != null) { LastRaid = rep; events.Add("Köyüne saldırıldı! %" + rep.Result.OverallDamagePct + " hasar" + (rep.Result.Victory ? " (saldırgan kazandı)" : " (savundun)")); }
            }
            return events;
        }

        private string CompleteJob(UpgradeJob j)
        {
            switch (j.Kind)
            {
                case JobKind.Building:
                    if (Buildings.TryGetValue(j.Pos, out var b)) { b.Level = j.TargetLevel; return ContentCatalog.Building(b.Type).Name + " Sv " + j.TargetLevel; }
                    return "Yapı yükseltildi";
                case JobKind.Tower:
                    if (Towers.TryGetValue(j.Pos, out var t)) { t.Level = j.TargetLevel; return ContentCatalog.Tower(t.Type).Name + " Sv " + j.TargetLevel; }
                    return "Kule yükseltildi";
                default:
                    SoldierLevels[j.Soldier] = j.TargetLevel;
                    return ContentCatalog.Soldier(j.Soldier).Name + " Sv " + j.TargetLevel;
            }
        }

        // ---------- Köy düzenleme ----------
        public bool TryPlaceRoad(GridPos p, out string error) => Layout.TryPlace(p, CellKind.Road, out error);
        public bool TryToggleEntrance(GridPos p, out string error) => Layout.ToggleEntrance(p, out error);

        public bool TryPlaceBuilding(GridPos p, BuildingType t, out string error)
        {
            var def = ContentCatalog.Building(t);
            int lvl = Level;
            if (lvl < def.UnlockLevel) { error = def.Name + " Sv " + def.UnlockLevel + "'de açılır"; return false; }
            if ((t == BuildingType.University || t == BuildingType.Barracks) && CountBuildings(t) >= (t == BuildingType.University ? 1 : 3))
            { error = def.Name + " sınırına ulaştın"; return false; }
            var cost = GameRules.Scale(GameRules.BuildCostOf(t), Config, lvl, 100);
            if (!Wallet.CanAfford(cost, lvl, SpendArea.Building, out error)) return false;
            if (!Layout.TryPlace(p, CellKind.Building, out error)) return false;
            Wallet.TrySpend(cost, lvl, SpendArea.Building, out error);
            Buildings[p] = new BuildingSpec(t, 1);
            PendingLevelUps.AddRange(AddXp(PlacementXpBuilding));
            return true;
        }

        public bool TryPlaceTower(GridPos p, TowerType t, out string error)
        {
            var def = ContentCatalog.Tower(t);
            int lvl = Level;
            if (lvl < def.UnlockLevel) { error = def.Name + " Sv " + def.UnlockLevel + "'de açılır"; return false; }
            if (Towers.Count >= GameRules.MaxTowers(lvl)) { error = "Kule sınırı " + GameRules.MaxTowers(lvl); return false; }
            var cost = GameRules.Scale(def.BuildCost, Config, lvl, 100);
            if (!Wallet.CanAfford(cost, lvl, SpendArea.Building, out error)) return false;
            if (!Layout.TryPlace(p, CellKind.Tower, out error)) return false;
            Wallet.TrySpend(cost, lvl, SpendArea.Building, out error);
            Towers[p] = new TowerSpec(t, 1, 0);
            Wears[p] = new TowerWear();
            Quests.Report(QuestEvent.TowersBuilt, 1, lvl);
            PendingLevelUps.AddRange(AddXp(PlacementXpTower));
            return true;
        }

        /// <summary>Sil: yapı/kule maliyetinin %50'si (seviye 1 maliyeti) iade edilir, tuzak ve yol bedelsiz.</summary>
        public bool TryRemove(GridPos p, out string error)
        {
            error = null;
            foreach (var j in Jobs) if ((j.Kind == JobKind.Building || j.Kind == JobKind.Tower) && j.Pos.Equals(p)) { error = "Yükseltme sürerken silinemez"; return false; }
            int lvl = Level;
            ResourceCost refund = default;
            if (Buildings.TryGetValue(p, out var b)) refund = GameRules.Scale(GameRules.BuildCostOf(b.Type), Config, lvl, GameRules.SellRefundPct);
            else if (Towers.TryGetValue(p, out var t)) refund = GameRules.Scale(ContentCatalog.Tower(t.Type).BuildCost, Config, lvl, GameRules.SellRefundPct);
            if (!Layout.Remove(p)) { error = "Silinecek bir şey yok"; return false; }
            Buildings.Remove(p); Towers.Remove(p); Wears.Remove(p); Traps.Remove(p);
            foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
                if (refund.Get(rt) > 0) Wallet.TryAdd(rt, refund.Get(rt), lvl, out _, out _);
            return true;
        }

        public bool TryToggleTrap(GridPos p, TrapType t, out string error)
        {
            error = null;
            if (Layout.Get(p) != CellKind.Road) { error = "Tuzak yalnızca yola konur"; return false; }
            if (Layout.IsEntrance(p)) { error = "Girişe tuzak konmaz"; return false; }
            if (Traps.TryGetValue(p, out var cur) && cur == t) { Traps.Remove(p); return true; }
            Traps[p] = t;
            return true;
        }

        // ---------- Yükseltme ----------
        public sealed class UpgradeInfo
        {
            public int TargetLevel; public ResourceCost Cost; public int Seconds; public string Block;
        }

        public UpgradeInfo PreviewUpgrade(JobKind kind, GridPos pos, SoldierType soldier)
        {
            int lvl = Level;
            var info = new UpgradeInfo();
            ResourceCost baseCost; int cur;
            switch (kind)
            {
                case JobKind.Building:
                    if (!Buildings.TryGetValue(pos, out var b)) { info.Block = "Yapı yok"; return info; }
                    cur = b.Level; baseCost = GameRules.BuildCostOf(b.Type); break;
                case JobKind.Tower:
                    if (!Towers.TryGetValue(pos, out var t)) { info.Block = "Kule yok"; return info; }
                    cur = t.Level; baseCost = ContentCatalog.Tower(t.Type).BuildCost; break;
                default:
                    cur = SoldierLevel(soldier); baseCost = MulCost(ContentCatalog.Soldier(soldier).Cost, 8); break;
            }
            info.TargetLevel = cur + 1;
            info.Cost = GameRules.UpgradeCost(baseCost, info.TargetLevel, Config, lvl, Research.Total(ResearchStat.UpgradeCost));
            info.Seconds = GameRules.UpgradeSeconds(info.TargetLevel, Research.Total(ResearchStat.BuildTime));
            if (info.TargetLevel > GameRules.MaxStructureLevel(lvl)) info.Block = info.TargetLevel > GameRules.MaxLevel ? "En üst seviye" : "Oyuncu seviyesi " + info.TargetLevel + " gerekli";
            else if (kind == JobKind.Soldier && BarracksMaxLevel() < info.TargetLevel) info.Block = "Kışla Sv " + info.TargetLevel + " gerekli";
            else if (kind == JobKind.Soldier && lvl < ContentCatalog.Soldier(soldier).UnlockLevel) info.Block = "Asker henüz açılmadı";
            else if (IsUpgrading(kind, pos, soldier)) info.Block = "Yükseltme sürüyor";
            else if (FreeBuilders <= 0) info.Block = "Boş işçi yok (Köylü Evi ekle)";
            return info;
        }

        private int BarracksMaxLevel() => MaxBuildingLevelOf(BuildingType.Barracks);

        private static ResourceCost MulCost(ResourceCost c, int k) => new ResourceCost(c.Wood * k, c.Stone * k, c.Gold * k, c.Iron * k);

        public bool IsUpgrading(JobKind kind, GridPos pos, SoldierType soldier)
        {
            foreach (var j in Jobs)
            {
                if (j.Kind != kind) continue;
                if (kind == JobKind.Soldier ? j.Soldier == soldier : j.Pos.Equals(pos)) return true;
            }
            return false;
        }

        public bool TryStartUpgrade(JobKind kind, GridPos pos, SoldierType soldier, out string error)
        {
            var info = PreviewUpgrade(kind, pos, soldier);
            error = info.Block;
            if (error != null) return false;
            if (!Wallet.TrySpend(info.Cost, Level, SpendArea.Upgrade, out error)) return false;
            Jobs.Add(new UpgradeJob { Kind = kind, Pos = pos, Soldier = soldier, TargetLevel = info.TargetLevel, RemainingSeconds = info.Seconds });
            return true;
        }

        // ---------- Kule tamiri ----------
        public bool TryRepair(GridPos p, out string error)
        {
            error = null;
            if (!Wears.TryGetValue(p, out var w) || w.PermanentDamagePct <= 0 || !Towers.TryGetValue(p, out var t)) { error = "Onarılacak hasar yok"; return false; }
            int lvl = Level;
            var cost = w.RepairCost(t.Level, Config, lvl);
            if (!Wallet.TrySpend(cost, lvl, SpendArea.Repair, out error)) return false;
            w.RepairAll();
            return true;
        }

        public int DamagedTowerCount()
        {
            int n = 0;
            foreach (var kv in Wears) if (kv.Value.PermanentDamagePct > 0) n++;
            return n;
        }

        public bool TryRepairAll(out string error)
        {
            error = null;
            int fixedCount = 0;
            foreach (var p in new List<GridPos>(Wears.Keys))
                if (Wears[p].PermanentDamagePct > 0)
                {
                    if (!TryRepair(p, out error)) return fixedCount > 0;
                    fixedCount++;
                }
            if (fixedCount == 0) error = "Onarılacak hasar yok";
            return fixedCount > 0;
        }

        // ---------- Eğitim ----------
        public bool TryTrain(SoldierType t, out string error)
        {
            if (BarracksLevelSum() <= 0) { error = "Önce Kışla kur"; return false; }
            if (ArmySize >= ArmyCapacity) { error = "Ordu kapasitesi dolu (" + ArmySize + "/" + ArmyCapacity + ")"; return false; }
            if (!Queue.TryEnqueue(t, Wallet, Level, out error)) return false;
            var it = Queue.Items[Queue.Items.Count - 1];
            it.RemainingSeconds = Math.Max(5, (int)((long)it.RemainingSeconds * (1000 - Research.Total(ResearchStat.TrainTime)) / 1000));
            return true;
        }

        public bool TryCancelTrain(int index, out string error) => Queue.Cancel(index, Wallet, Level, out error);

        // ---------- Araştırma ----------
        public bool TryResearch(string id, out string error)
        {
            var d = ResearchCatalog.Get(id);
            if (d == null) { error = "Araştırma yok"; return false; }
            bool ok = Research.TryStart(d, Wallet, Config, Level, UniversityLevel, out error);
            return ok;
        }

        // ---------- Saldırı ----------
        public BattleSim StartAttack(DefenseSetup target, int seed, out string error)
        {
            error = null;
            if (ArmySize - Queue.Items.Count <= 0) { error = "Saldırmak için asker eğit"; return null; }
            var stock = Inventory.ToStock();
            foreach (var k in new List<SoldierType>(stock.Keys)) Inventory.TryRemove(k, stock[k]);
            return new BattleSim(target, stock, new Dictionary<SoldierType, int>(SoldierLevels), seed) { AttackerMods = Research.Mods() };
        }

        /// <summary>Savaş sonucunu oyuncuya işler: dönen askerler, yağma, XP (hedef seviyesiyle büyür), görev ilerlemesi.</summary>
        public AttackOutcome FinishAttack(BattleSim sim, BattleResult r, int targetLevel)
        {
            var o = new AttackOutcome();
            int lvl = Level;
            Inventory.AddAll(r.Returned);
            o.LootGained = r.Loot;
            int lootTotal = 0;
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            {
                int n = r.Loot.Get(t);
                if (n <= 0) continue;
                Wallet.TryAdd(t, n, lvl, out int added, out _);
                lootTotal += added;
            }
            int towersDown = 0;
            foreach (var st in sim.Structures) if (st.Kind == StructKind.Tower && !st.Alive) towersDown++;
            BattlesFought++;
            if (r.Victory) BattlesWon++;
            TotalStars += r.Stars;
            Quests.Report(QuestEvent.BattleFought, 1, lvl);
            if (r.Victory) Quests.Report(QuestEvent.BattleWon, 1, lvl);
            Quests.Report(QuestEvent.StarsEarned, r.Stars, lvl);
            Quests.Report(QuestEvent.TowersDestroyed, towersDown, lvl);
            Quests.Report(QuestEvent.LootTaken, lootTotal, lvl);
            o.XpGained = (int)((long)r.Xp * (100 + 12 * (Math.Max(1, targetLevel) - 1)) / 100);
            o.LevelUps = AddXp(o.XpGained);
            return o;
        }

        // ---------- Görev ödülü ----------
        public bool TryClaimQuest(string id, out List<LevelUpInfo> levelUps, out string error)
        {
            levelUps = new List<LevelUpInfo>();
            int lvl = Level;
            if (!Quests.TryClaim(id, lvl, out var q, out error)) return false;
            var reward = QuestCatalog.RewardFor(q, Config, lvl);
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
                if (reward.Get(t) > 0) Wallet.TryAdd(t, reward.Get(t), lvl, out _, out _);
            levelUps = AddXp(q.Xp);
            return true;
        }
    }
}
