using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Plans effect geometry from logical cells; never changes the Board or manipulates views.
    public static class SpecialEffects
    {
        // Read the stable Board for the entire chain; callers apply the completed plan afterwards.
        public static SpecialEffectPlan Plan(Board board, IReadOnlyList<SpecialActivation> initialActivations,
            Random targetingRandom = null)
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
            // Equal positions must describe the same request; sorting must never choose a target color.
            var requestedColors = new Dictionary<GridPosition, TileType?>();
            foreach (SpecialActivation activation in initial)
            {
                if (requestedColors.TryGetValue(activation.Position, out var color)
                    && color != activation.TargetColor)
                {
                    throw new ArgumentException("Duplicate activation positions must have the same target color.",
                        nameof(initialActivations));
                }
                requestedColors[activation.Position] = activation.TargetColor;
            }
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
                var footprint = GetFootprint(board, activation.Position, activation.TargetColor);
                TileType specialType = board.GetTile(activation.Position);
                GridPosition? remoteTarget = null;
                if (specialType == TileType.Propeller)
                {
                    if (targetingRandom == null)
                    {
                        throw new ArgumentNullException(nameof(targetingRandom), "Propeller needs its own targeting stream.");
                    }
                    remoteTarget = FindRemoteTarget(board, footprint, removals, scheduled, targetingRandom);
                    if (remoteTarget.HasValue)
                    {
                        var combined = new List<GridPosition>(footprint) { remoteTarget.Value };
                        combined.Sort(ComparePositions);
                        footprint = combined.AsReadOnly();
                    }
                }
                effects.Add(new SpecialEffect(activation.Position, specialType, footprint, remoteTarget));
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

        // Base geometry includes holes. Propeller's remote target requires chain state and is added by Plan.
        public static IReadOnlyList<GridPosition> GetFootprint(Board board, GridPosition origin, TileType? targetColor = null)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!board.IsInBounds(origin))
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "Activation must be within the board.");
            }

            if (targetColor.HasValue && !TileRules.IsNormalMatchItem(targetColor.Value))
            {
                throw new ArgumentException("The target must be a normal color.", nameof(targetColor));
            }

            TileType type = board.GetTile(origin);
            if (targetColor.HasValue && type != TileType.LightBall)
            {
                throw new ArgumentException("Only Light Ball activations may specify a target color.", nameof(targetColor));
            }

            var cells = new List<GridPosition>();
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
                case TileType.Propeller:
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var cell = new GridPosition(origin.X + dx, origin.Y + dy);
                            if (Math.Abs(dx) + Math.Abs(dy) <= 1 && board.IsInBounds(cell))
                            {
                                cells.Add(cell);
                            }
                        }
                    }
                    break;
                case TileType.LightBall:
                    cells.Add(origin);
                    TileType? color = targetColor ?? MostCommonColor(board);
                    if (color.HasValue)
                    {
                        for (int y = 0; y < board.Height; y++)
                        {
                            for (int x = 0; x < board.Width; x++)
                            {
                                var cell = new GridPosition(x, y);
                                if (board.GetTile(cell) == color.Value)
                                {
                                    cells.Add(cell);
                                }
                            }
                        }
                    }
                    cells.Sort(ComparePositions);
                    break;
                default:
                    throw new ArgumentException("Activation requires a special at the origin.", nameof(origin));
            }

            return cells.AsReadOnly();
        }

        private static GridPosition? FindRemoteTarget(Board board, IReadOnlyList<GridPosition> local,
            HashSet<GridPosition> removals, HashSet<GridPosition> scheduled, Random random)
        {
            var takeoffArea = new HashSet<GridPosition>(local);
            var candidates = new List<GridPosition>();
            // Loop order provides stable Y-then-X candidate ordering without relying on a set's order.
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = new GridPosition(x, y);
                    TileType type = board.GetTile(cell);
                    if ((!TileRules.IsNormalMatchItem(type) && !TileRules.IsSpecial(type))
                        || takeoffArea.Contains(cell) || removals.Contains(cell)
                        || (TileRules.IsSpecial(type) && scheduled.Contains(cell)))
                    {
                        continue;
                    }
                    candidates.Add(cell);
                }
            }
            // No draw when there is nowhere to fly: preserve the targeting stream's next value.
            return candidates.Count == 0 ? (GridPosition?)null : candidates[random.Next(candidates.Count)];
        }

        private static TileType? MostCommonColor(Board board)
        {
            var counts = new Dictionary<TileType, int>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    TileType type = board.GetTile(new GridPosition(x, y));
                    if (TileRules.IsNormalMatchItem(type))
                    {
                        counts.TryGetValue(type, out int count);
                        counts[type] = count + 1;
                    }
                }
            }

            TileType? best = null;
            int bestCount = 0;
            foreach (var entry in counts)
            {
                if (!best.HasValue || entry.Value > bestCount
                    || (entry.Value == bestCount && (int)entry.Key < (int)best.Value))
                {
                    best = entry.Key;
                    bestCount = entry.Value;
                }
            }
            return best;
        }
    }
}
