using System.Collections.Generic;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Combat
{
    public sealed class TowerSpec
    {
        public TowerType Type; public int Level = 1; public int PermanentDamagePct;
        public TowerSpec(TowerType type, int level = 1, int permanentDamagePct = 0) { Type = type; Level = level; PermanentDamagePct = permanentDamagePct; }
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
