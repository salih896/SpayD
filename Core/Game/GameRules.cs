using System;
using GameCore.Combat;
using GameCore.Economy;

namespace GameCore.Game
{
    /// <summary>
    /// Köy ekonomisi/yükseltme kuralları. TÜM SAYILAR GEÇİCİ (dengeleme aşamasında değişir; sonra JSON'a taşınır).
    /// Kural: maliyetler yalnızca oyuncu seviyesinde AÇIK kaynaklarla ifade edilir.
    /// </summary>
    public static class GameRules
    {
        public const int MaxLevel = 30;
        public const int StarterWood = 600;
        public const int SellRefundPct = 50;

        /// <summary>Kule hakkı (GDD Bölüm 9, KESİN KARAR): başlangıçta 5, ilerledikçe en çok 10. Sv2'de 5, her 5 seviyede +1, Sv27+ = 10.</summary>
        public const int StartTowerSlots = 5, MaxTowerSlots = 10;
        public static int MaxTowers(int playerLevel) => Math.Min(MaxTowerSlots, StartTowerSlots + Math.Max(0, playerLevel - 2) / 5);

        /// <summary>
        /// O oyuncu seviyesindeki tipik aktif oyuncunun kule/asker/bina ortalama seviyesi (denge simülasyonundan kalibre,
        /// bkz. docs/balance-report.txt). Yapay hedef köyler ve günlük baskın gücü buna bağlıdır; ham oyuncu seviyesine değil.
        /// </summary>
        public static int ExpectedStructureLevel(int playerLevel)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, playerLevel));
            return Math.Max(1, Math.Min(MaxStructureLevel(l), (120 * l + 6 * l * l) / 750)); // asker: Sv10→2, 15→4, 20→6, 25→9, 30→12
        }

        /// <summary>Tipik oyuncunun KULE seviyesi (kule sayısı az olduğu için askerden hızlı yükselir). Hedef köy kuleleri buna bağlı.</summary>
        public static int ExpectedTowerLevel(int playerLevel)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, playerLevel));
            return Math.Max(1, Math.Min(MaxStructureLevel(l), (4 * l * l + 30 * l + 300) / 300)); // Sv10→3, 15→5, 20→8, 25→11, 30→16
        }

        /// <summary>Yapı/kule seviyesi oyuncu seviyesini geçemez.</summary>
        public static int MaxStructureLevel(int playerLevel) => Math.Min(MaxLevel, playerLevel);

        public static ResourceCost Scale(ResourceCost c, ResourceConfig cfg, int level, int pct)
        {
            Func<ResourceType, int, int> f = (t, v) => ResourceRules.IsUnlocked(cfg, t, level) ? (int)((long)v * pct / 100) : 0;
            return new ResourceCost(f(ResourceType.Wood, c.Wood), f(ResourceType.Stone, c.Stone), f(ResourceType.Gold, c.Gold), f(ResourceType.Iron, c.Iron));
        }

        /// <summary>Kurma maliyeti (seviye 1 yapı). Kuleler katalogdaki BuildCost'u kullanır.</summary>
        public static ResourceCost BuildCostOf(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.VillagerHouse: return new ResourceCost(60);
                case BuildingType.Forest: return new ResourceCost(80);
                case BuildingType.LumberDepot: return new ResourceCost(100);
                case BuildingType.Barracks: return new ResourceCost(150);
                case BuildingType.StoneQuarry: return new ResourceCost(160, 60);
                case BuildingType.StoneDepot: return new ResourceCost(180, 80);
                case BuildingType.University: return new ResourceCost(300, 150);
                case BuildingType.GoldMine: return new ResourceCost(400, 250, 100);
                case BuildingType.Bank: return new ResourceCost(450, 300, 150);
                case BuildingType.IronMine: return new ResourceCost(800, 500, 300, 100);
                default: return new ResourceCost(900, 600, 350, 200);
            }
        }

        public static ResourceCost BuildCostOf(TowerType t) => ContentCatalog.Tower(t).BuildCost;

        /// <summary>Hedef seviyeye (2..) yükseltme maliyeti.</summary>
        public static ResourceCost UpgradeCost(ResourceCost baseCost, int targetLevel, ResourceConfig cfg, int playerLevel, int upgradeCostReductionPermille)
        {
            int pct = (100 * targetLevel * targetLevel / 2 + 100) * (1000 - Math.Min(900, upgradeCostReductionPermille)) / 1000;
            return Scale(baseCost, cfg, playerLevel, pct);
        }

        /// <summary>Yükseltme süresi (sn); Köy Gelişimi araştırması kısaltır.</summary>
        public static int UpgradeSeconds(int targetLevel, int buildTimeReductionPermille)
        {
            long s = 20L * targetLevel + 6L * targetLevel * targetLevel;
            return (int)Math.Max(5, s * (1000 - Math.Min(900, buildTimeReductionPermille)) / 1000);
        }

        public static int Builders(int villagerHouses) => 1 + Math.Min(5, villagerHouses);

        /// <summary>Kışla toplam seviyesi başına ordu kapasitesi.</summary>
        public static int ArmyCapacity(int barracksLevelSum) => barracksLevelSum <= 0 ? 0 : 20 + barracksLevelSum * 6 / 5;

        // ---- Üretim ve depo ----
        public static ResourceType? ProducedBy(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Forest: return ResourceType.Wood;
                case BuildingType.StoneQuarry: return ResourceType.Stone;
                case BuildingType.GoldMine: return ResourceType.Gold;
                case BuildingType.IronMine: return ResourceType.Iron;
                default: return null;
            }
        }

        /// <summary>Seviye 1 bir üretim binasının saatlik verimi.</summary>
        public static int BasePerHour(ResourceType t)
        {
            switch (t)
            {
                case ResourceType.Wood: return 1500;
                case ResourceType.Stone: return 900;
                case ResourceType.Gold: return 500;
                default: return 300;
            }
        }

        public static int BaseCapacity(ResourceType t)
        {
            switch (t) { case ResourceType.Wood: return 1500; case ResourceType.Stone: return 1000; case ResourceType.Gold: return 600; default: return 350; }
        }

        public static int DepotCapacity(ResourceType t, int depotLevel)
        {
            int b;
            switch (t) { case ResourceType.Wood: b = 2500; break; case ResourceType.Stone: b = 1800; break; case ResourceType.Gold: b = 1000; break; default: b = 600; break; }
            return (int)((long)b * ContentCatalog.ScalePct(depotLevel) / 100);
        }
    }
}
