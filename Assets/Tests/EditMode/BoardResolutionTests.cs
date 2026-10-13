using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class BoardResolutionTests
    {
        [Test]
        public void ClearCells_SuppliedPositions_BecomeEmpty()
        {
            // The plan's example: R R R B with (0,0)(1,0)(2,0) supplied.
            var board = TestBoards.Create("RRRB");
            var positions = new[] { new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0) };

            BoardResolution.ClearCells(board, positions);

            AssertBoard(board, "...B");
        }

        [Test]
        public void ClearCells_HorizontalMatch_ClearsOnlyThatRun()
        {
            var board = TestBoards.Create(
                "GBYP",
                "RRRB");

            BoardResolution.ClearCells(board, MatchFinder.FindMatches(board));

            AssertBoard(board,
                "GBYP",
                "...B");
        }

        [Test]
        public void ClearCells_VerticalMatch_ClearsOnlyThatRun()
        {
            var board = TestBoards.Create(
                "RB",
                "RG",
                "RB",
                "YG");

            BoardResolution.ClearCells(board, MatchFinder.FindMatches(board));

            AssertBoard(board,
                ".B",
                ".G",
                ".B",
                "YG");
        }

        [Test]
        public void ClearCells_Intersection_ClearsBothRuns()
        {
            var board = TestBoards.Create(
                "G.R.",
                ".RRR",
                "B.R.");

            BoardResolution.ClearCells(board, MatchFinder.FindMatches(board));

            AssertBoard(board,
                "G...",
                "....",
                "B...");
        }

        [Test]
        public void ClearCells_OverlappingGroups_ClearsEveryCellOnce()
        {
            var board = TestBoards.Create(
                "RRRG",
                "RRRB",
                "RRRY");

            BoardResolution.ClearCells(board, MatchFinder.FindMatches(board));

            AssertBoard(board,
                "...G",
                "...B",
                "...Y");
        }

        [Test]
        public void ClearCells_MultipleSeparateMatches_ClearsAllOfThem()
        {
            var board = TestBoards.Create(
                "GGGP",
                "BYOB",
                "PYRB",
                "OYRB");

            BoardResolution.ClearCells(board, MatchFinder.FindMatches(board));

            AssertBoard(board,
                "...P",
                "B.O.",
                "P.R.",
                "O.R.");
        }

        [Test]
        public void ClearCells_EmptyInput_LeavesTheBoardUnchanged()
        {
            var board = TestBoards.Create(
                "RRRB",
                "GBYP");

            BoardResolution.ClearCells(board, new GridPosition[0]);

            AssertBoard(board,
                "RRRB",
                "GBYP");
        }

        [Test]
        public void ClearCells_RepeatedOrAlreadyEmptyPositions_AreHarmless()
        {
            var board = TestBoards.Create("R.GB");
            var positions = new[] { new GridPosition(0, 0), new GridPosition(0, 0), new GridPosition(1, 0) };

            BoardResolution.ClearCells(board, positions);

            AssertBoard(board, "..GB");
        }

        [Test]
        public void ClearCells_NullArguments_Throw()
        {
            var board = TestBoards.Create("RGB");

            Assert.Throws<ArgumentNullException>(() => BoardResolution.ClearCells(null, new GridPosition[0]));
            Assert.Throws<ArgumentNullException>(() => BoardResolution.ClearCells(board, null));
        }

        [Test]
        public void ClearCells_OutOfBoundsPosition_ThrowsWithoutChangingAnyCell()
        {
            var board = TestBoards.Create("RRRB");
            // The bad position is in the middle: the valid cells before it must not be cleared either.
            var positions = new List<GridPosition>
            {
                new GridPosition(0, 0), new GridPosition(4, 0), new GridPosition(1, 0)
            };

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => BoardResolution.ClearCells(board, positions));

            Assert.That(exception.ParamName, Is.EqualTo("positions"));
            AssertBoard(board, "RRRB");
        }

        [Test]
        public void ApplyGravity_NoEmptyCells_ReturnsNoMovesAndChangesNothing()
        {
            var board = TestBoards.Create(
                "RG",
                "BY");

            var moves = BoardResolution.ApplyGravity(board);

            Assert.That(moves, Is.Empty);
            AssertBoard(board,
                "RG",
                "BY");
        }

        [Test]
        public void ApplyGravity_AllEmptyColumn_StaysEmpty()
        {
            var board = TestBoards.Create(
                ".R",
                ".G",
                ".B");

            var moves = BoardResolution.ApplyGravity(board);

            Assert.That(moves, Is.Empty);
            AssertBoard(board,
                ".R",
                ".G",
                ".B");
        }

        [Test]
        public void ApplyGravity_OneGap_TileAboveFallsOneCell()
        {
            var board = TestBoards.Create(
                "G",
                ".",
                "R");

            var moves = BoardResolution.ApplyGravity(board);

            AssertBoard(board,
                ".",
                "G",
                "R");
            Assert.That(moves, Is.EqualTo(new[] { Move(TileType.Green, 0, 2, 0, 1) }));
        }

        [Test]
        public void ApplyGravity_PlanExample_CollapsesAndKeepsOrder()
        {
            // Bottom to top: G . B . R  becomes  G B R . .
            var board = TestBoards.Create(
                "R",
                ".",
                "B",
                ".",
                "G");

            var moves = BoardResolution.ApplyGravity(board);

            AssertBoard(board,
                ".",
                ".",
                "R",
                "B",
                "G");
            Assert.That(moves, Is.EqualTo(new[]
            {
                Move(TileType.Blue, 0, 2, 0, 1),
                Move(TileType.Red, 0, 4, 0, 2)
            }));
        }

        [Test]
        public void ApplyGravity_SeveralGaps_TopTileFallsSeveralCells()
        {
            // Bottom to top: R . . G . . Y
            var board = TestBoards.Create(
                "Y",
                ".",
                ".",
                "G",
                ".",
                ".",
                "R");

            var moves = BoardResolution.ApplyGravity(board);

            AssertBoard(board,
                ".",
                ".",
                ".",
                ".",
                "Y",
                "G",
                "R");
            Assert.That(moves, Is.EqualTo(new[]
            {
                Move(TileType.Green, 0, 3, 0, 1),
                Move(TileType.Yellow, 0, 6, 0, 2)
            }));
        }

        [Test]
        public void ApplyGravity_AlternatingEmptyAndFilled_EveryTileFalls()
        {
            // Bottom to top: . R . G . B
            var board = TestBoards.Create(
                "B",
                ".",
                "G",
                ".",
                "R",
                ".");

            var moves = BoardResolution.ApplyGravity(board);

            AssertBoard(board,
                ".",
                ".",
                ".",
                "B",
                "G",
                "R");
            Assert.That(moves, Is.EqualTo(new[]
            {
                Move(TileType.Red, 0, 1, 0, 0),
                Move(TileType.Green, 0, 3, 0, 1),
                Move(TileType.Blue, 0, 5, 0, 2)
            }));
        }

        [Test]
        public void ApplyGravity_MultipleColumns_EachCollapsesIndependently()
        {
            var board = TestBoards.Create(
                "RG.",
                ".B.",
                "Y.P",
                "..O");

            var moves = BoardResolution.ApplyGravity(board);

            AssertBoard(board,
                "...",
                "...",
                "RGP",
                "YBO");
            // Column by column, bottom to top within each; column 2 was already settled.
            Assert.That(moves, Is.EqualTo(new[]
            {
                Move(TileType.Yellow, 0, 1, 0, 0),
                Move(TileType.Red, 0, 3, 0, 1),
                Move(TileType.Blue, 1, 2, 1, 0),
                Move(TileType.Green, 1, 3, 1, 1)
            }));
        }

        [Test]
        public void ApplyGravity_SameColourTiles_KeepTheirOrder()
        {
            // Bottom to top: . R . R. The board alone can't tell the two reds apart; the moves can.
            var board = TestBoards.Create(
                "R",
                ".",
                "R",
                ".");

            var moves = BoardResolution.ApplyGravity(board);

            // The lower red lands lowest and the upper red lands above it: they never swap places.
            Assert.That(moves, Is.EqualTo(new[]
            {
                Move(TileType.Red, 0, 1, 0, 0),
                Move(TileType.Red, 0, 3, 0, 1)
            }));
        }

        [Test]
        public void ApplyGravity_CalledAgainOnACollapsedBoard_DoesNothing()
        {
            var board = TestBoards.Create(
                "RG.",
                ".B.",
                "Y.P",
                "..O");
            BoardResolution.ApplyGravity(board);

            var secondMoves = BoardResolution.ApplyGravity(board);

            Assert.That(secondMoves, Is.Empty);
            AssertBoard(board,
                "...",
                "...",
                "RGP",
                "YBO");
        }

        [Test]
        public void ApplyGravity_RandomBoards_EmptiesOnTopOrderKeptAndMovesReplayToTheResult()
        {
            var types = new[] { TileType.Empty, TileType.Red, TileType.Green, TileType.Blue };
            var random = new Random(2024);

            for (int boardIndex = 0; boardIndex < 100; boardIndex++)
            {
                var board = new Board(random.Next(1, 9), random.Next(1, 9));
                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        board.SetTile(new GridPosition(x, y), types[random.Next(types.Length)]);
                    }
                }

                var before = Copy(board);
                var moves = BoardResolution.ApplyGravity(board);

                for (int x = 0; x < board.Width; x++)
                {
                    // The column's tiles, bottom to top, ignoring holes, must be the same sequence as before.
                    var beforeTiles = new List<TileType>();
                    var afterTiles = new List<TileType>();
                    bool seenEmpty = false;
                    for (int y = 0; y < board.Height; y++)
                    {
                        var position = new GridPosition(x, y);
                        if (before.GetTile(position) != TileType.Empty)
                        {
                            beforeTiles.Add(before.GetTile(position));
                        }

                        TileType after = board.GetTile(position);
                        if (after == TileType.Empty)
                        {
                            seenEmpty = true;
                        }
                        else
                        {
                            Assert.That(seenEmpty, Is.False, $"Board {boardIndex}: tile above a hole in column {x}.");
                            afterTiles.Add(after);
                        }
                    }

                    Assert.That(afterTiles, Is.EqualTo(beforeTiles), $"Board {boardIndex}: column {x} order changed.");
                }

                // Replaying only the reported moves on the old board must give exactly the new board.
                foreach (var move in moves)
                {
                    Assert.That(move.From.X, Is.EqualTo(move.To.X), $"Board {boardIndex}: {move} changed column.");
                    Assert.That(move.From.Y, Is.GreaterThan(move.To.Y), $"Board {boardIndex}: {move} did not fall.");
                    Assert.That(before.GetTile(move.From), Is.EqualTo(move.Type), $"Board {boardIndex}: {move} wrong type.");
                    before.SetTile(move.To, move.Type);
                    before.SetTile(move.From, TileType.Empty);
                }

                Assert.That(Snapshot(before), Is.EqualTo(Snapshot(board)), $"Board {boardIndex}: moves do not explain the result.");
            }
        }

        [Test]
        public void ApplyGravity_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BoardResolution.ApplyGravity(null));
        }

        private static TileMove Move(TileType type, int fromX, int fromY, int toX, int toY)
        {
            return new TileMove(new GridPosition(fromX, fromY), new GridPosition(toX, toY), type);
        }

        private static Board Copy(Board board)
        {
            var copy = new Board(board.Width, board.Height);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    copy.SetTile(position, board.GetTile(position));
                }
            }

            return copy;
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

        // Compares the whole Board (dimensions and every cell) with an expected text layout.
        private static void AssertBoard(Board board, params string[] expectedRows)
        {
            var expected = TestBoards.Create(expectedRows);
            Assert.That(board.Width, Is.EqualTo(expected.Width));
            Assert.That(board.Height, Is.EqualTo(expected.Height));
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    Assert.That(board.GetTile(position), Is.EqualTo(expected.GetTile(position)), $"Cell ({x}, {y}).");
                }
            }
        }
    }
}
