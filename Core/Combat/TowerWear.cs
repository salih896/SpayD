using System;
using GameCore.Economy;

namespace GameCore.Combat
{
    /// <summary>Kalıcı kule hasarı ve tamir (GDD Bölüm 10). Yalnızca kulelerde; köy binalarında yok.</summary>
    public sealed class TowerWear
    {
        public const int MaxPermanentPct = 70;
        private static readonly int[] Limits = { 30, 20, 10, 5, 5 };   // 1..5. saldırı (toplam 70)

        public int PermanentDamagePct { get; private set; }
        public int AttackCount { get; private set; }
        public bool IsDisabled => PermanentDamagePct >= MaxPermanentPct;

        public TowerWear(int permanentDamagePct = 0, int attackCount = 0)
        {
            PermanentDamagePct = Math.Max(0, Math.Min(MaxPermanentPct, permanentDamagePct));
            AttackCount = Math.Max(0, attackCount);
        }

        /// <summary>Savaşta kulenin aldığı hasar yüzdesinden kalıcı hasar ekler: sınırlar 30/20/10/5/5, toplam en çok %70.</summary>
        public int ApplyBattle(int battleDamagePct)
        {
            if (battleDamagePct <= 0) return 0;
            int limit = AttackCount < Limits.Length ? Limits[AttackCount] : 0;
            int add = Math.Min(Math.Min(battleDamagePct, limit), MaxPermanentPct - PermanentDamagePct);
            if (add < 0) add = 0;
            PermanentDamagePct += add;
            AttackCount++;
            return add;
        }

        public void RepairAll() { PermanentDamagePct = 0; AttackCount = 0; }

        /// <summary>Her %5 hasar = 1 tamir birimi (yukarı yuvarlanır).</summary>
        public int RepairUnits => (PermanentDamagePct + 4) / 5;

        /// <summary>Tamir birimi başına saniye: Sv1 30sn, Sv2 60sn, Sv3 180sn (GDD örneği); sonrası GEÇİCİ: +120sn/seviye.</summary>
        public static int SecondsPerUnit(int towerLevel)
        {
            if (towerLevel <= 1) return 30;
            if (towerLevel == 2) return 60;
            if (towerLevel == 3) return 180;
            return 180 + 120 * (towerLevel - 3);
        }

        public int RepairSeconds(int towerLevel) => RepairUnits * SecondsPerUnit(towerLevel);

        /// <summary>Tamir maliyeti (GEÇİCİ): birim başına odun; altın yalnızca oyuncu seviyesinde açıksa (maliyet yalnızca açık kaynakla).</summary>
        public ResourceCost RepairCost(int towerLevel, ResourceConfig cfg, int playerLevel)
        {
            int wood = RepairUnits * 10 * Math.Max(1, towerLevel);
            int gold = ResourceRules.IsUnlocked(cfg, ResourceType.Gold, playerLevel) ? RepairUnits * 4 * Math.Max(1, towerLevel) : 0;
            return new ResourceCost(wood, 0, gold);
        }
    }
}
