using System;

namespace Match3.Core
{
    // One tile that changed cell during resolution (for example, falling under gravity).
    // Presentation uses From/To to move the right visual, instead of guessing from colours.
    public readonly struct TileMove : IEquatable<TileMove>
    {
        public GridPosition From { get; }
        public GridPosition To { get; }
        public TileType Type { get; }

        public TileMove(GridPosition from, GridPosition to, TileType type)
        {
            From = from;
            To = to;
            Type = type;
        }

        public bool Equals(TileMove other)
        {
            return From == other.From && To == other.To && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is TileMove other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To, Type);
        }

        // Readable test failure messages, e.g. "Red (0,4)->(0,1)".
        public override string ToString()
        {
            return $"{Type} ({From.X},{From.Y})->({To.X},{To.Y})";
        }
    }
}
