using NUnit.Framework;
using GameCore.Grid;
using GameCore.Village;

namespace GameCore.Tests
{
    public class VillageLayoutTests
    {
        private static GridPos P(int x, int y) => new GridPos(x, y);

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
        }

        [Test]
        public void Validate_ConnectedLayout_IsValid()
        {
            var v = new VillageLayout(11);
            v.ToggleEntrance(P(5, 0), out _);
            for (int y = 1; y <= 4; y++) v.TryPlace(P(5, y), CellKind.Road, out _);
            v.TryPlace(P(5, 5), CellKind.Building, out _);
            v.TryPlace(P(6, 3), CellKind.Tower, out _);
            Assert.IsTrue(v.Validate(out var err), err);
        }

        [Test]
        public void Validate_BrokenRoad_IsInvalid()
        {
            var v = new VillageLayout(11);
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
        public void Resize_KeepsInBoundsCells_DropsOthers()
        {
            var v = new VillageLayout(11);
            v.TryPlace(P(2, 2), CellKind.Building, out _);
            v.TryPlace(P(9, 9), CellKind.Building, out _);
            var r = v.Resized(7);
            Assert.AreEqual(7, r.Size);
            Assert.AreEqual(CellKind.Building, r.Get(P(2, 2)));
            Assert.AreEqual(CellKind.Empty, r.Get(P(9, 9)));
        }

        [Test]
        public void Entrance_Limit_IsTen()
        {
            var v = new VillageLayout(15);
            for (int x = 0; x < 10; x++) Assert.IsTrue(v.ToggleEntrance(P(x, 0), out _));
            Assert.IsFalse(v.ToggleEntrance(P(10, 0), out _));
        }
    }
}
