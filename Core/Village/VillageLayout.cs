using System.Collections.Generic;
using System.Linq;
using GameCore.Grid;

namespace GameCore.Village
{
    public enum CellKind { Empty = 0, Road = 1, Building = 2, Tower = 3 }

    /// <summary>Yol malzemesi (GDD Bölüm 5): tahta, altın+tahta, demir. Maliyet/araştırma kuralları henüz yok.</summary>
    public enum RoadTier { Wood = 0, Reinforced = 1, Iron = 2 }

    /// <summary>
    /// Savunma köyü düzeni (saf C#). Grid boyutu ve limitler parametriktir.
    /// Giriş kenarı: y == 0 (alt kenar); saldırgan gönderim alanı bu kenarın DIŞINDADIR (y = -1 .. -AttackZoneRows).
    /// Onaylı kurallar (docs/approved-mechanics.md #16-#21): yol sayısı TAM yol hakkı kadar (seviyeye bağlı),
    /// en az 1 giriş, tüm yollar girişe bağlı, dallanma ve çıkmaz serbest.
    /// </summary>
    public sealed class VillageLayout
    {
        public const int DefaultSize = 11;
        public const int DefaultRoadLimit = 10;
        public const int DefaultEntranceLimit = RoadRules.MaxEntrances; // en az 1 zorunlu, üst sınır 10
        public const int AttackZoneRows = 4; // GDD Bölüm 4: saldırı alanı 4x11

        public int Size { get; }
        public int RoadLimit { get; }
        public int EntranceLimit { get; }

        private readonly Dictionary<GridPos, CellKind> cells = new Dictionary<GridPos, CellKind>();
        private readonly HashSet<GridPos> entrances = new HashSet<GridPos>();
        private readonly Dictionary<GridPos, RoadTier> roadTiers = new Dictionary<GridPos, RoadTier>();

        private static readonly int[] Dx = { 0, 1, 0, -1 };
        private static readonly int[] Dy = { -1, 0, 1, 0 };

        public VillageLayout(int size = DefaultSize, int roadLimit = DefaultRoadLimit, int entranceLimit = DefaultEntranceLimit)
        {
            Size = size;
            RoadLimit = roadLimit;
            EntranceLimit = entranceLimit;
        }

        public bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < Size && p.Y < Size;
        public CellKind Get(GridPos p) => cells.TryGetValue(p, out var k) ? k : CellKind.Empty;
        public bool IsEntrance(GridPos p) => entrances.Contains(p);
        public int EntranceCount => entrances.Count;
        public int RoadCount => cells.Count(c => c.Value == CellKind.Road);

        /// <summary>Belirli türdeki hücreler, sıralı (Y sonra X): deterministik dolaşım için.</summary>
        public List<GridPos> CellsOf(CellKind kind)
        {
            var l = new List<GridPos>();
            foreach (var kv in cells) if (kv.Value == kind) l.Add(kv.Key);
            l.Sort(CompareYX);
            return l;
        }

        /// <summary>Giriş hücreleri, sıralı (Y sonra X).</summary>
        public List<GridPos> EntrancePositions()
        {
            var l = new List<GridPos>(entrances);
            l.Sort(CompareYX);
            return l;
        }

        private static int CompareYX(GridPos a, GridPos b)
        {
            int c = a.Y.CompareTo(b.Y);
            return c != 0 ? c : a.X.CompareTo(b.X);
        }

        public RoadTier GetRoadTier(GridPos p) => roadTiers.TryGetValue(p, out var t) ? t : RoadTier.Wood;

        public bool SetRoadTier(GridPos p, RoadTier tier)
        {
            if (Get(p) != CellKind.Road) return false;
            roadTiers[p] = tier;
            return true;
        }

        public bool TryPlace(GridPos p, CellKind kind, out string error, RoadTier tier = RoadTier.Wood)
        {
            error = null;
            if (!InBounds(p)) { error = "Grid dışı"; return false; }
            if (kind == CellKind.Empty) { error = "Geçersiz tür"; return false; }
            if (Get(p) != CellKind.Empty) { error = "Hücre dolu"; return false; }
            if (kind == CellKind.Road && RoadCount >= RoadLimit) { error = "Yol hakkı doldu (" + RoadLimit + "/" + RoadLimit + ")"; return false; }
            if (kind == CellKind.Tower && !HasAdjacentRoad(p)) { error = "Kule yola komşu olmalı"; return false; }
            cells[p] = kind;
            if (kind == CellKind.Road) roadTiers[p] = tier;
            return true;
        }

        public bool Remove(GridPos p)
        {
            if (!cells.Remove(p)) return false;
            entrances.Remove(p);
            roadTiers.Remove(p);
            return true;
        }

