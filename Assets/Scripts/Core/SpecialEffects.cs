using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Plans effect geometry from logical cells; never changes the Board or manipulates views.
    public static class SpecialEffects
    {
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
