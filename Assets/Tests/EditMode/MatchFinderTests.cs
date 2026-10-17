using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class MatchFinderTests
    {
        [TestCase(TileType.HorizontalRocket, false)]
        [TestCase(TileType.VerticalRocket, false)]
        [TestCase(TileType.Propeller, false)]
        [TestCase(TileType.TNT, false)]
        [TestCase(TileType.LightBall, false)]
        [TestCase((TileType)99, false)]
        [TestCase(TileType.HorizontalRocket, true)]
        [TestCase(TileType.VerticalRocket, true)]
        [TestCase(TileType.Propeller, true)]
        [TestCase(TileType.TNT, true)]
        [TestCase(TileType.LightBall, true)]
        [TestCase((TileType)99, true)]
        public void FindMatches_NonNormalCells_BreakColorRunsAndNeverMatch(TileType type, bool vertical)
        {
            var board = new Board(7, 7);
            var expected = new List<GridPosition>();
            for (int i = 0; i < 7; i++)
            {
                // RR?RRRR must only match its last four colors; a whole line of '?' must not match.
                var colorPosition = vertical ? new GridPosition(0, i) : new GridPosition(i, 0);
                var specialPosition = vertical ? new GridPosition(2, i) : new GridPosition(i, 2);
                board.SetTile(colorPosition, i == 2 ? type : TileType.Red);
                board.SetTile(specialPosition, type);
                if (i >= 3)
                {
                    expected.Add(colorPosition);
                }
            }

            Assert.That(MatchFinder.FindMatches(board), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestBoardNotation_PreservesColorsAndDistinguishesSpecials()
        {
            var board = TestBoards.Create(".RGBYPOHVFTL");
            var expected = new[]
            {
                TileType.Empty, TileType.Red, TileType.Green, TileType.Blue,
                TileType.Yellow, TileType.Purple, TileType.Orange,
                TileType.HorizontalRocket, TileType.VerticalRocket, TileType.Propeller,
                TileType.TNT, TileType.LightBall
            };

            for (int x = 0; x < expected.Length; x++)
            {
                Assert.That(board.GetTile(new GridPosition(x, 0)), Is.EqualTo(expected[x]));
            }
        }

        [TestCase("RRR.....", 0, 3)]
        [TestCase(".RRR....", 1, 3)]
        [TestCase(".....RRR", 5, 3)]
        [TestCase("RRRR....", 0, 4)]
        [TestCase("RRRRR...", 0, 5)]
        [TestCase("RRRRRRRR", 0, 8)]
        [TestCase("GRRRBGBG", 1, 3)]
        public void FindMatches_SingleHorizontalRun_ReturnsExactlyThatRun(string row, int start, int length)
        {
            var board = TestBoards.Create(
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
            var board = TestBoards.Create(row);

            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
        }

        [Test]
        public void FindMatches_TwoRunsInOneRow_ReturnsBoth()
        {
            // The runs touch: the type change alone must end the first run.
            var board = TestBoards.Create("RRRGBBBB");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(Run(0, 0, 3), Run(0, 4, 4))));
        }

        [Test]
        public void FindMatches_RunsOnSeveralRows_ReturnsEveryRun()
        {
            var board = TestBoards.Create(
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
            var board = TestBoards.Create(
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
            var board = TestBoards.Create(
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
            var board = TestBoards.Create(
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
            var board = TestBoards.Create(
                "GGG....B",
                ".......B",
                ".......B");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(Run(2, 0, 3), Column(7, 0, 3))));
        }

        [Test]
        public void FindMatches_RunsInAllFourCorners_ReturnsEveryRun()
        {
            var board = TestBoards.Create(
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
        public void FindMatches_TShape_ReturnsTheSharedCellOnce()
        {
            var board = TestBoards.Create(
                "RRR",
                ".R.",
                ".R.");

            var matches = MatchFinder.FindMatches(board);

            // 3 horizontal + 3 vertical cells, sharing the top middle (1, 2): 5 distinct cells.
            Assert.That(matches.Count, Is.EqualTo(5));
            Assert.That(matches, Is.EquivalentTo(Cells(Run(2, 0, 3), Column(1, 0, 3))));
        }

        [Test]
        public void FindMatches_SolidBlock_ReturnsEveryCellOnce()
        {
            // Every cell is in one row run and one column run: 6 overlapping runs, 9 distinct cells.
            var board = TestBoards.Create(
                "RRR",
                "RRR",
                "RRR");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches.Count, Is.EqualTo(9));
            Assert.That(matches, Is.EquivalentTo(Cells(Run(0, 0, 3), Run(1, 0, 3), Run(2, 0, 3))));
        }

        [Test]
        public void FindMatches_EmptyCells_NeverMatch()
        {
            Assert.That(MatchFinder.FindMatches(new Board(8, 8)), Is.Empty);
        }

        [Test]
        public void FindMatches_EmptyRunBetweenRealRuns_IsIgnored()
        {
            // Three Empty cells sit between two real runs; only the real runs match.
            var board = TestBoards.Create("RRR...GGG");

            var matches = MatchFinder.FindMatches(board);

            Assert.That(matches, Is.EquivalentTo(Cells(Run(0, 0, 3), Run(0, 6, 3))));
        }

        [TestCase(3)]
        [TestCase(6)]
        public void FindMatches_GeneratedBoards_HaveNoMatches(int paletteSize)
        {
            // BoardGenerator promises no starting runs of 3; MatchFinder must agree on every seed.
            var allTypes = new[]
            {
                TileType.Red, TileType.Green, TileType.Blue,
                TileType.Yellow, TileType.Purple, TileType.Orange
            };
            var palette = new TileType[paletteSize];
            Array.Copy(allTypes, palette, paletteSize);

            for (int seed = -25; seed < 25; seed++)
            {
                var board = BoardGenerator.Generate(8, 8, palette, seed);
                Assert.That(MatchFinder.FindMatches(board), Is.Empty, $"Seed {seed}.");
            }
        }

        [Test]
        public void FindMatches_CalledTwice_ReturnsEquivalentResults()
        {
            var board = TestBoards.Create(
                "..R.....",
                "RRRR..GG",
                "..R...GB");

            var first = MatchFinder.FindMatches(board);
            var second = MatchFinder.FindMatches(board);

            Assert.That(second, Is.EquivalentTo(first));
        }

        [Test]
        public void FindMatches_RandomBoards_AgreeWithANaiveCellByCellCheck()
        {
            // Few types plus Empty make runs common, so these boards exercise many match shapes.
            var types = new[] { TileType.Empty, TileType.Red, TileType.Green, TileType.Blue };
            var random = new Random(12345);

            for (int boardIndex = 0; boardIndex < 200; boardIndex++)
            {
                var board = new Board(random.Next(1, 9), random.Next(1, 9));
                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        board.SetTile(new GridPosition(x, y), types[random.Next(types.Length)]);
                    }
                }

                var matches = MatchFinder.FindMatches(board);

                // No duplicates: the result has as many entries as distinct positions.
                Assert.That(matches.Count, Is.EqualTo(new HashSet<GridPosition>(matches).Count), $"Board {boardIndex}.");
                foreach (var position in matches)
                {
                    Assert.That(board.IsInBounds(position), Is.True, $"Board {boardIndex}: {position.X},{position.Y}.");
                }

                // Same cells as the naive rule: nothing missing, nothing extra.
                var expected = new List<GridPosition>();
                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        var position = new GridPosition(x, y);
                        if (IsMatchedNaive(board, position))
                        {
                            expected.Add(position);
                        }
                    }
                }

                Assert.That(matches, Is.EquivalentTo(expected), $"Board {boardIndex}.");
            }
        }

        [Test]
        public void FindMatches_DoesNotChangeTheBoard()
        {
            var board = TestBoards.Create(
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

        // Deliberately simple, slow reference rule: a cell is matched if some 3 consecutive cells
        // that include it, in its row or column, are the same non-empty type.
        private static bool IsMatchedNaive(Board board, GridPosition position)
        {
            TileType type = board.GetTile(position);
            if (type == TileType.Empty)
            {
                return false;
            }

            var directions = new[] { new[] { 1, 0 }, new[] { 0, 1 } };
            foreach (var direction in directions)
            {
                // The window of 3 can start 2, 1, or 0 steps before this cell.
                for (int offset = -2; offset <= 0; offset++)
                {
                    bool allSame = true;
                    for (int step = 0; step < 3; step++)
                    {
                        var cell = new GridPosition(
                            position.X + (offset + step) * direction[0],
                            position.Y + (offset + step) * direction[1]);
                        if (!board.IsInBounds(cell) || board.GetTile(cell) != type)
                        {
                            allSame = false;
                            break;
                        }
                    }

                    if (allSame)
                    {
                        return true;
                    }
                }
            }

            return false;
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
