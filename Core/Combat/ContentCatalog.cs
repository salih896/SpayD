using GameCore.Economy;

namespace GameCore.Combat
{
    /// <summary>
    /// Tüm asker/kule/bina verisinin TEK merkezi. SAYILAR GEÇİCİ (placeholder), dengeleme aşamasında değişecek.
    /// Alanlar herkese açık düz sınıflardır: sonra JSON'dan (Unity JsonUtility / sunucu) yüklenebilir.
    /// Tasarım amacı: her asker/kule farklı bir rol oynar (taş-kağıt-makas), böylece oyuncu köyünü ve ordusunu
    /// sürekli yeniden kurgular ve uzun süre gelişim hedefi kalır.
    /// </summary>
    public static class ContentCatalog
    {
        public const int MaxSoldierLevel = 5, MaxTowerLevel = 10, MaxBuildingLevel = 10;

        private static readonly SoldierDef[] Soldiers =
        {
            // Dengeli ön hat
            new SoldierDef { Type = SoldierType.Swordsman, Name = "Kılıçlı", Hp = 120, Attack = 14, IntervalTicks = 8, RangeMilli = 1500, SpeedMilli = 70, ArmorPct = 10, TrainSeconds = 120, UnlockLevel = 3, Cost = new ResourceCost(40) },
            // Uzaktan, kırılgan
            new SoldierDef { Type = SoldierType.Archer, Name = "Okçu", Hp = 60, Attack = 10, IntervalTicks = 10, RangeMilli = 4000, SpeedMilli = 70, TrainSeconds = 180, UnlockLevel = 3, Cost = new ResourceCost(45) },
            // Alan hasarı, ateşe dirençli
            new SoldierDef { Type = SoldierType.Mage, Name = "Büyücü", Hp = 50, Attack = 22, IntervalTicks = 18, RangeMilli = 3500, SpeedMilli = 60, FireResistPct = 25, SplashMilli = 1500, TrainSeconds = 300, UnlockLevel = 7, Cost = new ResourceCost(70, 30) },
            // Çok hızlı, ucuz, kaynak avcısı
            new SoldierDef { Type = SoldierType.Goblin, Name = "Goblin", Hp = 35, Attack = 6, IntervalTicks = 6, RangeMilli = 1500, SpeedMilli = 130, TrainSeconds = 120, UnlockLevel = 4, Cost = new ResourceCost(20) },
            // Hızlı ve dayanıklı, mızrak kulesine zayıf
            new SoldierDef { Type = SoldierType.Cavalry, Name = "Atlı Kılıçlı", Hp = 260, Attack = 22, IntervalTicks = 10, RangeMilli = 1500, SpeedMilli = 150, ArmorPct = 20, Mounted = true, TrainSeconds = 600, UnlockLevel = 9, Cost = new ResourceCost(90, 60) },
            // Yavaş, yüksek zırh
            new SoldierDef { Type = SoldierType.Spearman, Name = "Mızraklı", Hp = 150, Attack = 12, IntervalTicks = 10, RangeMilli = 2000, SpeedMilli = 65, ArmorPct = 30, TrainSeconds = 300, UnlockLevel = 5, Cost = new ResourceCost(60, 15) },
            // Uçar, yolu yok sayar; mızrak kulesi vuramaz
            new SoldierDef { Type = SoldierType.Fairy, Name = "Peri", Hp = 55, Attack = 9, IntervalTicks = 8, RangeMilli = 3000, SpeedMilli = 100, Flying = true, TrainSeconds = 300, UnlockLevel = 11, Cost = new ResourceCost(100, 80) },
        };

        private static readonly TowerDef[] Towers =
        {
            new TowerDef { Type = TowerType.Archer, Name = "Okçu Kulesi", Hp = 400, Attack = 12, IntervalTicks = 8, RangeMilli = 5000, CanHitAir = true, UnlockLevel = 2, BuildCost = new ResourceCost(120) },
            new TowerDef { Type = TowerType.Mage, Name = "Büyücü Kulesi", Hp = 320, Attack = 26, IntervalTicks = 20, RangeMilli = 4000, SplashMilli = 1200, ArmorPiercePct = 30, CanHitAir = true, UnlockLevel = 6, BuildCost = new ResourceCost(200, 80) },
            new TowerDef { Type = TowerType.Spear, Name = "Mızrak Kulesi", Hp = 500, Attack = 30, IntervalTicks = 15, RangeMilli = 2500, BonusVsMountedPct = 150, CanHitAir = false, UnlockLevel = 4, BuildCost = new ResourceCost(160) },
            new TowerDef { Type = TowerType.Elf, Name = "Elf Kulesi", Hp = 300, Attack = 7, IntervalTicks = 4, RangeMilli = 4500, CanHitAir = true, BonusVsAirPct = 100, UnlockLevel = 8, BuildCost = new ResourceCost(180, 120) },
        };

        private static readonly BuildingDef[] Buildings =
        {
            new BuildingDef { Type = BuildingType.Forest, Name = "Orman", Hp = 300, IsResource = true, UnlockLevel = 1 },
            new BuildingDef { Type = BuildingType.LumberDepot, Name = "Kereste Deposu", Hp = 450, IsResource = true, IsDepot = true, Stores = ResourceType.Wood, UnlockLevel = 1 },
            new BuildingDef { Type = BuildingType.StoneQuarry, Name = "Taş Ocağı", Hp = 400, IsResource = true, UnlockLevel = 5 },
            new BuildingDef { Type = BuildingType.StoneDepot, Name = "Taş Deposu", Hp = 550, IsResource = true, IsDepot = true, Stores = ResourceType.Stone, UnlockLevel = 5 },
            new BuildingDef { Type = BuildingType.GoldMine, Name = "Altın Madeni", Hp = 500, IsResource = true, UnlockLevel = 15 },
            new BuildingDef { Type = BuildingType.Bank, Name = "Banka", Hp = 800, IsResource = true, IsDepot = true, Stores = ResourceType.Gold, UnlockLevel = 15 },
            new BuildingDef { Type = BuildingType.IronMine, Name = "Demir Madeni", Hp = 600, IsResource = true, UnlockLevel = 25 },
            new BuildingDef { Type = BuildingType.IronDepot, Name = "Demir Deposu", Hp = 700, IsResource = true, IsDepot = true, Stores = ResourceType.Iron, UnlockLevel = 25 },
            new BuildingDef { Type = BuildingType.Barracks, Name = "Kışla", Hp = 500, UnlockLevel = 3 },
            new BuildingDef { Type = BuildingType.University, Name = "Üniversite", Hp = 600, UnlockLevel = 5 },
            new BuildingDef { Type = BuildingType.VillagerHouse, Name = "Köylü Evi", Hp = 300, UnlockLevel = 1 },
        };

        public static SoldierDef Soldier(SoldierType t) => Soldiers[(int)t];
        public static TowerDef Tower(TowerType t) => Towers[(int)t];
        public static BuildingDef Building(BuildingType t) => Buildings[(int)t];
        public static int SoldierTypeCount => Soldiers.Length;

        /// <summary>Seviye çarpanı (%): Sv1=100, her seviyede hızlanan artış. Asker/kule/bina için ortak (GEÇİCİ).</summary>
        public static int ScalePct(int level)
        {
            int l = level < 1 ? 0 : level - 1;
            return 100 + 20 * l + 2 * l * l;
        }

        public static int Scaled(int value, int level) => (int)((long)value * ScalePct(level) / 100);
    }
}
