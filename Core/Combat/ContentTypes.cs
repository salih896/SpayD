using GameCore.Economy;

namespace GameCore.Combat
{
    public enum SoldierType { Swordsman = 0, Archer = 1, Mage = 2, Goblin = 3, Cavalry = 4, Spearman = 5, Fairy = 6 }
    public enum TowerType { Archer = 0, Mage = 1, Spear = 2, Elf = 3 }
    public enum BuildingType
    {
        Forest = 0, LumberDepot = 1, StoneQuarry = 2, StoneDepot = 3, GoldMine = 4, Bank = 5,
        IronMine = 6, IronDepot = 7, Barracks = 8, University = 9, VillagerHouse = 10
    }
    public enum TrapType { None = 0, Spikes = 1, Slippery = 2, Fire = 3 }

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
