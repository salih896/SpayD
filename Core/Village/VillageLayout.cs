using System.Collections.Generic;
using System.Linq;
using GameCore.Grid;

namespace GameCore.Village
{
    public enum CellKind { Empty = 0, Road = 1, Building = 2, Tower = 3 }

    /// <summary>Yol malzemesi (GDD Bölüm 5): tahta, altın+tahta, demir. Maliyet/araştırma kuralları henüz yok.</summary>
    public enum RoadTier { Wood = 0, Reinforced = 1, Iron = 2 }

    /// <summary>
    /// Savunma köyü düzeni (saf C#). Grid boyutu parametriktir (varsayılan 11).
    /// Giriş kenarı: y == 0 (alt kenar). Kurallar GDD Bölüm 4-5.
    /// </summary>
    public sealed class VillageLayout
    {
        public const int MaxEntrances = 10;

        public int Size { get; }
        private readonly Dictionary<GridPos, CellKind> cells = new Dictionary<GridPos, CellKind>();
        private readonly HashSet<GridPos> entrances = new HashSet<GridPos>();
        private readonly Dictionary<GridPos, RoadTier> roadTiers = new Dictionary<GridPos, RoadTier>();

        public VillageLayout(int size = 11) { Size = size; }

        public bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < Size && p.Y < Size;
        public CellKind Get(GridPos p) => cells.TryGetValue(p, out var k) ? k : CellKind.Empty;
        public bool IsEntrance(GridPos p) => entrances.Contains(p);
        public int EntranceCount => entrances.Count;

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

        /// <summary>Alt kenarda giriş aç/kapat. Boş hücreye basılırsa yol + giriş birlikte konur.</summary>
        public bool ToggleEntrance(GridPos p, out string error)
        {
            error = null;
            if (!InBounds(p) || p.Y != 0) { error = "Giriş yalnızca alt kenarda olabilir"; return false; }
            var kind = Get(p);
            if (kind != CellKind.Empty && kind != CellKind.Road) { error = "Hücre dolu"; return false; }
            if (entrances.Contains(p)) { entrances.Remove(p); return true; }
            if (entrances.Count >= MaxEntrances) { error = "En fazla " + MaxEntrances + " giriş"; return false; }
            cells[p] = CellKind.Road;
            entrances.Add(p);
            return true;
        }

        /// <summary>Yeni boyutta kopya; sınır içindeki hücreler korunur.</summary>
        public VillageLayout Resized(int newSize)
        {
            var n = new VillageLayout(newSize);
            foreach (var kv in cells) if (n.InBounds(kv.Key)) n.cells[kv.Key] = kv.Value;
            foreach (var e in entrances) if (n.InBounds(e)) n.entrances.Add(e);
            foreach (var kv in roadTiers) if (n.InBounds(kv.Key)) n.roadTiers[kv.Key] = kv.Value;
            return n;
        }

        /// <summary>Savunma düzeni kaydedilebilir mi? (GDD Bölüm 5)</summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entrances.Count == 0) { error = "En az 1 aktif giriş gerekli (alt kenar)"; return false; }

            var buildings = new HashSet<GridPos>(cells.Where(c => c.Value == CellKind.Building).Select(c => c.Key));
            if (buildings.Count == 0) { error = "En az 1 kaynak binası gerekli"; return false; }

            foreach (var t in cells.Where(c => c.Value == CellKind.Tower).Select(c => c.Key))
                if (!HasAdjacentRoad(t)) { error = "Kule " + t + " yola komşu değil"; return false; }

            var roads = new HashSet<GridPos>(cells.Where(c => c.Value == CellKind.Road).Select(c => c.Key));
            foreach (var e in entrances.OrderBy(e => e.X))
                if (PathFinder.FindPath(Size, roads, e, buildings) == null)
                { error = "Giriş " + e + " kaynak binasına ulaşamıyor"; return false; }

            return true;
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
