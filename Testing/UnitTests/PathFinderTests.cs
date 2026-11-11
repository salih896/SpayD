using System.Collections.Generic;
using NUnit.Framework;
using GameCore.Grid;

namespace GameCore.Tests
{
    public class PathFinderTests
    {
        private static HashSet<GridPos> Road(params (int, int)[] cells)
        {
            var s = new HashSet<GridPos>();
            foreach (var (x, y) in cells) s.Add(new GridPos(x, y));
            return s;
        }

        [Test]
        public void StraightRoad_FindsPath()
        {
            var road = Road((5, 10), (5, 9), (5, 8));
            var goals = new HashSet<GridPos> { new GridPos(5, 7) }; // kaynak binası
            var p = PathFinder.FindPath(11, road, new GridPos(5, 10), goals);
            Assert.NotNull(p);
            Assert.AreEqual(4, p.Count);
            Assert.AreEqual(new GridPos(5, 7), p[p.Count - 1]);
        }

        [Test]
        public void BrokenRoad_ReturnsNull()
        {
            var road = Road((5, 10), (5, 9), (5, 7)); // (5,8) eksik
            var goals = new HashSet<GridPos> { new GridPos(5, 6) };
            Assert.IsNull(PathFinder.FindPath(11, road, new GridPos(5, 10), goals));
        }

        [Test]
        public void EqualLengthRoutes_AreDeterministic()
        {
            // İki eşit uzunlukta yol: sol ve sağ halka.
            var road = Road((5, 10), (5, 9), (4, 9), (4, 8), (6, 9), (6, 8), (5, 8));
            var goals = new HashSet<GridPos> { new GridPos(5, 7) };
            var a = PathFinder.FindPath(11, road, new GridPos(5, 10), goals);
            for (int i = 0; i < 50; i++)
            {
                var b = PathFinder.FindPath(11, road, new GridPos(5, 10), goals);
                CollectionAssert.AreEqual(a, b);
            }
        }

        [Test]
        public void AllEntrancesConnected_FailsIfOneIsolated()
        {
            var road = Road((5, 10), (5, 9), (5, 8), (2, 10));
            var goals = new HashSet<GridPos> { new GridPos(5, 7) };
            var ents = new[] { new GridPos(5, 10), new GridPos(2, 10) };
            Assert.IsFalse(PathFinder.AllEntrancesConnected(11, road, ents, goals));
        }
    }
}
