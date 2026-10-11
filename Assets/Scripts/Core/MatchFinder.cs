using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Finds every cell that belongs to a run of 3+ identical, non-empty tiles. Reads the Board; never changes it.
    public static class MatchFinder
    {
        private const int MinimumRunLength = 3;

        // A set, so a cell that is part of more than one run is still reported only once.
        public static IReadOnlyCollection<GridPosition> FindMatches(Board board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var matches = new HashSet<GridPosition>();
            AddHorizontalMatches(board, matches);
            return matches;
        }

        private static void AddHorizontalMatches(Board board, HashSet<GridPosition> matches)
        {
            for (int y = 0; y < board.Height; y++)
            {
                int runStart = 0;
                TileType runType = board.GetTile(new GridPosition(0, y));

                // x runs one past the last column: x == Width acts as "end of row" and closes the final run.
                for (int x = 1; x <= board.Width; x++)
                {
                    if (x < board.Width)
                    {
                        TileType current = board.GetTile(new GridPosition(x, y));
                        if (current == runType)
                        {
                            continue;
                        }

                        // The run [runStart, x) just ended because the type changed.
                        AddRunIfMatch(matches, runType, runStart, x, y);
                        runStart = x;
                        runType = current;
                    }
                    else
                    {
                        // Row ended: the right-edge run must be checked too.
                        AddRunIfMatch(matches, runType, runStart, x, y);
                    }
                }
            }
        }

        // Adds cells runStart..runEnd-1 of row y when they form a match.
        private static void AddRunIfMatch(HashSet<GridPosition> matches, TileType runType, int runStart, int runEnd, int y)
        {
            // Empty cells are holes, not a colour, so a run of them is never a match.
            if (runType == TileType.Empty || runEnd - runStart < MinimumRunLength)
            {
                return;
            }

            for (int x = runStart; x < runEnd; x++)
            {
                matches.Add(new GridPosition(x, y));
            }
        }
    }
}
