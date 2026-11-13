using System.Collections.Generic;
using GameCore.Economy;
using GameCore.Grid;

namespace GameCore.Combat
{
    public sealed class TowerDamageRecord
    {
        public GridPos Pos; public int DamagePct;
        public TowerDamageRecord(GridPos pos, int damagePct) { Pos = pos; DamagePct = damagePct; }
    }

    /// <summary>Savaşın sunucuda kaydedilecek sonucu.</summary>
    public sealed class BattleResult
    {
        public bool Victory;
        public int Stars;
        public int OverallDamagePct;
        public int DurationTicks;
        public bool Retreated;
        public int Xp;
        public ResourceCost Loot;
        public List<int> StructureDamagePcts = new List<int>();
        public List<TowerDamageRecord> TowerDamages = new List<TowerDamageRecord>();

        /// <summary>Envantere dönenler (kullanılmayan stok + sağlıklı hayatta kalanlar).</summary>
        public Dictionary<SoldierType, int> Returned = new Dictionary<SoldierType, int>();
        public Dictionary<SoldierType, int> LostDead = new Dictionary<SoldierType, int>();
        public Dictionary<SoldierType, int> LostInjured = new Dictionary<SoldierType, int>();
        public Dictionary<SoldierType, int> LostSacrificed = new Dictionary<SoldierType, int>();

        public static int Total(Dictionary<SoldierType, int> d)
        {
            int n = 0;
            foreach (var kv in d) n += kv.Value;
            return n;
        }
    }
}
