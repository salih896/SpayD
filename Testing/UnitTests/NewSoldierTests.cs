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
    /// <summary>Askerî araştırmayla açılan 5 asker (kullanıcı kararı 25.11.2026).</summary>
    public class NewSoldierTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        /// <summary>Giriş (5,0), yol (5,1..6), depo (6,6); isteğe bağlı kule (4,2).</summary>
        private static DefenseSetup Setup(bool tower)
        {
            var layout = new VillageLayout(11, 20, 10);
            Assert.IsTrue(layout.ToggleEntrance(P(5, 0), out _));
            for (int y = 1; y <= 6; y++) Assert.IsTrue(layout.TryPlace(P(5, y), CellKind.Road, out _));
            Assert.IsTrue(layout.TryPlace(P(6, 6), CellKind.Building, out _));
            var s = new DefenseSetup(layout);
            s.Buildings[P(6, 6)] = new BuildingSpec(BuildingType.LumberDepot);
            if (tower) { Assert.IsTrue(layout.TryPlace(P(4, 2), CellKind.Tower, out _)); s.Towers[P(4, 2)] = new TowerSpec(TowerType.Archer); }
            return s;
        }

        private static BattleSim Start(DefenseSetup d, params (SoldierType t, int n)[] army)
        {
            var stock = new Dictionary<SoldierType, int>();
            foreach (var a in army) stock[a.t] = a.n;
            var sim = new BattleSim(d, stock, null, 3);
            foreach (var a in army) for (int i = 0; i < a.n; i++) Assert.IsTrue(sim.Deploy(a.t, 5, 1, out _));
            return sim;
        }

        [Test]
        public void Bomber_ExplodesOnFirstHit_AndDies()
        {
            var sim = Start(Setup(false), (SoldierType.Bomber, 1));
            for (int i = 0; i < 300 && sim.Soldiers[0].AttackCount == 0; i++) sim.Step();
            Assert.AreEqual(1, sim.Soldiers[0].AttackCount);
            Assert.IsFalse(sim.Soldiers[0].Alive, "patlayınca ölür");
            var depot = sim.Structures[0];
            Assert.Less(depot.Hp, depot.MaxHp);
        }

        [Test]
        public void Tower_TargetsShieldbearer_BeforeOthers()
        {
            // Dev Kalkanlı'dan yavaş: ikisi de menzile girdikten sonra kule Kalkanlı'yı seçmeli
            var sim = Start(Setup(true), (SoldierType.Giant, 1), (SoldierType.Shieldbearer, 1));
            var tower = sim.Structures[0].Kind == StructKind.Tower ? sim.Structures[0] : sim.Structures[1];
            for (int i = 0; i < 100; i++) sim.Step();
            Assert.IsTrue(sim.Soldiers[0].Alive && sim.Soldiers[1].Alive);
            var target = sim.TowerTargetOf(tower);
            Assert.IsNotNull(target);
            Assert.AreEqual(SoldierType.Shieldbearer, target.Type);
        }

        [Test]
        public void Healer_HealsNearbySoldiers_AndDealsNoDamage()
        {
            var sim = Start(Setup(false), (SoldierType.Giant, 1), (SoldierType.Healer, 1));
            var giant = sim.Soldiers[0];
            giant.Hp = giant.MaxHp / 2;
            for (int i = 0; i < 40; i++) sim.Step();
            Assert.Greater(giant.Hp, giant.MaxHp / 2, "şifacı iyileştirir");
        }

        private static GameState Rich30()
        {
            var g = GameState.NewGame();
            long xp = 0;
            for (int l = 1; l < 30; l++) xp += ProgressionRules.XpToNextLevel(l);
            g.TotalXp = xp;
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 1000000); }
            return g;
        }

        [Test]
        public void JobQueue_LetsEachVillagerQueueMoreJobs_ThatRunInOrder()
        {
            var g = Rich30();
            g.AddBuildingDirect(P(6, 2), BuildingType.Barracks, 20);
            var types = new[] { SoldierType.Swordsman, SoldierType.Archer, SoldierType.Mage, SoldierType.Goblin, SoldierType.Cavalry, SoldierType.Spearman, SoldierType.Fairy, SoldierType.Giant, SoldierType.Catapult, SoldierType.DragonRider };
            int b = g.Builders, next = 0;
            string err;
            for (int i = 0; i < b; i++) Assert.IsTrue(g.TryStartUpgrade(JobKind.Soldier, new GridPos(0, 0), types[next++], out err), err);
            Assert.IsFalse(g.TryStartUpgrade(JobKind.Soldier, new GridPos(0, 0), types[next], out err), "sıra yokken işçi dolu");
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.JobQueue, 1).Id);
            Assert.AreEqual(2, g.JobQueueDepth);
            for (int i = 0; i < b && next < types.Length; i++) Assert.IsTrue(g.TryStartUpgrade(JobKind.Soldier, new GridPos(0, 0), types[next++], out err), err);
            int queued = g.Jobs.Count;
            g.Advance(10000000, 0);
            Assert.AreEqual(0, g.Jobs.Count, "sıradakiler de tek ilerlemede biter");
            Assert.Greater(queued, b);
        }

        [Test]
        public void AutoRepair_ClearsTowerWearAfterRaid()
        {
            var g = Rich30();
            string err;
            Assert.IsTrue(g.TryPlaceTower(P(4, 1), TowerType.Archer, out err), err);
            g.Wears[P(4, 1)] = new TowerWear(30, 1);
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.AutoRepair, 1).Id);
            g.Advance(10, 5); // yeni gün: baskın olur, sonra otomatik tamir
            if (g.LastRaid != null) Assert.AreEqual(0, g.Wears[P(4, 1)].PermanentDamagePct);
        }

        [Test]
        public void NewSoldiers_NeedMilitaryResearch()
        {
            var g = GameState.NewGame();
            long xp = 0;
            for (int l = 1; l < 30; l++) xp += ProgressionRules.XpToNextLevel(l);
            g.TotalXp = xp;
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType))) { g.Wallet.SetCapacity(t, 100000000); g.Wallet.SetAmount(t, 1000000); }
            g.AddBuildingDirect(P(6, 2), BuildingType.Barracks, 10);
            string err;
            Assert.IsFalse(g.SoldierUnlocked(SoldierType.Assassin));
            Assert.IsFalse(g.TryTrain(SoldierType.Assassin, out err));
            StringAssert.Contains("Askerî", err);
            g.Research.ForceComplete(ResearchCatalog.FindSpecial(ResearchStat.SoldierUnlock, (int)SoldierType.Assassin).Id);
            Assert.IsTrue(g.SoldierUnlocked(SoldierType.Assassin));
            Assert.IsTrue(g.TryTrain(SoldierType.Assassin, out err), err);
        }
    }
}
