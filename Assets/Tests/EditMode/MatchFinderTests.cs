using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class MatchFinderTests
    {
        [TestCase("RRR.....", 0, 3)]
        [TestCase(".RRR....", 1, 3)]
        [TestCase(".....RRR", 5, 3)]
        [TestCase("RRRR....", 0, 4)]
        [TestCase("RRRRR...", 0, 5)]
        [TestCase("RRRRRRRR", 0, 8)]
        [TestCase("GRRRBGBG", 1, 3)]
        public void FindMatches_SingleHorizontalRun_ReturnsExactlyThatRun(string row, int start, int length)
        {
            var board = CreateBoard(
                "........",
                row);

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Run(0, start, length)));
        }

        [TestCase("R.R.R...")]
        [TestCase("RRGGRRGG")]
        [TestCase("RRBRRBRR")]
        public void FindMatches_NoRunOfThree_ReturnsNoMatches(string row)
        {
            var board = CreateBoard(row);

            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
        }

        [Test]
        public void FindMatches_TwoRunsInOneRow_ReturnsBoth()
        {
            // The runs touch: the type change alone must end the first run.
            var board = CreateBoard("RRRGBBBB");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(Run(0, 0, 3), Run(0, 4, 4))));
        }

        [Test]
        public void FindMatches_RunsOnSeveralRows_ReturnsEveryRun()
        {
            var board = CreateBoard(
                "GGG.....",
                "........",
                ".....BBB");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(Run(2, 0, 3), Run(0, 5, 3))));
        }

        [TestCase(0, 3)]
        [TestCase(2, 3)]
        [TestCase(5, 3)]
        [TestCase(0, 4)]
        [TestCase(0, 5)]
        [TestCase(0, 8)]
        public void FindMatches_SingleVerticalRun_ReturnsExactlyThatRun(int start, int length)
        {
            // Column 0 alternates Green/Blue (never 3 in a row), then a Red run is written over it.
            var board = new Board(2, 8);
            for (int y = 0; y < board.Height; y++)
            {
                board.SetTile(new GridPosition(0, y), y % 2 == 0 ? TileType.Green : TileType.Blue);
            }

            for (int y = start; y < start + length; y++)
            {
                board.SetTile(new GridPosition(0, y), TileType.Red);
            }

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Column(0, start, length)));
        }

        [Test]
        public void FindMatches_LShape_ReturnsTheSharedCornerOnce()
        {
            var board = CreateBoard(
                "RRR.....",
                "..R.....",
                "..R.....");

            var matches = MatchFinder.FindMatches(board);

            // 3 horizontal + 3 vertical cells, sharing (2, 2): 5 distinct cells.
            Assert.That(matches.Count, Is.EqualTo(5));
            Assert.That(matches, Is.EquivalentTo(Cells(Run(2, 0, 3), Column(2, 0, 3))));
        }

        [Test]
        public void FindMatches_CrossShape_ReturnsTheIntersectionOnce()
        {
            var board = CreateBoard(
                "..R.....",
                "RRRR....",
                "..R.....");

            var matches = MatchFinder.FindMatches(board);

            // 4 horizontal + 3 vertical cells, sharing (2, 1): 6 distinct cells.
            Assert.That(matches.Count, Is.EqualTo(6));
            Assert.That(matches, Is.EquivalentTo(Cells(Run(1, 0, 4), Column(2, 0, 3))));
        }

        [Test]
        public void FindMatches_HShape_ReturnsBothIntersectionsOnce()
        {
            var board = CreateBoard(
                "R.R.....",
                "RRR.....",
                "R.R.....");

            var matches = MatchFinder.FindMatches(board);

            // Two columns of 3 plus a row of 3 that crosses both: 7 distinct cells.
            Assert.That(matches.Count, Is.EqualTo(7));
            Assert.That(matches, Is.EquivalentTo(Cells(Run(1, 0, 3), Column(0, 0, 3), Column(2, 0, 3))));
        }

        [Test]
        public void FindMatches_SeparateHorizontalAndVerticalRuns_ReturnsBoth()
        {
            var board = CreateBoard(
                "GGG....B",
                ".......B",
                ".......B");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(Run(2, 0, 3), Column(7, 0, 3))));
        }

        [Test]
        public void FindMatches_RunsInAllFourCorners_ReturnsEveryRun()
        {
            var board = CreateBoard(
                "RRR.G",
                "....G",
                "B...G",
                "B....",
                "B.YYY");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(
                Run(4, 0, 3), Column(4, 2, 3), Column(0, 0, 3), Run(0, 2, 3))));
        }

        [Test]
        public void FindMatches_EmptyCells_NeverMatch()
        {
            Assert.That(MatchFinder.FindMatches(new Board(8, 8)), Is.Empty);
        }

        [Test]
        public void FindMatches_DoesNotChangeTheBoard()
        {
            var board = CreateBoard(
                "RRRGBBBB",
                "GBRGBYPO");
            var before = Snapshot(board);

            MatchFinder.FindMatches(board);

            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        [Test]
        public void FindMatches_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MatchFinder.FindMatches(null));
        }

        // Rows are written top-down as they appear on screen: rows[0] is the highest y.
        // Letters are tile colours; '.' is Empty.
        private static Board CreateBoard(params string[] rows)
        {
            var board = new Board(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
            {
                Assert.That(rows[row].Length, Is.EqualTo(board.Width), "All rows must have the same length.");
                int y = board.Height - 1 - row;
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), ToTileType(rows[row][x]));
                }
            }

            return board;
        }

        private static TileType ToTileType(char symbol)
        {
            switch (symbol)
            {
                case '.': return TileType.Empty;
                case 'R': return TileType.Red;
                case 'G': return TileType.Green;
                case 'B': return TileType.Blue;
                case 'Y': return TileType.Yellow;
                case 'P': return TileType.Purple;
                case 'O': return TileType.Orange;
                default: throw new ArgumentException($"Unknown tile symbol '{symbol}'.", nameof(symbol));
            }
        }

        // The positions (start..start+length-1, y).
        private static List<GridPosition> Run(int y, int start, int length)
        {
            var positions = new List<GridPosition>();
            for (int x = start; x < start + length; x++)
            {
                positions.Add(new GridPosition(x, y));
            }

            return positions;
        }

        // The positions (x, start..start+length-1).
        private static List<GridPosition> Column(int x, int start, int length)
        {
            var positions = new List<GridPosition>();
            for (int y = start; y < start + length; y++)
            {
                positions.Add(new GridPosition(x, y));
            }

            return positions;
        }

        // Every cell from the given runs, each listed once, even where runs overlap.
        private static List<GridPosition> Cells(params List<GridPosition>[] runs)
        {
            var distinct = new HashSet<GridPosition>();
            foreach (var run in runs)
            {
                distinct.UnionWith(run);
            }

            return new List<GridPosition>(distinct);
        }

        private static TileType[,] Snapshot(Board board)
        {
            var tiles = new TileType[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    tiles[x, y] = board.GetTile(new GridPosition(x, y));
                }
            }

            return tiles;
        }
    }
}
