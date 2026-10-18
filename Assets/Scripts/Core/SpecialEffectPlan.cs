using System.Collections.Generic;

namespace Match3.Core
{
    // One effect's immutable geometry. Footprint can include holes; only occupied cells become removals.
    public sealed class SpecialEffect
    {
        public GridPosition Origin { get; }
        public TileType Type { get; }
        public IReadOnlyList<GridPosition> Footprint { get; }
        public GridPosition? RemoteTarget { get; }

        internal SpecialEffect(GridPosition origin, TileType type, IEnumerable<GridPosition> footprint,
            GridPosition? remoteTarget = null)
        {
            Origin = origin;
            Type = type;
            Footprint = new List<GridPosition>(footprint).AsReadOnly();
            RemoteTarget = remoteTarget;
        }
    }

    // A completed chain, calculated before any Board mutation; effects retain their activation order.
    public sealed class SpecialEffectPlan
    {
        public IReadOnlyList<GridPosition> RemovedPositions { get; }
        public IReadOnlyList<SpecialEffect> Effects { get; }

        internal SpecialEffectPlan(IEnumerable<GridPosition> removedPositions, IEnumerable<SpecialEffect> effects)
        {
            RemovedPositions = new List<GridPosition>(removedPositions).AsReadOnly();
            Effects = new List<SpecialEffect>(effects).AsReadOnly();
        }
    }
}