        /// <summary>Alt kenarda giriş aç/kapat. Boş hücreye basılırsa yol + giriş birlikte konur (yol hakkından düşer).</summary>
        public bool ToggleEntrance(GridPos p, out string error)
        {
            error = null;
            if (!InBounds(p) || p.Y != 0) { error = "Giriş yalnızca alt kenarda olabilir"; return false; }
            var kind = Get(p);
            if (kind != CellKind.Empty && kind != CellKind.Road) { error = "Hücre dolu"; return false; }
            if (entrances.Contains(p)) { entrances.Remove(p); return true; }
            if (entrances.Count >= EntranceLimit) { error = "Giriş sınırı " + EntranceLimit + ": önce mevcut girişi kaldır"; return false; }
            if (kind == CellKind.Empty)
            {
                if (RoadCount >= RoadLimit) { error = "Yol hakkı doldu (" + RoadLimit + "/" + RoadLimit + ")"; return false; }
                cells[p] = CellKind.Road;
            }
            entrances.Add(p);
            return true;
        }

        /// <summary>Yolu bir malzeme basamağı yükseltir (tahta → altın+tahta → demir). Kapı kontrolü (seviye/araştırma) RoadRules.CanUpgrade'dedir.</summary>
        public bool TryUpgradeRoad(GridPos p, out string error)
        {
            error = null;
            if (Get(p) != CellKind.Road) { error = "Geliştirmek için bir yola dokun"; return false; }
            var t = GetRoadTier(p);
            if (t == RoadTier.Iron) { error = "Bu yol zaten en üst seviyede"; return false; }
            roadTiers[p] = t + 1;
            return true;
        }

        /// <summary>Aynı içerikle yeni limitler (ör. oyuncu seviye atladığında yol hakkı artar).</summary>
        public VillageLayout WithLimits(int roadLimit, int entranceLimit)
        {
            var n = new VillageLayout(Size, roadLimit, entranceLimit);
            foreach (var kv in cells) n.cells[kv.Key] = kv.Value;
            foreach (var e in entrances) n.entrances.Add(e);
            foreach (var kv in roadTiers) n.roadTiers[kv.Key] = kv.Value;
            return n;
        }

        /// <summary>Yeni boyutta kopya; sınır içindeki hücreler korunur.</summary>
        public VillageLayout Resized(int newSize)
        {
            var n = new VillageLayout(newSize, RoadLimit, EntranceLimit);
            foreach (var kv in cells) if (n.InBounds(kv.Key)) n.cells[kv.Key] = kv.Value;
            foreach (var e in entrances) if (n.InBounds(e)) n.entrances.Add(e);
            foreach (var kv in roadTiers) if (n.InBounds(kv.Key)) n.roadTiers[kv.Key] = kv.Value;
            return n;
        }

        /// <summary>Savunma düzeni kaydedilebilir mi? (GDD Bölüm 5 + onaylı yol kuralları)</summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entrances.Count == 0) { error = "En az 1 giriş karesi koymalısın (alt kenar)"; return false; }

            var buildings = new HashSet<GridPos>(cells.Where(c => c.Value == CellKind.Building).Select(c => c.Key));
            if (buildings.Count == 0) { error = "En az 1 kaynak binası gerekli"; return false; }

            var roads = new HashSet<GridPos>(cells.Where(c => c.Value == CellKind.Road).Select(c => c.Key));
            if (roads.Count < RoadLimit) { error = "Tüm yol haklarını kullanmalısın (" + roads.Count + "/" + RoadLimit + ")"; return false; }
            if (roads.Count > RoadLimit) { error = "Fazla yol var (" + roads.Count + "/" + RoadLimit + "): " + (roads.Count - RoadLimit) + " yol kaldır"; return false; }

            int unreached = roads.Count - FloodFromEntrances(roads);
            if (unreached > 0) { error = "Tüm yollar girişe bağlı olmalı (" + unreached + " parça kopuk)"; return false; }

            foreach (var t in cells.Where(c => c.Value == CellKind.Tower).Select(c => c.Key))
                if (!HasAdjacentRoad(t)) { error = "Kule " + t + " yola komşu değil"; return false; }

            foreach (var e in entrances.OrderBy(e => e.X))
                if (PathFinder.FindPath(Size, roads, e, buildings) == null)
                { error = "Giriş " + e + " kaynak binasına ulaşamıyor"; return false; }

            return true;
        }

        /// <summary>Girişlerden 4 yönlü ulaşılabilen yol parçası sayısı.</summary>
        private int FloodFromEntrances(HashSet<GridPos> roads)
        {
            var seen = new HashSet<GridPos>();
            var q = new Queue<GridPos>();
            foreach (var e in entrances) if (roads.Contains(e) && seen.Add(e)) q.Enqueue(e);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    var n = new GridPos(cur.X + Dx[i], cur.Y + Dy[i]);
                    if (roads.Contains(n) && seen.Add(n)) q.Enqueue(n);
                }
            }
            return seen.Count;
        }

        private bool HasAdjacentRoad(GridPos p)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (Get(new GridPos(p.X + dx, p.Y + dy)) == CellKind.Road) return true;
                }
            return false;
        }
    }
}
