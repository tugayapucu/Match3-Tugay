using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Finds every cell that belongs to a run of 3+ identical, non-empty tiles. Reads the Board; never changes it.
    public static class MatchFinder
    {
        private const int MinimumRunLength = 3;

        // A set, so a cell that is part of both a horizontal and a vertical run is reported only once.
        public static IReadOnlyCollection<GridPosition> FindMatches(Board board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var matches = new HashSet<GridPosition>();

            // Each row is a line going right: start at (0, y), step (1, 0).
            for (int y = 0; y < board.Height; y++)
            {
                AddLineMatches(board, matches, new GridPosition(0, y), 1, 0, board.Width);
            }

            // Each column is a line going up: start at (x, 0), step (0, 1).
            for (int x = 0; x < board.Width; x++)
            {
                AddLineMatches(board, matches, new GridPosition(x, 0), 0, 1, board.Height);
            }

            return matches;
        }

        // Scans one straight line of cells and adds every cell that is part of a matching run.
        private static void AddLineMatches(
            Board board, HashSet<GridPosition> matches, GridPosition lineStart, int stepX, int stepY, int lineLength)
        {
            // The current run covers line indices [runStart, i).
            int runStart = 0;
            TileType runType = board.GetTile(lineStart);

            // i runs one past the last cell: i == lineLength means "line ended" and closes the final run.
            for (int i = 1; i <= lineLength; i++)
            {
                bool lineEnded = i == lineLength;
                // Only read a cell that exists; past the end, the run simply ends.
                TileType current = TileType.Empty;
                if (!lineEnded)
                {
                    current = board.GetTile(CellAt(lineStart, stepX, stepY, i));
                    if (current == runType)
                    {
                        // Same type: the run keeps growing.
                        continue;
                    }
                }

                // The run just ended (type changed or line ended): keep its cells if it is a match.
                if (IsMatchingRun(runType, i - runStart))
                {
                    for (int k = runStart; k < i; k++)
                    {
                        matches.Add(CellAt(lineStart, stepX, stepY, k));
                    }
                }

                runStart = i;
                runType = current;
            }
        }

        // The single place that defines a match: 3+ cells of a real tile type.
        private static bool IsMatchingRun(TileType runType, int runLength)
        {
            // Empty cells are holes, not a colour, so a run of them is never a match.
            return runType != TileType.Empty && runLength >= MinimumRunLength;
        }

        // The cell `index` steps along a line from its start.
        private static GridPosition CellAt(GridPosition lineStart, int stepX, int stepY, int index)
        {
            return new GridPosition(lineStart.X + index * stepX, lineStart.Y + index * stepY);
        }
    }
}
