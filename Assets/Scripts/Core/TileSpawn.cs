using System;

namespace Match3.Core
{
    // One new tile created by refill: which cell it fills and what it is.
    // Logical only; Presentation decides where above the board the new visual appears.
    public readonly struct TileSpawn : IEquatable<TileSpawn>
    {
        public GridPosition Position { get; }
        public TileType Type { get; }

        public TileSpawn(GridPosition position, TileType type)
        {
            Position = position;
            Type = type;
        }

        public bool Equals(TileSpawn other)
        {
            return Position == other.Position && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is TileSpawn other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Type);
        }

        // Readable test failure messages, e.g. "Red at (0,7)".
        public override string ToString()
        {
            return $"{Type} at ({Position.X},{Position.Y})";
        }
    }
}
