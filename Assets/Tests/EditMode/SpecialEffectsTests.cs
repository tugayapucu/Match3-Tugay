using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialEffectsTests
    {
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

        [TestCase(TileType.Propeller)]
        [TestCase(TileType.LightBall)]
        public void DeferredEffects_AreRejectedExplicitly(TileType type)
        {
            var board = new Board(1, 1);
            board.SetTile(new GridPosition(0, 0), type);
            Assert.Throws<NotSupportedException>(() => SpecialEffects.GetFootprint(board, new GridPosition(0, 0)));
        }
    }
}
