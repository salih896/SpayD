using NUnit.Framework;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Tests
{
    public class VillageLayoutTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

        // 10 yollu düz gövde: (5,0)=giriş ... (5,9); bina (5,10)
        private static VillageLayout ValidStraight()
        {
            var v = new VillageLayout(11);
            v.ToggleEntrance(P(5, 0), out _);
            for (int y = 1; y <= 9; y++) v.TryPlace(P(5, y), CellKind.Road, out _);
            v.TryPlace(P(5, 10), CellKind.Building, out _);
            return v;
        }

        [Test]
        public void Defaults_AreTenRoads_EntranceMaxTen()
        {
            var v = new VillageLayout();
            Assert.AreEqual(11, v.Size);
            Assert.AreEqual(10, v.RoadLimit);
            Assert.AreEqual(RoadRules.MaxEntrances, v.EntranceLimit);
            Assert.AreEqual(1, RoadRules.MinEntrances);
        }

        [Test]
        public void Place_OnOccupiedCell_Fails()
        {
            var v = new VillageLayout(11);
            Assert.IsTrue(v.TryPlace(P(3, 3), CellKind.Road, out _));
            Assert.IsFalse(v.TryPlace(P(3, 3), CellKind.Building, out var err));
            Assert.IsNotNull(err);
        }

        [Test]
        public void Tower_RequiresAdjacentRoad()
        {
            var v = new VillageLayout(11);
            Assert.IsFalse(v.TryPlace(P(5, 5), CellKind.Tower, out _));
            v.TryPlace(P(4, 5), CellKind.Road, out _);
            Assert.IsTrue(v.TryPlace(P(5, 5), CellKind.Tower, out _));
        }

        [Test]
        public void Entrance_OnlyBottomEdge_AndCreatesRoad()
        {
            var v = new VillageLayout(11);
            Assert.IsFalse(v.ToggleEntrance(P(5, 4), out _));
            Assert.IsTrue(v.ToggleEntrance(P(5, 0), out _));
            Assert.AreEqual(CellKind.Road, v.Get(P(5, 0)));
            Assert.IsTrue(v.IsEntrance(P(5, 0)));
            Assert.AreEqual(1, v.RoadCount); // giriş yol hakkından düşer
        }

        [Test]
        public void Entrance_CanHaveSeveral_UpToLimit()
        {
            var v = new VillageLayout(11, 10, 2);
            Assert.IsTrue(v.ToggleEntrance(P(3, 0), out _));
            Assert.IsTrue(v.ToggleEntrance(P(4, 0), out _));
            Assert.IsFalse(v.ToggleEntrance(P(5, 0), out var err));
            Assert.IsNotNull(err);
            Assert.IsTrue(v.ToggleEntrance(P(3, 0), out _)); // kapat
            Assert.IsTrue(v.ToggleEntrance(P(5, 0), out _)); // yeni giriş açılabilir
        }

        [Test]
        public void Entrance_LimitCanBeRaised_ToTen()
        {
            var v = new VillageLayout(15, 20, 10);
            for (int x = 0; x < 10; x++) Assert.IsTrue(v.ToggleEntrance(P(x, 0), out _));
            Assert.IsFalse(v.ToggleEntrance(P(10, 0), out _));
        }

        [Test]
        public void RoadLimit_IsEnforced()
        {
            var v = new VillageLayout(11, 2, 1);
            Assert.IsTrue(v.TryPlace(P(1, 1), CellKind.Road, out _));
            Assert.IsTrue(v.TryPlace(P(1, 2), CellKind.Road, out _));
            Assert.IsFalse(v.TryPlace(P(1, 3), CellKind.Road, out var err));
            StringAssert.Contains("Yol hakkı", err);
            Assert.IsFalse(v.ToggleEntrance(P(4, 0), out _)); // giriş de yol hakkı ister
        }

        [Test]
        public void RemovingRoad_FreesRoadRight()
        {
            var v = new VillageLayout(11, 1, 1);
            v.TryPlace(P(1, 1), CellKind.Road, out _);
            Assert.IsFalse(v.TryPlace(P(2, 2), CellKind.Road, out _));
            v.Remove(P(1, 1));
            Assert.IsTrue(v.TryPlace(P(2, 2), CellKind.Road, out _));
        }

        [Test]
        public void Validate_StraightTenRoads_IsValid()
        {
            var v = ValidStraight();
            Assert.AreEqual(10, v.RoadCount);
            Assert.IsTrue(v.Validate(out var err), err);
        }

        [Test]
        public void Validate_FewerThanTenRoads_IsInvalid()
        {
            var v = new VillageLayout(11);
            v.ToggleEntrance(P(5, 0), out _);
            for (int y = 1; y <= 3; y++) v.TryPlace(P(5, y), CellKind.Road, out _);
            v.TryPlace(P(5, 4), CellKind.Building, out _);
            Assert.IsFalse(v.Validate(out var err));
            StringAssert.Contains("yol haklarını", err);
        }

        [Test]
        public void Validate_ThreeWayBranch_IsValid()
        {
            // gövde (5,0..3) + sol (4,3),(3,3) + sağ (6,3),(7,3) + yukarı (5,4),(5,5) = 10
            var v = new VillageLayout(11);
            v.ToggleEntrance(P(5, 0), out _);
            foreach (var (x, y) in new[] { (5, 1), (5, 2), (5, 3), (4, 3), (3, 3), (6, 3), (7, 3), (5, 4), (5, 5) })
                Assert.IsTrue(v.TryPlace(P(x, y), CellKind.Road, out _));
            v.TryPlace(P(5, 6), CellKind.Building, out _);
            v.TryPlace(P(4, 4), CellKind.Tower, out _);
            Assert.AreEqual(10, v.RoadCount);
            Assert.IsTrue(v.Validate(out var err), err);
        }

        [Test]
        public void Validate_DisconnectedRoadPiece_IsInvalid()
        {
            var v = new VillageLayout(11, 4, 1);
            v.ToggleEntrance(P(5, 0), out _);
            v.TryPlace(P(5, 1), CellKind.Road, out _);
            v.TryPlace(P(8, 8), CellKind.Road, out _);   // girişe bağlı değil
            v.TryPlace(P(8, 9), CellKind.Road, out _);
            v.TryPlace(P(5, 2), CellKind.Building, out _);
            Assert.IsFalse(v.Validate(out var err));
            StringAssert.Contains("bağlı", err);
        }

        [Test]
        public void Validate_BrokenRoad_IsInvalid()
        {
            var v = new VillageLayout(11, 3, 1);
            v.ToggleEntrance(P(5, 0), out _);
            v.TryPlace(P(5, 1), CellKind.Road, out _);
            v.TryPlace(P(5, 3), CellKind.Road, out _); // (5,2) eksik
            v.TryPlace(P(5, 4), CellKind.Building, out _);
            Assert.IsFalse(v.Validate(out _));
        }

        [Test]
        public void Validate_NoEntrance_IsInvalid()
        {
            var v = new VillageLayout(11);
            v.TryPlace(P(5, 5), CellKind.Building, out _);
            Assert.IsFalse(v.Validate(out _));
        }

        [Test]
        public void Resize_KeepsInBoundsCells_AndLimits()
        {
            var v = new VillageLayout(11);
            v.TryPlace(P(2, 2), CellKind.Building, out _);
            v.TryPlace(P(9, 9), CellKind.Building, out _);
            var r = v.Resized(7);
            Assert.AreEqual(7, r.Size);
            Assert.AreEqual(10, r.RoadLimit);
            Assert.AreEqual(CellKind.Building, r.Get(P(2, 2)));
            Assert.AreEqual(CellKind.Empty, r.Get(P(9, 9)));
        }

        [Test]
        public void RoadTier_DefaultsToWood_CanBeSet_AndSurvivesResize()
        {
            var v = new VillageLayout(11);
            v.TryPlace(P(2, 2), CellKind.Road, out _);
            Assert.AreEqual(RoadTier.Wood, v.GetRoadTier(P(2, 2)));
            Assert.IsTrue(v.SetRoadTier(P(2, 2), RoadTier.Iron));
            Assert.AreEqual(RoadTier.Iron, v.Resized(13).GetRoadTier(P(2, 2)));
            Assert.IsFalse(v.SetRoadTier(P(3, 3), RoadTier.Iron)); // yol değil
        }

        [Test]
        public void Entrance_ToggleOnExistingRoad_KeepsTier()
        {
            var v = new VillageLayout(11);
            v.TryPlace(P(4, 0), CellKind.Road, out _, RoadTier.Reinforced);
            v.ToggleEntrance(P(4, 0), out _);
            Assert.AreEqual(RoadTier.Reinforced, v.GetRoadTier(P(4, 0)));
        }

        [Test]
        public void RoadLimit_PerLevel_IsTenPlusLevelMinusOne()
        {
            Assert.AreEqual(10, RoadRules.RoadLimitForLevel(1));
            Assert.AreEqual(11, RoadRules.RoadLimitForLevel(2));
            Assert.AreEqual(12, RoadRules.RoadLimitForLevel(3));
            Assert.AreEqual(39, RoadRules.RoadLimitForLevel(30)); // GDD: 10 + 29
        }

        [Test]
        public void LevelUp_RaisesRoadLimit_AndLayoutBecomesInvalidUntilNewRoadAdded()
        {
            var v = ValidStraight();                       // Sv1: 10/10 geçerli
            Assert.IsTrue(v.Validate(out _));
            var up = v.WithLimits(RoadRules.RoadLimitForLevel(2), v.EntranceLimit); // Sv2: 11
            Assert.IsFalse(up.Validate(out var err));      // 10/11 -> eksik
            StringAssert.Contains("yol haklarını", err);
            Assert.IsTrue(up.TryPlace(P(4, 9), CellKind.Road, out _)); // çıkmaz kol
            Assert.IsTrue(up.Validate(out err), err);
        }

        [Test]
        public void TooManyRoads_AfterLimitDrops_IsInvalid()
        {
            var v = ValidStraight().WithLimits(9, 10);
            Assert.IsFalse(v.Validate(out var err));
            StringAssert.Contains("Fazla yol", err);
        }

        [Test]
        public void DeadEndRoads_AreAllowed()
        {
            var v = new VillageLayout(11, 6, 10);
            v.ToggleEntrance(P(5, 0), out _);
            foreach (var (x, y) in new[] { (5, 1), (5, 2), (4, 2), (3, 2), (6, 2) }) // sol/sağ çıkmaz kollar
                v.TryPlace(P(x, y), CellKind.Road, out _);
            v.TryPlace(P(5, 3), CellKind.Building, out _);
            Assert.IsTrue(v.Validate(out var err), err);
        }

        [Test]
        public void TwoEntrances_BothConnected_IsValid()
        {
            var v = new VillageLayout(11, 9, 10);
            v.ToggleEntrance(P(3, 0), out _);
            v.ToggleEntrance(P(7, 0), out _);
            foreach (var (x, y) in new[] { (3, 1), (3, 2), (4, 2), (5, 2), (6, 2), (7, 2), (7, 1) })
                v.TryPlace(P(x, y), CellKind.Road, out _);
            v.TryPlace(P(5, 3), CellKind.Building, out _);
            Assert.IsTrue(v.Validate(out var err), err);
        }

        [Test]
        public void SecondEntrance_NotReachingBuilding_IsInvalid()
        {
            var v = new VillageLayout(11, 4, 10);
            v.ToggleEntrance(P(5, 0), out _);
            v.TryPlace(P(5, 1), CellKind.Road, out _);
            v.ToggleEntrance(P(9, 0), out _);              // yalnız duran ikinci giriş
            v.TryPlace(P(9, 1), CellKind.Road, out _);
            v.TryPlace(P(5, 2), CellKind.Building, out _);
            Assert.IsFalse(v.Validate(out _));
        }

        [Test]
        public void UpgradeRoad_StepsThroughTiers_PerTile()
        {
            var v = new VillageLayout(11);
            v.TryPlace(P(2, 2), CellKind.Road, out _);
            v.TryPlace(P(2, 3), CellKind.Road, out _);
            Assert.IsTrue(v.TryUpgradeRoad(P(2, 2), out _));
            Assert.AreEqual(RoadTier.Reinforced, v.GetRoadTier(P(2, 2)));
            Assert.AreEqual(RoadTier.Wood, v.GetRoadTier(P(2, 3)));   // diğer yol etkilenmez
            Assert.IsTrue(v.TryUpgradeRoad(P(2, 2), out _));
            Assert.AreEqual(RoadTier.Iron, v.GetRoadTier(P(2, 2)));
            Assert.IsFalse(v.TryUpgradeRoad(P(2, 2), out _));         // en üstte
            Assert.IsFalse(v.TryUpgradeRoad(P(8, 8), out _));         // yol değil
        }

        [Test]
        public void UpgradeGate_RequiresResearch_AtAnyLevel()
        {
            Assert.IsFalse(RoadRules.CanUpgrade(false, out var r));
            StringAssert.Contains("araştırma", r);
            Assert.IsTrue(RoadRules.CanUpgrade(true, out _));
        }
    }
}
