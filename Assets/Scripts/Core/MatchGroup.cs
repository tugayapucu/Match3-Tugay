using System.Collections.Generic;

namespace Match3.Core
{
    // Orthogonally connected matched cells of one normal color; unmatched cells cannot bridge groups.
    public sealed class MatchGroup
    {
        public TileType Color { get; }
        public IReadOnlyList<GridPosition> Positions { get; }
        public IReadOnlyList<MatchPattern> Patterns { get; }

        internal MatchGroup(TileType color, IEnumerable<GridPosition> positions, IEnumerable<MatchPattern> patterns)
        {
            Color = color;
            Positions = new List<GridPosition>(positions).AsReadOnly();
            Patterns = new List<MatchPattern>(patterns).AsReadOnly();
        }
    }
}
