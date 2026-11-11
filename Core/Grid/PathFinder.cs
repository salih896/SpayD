using System.Collections.Generic;

namespace GameCore.Grid
{
    /// <summary>
    /// Deterministik 4 yönlü BFS. Komşu sırası sabittir: Yukarı(0,-1), Sağ(1,0), Aşağı(0,1), Sol(-1,0).
    /// Aynı girdi her zaman aynı yolu üretir (istemci = sunucu).
    /// </summary>
    public static class PathFinder
    {
        // Sabit komşu sırası: değiştirme, determinizm bozulur.
        private static readonly int[] Dx = { 0, 1, 0, -1 };
        private static readonly int[] Dy = { -1, 0, 1, 0 };

        /// <summary>start'tan hedeflerden birine en kısa yol. Yoksa null. Eşit uzunlukta ilk bulunan (sabit sıra) seçilir.</summary>
        public static List<GridPos> FindPath(int size, HashSet<GridPos> walkable, GridPos start, HashSet<GridPos> goals)
        {
            if (!walkable.Contains(start)) return null;
            var prev = new Dictionary<GridPos, GridPos>();
            var seen = new HashSet<GridPos> { start };
            var queue = new Queue<GridPos>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (goals.Contains(cur)) return Build(prev, start, cur);

                for (int i = 0; i < 4; i++)
                {
                    var n = new GridPos(cur.X + Dx[i], cur.Y + Dy[i]);
                    if (n.X < 0 || n.Y < 0 || n.X >= size || n.Y >= size) continue;
                    if (!walkable.Contains(n) && !goals.Contains(n)) continue;
                    if (!seen.Add(n)) continue;
                    prev[n] = cur;
                    queue.Enqueue(n);
                }
            }
            return null;
        }

        /// <summary>Her aktif girişten en az bir hedefe yol var mı? (GDD Bölüm 5 kayıt kuralı)</summary>
        public static bool AllEntrancesConnected(int size, HashSet<GridPos> walkable, IEnumerable<GridPos> entrances, HashSet<GridPos> goals)
        {
            foreach (var e in entrances)
                if (FindPath(size, walkable, e, goals) == null) return false;
            return true;
        }

        private static List<GridPos> Build(Dictionary<GridPos, GridPos> prev, GridPos start, GridPos end)
        {
            var path = new List<GridPos> { end };
            var c = end;
            while (!c.Equals(start)) { c = prev[c]; path.Add(c); }
            path.Reverse();
            return path;
        }
    }
}
