namespace GameCore.Combat
{
    /// <summary>Araştırmadan gelen savaş çarpanları, binde (‰) cinsinden: 50 = %5. Tamsayı; sunucu/istemci aynı sonucu üretir.</summary>
    public sealed class CombatMods
    {
        public int SoldierDamage, SoldierHp, TowerDamage, TowerHp, BuildingHp;

        public static int Apply(int value, int permille)
        {
            if (permille == 0) return value;
            long v = (long)value * (1000 + permille) / 1000;
            return v < 1 ? 1 : (int)v;
        }
    }
}
