using System;
using System.Collections.Generic;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Village;

namespace GameCore.Game
{
    /// <summary>
    /// Oyuncu çevrimdışıyken günde bir kez yapay bir saldırgan köye saldırır (Sv3'ten itibaren, geçerli savunma varsa).
    /// Oyuncunun savunma kurma motivasyonu: kule aşınması + yağma kaybı + rapor. (GEÇİCİ; gerçek PvP gelince bu kalkar.)
    /// </summary>
    public static class RaidSimulator
    {
        public const int MinLevel = 3;
        public const int MaxLootLossPct = 30; // bir baskında depodaki kaynağın en çok %30'u

        public static Dictionary<SoldierType, int> RaiderArmy(int level)
        {
            var stock = new Dictionary<SoldierType, int>();
            var unlocked = new List<SoldierType>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++)
                if (ContentCatalog.Soldier((SoldierType)i).UnlockLevel <= level) unlocked.Add((SoldierType)i);
            if (unlocked.Count == 0) unlocked.Add(SoldierType.Swordsman);
            int total = 4 + level + level / 2;
            for (int i = 0; i < total; i++)
            {
                var t = unlocked[i % unlocked.Count];
                stock[t] = (stock.TryGetValue(t, out var n) ? n : 0) + 1;
            }
            return stock;
        }

        public static RaidReport TryRaid(GameState g, int day)
        {
            int level = g.Level;
            if (level < MinLevel) return null;
            string err;
            if (!g.VillageValid(out err) || g.Towers.Count == 0) return null;

            var levels = new Dictionary<SoldierType, int>();
            for (int i = 0; i < ContentCatalog.SoldierTypeCount; i++) levels[(SoldierType)i] = Math.Max(1, level - 2);
            var sim = BotBattle.Run(g.MakeDefense(), RaiderArmy(level), levels, null, 9001 + day * 17 + level);
            var res = sim.Result(g.ResourcesNow(), g.Config, level);

            int damaged = 0;
            foreach (var rec in res.TowerDamages)
            {
                if (!g.Wears.TryGetValue(rec.Pos, out var w)) { w = new TowerWear(); g.Wears[rec.Pos] = w; }
                if (rec.DamagePct > 0) { w.ApplyBattle(rec.DamagePct); damaged++; }
            }
            // Kaynak kaybı: yalnızca saldırgan kazandıysa, en çok %30
            var lost = new ResourceCost(0);
            if (res.Victory)
            {
                int w0 = Math.Min(res.Loot.Wood, g.Wallet.Get(ResourceType.Wood) * MaxLootLossPct / 100);
                int s0 = Math.Min(res.Loot.Stone, g.Wallet.Get(ResourceType.Stone) * MaxLootLossPct / 100);
                int g0 = Math.Min(res.Loot.Gold, g.Wallet.Get(ResourceType.Gold) * MaxLootLossPct / 100);
                int i0 = Math.Min(res.Loot.Iron, g.Wallet.Get(ResourceType.Iron) * MaxLootLossPct / 100);
                lost = g.Wallet.Plunder(new ResourceCost(w0, s0, g0, i0));
            }
            return new RaidReport { Day = day, Result = res, LootLost = lost, TowersDamaged = damaged };
        }
    }
}
