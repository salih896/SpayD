using GameCore.Economy;

namespace GameCore.Combat
{
    /// <summary>10-14: Askerî ağaç araştırmasıyla açılan askerler (kullanıcı kararı 25.11.2026).</summary>
    public enum SoldierType { Swordsman = 0, Archer = 1, Mage = 2, Goblin = 3, Cavalry = 4, Spearman = 5, Fairy = 6, Giant = 7, Catapult = 8, DragonRider = 9,
        Shieldbearer = 10, Assassin = 11, Healer = 12, Crossbowman = 13, Bomber = 14 }
    /// <summary>Kule türleri. 2 = Duvar (eski Mızrak Kulesi; kayıt uyumu için değer korunur).</summary>
    /// <summary>Kurulabilen kuleler (kullanıcı kararı 25.11.2026): yalnızca Okçu Kulesi ve Duvar. Mage/Elf/Cannon/Ice/Lightning eski kayıtlar için kalır (Okçu + element'e çevrilir).</summary>
    public enum TowerType { Archer = 0, Mage = 1, Wall = 2, Elf = 3, Cannon = 4, Ice = 5, Lightning = 6 }

    /// <summary>Okçu Kulesi elementi: önce Akademi Element ağacı Sv1'de araştırılır, sonra kuleye kısa süre + az kaynakla uygulanır.</summary>
    public enum TowerElement { None = 0, Arrow = 1, Fire = 2, Ice = 3, Poison = 4, Magic = 5, Lightning = 6, Ballista = 7 }
    public enum BuildingType
    {
        Forest = 0, LumberDepot = 1, StoneQuarry = 2, StoneDepot = 3, GoldMine = 4, Bank = 5,
        IronMine = 6, IronDepot = 7, Barracks = 8, University = 9, VillagerHouse = 10
    }
    public enum TrapType { None = 0, Spikes = 1, Slippery = 2, Fire = 3, Poison = 4, Frost = 5, Bomb = 6, Snare = 7, Shock = 8 }

    /// <summary>Asker tanımı. TÜM SAYILAR GEÇİCİDİR (dengeleme aşamasında değişecek). Süreler tick (10/sn), uzaklıklar mili-hücre (1000 = 1 hücre).</summary>
    public sealed class SoldierDef
    {
        public SoldierType Type; public string Name;
        public int Hp, Attack, IntervalTicks, RangeMilli, SpeedMilli;
        public int ArmorPct, FireResistPct, SplashMilli;
        public bool Flying, Mounted;
        public int TrainSeconds, UnlockLevel;
        public ResourceCost Cost;
    }

    public sealed class TowerDef
    {
        public TowerType Type; public string Name;
        public int Hp, Attack, IntervalTicks, RangeMilli, SplashMilli;
        public int ArmorPiercePct;
        public bool CanHitAir;
        public int BonusVsAirPct, BonusVsMountedPct;
        public int SlowTicks;     // Buz: vurduğu askeri bu kadar tick yavaşlatır (hız yarıya)
        public int ChainCount;    // Şimşek: hedeften sonra sıçradığı ek asker sayısı (%70 hasar)
        public int TauntMilli;    // Duvar: bu mesafedeki yer askerleri hedef seçerken duvarı önceler
        public int CritPct, CritMultPct;          // Ok: kritik vuruş şansı ve çarpanı
        public int BurnTicks, BurnPermille;       // Ateş: yanma süresi ve tick başına hasar (vuruşun ‰'i)
        public int FreezeStackPct, FreezeMaxStacks; // Buz: aynı askere üst üste vurdukça her yığın +% hasar
        public int PoisonTicks, PoisonPermille, WeakenPct; // Zehir: zamanla hasar + askerin saldırısını % düşürür
        public int UnlockLevel;
        public ResourceCost BuildCost;
    }

    public sealed class BuildingDef
    {
        public BuildingType Type; public string Name;
        public int Hp;
        public bool IsResource;          // yağmalanabilir kaynak binası (üretim/depo)
        public bool IsDepot;             // depo: yağma bu binanın hasarına bağlı
        public ResourceType Stores;      // IsDepot ise depolanan kaynak
        public int UnlockLevel;
    }
}
