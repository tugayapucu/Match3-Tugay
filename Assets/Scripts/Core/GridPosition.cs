using System;

namespace Match3.Core
{
    // A logical cell address (column X, row Y) on the board; not a Unity world position.
    // Immutable struct: copied by value, no heap allocation, safe to use as a dictionary key.
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        // Two positions are equal when their coordinates match (value equality, not reference equality).
        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        // Fallback for non-generic callers; the typed Equals above avoids boxing.
        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        // Must agree with Equals so HashSet/Dictionary lookups work.
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }
    }
}
