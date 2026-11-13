using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Economy;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Tests
{
    public class BattleSimTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);
        private static readonly ResourceConfig Cfg = ResourceConfig.CreateDefault();

        /// <summary>Giriş (5,0), yol (5,1..4), depo (6,3). İsteğe bağlı kule (4,2).</summary>
        private static DefenseSetup Basic(BuildingType depot = BuildingType.LumberDepot, TowerSpec tower = null)
        {
            var layout = new VillageLayout(11, 20, 10);
            Assert.IsTrue(layout.ToggleEntrance(P(5, 0), out _));
            for (int y = 1; y <= 4; y++) Assert.IsTrue(layout.TryPlace(P(5, y), CellKind.Road, out _));
            Assert.IsTrue(layout.TryPlace(P(6, 3), CellKind.Building, out _));
            var setup = new DefenseSetup(layout);
            setup.Buildings[P(6, 3)] = new BuildingSpec(depot);
            if (tower != null)
            {
                Assert.IsTrue(layout.TryPlace(P(4, 2), CellKind.Tower, out _));
                setup.Towers[P(4, 2)] = tower;
            }
            return setup;
        }

        private static BattleSim Run(DefenseSetup setup, SoldierType type, int count, int maxTicks = 1800)
        {
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int> { { type, count } }, null, 7);
            for (int i = 0; i < count; i++) Assert.IsTrue(sim.Deploy(type, 5, 1, out _));
            for (int t = 0; t < maxTicks && !sim.Finished; t++) sim.Step();
            return sim;
        }

        [Test]
        public void Simulation_IsDeterministic()
        {
            var a = Run(Basic(tower: new TowerSpec(TowerType.Archer)), SoldierType.Swordsman, 6);
            var b = Run(Basic(tower: new TowerSpec(TowerType.Archer)), SoldierType.Swordsman, 6);
            var ra = a.Result(new ResourceCost(0), Cfg, 5); var rb = b.Result(new ResourceCost(0), Cfg, 5);
            Assert.AreEqual(a.Tick, b.Tick);
            Assert.AreEqual(ra.OverallDamagePct, rb.OverallDamagePct);
            Assert.AreEqual(BattleResult.Total(ra.LostDead), BattleResult.Total(rb.LostDead));
            for (int i = 0; i < a.Soldiers.Count; i++)
            {
                Assert.AreEqual(a.Soldiers[i].Hp, b.Soldiers[i].Hp);
                Assert.AreEqual(a.Soldiers[i].X, b.Soldiers[i].X);
                Assert.AreEqual(a.Soldiers[i].Y, b.Soldiers[i].Y);
            }
        }

        [Test]
        public void Goblins_DestroyUndefendedDepot_ThreeStars()
        {
            var sim = Run(Basic(), SoldierType.Goblin, 10);
            Assert.IsTrue(sim.Finished);
            var r = sim.Result(new ResourceCost(0), Cfg, 5);
            Assert.AreEqual(100, r.OverallDamagePct);
            Assert.IsTrue(r.Victory);
            Assert.AreEqual(3, r.Stars);
            Assert.AreEqual(10, r.Returned[SoldierType.Goblin]);          // kule yok: hepsi sağlam döner
        }

        [Test]
        public void ArcherTower_KillsLoneGoblin_BeforeItReachesDepot()
        {
            var sim = Run(Basic(tower: new TowerSpec(TowerType.Archer)), SoldierType.Goblin, 1);
            var r = sim.Result(new ResourceCost(0), Cfg, 5);
            Assert.AreEqual(1, r.LostDead[SoldierType.Goblin]);
            Assert.IsFalse(r.Victory);
            Assert.AreEqual(0, r.OverallDamagePct);
        }

        [Test]
        public void SpearTower_CannotShootFairy_ButElfTowerCan()
        {
            var spear = Basic(tower: new TowerSpec(TowerType.Spear));
            var sim = new BattleSim(spear, new Dictionary<SoldierType, int> { { SoldierType.Fairy, 1 } }, null, 1);
            sim.Deploy(SoldierType.Fairy, 5, 1, out _);
            for (int i = 0; i < 150; i++) sim.Step();
            Assert.IsTrue(sim.Soldiers[0].Alive);
            Assert.Less(sim.Structures[1].Hp, sim.Structures[1].MaxHp);   // fairy depoya vuruyor

            var elf = Basic(tower: new TowerSpec(TowerType.Elf));
            var sim2 = new BattleSim(elf, new Dictionary<SoldierType, int> { { SoldierType.Fairy, 1 } }, null, 1);
            sim2.Deploy(SoldierType.Fairy, 5, 1, out _);
            for (int i = 0; i < 150 && !sim2.Finished; i++) sim2.Step();
            Assert.IsFalse(sim2.Soldiers[0].Alive);
        }

        [Test]
        public void Targets_FollowPriority_GoblinResource_CavalryTower()
        {
            var setup = Basic(tower: new TowerSpec(TowerType.Archer, 1, 70));   // %70 aşınmış: devre dışı, ateş etmez
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int> { { SoldierType.Goblin, 1 }, { SoldierType.Cavalry, 1 } }, null, 1);
            sim.Deploy(SoldierType.Goblin, 5, 1, out _);
            sim.Deploy(SoldierType.Cavalry, 5, 1, out _);
            sim.Step();
            Assert.AreEqual(1, sim.Soldiers[0].TargetId);   // kule id 0, depo id 1: goblin kaynak
            Assert.AreEqual(0, sim.Soldiers[1].TargetId);   // atlı kule
        }

        [Test]
        public void DisabledTower_NeverFires_AndStartsWithReducedHp()
        {
            var setup = Basic(tower: new TowerSpec(TowerType.Archer, 1, 70));
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int> { { SoldierType.Goblin, 1 } }, null, 1);
            Assert.IsTrue(sim.Structures[0].Disabled);
            Assert.AreEqual(sim.Structures[0].MaxHp * 30 / 100, sim.Structures[0].Hp);
            sim.Deploy(SoldierType.Goblin, 5, 1, out _);
            for (int i = 0; i < 100; i++) sim.Step();
            Assert.IsTrue(sim.Soldiers[0].Alive);
            Assert.AreEqual(sim.Soldiers[0].MaxHp, sim.Soldiers[0].Hp);
        }

        [Test]
        public void Deploy_Validates_Zone_And_Stock()
        {
            var sim = new BattleSim(Basic(), new Dictionary<SoldierType, int> { { SoldierType.Goblin, 1 } }, null, 1);
            Assert.IsFalse(sim.Deploy(SoldierType.Goblin, 5, 0, out _));     // alan dışı (köy içi)
            Assert.IsFalse(sim.Deploy(SoldierType.Goblin, 5, 5, out _));
            Assert.IsFalse(sim.Deploy(SoldierType.Goblin, 11, 1, out _));
            Assert.IsFalse(sim.Deploy(SoldierType.Archer, 5, 1, out _));     // stok yok
            Assert.IsTrue(sim.Deploy(SoldierType.Goblin, 5, 4, out _));
            Assert.IsFalse(sim.Deploy(SoldierType.Goblin, 5, 4, out _));     // stok bitti
            Assert.AreEqual(0, sim.StockOf(SoldierType.Goblin));
        }

        [Test]
        public void Spikes_HurtGroundSoldier_ByArmorReducedAmount()
        {
            var setup = Basic();
            setup.Traps[P(5, 0)] = TrapType.Spikes;
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 1 } }, null, 1);
            sim.Deploy(SoldierType.Swordsman, 5, 1, out _);
            for (int i = 0; i < 60; i++) sim.Step();
            Assert.AreEqual(120 - 18, sim.Soldiers[0].Hp);                    // 20 hasar, zırh %10
        }

        [Test]
        public void FireTrap_BurnsGroundSoldier()
        {
            var setup = Basic();
            setup.Traps[P(5, 0)] = TrapType.Fire;
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 1 } }, null, 1);
            sim.Deploy(SoldierType.Swordsman, 5, 1, out _);
            for (int i = 0; i < 80; i++) sim.Step();
            Assert.Less(sim.Soldiers[0].Hp, sim.Soldiers[0].MaxHp);
            Assert.Greater(sim.Soldiers[0].Hp, 0);
        }

        [Test]
        public void Retreat_SacrificesQuarterOfHealthy_Deterministically()
        {
            var stock = new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 25 } };
            var sim = new BattleSim(Basic(), stock, null, 99);
            for (int i = 0; i < 20; i++) sim.Deploy(SoldierType.Swordsman, 5, 1, out _);
            sim.Retreat();
            var r = sim.Result(new ResourceCost(0), Cfg, 5);
            Assert.IsTrue(r.Retreated);
            Assert.AreEqual(5, BattleResult.Total(r.LostSacrificed));                    // 20 * %25
            Assert.AreEqual(5 + 15, r.Returned[SoldierType.Swordsman]);                  // 5 stok + 15 sağlam
        }

        [Test]
        public void Loot_GddExample_BankHalfDestroyed_Gives350Gold()
        {
            var layout = new VillageLayout(11, 20, 10);
            layout.ToggleEntrance(P(5, 0), out _);
            layout.TryPlace(P(5, 1), CellKind.Road, out _);
            layout.TryPlace(P(6, 3), CellKind.Building, out _);
            layout.TryPlace(P(4, 3), CellKind.Building, out _);
            var setup = new DefenseSetup(layout);
            setup.Buildings[P(4, 3)] = new BuildingSpec(BuildingType.LumberDepot);
            setup.Buildings[P(6, 3)] = new BuildingSpec(BuildingType.Bank);
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int>(), null, 1);
            var lumber = sim.Structures[0]; var bank = sim.Structures[1];
            lumber.Hp = 0; lumber.Alive = false;               // %100
            bank.Hp = bank.MaxHp / 2;                          // %50
            var r = sim.Result(new ResourceCost(0, 0, 1000, 0), Cfg, 15);
            Assert.AreEqual(75, r.OverallDamagePct);
            Assert.IsTrue(r.Victory);
            Assert.AreEqual(350, r.Loot.Gold);                 // 1000 * %70 * %50
            var locked = sim.Result(new ResourceCost(0, 0, 1000, 0), Cfg, 10);
            Assert.AreEqual(0, locked.Loot.Gold);              // altın Sv15'te açık: yağma yok
        }

        [Test]
        public void Loss_GivesNoLoot_ButSomeXp()
        {
            var sim = new BattleSim(Basic(BuildingType.Bank), new Dictionary<SoldierType, int>(), null, 1);
            sim.Structures[0].Hp = sim.Structures[0].MaxHp * 3 / 4;   // %25 hasar
            var r = sim.Result(new ResourceCost(0, 0, 1000, 0), Cfg, 15);
            Assert.IsFalse(r.Victory);
            Assert.AreEqual(0, r.Loot.Gold);
            Assert.Greater(r.Xp, 0);
        }

        [Test]
        public void TowerDamageRecords_FeedTowerWear()
        {
            var setup = Basic(tower: new TowerSpec(TowerType.Archer));
            var sim = new BattleSim(setup, new Dictionary<SoldierType, int>(), null, 1);
            sim.Structures[0].Hp = sim.Structures[0].MaxHp / 2;
            var r = sim.Result(new ResourceCost(0), Cfg, 5);
            Assert.AreEqual(1, r.TowerDamages.Count);
            Assert.AreEqual(50, r.TowerDamages[0].DamagePct);
            var wear = new TowerWear();
            Assert.AreEqual(30, wear.ApplyBattle(r.TowerDamages[0].DamagePct));   // 1. savaş sınırı %30
        }

        [Test]
        public void SoldierLevel_ScalesStats()
        {
            var levels = new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 3 } };
            var sim = new BattleSim(Basic(), new Dictionary<SoldierType, int> { { SoldierType.Swordsman, 1 } }, levels, 1);
            sim.Deploy(SoldierType.Swordsman, 5, 1, out _);
            Assert.AreEqual(ContentCatalog.Scaled(120, 3), sim.Soldiers[0].MaxHp);
        }
    }
}
