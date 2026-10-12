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
