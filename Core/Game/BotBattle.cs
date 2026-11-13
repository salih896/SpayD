using System;
using System.Collections.Generic;
using GameCore.Combat;

namespace GameCore.Game
{
    /// <summary>
    /// Otomatik saldırgan: tüm askerleri başta alana yayar ve savaşı sonuna kadar oynatır.
    /// Kullanımı: çevrimdışı baskın simülasyonu ve denge testleri. İnsan oyuncu bunun yerine dokunarak ve zamanlayarak oynar.
    /// </summary>
    public static class BotBattle
    {
        private static readonly SoldierType[] Order =
        {
            SoldierType.Spearman, SoldierType.Swordsman, SoldierType.Cavalry, SoldierType.Archer,
            SoldierType.Mage, SoldierType.Fairy, SoldierType.Goblin
        };

        public static BattleSim Run(DefenseSetup defense, Dictionary<SoldierType, int> stock, Dictionary<SoldierType, int> levels, CombatMods mods, int seed)
        {
            var sim = new BattleSim(defense, stock, levels, seed);
            if (mods != null) sim.AttackerMods = mods;
            int size = defense.Layout.Size;
            int i = 0;
            foreach (var t in Order)
            {
                int n = sim.StockOf(t);
                for (int k = 0; k < n; k++, i++)
                {
                    string err;
                    sim.Deploy(t, (i * 3) % size, 1 + (i % VillageLayoutRows), out err);
                }
            }
            int guard = BattleSim.TimeLimitTicks + 5;
            while (!sim.Finished && guard-- > 0) sim.Step();
            return sim;
        }

        private const int VillageLayoutRows = 4;
    }
}
