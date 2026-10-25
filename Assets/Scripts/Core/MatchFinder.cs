using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Describes normal-color runs and squares. Reads the Board; never changes it or chooses specials.
    public static class MatchFinder
    {
        private const int MinimumRunLength = 3;
        private static readonly GridPosition[] NeighborOffsets =
        {
            new GridPosition(-1, 0), new GridPosition(1, 0),
            new GridPosition(0, -1), new GridPosition(0, 1)
        };

        // Convenience API for callers that need matched cells without the shape and group details.
        public static IReadOnlyCollection<GridPosition> FindMatches(Board board)
        {
            return Analyze(board).MatchedPositions;
        }

        public static MatchAnalysis Analyze(Board board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var matches = new HashSet<GridPosition>();
            var patterns = new List<MatchPattern>();

            // Each row is a line going right: start at (0, y), step (1, 0).
            for (int y = 0; y < board.Height; y++)
            {
                AddLinePatterns(board, matches, patterns, new GridPosition(0, y), 1, 0, board.Width);
            }

            // Each column is a line going up: start at (x, 0), step (0, 1).
            for (int x = 0; x < board.Width; x++)
            {
                AddLinePatterns(board, matches, patterns, new GridPosition(x, 0), 0, 1, board.Height);
            }

            AddSquarePatterns(board, matches, patterns);
            return BuildGroups(board, matches, patterns);
        }

        // Scans one straight line of cells and adds every cell that is part of a matching run.
        private static void AddLinePatterns(
            Board board, HashSet<GridPosition> matches, List<MatchPattern> patterns,
            GridPosition lineStart, int stepX, int stepY, int lineLength)
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
                    var cells = new List<GridPosition>();
                    for (int k = runStart; k < i; k++)
                    {
                        GridPosition cell = CellAt(lineStart, stepX, stepY, k);
                        cells.Add(cell);
                        matches.Add(cell);
                    }

                    var kind = stepX == 1 ? MatchPatternKind.HorizontalRun : MatchPatternKind.VerticalRun;
                    patterns.Add(new MatchPattern(kind, cells));
                }

                runStart = i;
                runType = current;
            }
        }

        private static void AddSquarePatterns(Board board, HashSet<GridPosition> matches, List<MatchPattern> patterns)
        {
            for (int y = 0; y < board.Height - 1; y++)
            {
                for (int x = 0; x < board.Width - 1; x++)
                {
                    var bottomLeft = new GridPosition(x, y);
                    TileType color = board.GetTile(bottomLeft);
                    var bottomRight = new GridPosition(x + 1, y);
                    var topLeft = new GridPosition(x, y + 1);
                    var topRight = new GridPosition(x + 1, y + 1);
                    if (TileRules.IsNormalMatchItem(color)
                        && board.GetTile(bottomRight) == color
                        && board.GetTile(topLeft) == color
                        && board.GetTile(topRight) == color)
                    {
                        var cells = new[] { bottomLeft, bottomRight, topLeft, topRight };
                        patterns.Add(new MatchPattern(MatchPatternKind.Square, cells));
                        matches.UnionWith(cells);
                    }
                }
            }
        }

        private static MatchAnalysis BuildGroups(Board board, HashSet<GridPosition> matches, List<MatchPattern> patterns)
        {
            var orderedMatches = new List<GridPosition>(matches);
            orderedMatches.Sort(ComparePositions);
            var remaining = new HashSet<GridPosition>(matches);
            var groups = new List<MatchGroup>();
            var queue = new Queue<GridPosition>();

            foreach (GridPosition start in orderedMatches)
            {
                if (!remaining.Remove(start))
                {
                    continue;
                }

                TileType color = board.GetTile(start);
                var groupCells = new HashSet<GridPosition> { start };
                queue.Enqueue(start);
                // Flood-fill only matched neighbors of this color, never all occupied cells.
                while (queue.Count > 0)
                {
                    GridPosition cell = queue.Dequeue();
                    foreach (GridPosition offset in NeighborOffsets)
                    {
                        var neighbor = new GridPosition(cell.X + offset.X, cell.Y + offset.Y);
                        if (remaining.Contains(neighbor) && board.GetTile(neighbor) == color)
                        {
                            remaining.Remove(neighbor);
                            groupCells.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                var groupPatterns = new List<MatchPattern>();
                foreach (MatchPattern pattern in patterns)
                {
                    // Every pattern is connected and one color, so its first cell identifies its group.
                    if (groupCells.Contains(pattern.Positions[0]))
                    {
                        groupPatterns.Add(pattern);
                    }
                }

                var orderedCells = new List<GridPosition>(groupCells);
                orderedCells.Sort(ComparePositions);
                groups.Add(new MatchGroup(color, orderedCells, groupPatterns));
            }

            return new MatchAnalysis(orderedMatches, groups);
        }

        private static int ComparePositions(GridPosition first, GridPosition second)
        {
            int byY = first.Y.CompareTo(second.Y);
            return byY != 0 ? byY : first.X.CompareTo(second.X);
        }

        // An ordinary line match needs 3+ cells of the same normal color.
        private static bool IsMatchingRun(TileType runType, int runLength)
        {
            // Neither holes nor specials count as matching colors, even when repeated.
            return TileRules.IsNormalMatchItem(runType) && runLength >= MinimumRunLength;
        }

        // The cell `index` steps along a line from its start.
        private static GridPosition CellAt(GridPosition lineStart, int stepX, int stepY, int index)
        {
            return new GridPosition(lineStart.X + index * stepX, lineStart.Y + index * stepY);
        }
    }
}
