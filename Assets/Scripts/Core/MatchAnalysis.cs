using System.Collections.Generic;

namespace Match3.Core
{
    // Immutable snapshot of a scan, independent of later Board changes. Positions/groups use Y-then-X order.
    public sealed class MatchAnalysis
    {
        public IReadOnlyList<GridPosition> MatchedPositions { get; }
        public IReadOnlyList<MatchGroup> Groups { get; }

        internal MatchAnalysis(IEnumerable<GridPosition> matchedPositions, IEnumerable<MatchGroup> groups)
        {
            MatchedPositions = new List<GridPosition>(matchedPositions).AsReadOnly();
            Groups = new List<MatchGroup>(groups).AsReadOnly();
        }
    }
}
