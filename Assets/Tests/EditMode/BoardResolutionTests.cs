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

        [Test]
        public void Refill_FullBoard_ReturnsNoSpawnsAndChangesNothing()
        {
            var board = TestBoards.Create(
                "RG",
                "BY");

            var spawns = BoardResolution.Refill(board, RGB, new ScriptedRandom());

            Assert.That(spawns, Is.Empty);
            AssertBoard(board,
                "RG",
                "BY");
        }

        [Test]
        public void Refill_AllEmptyBoard_FillsEveryCellColumnByColumnBottomToTop()
        {
            var board = new Board(3, 2);
            // Palette indices 0,1,2 = R,G,B, consumed in fill order.
            var random = new ScriptedRandom(0, 1, 2, 0, 1, 2);

            var spawns = BoardResolution.Refill(board, RGB, random);

            Assert.That(spawns, Is.EqualTo(new[]
            {
                Spawn(TileType.Red, 0, 0), Spawn(TileType.Green, 0, 1),
                Spawn(TileType.Blue, 1, 0), Spawn(TileType.Red, 1, 1),
                Spawn(TileType.Green, 2, 0), Spawn(TileType.Blue, 2, 1)
            }));
            AssertBoard(board,
                "GRB",
                "RBG");
        }

        [Test]
        public void Refill_PartiallyEmptyBoard_FillsOnlyTheEmptyCells()
        {
            var board = TestBoards.Create(
                "..P",
                "G.Y",
                "RBO");
            var random = new ScriptedRandom(1, 2, 0);

            var spawns = BoardResolution.Refill(board, RGB, random);

            // Existing tiles (including P, Y, O outside the palette) are untouched.
            Assert.That(spawns, Is.EqualTo(new[]
            {
                Spawn(TileType.Green, 0, 2),
                Spawn(TileType.Blue, 1, 1),
                Spawn(TileType.Red, 1, 2)
            }));
            AssertBoard(board,
                "GRP",
                "GBY",
                "RBO");
        }

        [Test]
        public void Refill_CanCreateAMatch()
        {
            // Refill must not avoid matches: here it is forced to complete R R _.
            var board = TestBoards.Create("RR.");

            BoardResolution.Refill(board, RGB, new ScriptedRandom(0));

            AssertBoard(board, "RRR");
            Assert.That(MatchFinder.FindMatches(board), Does.Contain(new GridPosition(2, 0)));
        }

        [Test]
        public void Refill_SameSeedTwice_ProducesTheSameSpawnsAndBoard()
        {
            var first = TestBoards.Create(
                "....",
                "R..G",
                "BY.R");
            var second = Copy(first);

            var firstSpawns = BoardResolution.Refill(first, RGB, new Random(42));
            var secondSpawns = BoardResolution.Refill(second, RGB, new Random(42));

            Assert.That(secondSpawns, Is.EqualTo(firstSpawns));
            Assert.That(Snapshot(second), Is.EqualTo(Snapshot(first)));
        }

        [Test]
        public void Refill_RandomBoards_SpawnsExactlyMatchTheFilledCells()
        {
            var layoutTypes = new[] { TileType.Empty, TileType.Red, TileType.Yellow, TileType.Purple };
            var layoutRandom = new Random(7);
            var refillRandom = new Random(99);

            for (int boardIndex = 0; boardIndex < 50; boardIndex++)
            {
                var board = new Board(layoutRandom.Next(1, 9), layoutRandom.Next(1, 9));
                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        board.SetTile(new GridPosition(x, y), layoutTypes[layoutRandom.Next(layoutTypes.Length)]);
                    }
                }

                var before = Copy(board);
                var spawns = BoardResolution.Refill(board, RGB, refillRandom);

                // Expected spawn cells: every originally Empty cell, in fill order.
                var expectedPositions = new List<GridPosition>();
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        var position = new GridPosition(x, y);
                        if (before.GetTile(position) == TileType.Empty)
                        {
                            expectedPositions.Add(position);
                        }
                        else
                        {
                            Assert.That(board.GetTile(position), Is.EqualTo(before.GetTile(position)),
                                $"Board {boardIndex}: existing tile at ({x},{y}) changed.");
                        }

                        Assert.That(board.GetTile(position), Is.Not.EqualTo(TileType.Empty),
                            $"Board {boardIndex}: ({x},{y}) still Empty.");
                    }
                }

                var spawnPositions = new List<GridPosition>();
                foreach (var spawn in spawns)
                {
                    spawnPositions.Add(spawn.Position);
                    Assert.That(RGB, Does.Contain(spawn.Type), $"Board {boardIndex}: {spawn} not from the palette.");
                    Assert.That(board.GetTile(spawn.Position), Is.EqualTo(spawn.Type), $"Board {boardIndex}: {spawn} disagrees with the Board.");
                }

                Assert.That(spawnPositions, Is.EqualTo(expectedPositions), $"Board {boardIndex}.");
            }
        }

        [Test]
        public void Refill_InvalidPalette_ThrowsLikeBoardGeneratorAndChangesNothing()
        {
            AssertRefillRejects<ArgumentNullException>(null);
            AssertRefillRejects<ArgumentException>(new[] { TileType.Red, TileType.Green });
            AssertRefillRejects<ArgumentException>(new[] { TileType.Red, TileType.Green, TileType.Empty });
            AssertRefillRejects<ArgumentException>(new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Red });
            AssertRefillRejects<ArgumentException>(new[] { TileType.Red, TileType.Green, (TileType)99 });
        }

        [Test]
        public void Refill_NullBoardOrRandom_Throws()
        {
            var board = TestBoards.Create("R.");

            Assert.Throws<ArgumentNullException>(() => BoardResolution.Refill(null, RGB, new Random(1)));
            Assert.Throws<ArgumentNullException>(() => BoardResolution.Refill(board, RGB, null));
            AssertBoard(board, "R.");
        }

        private static readonly TileType[] RGB = { TileType.Red, TileType.Green, TileType.Blue };

        // Both the exception type and "nothing changed" must match BoardGenerator's palette behaviour.
        private static void AssertRefillRejects<TException>(TileType[] palette) where TException : Exception
        {
            var board = TestBoards.Create(
                "R.",
                ".G");

            Assert.Throws<TException>(() => BoardResolution.Refill(board, palette, new Random(1)));
            Assert.Throws<TException>(() => BoardGenerator.Generate(2, 2, palette, 1));
            AssertBoard(board,
                "R.",
                ".G");
        }

        private static TileSpawn Spawn(TileType type, int x, int y)
        {
            return new TileSpawn(new GridPosition(x, y), type);
        }

        // A Random whose results are scripted, so tests can force exact refill choices.
        // Overrides every integer overload, so the test does not depend on which one Refill calls.
        private sealed class ScriptedRandom : Random
        {
            private readonly Queue<int> values;

            public ScriptedRandom(params int[] values)
            {
                this.values = new Queue<int>(values);
            }

            public override int Next()
            {
                return NextScripted(int.MaxValue);
            }

            public override int Next(int maxValue)
            {
                return NextScripted(maxValue);
            }

            public override int Next(int minValue, int maxValue)
            {
                return minValue + NextScripted(maxValue - minValue);
            }

            private int NextScripted(int range)
            {
                Assert.That(values.Count, Is.GreaterThan(0), "ScriptedRandom ran out of scripted values.");
                int value = values.Dequeue();
                Assert.That(value, Is.InRange(0, range - 1), "Scripted value outside the requested range.");
                return value;
            }
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
