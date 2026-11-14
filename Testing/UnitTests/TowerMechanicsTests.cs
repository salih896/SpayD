using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Combat;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Tests
{
    /// <summary>Duvar (üstüne çekme), Buz (yavaşlatma), Şimşek (zincir), Top (alan, hava yok) — 14.11.2026.</summary>
    public class TowerMechanicsTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        /// <summary>Giriş (5,0), yol (5,1..6), depo (6,6) yolun sonunda; kule/duvar (4,2).</summary>
        private static DefenseSetup Setup(TowerType tower)
        {
            var layout = new VillageLayout(11, 20, 10);
            Assert.IsTrue(layout.ToggleEntrance(P(5, 0), out _));
            for (int y = 1; y <= 6; y++) Assert.IsTrue(layout.TryPlace(P(5, y), CellKind.Road, out _));
            Assert.IsTrue(layout.TryPlace(P(6, 6), CellKind.Building, out _));
            Assert.IsTrue(layout.TryPlace(P(4, 2), CellKind.Tower, out _));
            var s = new DefenseSetup(layout);
            s.Buildings[P(6, 6)] = new BuildingSpec(BuildingType.LumberDepot);
            s.Towers[P(4, 2)] = new TowerSpec(tower);
            return s;
        }

        private static BattleSim Start(DefenseSetup d, SoldierType t, int n)
        {
            var sim = new BattleSim(d, new Dictionary<SoldierType, int> { { t, n } }, null, 3);
            for (int i = 0; i < n; i++) Assert.IsTrue(sim.Deploy(t, 5, 1, out _));
            return sim;
        }

        [Test]
        public void Wall_TauntsGroundSoldiers_AndNeverShoots()
        {
            var sim = Start(Setup(TowerType.Wall), SoldierType.Swordsman, 3);
            for (int i = 0; i < 60; i++) sim.Step();
            int wallId = sim.Structures[0].Kind == StructKind.Tower ? sim.Structures[0].Id : sim.Structures[1].Id;
            foreach (var s in sim.Soldiers) Assert.AreEqual(wallId, s.TargetId, "Kılıçlılar depo yerine yakındaki duvarı seçmeli");
            foreach (var s in sim.Soldiers) Assert.AreEqual(s.MaxHp, s.Hp, "Duvar hasar vermez");
        }

        [Test]
        public void Wall_IsIgnoredByFlyers()
        {
            var sim = Start(Setup(TowerType.Wall), SoldierType.Fairy, 1);
            for (int i = 0; i < 40; i++) sim.Step();
            var depot = sim.Structures[0].Kind == StructKind.Building ? sim.Structures[0] : sim.Structures[1];
            Assert.AreEqual(depot.Id, sim.Soldiers[0].TargetId, "Peri kaynak binasına gider, duvara değil");
        }

        [Test]
        public void IceTower_SlowsTarget()
        {
            var sim = Start(Setup(TowerType.Ice), SoldierType.Giant, 1);
            bool slowed = false;
            for (int i = 0; i < 200 && !slowed; i++) { sim.Step(); slowed = sim.Soldiers[0].SlowUntil > sim.Tick; }
            Assert.IsTrue(slowed, "Buz kulesi vurduğu askeri yavaşlatmalı");
        }

        [Test]
        public void LightningTower_ChainsToNearbySoldiers()
        {
            var sim = Start(Setup(TowerType.Lightning), SoldierType.Giant, 4);
            for (int i = 0; i < 200; i++) sim.Step();
            int damaged = 0;
            foreach (var s in sim.Soldiers) if (s.Hp < s.MaxHp) damaged++;
            Assert.GreaterOrEqual(damaged, 3, "Şimşek hedef + 2 askere sıçramalı");
        }

        [Test]
        public void CannonTower_CannotHitFlyers()
        {
            var sim = Start(Setup(TowerType.Cannon), SoldierType.Fairy, 1);
            for (int i = 0; i < 200; i++) sim.Step();
            Assert.AreEqual(sim.Soldiers[0].MaxHp, sim.Soldiers[0].Hp);
        }

        [Test]
        public void Catalog_HasSevenTowerTypes()
        {
            Assert.AreEqual(7, BattleSim.TowerTypeCount);
            for (int i = 0; i < 7; i++) Assert.AreEqual((TowerType)i, ContentCatalog.Tower((TowerType)i).Type, "Katalog sırası enum ile aynı olmalı");
        }
    }
}
