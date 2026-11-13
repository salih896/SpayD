using System;
using System.Collections.Generic;

namespace GameCore.Combat
{
    /// <summary>Saf tamsayı savaş matematiği (float yok). Sunucu ve istemci aynı sonucu üretir.</summary>
    public static class CombatMath
    {
        public const int VictoryThresholdPct = 51;   // GDD Bölüm 17

        public static long DistSq(int ax, int ay, int bx, int by)
        {
            long dx = ax - bx, dy = ay - by;
            return dx * dx + dy * dy;
        }

        public static int ISqrt(long v)
        {
            if (v <= 0) return 0;
            long x = (long)Math.Sqrt(v);
            while (x * x > v) x--;
            while ((x + 1) * (x + 1) <= v) x++;
            return (int)x;
        }

        public static int ReduceByArmor(int dmg, int armorPct)
        {
            if (armorPct < 0) armorPct = 0;
            if (armorPct > 90) armorPct = 90;
            int r = dmg * (100 - armorPct) / 100;
            return r < 1 ? 1 : r;
        }

        /// <summary>Kule vuruşu: seviye çarpanı, uçan/atlı bonusu, zırh (delici oranı kadar azalır).</summary>
        public static int TowerDamage(TowerDef def, int level, bool targetFlying, bool targetMounted, int targetArmorPct)
        {
            int raw = ContentCatalog.Scaled(def.Attack, level);
            int bonus = 0;
            if (targetFlying) bonus += def.BonusVsAirPct;
            if (targetMounted) bonus += def.BonusVsMountedPct;
            raw = raw * (100 + bonus) / 100;
            int armor = targetArmorPct * (100 - def.ArmorPiercePct) / 100;
            return ReduceByArmor(raw, armor);
        }

        /// <summary>Genel hasar: yapıların hasar yüzdelerinin basit aritmetik ortalaması (GDD Bölüm 17). Tam sayıya aşağı yuvarlanır.</summary>
        public static int OverallDamagePct(IList<int> pcts)
        {
            if (pcts == null || pcts.Count == 0) return 0;
            long sum = 0;
            for (int i = 0; i < pcts.Count; i++) sum += pcts[i];
            return (int)(sum / pcts.Count);
        }

        public static bool IsVictory(int overallPct) => overallPct >= VictoryThresholdPct;

        /// <summary>0 = yenilgi; 1 yıldız: ≥%51 zafer; 2 yıldız: ≥%75; 3 yıldız: %100 (GEÇİCİ eşikler).</summary>
        public static int Stars(int overallPct)
        {
            if (overallPct < VictoryThresholdPct) return 0;
            if (overallPct >= 100) return 3;
            return overallPct >= 75 ? 2 : 1;
        }
    }
}
