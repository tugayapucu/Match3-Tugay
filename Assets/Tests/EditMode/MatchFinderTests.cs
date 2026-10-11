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

            Assert.That(matches, Is.EquivalentTo(Concat(Run(0, 0, 3), Run(0, 4, 4))));
        }

        [Test]
        public void FindMatches_RunsOnSeveralRows_ReturnsEveryRun()
        {
            var board = CreateBoard(
                "GGG.....",
                "........",
                ".....BBB");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Concat(Run(2, 0, 3), Run(0, 5, 3))));
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

        private static List<GridPosition> Concat(List<GridPosition> first, List<GridPosition> second)
        {
            var all = new List<GridPosition>(first);
            all.AddRange(second);
            return all;
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
