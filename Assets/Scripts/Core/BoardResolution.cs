using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // The logical steps that resolve matches on a Board. Pure data changes: no visuals, no animation.
    public static class BoardResolution
    {
        // Sets every supplied cell to Empty (typically the cells MatchFinder found).
        // All positions are checked first, so invalid input leaves the Board unchanged.
        public static void ClearCells(Board board, IReadOnlyCollection<GridPosition> positions)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            foreach (GridPosition position in positions)
            {
                if (!board.IsInBounds(position))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(positions), position, "Every position must be within the board bounds.");
                }
            }

            // Empty marks a hole that gravity and refill will deal with in later steps.
            foreach (GridPosition position in positions)
            {
                board.SetTile(position, TileType.Empty);
            }
        }
    }
}
