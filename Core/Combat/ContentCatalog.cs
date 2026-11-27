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
            new SoldierDef { Type = SoldierType.Swordsman, Name = "Uzun Kılıç", Hp = 70, Attack = 14, IntervalTicks = 8, RangeMilli = 1500, SpeedMilli = 70, ArmorPct = 10, TrainSeconds = 120, UnlockLevel = 3, Cost = new ResourceCost(40) },
            // Uzaktan, kırılgan
            new SoldierDef { Type = SoldierType.Archer, Name = "Okçu", Hp = 60, Attack = 17, IntervalTicks = 10, RangeMilli = 2000, SpeedMilli = 70, TrainSeconds = 180, UnlockLevel = 3, Cost = new ResourceCost(45) },
            // Alan hasarı, ateşe dirençli
            new SoldierDef { Type = SoldierType.Mage, Name = "Büyücü", Hp = 50, Attack = 43, IntervalTicks = 18, RangeMilli = 2000, SpeedMilli = 60, FireResistPct = 25, SplashMilli = 1500, TrainSeconds = 300, UnlockLevel = 7, Cost = new ResourceCost(70, 30) },
            // Çok hızlı, ucuz, kaynak avcısı
            new SoldierDef { Type = SoldierType.Goblin, Name = "Goblin", Hp = 35, Attack = 13, IntervalTicks = 6, RangeMilli = 1500, SpeedMilli = 160, TrainSeconds = 120, UnlockLevel = 4, Cost = new ResourceCost(20) },
            // Hızlı ve dayanıklı, mızrak kulesine zayıf
            new SoldierDef { Type = SoldierType.Cavalry, Name = "Atlı Kılıçlı", Hp = 260, Attack = 22, IntervalTicks = 10, RangeMilli = 1500, SpeedMilli = 150, ArmorPct = 20, Mounted = true, TrainSeconds = 600, UnlockLevel = 9, Cost = new ResourceCost(90, 60) },
            // Yavaş, yüksek zırh
            new SoldierDef { Type = SoldierType.Spearman, Name = "Mızraklı", Hp = 150, Attack = 12, IntervalTicks = 10, RangeMilli = 2000, SpeedMilli = 40, ArmorPct = 30, TrainSeconds = 300, UnlockLevel = 5, Cost = new ResourceCost(60, 15) },
            // Uçar, yolu yok sayar; mızrak kulesi vuramaz
            new SoldierDef { Type = SoldierType.Fairy, Name = "Peri", Hp = 55, Attack = 9, IntervalTicks = 8, RangeMilli = 2000, SpeedMilli = 100, Flying = true, TrainSeconds = 300, UnlockLevel = 11, Cost = new ResourceCost(100, 80) },
            // Çok dayanıklı, yavaş; yalnızca kulelere yürür (kule ateşini üstüne çeker)
            new SoldierDef { Type = SoldierType.Giant, Name = "Dev", Hp = 600, Attack = 30, IntervalTicks = 15, RangeMilli = 1500, SpeedMilli = 30, ArmorPct = 25, TrainSeconds = 600, UnlockLevel = 13, Cost = new ResourceCost(160, 110) },
            // Kuşatma: çok uzak menzil, alan hasarı, kırılgan ve yavaş
            new SoldierDef { Type = SoldierType.Catapult, Name = "Mancınık", Hp = 140, Attack = 70, IntervalTicks = 35, RangeMilli = 2000, SpeedMilli = 30, SplashMilli = 1500, TrainSeconds = 600, UnlockLevel = 17, Cost = new ResourceCost(220, 140, 60) },
            // Uçan ağır birlik: ateşe dirençli, alan hasarı; elf kulesine zayıf
            new SoldierDef { Type = SoldierType.DragonRider, Name = "Ejderha Binicisi", Hp = 420, Attack = 34, IntervalTicks = 12, RangeMilli = 2000, SpeedMilli = 90, FireResistPct = 50, SplashMilli = 1000, Flying = true, TrainSeconds = 600, UnlockLevel = 21, Cost = new ResourceCost(260, 200, 110) },
            // Askerî araştırmayla açılanlar (kullanıcı kararı 25.11.2026; değerler GEÇİCİ)
            // Kalkanlı: çok zırhlı, yavaş; kuleler önce onu hedefler
            new SoldierDef { Type = SoldierType.Shieldbearer, Name = "Kalkanlı", Hp = 380, Attack = 8, IntervalTicks = 10, RangeMilli = 1500, SpeedMilli = 50, ArmorPct = 40, TrainSeconds = 300, UnlockLevel = 4, Cost = new ResourceCost(80) },
            // Suikastçı: çok hızlı, yüksek hasar, az can; önce kulelere gider
            new SoldierDef { Type = SoldierType.Assassin, Name = "Suikastçı", Hp = 80, Attack = 26, IntervalTicks = 8, RangeMilli = 1500, SpeedMilli = 140, TrainSeconds = 300, UnlockLevel = 9, Cost = new ResourceCost(90, 40) },
            // Şifacı: saldırmaz, 2 hücredeki askerleri iyileştirir (uzaktan durur)
            new SoldierDef { Type = SoldierType.Healer, Name = "Şifacı", Hp = 140, Attack = 0, IntervalTicks = 10, RangeMilli = 2000, SpeedMilli = 70, TrainSeconds = 420, UnlockLevel = 14, Cost = new ResourceCost(120, 80) },
            // Arbaletli: uzun menzil, kulelere bonus
            new SoldierDef { Type = SoldierType.Crossbowman, Name = "Arbaletli", Hp = 90, Attack = 26, IntervalTicks = 36, RangeMilli = 3000, SpeedMilli = 60, TrainSeconds = 360, UnlockLevel = 19, Cost = new ResourceCost(150, 90, 40) },
            // Bombacı: yapıya ulaşınca patlar (büyük alan hasarı), kendisi ölür
            new SoldierDef { Type = SoldierType.Bomber, Name = "Bombacı", Hp = 110, Attack = 200, IntervalTicks = 10, RangeMilli = 1500, SpeedMilli = 110, SplashMilli = 1500, TrainSeconds = 360, UnlockLevel = 24, Cost = new ResourceCost(160, 100, 50) },
        };

        // Kule hakkı 5-10 (GDD Bölüm 9): az ama güçlü kuleler. Can/hasar denge turu 2'de ×2,5 (13.11.2026).
        /// <summary>Kaldırılmış kule türü: kurulamaz (eski kayıtlar Okçu + element olur; savaş kodu eski türü yine çalıştırabilir).</summary>
        public const int Removed = 999;

        private static readonly TowerDef[] Towers =
        {
            new TowerDef { Type = TowerType.Archer, Name = "Okçu Kulesi", Hp = 1000, Attack = 20, IntervalTicks = 5, RangeMilli = 2500, CanHitAir = true, UnlockLevel = 2, BuildCost = new ResourceCost(120) },
            new TowerDef { Type = TowerType.Mage, Name = "Büyücü Kulesi", Hp = 800, Attack = 20, IntervalTicks = 14, RangeMilli = 2500, SplashMilli = 1200, ArmorPiercePct = 30, CanHitAir = true, UnlockLevel = Removed, BuildCost = new ResourceCost(200, 80) },
            // Duvar (kullanıcı kararı 14.11.2026: Mızrak Kulesi → Duvar): saldırmaz, çok dayanıklı, yakındaki yer askerlerini üstüne çeker
            new TowerDef { Type = TowerType.Wall, Name = "Duvar", Hp = 3200, Attack = 0, IntervalTicks = 10, RangeMilli = 0, CanHitAir = false, TauntMilli = 2500, UnlockLevel = 4, BuildCost = new ResourceCost(160) },
            new TowerDef { Type = TowerType.Elf, Name = "Elf Kulesi", Hp = 750, Attack = 20, IntervalTicks = 3, RangeMilli = 3000, CanHitAir = true, BonusVsAirPct = 100, UnlockLevel = Removed, BuildCost = new ResourceCost(180, 120) },
            // Top: yavaş, yüksek alan hasarı, havayı vuramaz
            new TowerDef { Type = TowerType.Cannon, Name = "Top Kulesi", Hp = 1100, Attack = 20, IntervalTicks = 22, RangeMilli = 3000, SplashMilli = 1200, CanHitAir = false, UnlockLevel = Removed, BuildCost = new ResourceCost(300, 150) },
            // Buz: düşük hasar, vurduğunu yavaşlatır
            new TowerDef { Type = TowerType.Ice, Name = "Buz Kulesi", Hp = 900, Attack = 20, IntervalTicks = 7, RangeMilli = 2500, SlowTicks = 25, CanHitAir = true, UnlockLevel = Removed, BuildCost = new ResourceCost(250, 200) },
            // Şimşek: hedeften 2 askere daha sıçrar
            new TowerDef { Type = TowerType.Lightning, Name = "Şimşek Kulesi", Hp = 850, Attack = 20, IntervalTicks = 13, RangeMilli = 2500, ChainCount = 2, CanHitAir = true, UnlockLevel = Removed, BuildCost = new ResourceCost(300, 200, 120) },
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
            new BuildingDef { Type = BuildingType.University, Name = "Akademi", Hp = 600, UnlockLevel = 5 },
            new BuildingDef { Type = BuildingType.VillagerHouse, Name = "Köylü Evi", Hp = 300, UnlockLevel = 1 },
        };

        // ---------- Kule elementleri (kullanıcı kararı 25.11.2026; sayılar GEÇİCİ) ----------
        private static readonly TowerDef[] elementDefs = BuildElementDefs();

        private static TowerDef[] BuildElementDefs()
        {
            var a = Towers[(int)TowerType.Archer];
            TowerDef E(TowerElement e, string name, int atkPct, int interval) => new TowerDef
            {
                Type = TowerType.Archer, Name = name, Hp = a.Hp, Attack = a.Attack * atkPct / 100, IntervalTicks = interval, RangeMilli = a.RangeMilli,
                CanHitAir = true, UnlockLevel = a.UnlockLevel, BuildCost = a.BuildCost
            };
            var d = new TowerDef[7];
            d[0] = a;
            d[1] = E(TowerElement.Arrow, "Okçu Kulesi (Ok)", 130, 4); d[1].CritPct = 20; d[1].CritMultPct = 200;               // yüksek hasar, kritik, hızlı
            d[2] = E(TowerElement.Fire, "Okçu Kulesi (Ateş)", 260, 25); d[2].SplashMilli = 1200; d[2].BurnTicks = 30; d[2].BurnPermille = 40; // alan + yanma, çok yavaş
            d[3] = E(TowerElement.Ice, "Okçu Kulesi (Buz)", 90, 7); d[3].SlowTicks = 25; d[3].FreezeStackPct = 15; d[3].FreezeMaxStacks = 5;  // yavaşlatma + birikerek donma
            d[4] = E(TowerElement.Poison, "Okçu Kulesi (Zehir)", 70, 8); d[4].PoisonTicks = 40; d[4].PoisonPermille = 30; d[4].WeakenPct = 30; // zayıflatma + zamanla hasar
            d[5] = E(TowerElement.Magic, "Okçu Kulesi (Büyü)", 130, 9); d[5].BonusVsAirPct = 100; d[5].ArmorPiercePct = 30;      // uçanlara büyük bonus
            d[6] = E(TowerElement.Lightning, "Okçu Kulesi (Şimşek)", 120, 12); d[6].ChainCount = 2;                             // 2 askere sıçrar
            return d;
        }

        /// <summary>Kulenin savaş tanımı: Okçu Kulesi + element; elementsiz veya eski tür → katalogdaki tanım.</summary>
        public static TowerDef TowerFor(TowerType t, TowerElement e) => t == TowerType.Archer && e != TowerElement.None ? elementDefs[(int)e] : Towers[(int)t];

        public static string ElementName(TowerElement e)
        {
            switch (e)
            {
                case TowerElement.Arrow: return "Ok"; case TowerElement.Fire: return "Ateş"; case TowerElement.Ice: return "Buz";
                case TowerElement.Poison: return "Zehir"; case TowerElement.Magic: return "Büyü"; case TowerElement.Lightning: return "Şimşek";
                default: return "Yok";
            }
        }

        public static string ElementEffect(TowerElement e)
        {
            switch (e)
            {
                case TowerElement.Arrow: return "Yüksek hasar, %20 kritik (x2), hızlı atış";
                case TowerElement.Fire: return "Alan hasarı + yanma; çok yavaş atış";
                case TowerElement.Ice: return "Yavaşlatır; üst üste vurdukça donma hasarı artar";
                case TowerElement.Poison: return "Zehirler (zamanla hasar), askerin hasarını %30 düşürür";
                case TowerElement.Magic: return "Uçanlara +%100 hasar, zırhı kısmen deler";
                case TowerElement.Lightning: return "Vurduğu askerin yanındaki 2 askere sıçrar";
                default: return "Elementsiz okçu";
            }
        }

        /// <summary>Eski kule türü → Okçu Kulesi elementi (kayıt dönüşümü).</summary>
        public static TowerElement LegacyElement(TowerType t)
        {
            switch (t)
            {
                case TowerType.Mage: return TowerElement.Magic; case TowerType.Elf: return TowerElement.Arrow; case TowerType.Cannon: return TowerElement.Fire;
                case TowerType.Ice: return TowerElement.Ice; case TowerType.Lightning: return TowerElement.Lightning;
                default: return TowerElement.None;
            }
        }

        /// <summary>Görsel: element → çizilecek eski kule görünümü (model) ve mermi.</summary>
        public static TowerType ModelLook(TowerType t, TowerElement e)
        {
            if (t != TowerType.Archer) return t;
            switch (e)
            {
                case TowerElement.Arrow: return TowerType.Elf; case TowerElement.Fire: return TowerType.Cannon; case TowerElement.Ice: return TowerType.Ice;
                case TowerElement.Magic: return TowerType.Mage; case TowerElement.Lightning: return TowerType.Lightning;
                default: return TowerType.Archer;
            }
        }

        public static TowerType ProjectileLook(TowerType t, TowerElement e) => e == TowerElement.Poison ? TowerType.Elf : e == TowerElement.Arrow ? TowerType.Archer : ModelLook(t, e);

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
