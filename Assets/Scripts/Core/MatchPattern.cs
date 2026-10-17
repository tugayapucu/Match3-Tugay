using System.Collections.Generic;

namespace Match3.Core
{
    public enum MatchPatternKind
    {
        HorizontalRun,
        VerticalRun,
        Square
    }

    // Shape facts only: maximal runs are ordered left-to-right/bottom-to-top; squares start bottom-left.
    public sealed class MatchPattern
    {
        public MatchPatternKind Kind { get; }
        public IReadOnlyList<GridPosition> Positions { get; }

        internal MatchPattern(MatchPatternKind kind, IEnumerable<GridPosition> positions)
        {
            Kind = kind;
            Positions = new List<GridPosition>(positions).AsReadOnly();
        }

        internal bool Contains(GridPosition position)
        {
            foreach (GridPosition cell in Positions)
            {
                if (cell == position)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
