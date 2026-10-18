using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Plans effect geometry from logical cells; never changes the Board or manipulates views.
    public static class SpecialEffects
    {
        // Read the stable Board for the entire chain; callers apply the completed plan afterwards.
        public static SpecialEffectPlan Plan(Board board, IReadOnlyList<SpecialActivation> initialActivations)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }
            if (initialActivations == null)
            {
                throw new ArgumentNullException(nameof(initialActivations));
            }

            var initial = new List<SpecialActivation>(initialActivations);
            initial.Sort((first, second) => ComparePositions(first.Position, second.Position));
            var queue = new Queue<SpecialActivation>();
            // Keep entries after dequeue: the set covers both queued and already-processed specials.
            var scheduled = new HashSet<GridPosition>();
            foreach (SpecialActivation activation in initial)
            {
                if (scheduled.Add(activation.Position))
                {
                    queue.Enqueue(activation);
                }
            }

            var removals = new HashSet<GridPosition>();
            var effects = new List<SpecialEffect>();
            while (queue.Count > 0)
            {
                SpecialActivation activation = queue.Dequeue();
                var footprint = GetFootprint(board, activation.Position);
                effects.Add(new SpecialEffect(activation.Position, board.GetTile(activation.Position), footprint));
                foreach (GridPosition cell in footprint)
                {
                    TileType type = board.GetTile(cell);
                    if (TileRules.IsEmpty(type))
                    {
                        continue;
                    }

                    removals.Add(cell);
                    if (TileRules.IsSpecial(type) && scheduled.Add(cell))
                    {
                        queue.Enqueue(new SpecialActivation(cell));
                    }
                }
            }

            var orderedRemovals = new List<GridPosition>(removals);
            orderedRemovals.Sort(ComparePositions);
            return new SpecialEffectPlan(orderedRemovals, effects);
        }

        private static int ComparePositions(GridPosition first, GridPosition second)
        {
            int byY = first.Y.CompareTo(second.Y);
            return byY != 0 ? byY : first.X.CompareTo(second.X);
        }

        // Geometry includes Empty cells; the chain planner later selects occupied cells for removal.
        public static IReadOnlyList<GridPosition> GetFootprint(Board board, GridPosition origin)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.IsInBounds(origin))
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "Activation must be within the board.");
            }

            var cells = new List<GridPosition>();
            TileType type = board.GetTile(origin);
            switch (type)
            {
                case TileType.HorizontalRocket:
                    for (int x = 0; x < board.Width; x++)
                    {
                        cells.Add(new GridPosition(x, origin.Y));
                    }
                    break;
                case TileType.VerticalRocket:
                    for (int y = 0; y < board.Height; y++)
                    {
                        cells.Add(new GridPosition(origin.X, y));
                    }
                    break;
                case TileType.TNT:
                    // Y outside/X inside keeps every footprint in the same coordinate order.
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            var cell = new GridPosition(origin.X + dx, origin.Y + dy);
                            if (board.IsInBounds(cell))
                            {
                                cells.Add(cell);
                            }
                        }
                    }
                    break;
                default:
                    if (TileRules.IsSpecial(type))
                    {
                        throw new NotSupportedException($"{type} effects are not implemented yet.");
                    }
                    throw new ArgumentException("Activation requires a special at the origin.", nameof(origin));
            }

            return cells.AsReadOnly();
        }
    }
}
