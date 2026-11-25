using System.Collections.Generic;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Combat
{
    public sealed class TowerSpec
    {
        public TowerType Type; public int Level = 1; public int PermanentDamagePct;
        public TowerElement Element;
        /// <summary>Savunma elementi (Ateş/Buz/Zehir): bu elementle saldıran askerlerden az hasar alır (direnç) / döngüye göre.</summary>
        public TowerElement DefElement;
        /// <summary>Geliştirme dalları (kuleye tıklayarak): Okçu A = saldırı hızı, B = hasar; Duvar A = can, B = zırh. Her yükseltme seviye +1 ve seçilen dal +1.</summary>
        public int BranchA, BranchB;
        public TowerSpec(TowerType type, int level = 1, int permanentDamagePct = 0) { Type = type; Level = level; PermanentDamagePct = permanentDamagePct; }
        public TowerSpec With(int permanentDamagePct) => new TowerSpec(Type, Level, permanentDamagePct) { Element = Element, DefElement = DefElement, BranchA = BranchA, BranchB = BranchB };

        // Dal etkileri (GEÇİCİ): Okçu hız %3/puan (en az %40 aralık), hasar +%4/puan; Duvar can +%5/puan, zırh %2/puan (en çok %60).
        public int IntervalPct => System.Math.Max(40, 100 - 3 * BranchA);
        public int DamagePct => 100 + 4 * BranchB;
        public int WallHpPct => 100 + 5 * BranchA;
        public int WallArmorPct => System.Math.Min(60, 2 * BranchB);
    }

    public sealed class BuildingSpec
    {
        public BuildingType Type; public int Level = 1;
        public BuildingSpec(BuildingType type, int level = 1) { Type = type; Level = level; }
    }

    /// <summary>Savaşa giren savunma: geometri (VillageLayout) + hangi hücrede hangi kule/bina/tuzak olduğu. Savaş başında değişmez kopya alınır.</summary>
    public sealed class DefenseSetup
    {
        public VillageLayout Layout;
        public Dictionary<GridPos, TowerSpec> Towers = new Dictionary<GridPos, TowerSpec>();
        public Dictionary<GridPos, BuildingSpec> Buildings = new Dictionary<GridPos, BuildingSpec>();
        public Dictionary<GridPos, TrapType> Traps = new Dictionary<GridPos, TrapType>();

        public CombatMods Mods = new CombatMods(); // savunmacının araştırma bonusları

        public DefenseSetup(VillageLayout layout) { Layout = layout; }
    }
}
