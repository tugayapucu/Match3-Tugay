using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    // Every board here is the state AFTER the swap, matching SwapValidator's contract.
    public class SwapValidatorTests
    {
        [Test]
        public void IsValidResult_FirstSwappedCellInMatch_ReturnsTrue()
        {
            // Before the swap: R R B R. Swapping (2,0) and (3,0) brings R into (2,0).
            var board = TestBoards.Create("RRRB");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(2, 0), new GridPosition(3, 0)), Is.True);
        }

        [Test]
        public void IsValidResult_SecondSwappedCellInMatch_ReturnsTrue()
        {
            var board = TestBoards.Create("RRRB");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(3, 0), new GridPosition(2, 0)), Is.True);
        }

        [Test]
        public void IsValidResult_BothSwappedCellsInMatches_ReturnsTrue()
        {
            // Before the swap the left column was G over R; swapping it completes both rows.
            var board = TestBoards.Create(
                "RRR",
                "GGG");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(0, 1), new GridPosition(0, 0)), Is.True);
        }

        [Test]
        public void IsValidResult_NoMatches_ReturnsFalse()
        {
            var board = TestBoards.Create("RGBR");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(1, 0), new GridPosition(2, 0)), Is.False);
        }

        [Test]
        public void IsValidResult_OnlyAnUnrelatedMatch_ReturnsFalse()
        {
            // The board has a match (top row), but the swapped B and G are not part of it.
            var board = TestBoards.Create(
                "RRR...",
                "...BG.");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(3, 0), new GridPosition(4, 0)), Is.False);
        }

        [Test]
        public void IsValidResult_MatchRightNextToTheSwappedCells_ReturnsFalse()
        {
            // (3,0) touches the R run but is a different colour, so it is not part of the match.
            var board = TestBoards.Create("RRRGB");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(3, 0), new GridPosition(4, 0)), Is.False);
        }

        [Test]
        public void IsValidResult_SwappedCellIsAnIntersection_ReturnsTrue()
        {
            // (2,2) is the corner of an L: in both the horizontal and the vertical run.
            var board = TestBoards.Create(
                "RRRG",
                "..R.",
                "..R.");

            Assert.That(SwapValidator.IsValidResult(board, new GridPosition(2, 2), new GridPosition(3, 2)), Is.True);
        }

        [TestCase("RRRRG", 3)]
        [TestCase("RRRRRG", 4)]
        public void IsValidResult_LongRunIncludingASwappedCell_ReturnsTrue(string row, int swappedX)
        {
            var board = TestBoards.Create(row);

            Assert.That(
                SwapValidator.IsValidResult(board, new GridPosition(swappedX, 0), new GridPosition(swappedX + 1, 0)),
                Is.True);
        }

        [Test]
        public void IsValidResult_DoesNotChangeTheBoardAndRepeatsTheSameAnswer()
        {
            var board = TestBoards.Create(
                "RRR...",
                "...BG.");
            var first = new GridPosition(3, 0);
            var second = new GridPosition(4, 0);

            bool firstAnswer = SwapValidator.IsValidResult(board, first, second);
            bool secondAnswer = SwapValidator.IsValidResult(board, first, second);

            Assert.That(secondAnswer, Is.EqualTo(firstAnswer));
            Assert.That(board.GetTile(first), Is.EqualTo(TileType.Blue));
            Assert.That(board.GetTile(second), Is.EqualTo(TileType.Green));
            Assert.That(board.GetTile(new GridPosition(0, 1)), Is.EqualTo(TileType.Red));
        }

        [Test]
        public void IsValidResult_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SwapValidator.IsValidResult(null, new GridPosition(0, 0), new GridPosition(1, 0)));
        }

        [TestCase(-1, 0)]
        [TestCase(4, 0)]
        [TestCase(0, 1)]
        public void IsValidResult_FirstOutOfBounds_Throws(int x, int y)
        {
            var board = TestBoards.Create("RGBR");

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => SwapValidator.IsValidResult(board, new GridPosition(x, y), new GridPosition(0, 0)));

            Assert.That(exception.ParamName, Is.EqualTo("first"));
        }

        [Test]
        public void IsValidResult_SecondOutOfBounds_Throws()
        {
            var board = TestBoards.Create("RGBR");

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => SwapValidator.IsValidResult(board, new GridPosition(3, 0), new GridPosition(4, 0)));

            Assert.That(exception.ParamName, Is.EqualTo("second"));
        }
    }
}
