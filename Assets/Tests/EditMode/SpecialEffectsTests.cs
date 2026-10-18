using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialEffectsTests
    {
        [TestCase(7, 7, 3, 3, 25)]
        [TestCase(7, 7, 3, 0, 15)]
        [TestCase(7, 7, 0, 0, 9)]
        [TestCase(7, 7, 6, 6, 9)]
        [TestCase(1, 1, 0, 0, 1)]
        [TestCase(2, 3, 0, 1, 6)]
        [TestCase(8, 4, 7, 3, 9)]
        public void Tnt_ExactSquareIsClippedToBoardBounds(int width, int height, int x, int y, int count)
        {
            var board = new Board(width, height);
            var origin = new GridPosition(x, y);
            board.SetTile(origin, TileType.TNT);
            var expected = new List<GridPosition>();
            // Independent distance rule, rather than copying the production offset loops.
            for (int cellY = 0; cellY < height; cellY++)
                for (int cellX = 0; cellX < width; cellX++)
                    if (Math.Max(Math.Abs(cellX - x), Math.Abs(cellY - y)) <= 2)
                        expected.Add(new GridPosition(cellX, cellY));

            var footprint = SpecialEffects.GetFootprint(board, origin);

            Assert.That(footprint.Count, Is.EqualTo(count));
            Assert.That(footprint, Is.EqualTo(expected));
            Assert.That(board.GetTile(origin), Is.EqualTo(TileType.TNT));
        }

        [Test]
        public void TntAfterSwap_IsCenteredOnItsNewCell()
        {
            var board = new Board(7, 7);
            var oldCell = new GridPosition(3, 2);
            var newCell = new GridPosition(3, 3);
            board.SetTile(oldCell, TileType.TNT);
            board.SetTile(newCell, TileType.Blue);
            board.TrySwapAdjacent(oldCell, newCell);

            var footprint = SpecialEffects.GetFootprint(board, newCell);

            Assert.That(footprint, Does.Contain(new GridPosition(3, 5)));
            Assert.That(footprint, Has.No.Member(new GridPosition(3, 0)));
        }

        [TestCase(TileType.HorizontalRocket, 0, 0)]
        [TestCase(TileType.HorizontalRocket, 4, 2)]
        [TestCase(TileType.HorizontalRocket, 2, 1)]
        [TestCase(TileType.VerticalRocket, 0, 0)]
        [TestCase(TileType.VerticalRocket, 4, 2)]
        [TestCase(TileType.VerticalRocket, 2, 1)]
        public void Rocket_ExactRowOrColumnIncludesEdgesInCoordinateOrder(TileType type, int x, int y)
        {
            var board = new Board(5, 3);
            var origin = new GridPosition(x, y);
            board.SetTile(origin, type);
            var expected = new List<GridPosition>();
            int length = type == TileType.HorizontalRocket ? board.Width : board.Height;
            for (int i = 0; i < length; i++)
                expected.Add(type == TileType.HorizontalRocket ? new GridPosition(i, y) : new GridPosition(x, i));

            Assert.That(SpecialEffects.GetFootprint(board, origin), Is.EqualTo(expected));
            Assert.That(board.GetTile(origin), Is.EqualTo(type), "Calculating an effect must not consume it.");
        }

        [Test]
        public void RocketMovedVertically_UsesNewRowInsteadOfOldRow()
        {
            var board = TestBoards.Create("GBG", "RHB");
            var oldCell = new GridPosition(1, 0);
            var newCell = new GridPosition(1, 1);
            board.TrySwapAdjacent(oldCell, newCell);

            Assert.That(SpecialEffects.GetFootprint(board, newCell), Is.EqualTo(new[]
            {
                new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(2, 1)
            }));
            Assert.That(board.GetTile(oldCell), Is.EqualTo(TileType.Blue));
        }

        [Test]
        public void Footprint_InvalidInputFailsWithoutChangingBoard()
        {
            var board = TestBoards.Create("R.");
            Assert.Throws<ArgumentNullException>(() => SpecialEffects.GetFootprint(null, new GridPosition(0, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpecialEffects.GetFootprint(board, new GridPosition(-1, 0)));
            Assert.Throws<ArgumentException>(() => SpecialEffects.GetFootprint(board, new GridPosition(0, 0)));
            Assert.Throws<ArgumentException>(() => SpecialEffects.GetFootprint(board, new GridPosition(1, 0)));
            Assert.That(board.GetTile(new GridPosition(0, 0)), Is.EqualTo(TileType.Red));
        }

    }
}
