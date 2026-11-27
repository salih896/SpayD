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

        /// <summary>Kule hakkı (kullanıcı kararı 25.11.2026): 3 ile başlar; yalnızca Akademi "Kule Hakkı I-V" araştırmalarıyla +5 → en çok 8.</summary>
        public const int StartTowerSlots = 3, MaxTowerSlots = 8;
        /// <summary>Tuzak hakkı (kullanıcı isteği): başta 4, Akademi "Tuzak Ustalığı" araştırmasıyla 6.</summary>
        public const int StartTrapSlots = 4, ResearchTrapSlots = 6;
        /// <summary>Tuzak Ustalığı araştırması (GEÇİCİ): 12 saat; 3000 odun, 1500 taş.</summary>
        /// <summary>Köylü evi: başta 1, Akademi "Köylü Evi I-II" araştırmalarıyla en çok 3.</summary>
        public const int StartVillagerHouses = 1, MaxVillagerHouses = 3;

        /// <summary>Kışla (kullanıcı kararı 25.11.2026): 1. Sv3'te; 2. ve 3. Akademi "Kışla II-III" araştırmasıyla. Akademi: 1. Sv5'te, 2. "Akademi II" ile.</summary>
        public const int MaxBarracksCount = 3, MaxAcademyCount = 2;

        /// <summary>
        /// Yapı başına taban sayı 1 (kullanıcı kararı 25.11.2026): kaynak yapılarının 2. kopyası Ekonomi "Bölge II" araştırmasıyla
        /// (üretim + depo birlikte; en çok 2). Kışla/Akademi/Köylü evi hakları da araştırmayla (GameState.MaxCountOf).
        /// </summary>
        public const int MaxResourceBuildingCount = 2;
        public static int MaxBuildingCount(BuildingType t, int playerLevel = MaxLevel) => 1;

        /// <summary>Tipik oyuncunun kışla sayısı (Kışla II ≈ Sv10, Kışla III ≈ Sv20 civarı araştırılır). Yapay ordu büyüklüğü için.</summary>
        public static int ExpectedBarracks(int playerLevel) => playerLevel < 3 ? 0 : playerLevel >= 20 ? 3 : playerLevel >= 10 ? 2 : 1;

        /// <summary>Tipik oyuncunun o seviyedeki kule hakkı (Kule Hakkı araştırmalarını zamanla yapar). Yapay hedef köylerin kule sayısı buna bağlı.</summary>
        public static int ExpectedTowerSlots(int playerLevel) =>
            Math.Min(MaxTowerSlots, StartTowerSlots + (playerLevel >= 6 ? 1 : 0) + (playerLevel >= 10 ? 1 : 0) + (playerLevel >= 12 ? 1 : 0) + (playerLevel >= 18 ? 1 : 0) + (playerLevel >= 20 ? 1 : 0));

        /// <summary>Askerin orduda kapladığı yer (kullanıcı kararı 15.11.2026, "yumuşak tablo").</summary>
        public static int Housing(SoldierType t)
        {
            switch (t)
            {
                case SoldierType.Spearman: case SoldierType.Fairy: case SoldierType.Assassin: case SoldierType.Bomber: return 2;
                case SoldierType.Mage: case SoldierType.Cavalry: case SoldierType.Shieldbearer: case SoldierType.Healer: return 3;
                case SoldierType.Giant: case SoldierType.Crossbowman: return 5; // Arbaletli 5 yer (approved #101)
                case SoldierType.Catapult: return 6;
                case SoldierType.DragonRider: return 10;
                default: return 1; // Kılıçlı, Okçu, Goblin
            }
        }

        /// <summary>Tipik oyuncunun kışla seviyeleri toplamı (yapay ordu büyüklüğü için).</summary>
        public static int ExpectedBarracksLevelSum(int playerLevel) => ExpectedBarracks(playerLevel) * ExpectedStructureLevel(playerLevel);

        /// <summary>
        /// O oyuncu seviyesindeki tipik aktif oyuncunun kule/asker/bina ortalama seviyesi (denge simülasyonundan kalibre,
        /// bkz. docs/balance-report.txt). Yapay hedef köyler ve günlük baskın gücü buna bağlıdır; ham oyuncu seviyesine değil.
        /// </summary>
        public static int ExpectedStructureLevel(int playerLevel)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, playerLevel));
            return Math.Max(1, Math.Min(MaxStructureLevel(l), (l * l + 22 * l) / 90)); // asker: Sv10→3, 15→6, 20→9, 25→13, 30→17 (100 günlük ilerleme)
        }

        /// <summary>Tipik oyuncunun KULE seviyesi (kule sayısı az olduğu için askerden hızlı yükselir). Hedef köy kuleleri buna bağlı.</summary>
        public static int ExpectedTowerLevel(int playerLevel)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, playerLevel));
            return Math.Max(1, Math.Min(MaxStructureLevel(l), (l * l + 10 * l) / 40)); // Sv10→5, 15→9, 20→15, 25→21, 30→30 (100 günlük ilerleme)
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

        /// <summary>Kuleye element uygulama (kullanıcı kararı 25.11.2026: 1-2 dk, biraz kaynak). GEÇİCİ: 90 sn; kule seviyesiyle artan küçük maliyet.</summary>
        public const int ElementApplySeconds = 90;
        public static ResourceCost ElementCost(int towerLevel, ResourceConfig cfg, int playerLevel) =>
            Scale(new ResourceCost(150, 75, 40, 20), cfg, playerLevel, 100 + 15 * Math.Max(0, towerLevel - 1));

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

        /// <summary>Ordu kapasitesi (yer birimi; asker türüne göre 1-10 yer kaplar). Kışla seviyeleri toplamına bağlı.</summary>
        public static int ArmyCapacity(int barracksLevelSum) => barracksLevelSum <= 0 ? 0 : 20 + barracksLevelSum * 4; // Sv13 ≈ 40, Sv20 ≈ 92, Sv30 ≈ 224

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
