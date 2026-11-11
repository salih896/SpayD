using System;

namespace GameCore.Grid
{
    /// <summary>Mantıksal grid koordinatı (tamsayı). Görsel/izometri ile ilgisi yoktur.</summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;
        public GridPos(int x, int y) { X = x; Y = y; }

        public bool Equals(GridPos o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is GridPos p && Equals(p);
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => $"({X},{Y})";
    }
}
