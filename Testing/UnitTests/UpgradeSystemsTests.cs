using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Game;
using GameCore.Grid;
using GameCore.Progression;
using GameCore.Village;

namespace GameCore.Tests
{
    /// <summary>Tuzak hakkı/araştırma, 8 tuzak etkisi, yol basamağı yavaşlatması, asker yetenekleri (14.11.2026).</summary>
    public class UpgradeSystemsTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        private static long XpFor(int level)
        {
            long s = 0;
            for (int l = 1; l < level; l++) s += ProgressionRules.XpToNextLevel(l);
            return s;
        }

        private static GameState Rich(int level)
        {
            var g = GameState.NewGame();
            g.TotalXp = XpFor(level);
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 1000000); }
            return g;
        }

        // ---------- Tuzaklar ----------
        [Test]
        public void Traps_FourSlots_ResearchGivesSix_UnlockAndCost()
        {
            var g = Rich(30);
            string err;
            int wood = g.Wallet.Get(ResourceType.Wood);
            for (int y = 1; y <= 4; y++) Assert.IsTrue(g.TryToggleTrap(P(5, y), TrapType.Spikes, out err), err);
            Assert.Less(g.Wallet.Get(ResourceType.Wood), wood, "Tuzak ücretli");
            Assert.IsFalse(g.TryToggleTrap(P(5, 5), TrapType.Spikes, out err));
            StringAssert.Contains("Tuzak hakkı 4", err);
            Assert.IsTrue(g.TryToggleTrap(P(5, 1), TrapType.Bomb, out err), "Var olan tuzağı değiştirmek hakkı aşmaz: " + err);

            var trapRs = new List<ResearchDef>(ResearchCatalog.All).FindAll(d => d.Stat == ResearchStat.TrapSlot);
            Assert.AreEqual(2, trapRs.Count, "Savunma: Tuzak Hakkı I-II");
            g.Research.ForceComplete(trapRs[0].Id);
            Assert.AreEqual(5, g.MaxTraps);
            g.Research.ForceComplete(trapRs[1].Id);
            Assert.AreEqual(6, g.MaxTraps);
            Assert.IsTrue(g.TryToggleTrap(P(5, 5), TrapType.Shock, out err), err);
            Assert.IsTrue(g.TryToggleTrap(P(5, 6), TrapType.Frost, out err), err);
            Assert.IsFalse(g.TryToggleTrap(P(5, 7), TrapType.Frost, out err));

            var low = Rich(9);
            Assert.IsFalse(low.TryToggleTrap(P(5, 1), TrapType.Poison, out err), "Zehirli Sv10");
            StringAssert.Contains("Sv 10", err);
        }

        [Test]
        public void TrapCatalog_HasEightTypes_WithIncreasingUnlocks()
        {
            Assert.AreEqual(8, TrapCatalog.Count);
            int prev = 0;
            for (int i = 1; i <= 8; i++) { var d = TrapCatalog.Get((TrapType)i); Assert.IsNotNull(d); Assert.Greater(d.UnlockLevel, prev); prev = d.UnlockLevel; }
        }

        private static DefenseSetup Road(TrapType trap, RoadTier tier = RoadTier.Wood)
        {
            var layout = new VillageLayout(11, 20, 10);
            layout.ToggleEntrance(P(5, 0), out _);
            for (int y = 1; y <= 8; y++) layout.TryPlace(P(5, y), CellKind.Road, out _, tier);
            layout.TryPlace(P(6, 8), CellKind.Building, out _);
            var s = new DefenseSetup(layout);
            s.Buildings[P(6, 8)] = new BuildingSpec(BuildingType.LumberDepot);
            if (trap != TrapType.None) s.Traps[P(5, 2)] = trap;
            return s;
        }

        private static BattleSim Run(DefenseSetup d, SoldierType t, int n, int ticks, int level = 1)
        {
            var sim = new BattleSim(d, new Dictionary<SoldierType, int> { { t, n } }, new Dictionary<SoldierType, int> { { t, level } }, 5);
            for (int i = 0; i < n; i++) sim.Deploy(t, 5, 1, out _);
            for (int i = 0; i < ticks && !sim.Finished; i++) sim.Step();
            return sim;
        }

        [Test]
        public void Bomb_DamagesGroup()
        {
            var sim = Run(Road(TrapType.Bomb), SoldierType.Giant, 3, 120);
            int hurt = 0; foreach (var s in sim.Soldiers) if (s.Hp < s.MaxHp) hurt++;
            Assert.GreaterOrEqual(hurt, 2, "Patlama birden çok askere");
        }

        [Test]
        public void Snare_HoldsSoldier_SpearmanSv20Immune()
        {
            var a = Run(Road(TrapType.Snare), SoldierType.Swordsman, 1, 60);
            var b = Run(Road(TrapType.None), SoldierType.Swordsman, 1, 60);
            Assert.Less(a.Soldiers[0].Y, b.Soldiers[0].Y, "Kapan askeri tutar (daha geride kalır)");
            var spear = Run(Road(TrapType.Snare), SoldierType.Spearman, 1, 1, 20);
            Assert.IsTrue(spear.Soldiers[0].Perks.ImmuneSlow, "Mızraklı Sv20: Sarsılmaz");
        }

        [Test]
        public void Poison_And_Fire_DealDamageOverTime()
        {
            foreach (var t in new[] { TrapType.Poison, TrapType.Fire })
            {
                var sim = Run(Road(t), SoldierType.Giant, 1, 120);
                Assert.Less(sim.Soldiers[0].Hp, sim.Soldiers[0].MaxHp, t.ToString());
            }
        }

        // ---------- Yol basamağı ----------
        [Test]
        public void HigherRoadTier_SlowsEnemies()
        {
            var wood = Run(Road(TrapType.None, RoadTier.Wood), SoldierType.Swordsman, 1, 70);
            var crystal = Run(Road(TrapType.None, RoadTier.Crystal), SoldierType.Swordsman, 1, 70);
            Assert.Less(crystal.Soldiers[0].Y, wood.Soldiers[0].Y, "Kristal yolda asker daha yavaş");
            Assert.AreEqual(35, RoadRules.SlowPct(RoadTier.Crystal));
            Assert.AreEqual(200, RoadRules.TrapBonusPct(RoadTier.Crystal));
        }

        // ---------- Asker yetenekleri ----------
        [Test]
        public void Abilities_UnlockAt2_3_8_AndApply()
        {
            Assert.AreEqual(0, SoldierAbilities.PerksFor(SoldierType.Swordsman, 1).DmgTakenPermille);
            Assert.AreEqual(200, SoldierAbilities.PerksFor(SoldierType.Swordsman, 2).DmgTakenPermille);
            Assert.AreEqual(30, SoldierAbilities.PerksFor(SoldierType.Swordsman, 3).LowHpAtkPct);
            Assert.AreEqual(0, SoldierAbilities.PerksFor(SoldierType.Swordsman, 7).SplashAdd);
            Assert.Greater(SoldierAbilities.PerksFor(SoldierType.Swordsman, 8).SplashAdd, 0);
            foreach (SoldierType t in System.Enum.GetValues(typeof(SoldierType))) Assert.AreEqual(3, SoldierAbilities.Of(t).Length, t.ToString());

            var archer1 = Run(Road(TrapType.None), SoldierType.Archer, 1, 1, 1);
            var archer5 = Run(Road(TrapType.None), SoldierType.Archer, 1, 1, 5);
            Assert.Greater(archer5.Soldiers[0].RangeMilli, archer1.Soldiers[0].RangeMilli, "Kartal Gözü: menzil");
        }

        [Test]
        public void Fairy_Sv10_HealsNearbyAllies()
        {
            var d = Road(TrapType.None);
            var sim = new BattleSim(d, new Dictionary<SoldierType, int> { { SoldierType.Giant, 1 }, { SoldierType.Fairy, 1 } },
                new Dictionary<SoldierType, int> { { SoldierType.Giant, 1 }, { SoldierType.Fairy, 10 } }, 5);
            sim.Deploy(SoldierType.Giant, 5, 1, out _);
            sim.Soldiers[0].Hp = sim.Soldiers[0].MaxHp / 2;
            sim.Deploy(SoldierType.Fairy, 5, 1, out _);
            int before = sim.Soldiers[0].Hp;
            for (int i = 0; i < 3; i++) sim.Step();
            Assert.Greater(sim.Soldiers[0].Hp, before, "Peri Şifa");
        }

        [Test]
        public void SoldierUpgradeTime_ScalesWithTrainTime()
        {
            var g = Rich(30);
            g.AddBuildingDirect(P(6, 7), BuildingType.Barracks, 30);
            var goblin = g.PreviewUpgrade(JobKind.Soldier, default, SoldierType.Goblin);
            var giant = g.PreviewUpgrade(JobKind.Soldier, default, SoldierType.Giant);
            Assert.Greater(giant.Seconds, goblin.Seconds * 4, "Dev (10 dk eğitim) Goblin'den (2 dk) çok daha uzun gelişir");
        }

        [Test]
        public void SaveLoad_KeepsTrapResearchAndRoadJob()
        {
            var g = Rich(10);
            g.AddBuildingDirect(P(2, 2), BuildingType.University, 1);
            foreach (var d in ResearchCatalog.All) if (d.Stat == ResearchStat.RoadTier && d.Permille <= 1) g.Research.ForceComplete(d.Id);
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.TrapSlot, 1).Id);
            string err;
            Assert.IsTrue(g.TryUpgradeRoad(P(5, 3), out err), err);
            g.Advance(50, 0);
            string text = GameSave.Save(g, 3);
            var h = GameSave.Load(text, ResourceConfig.CreateDefault(), out _);
            Assert.AreEqual(5, h.MaxTraps);
            Assert.IsTrue(h.IsUpgrading(JobKind.Road, P(5, 3), SoldierType.Swordsman));
            Assert.AreEqual(text, GameSave.Save(h, 3));
        }
    }
}
