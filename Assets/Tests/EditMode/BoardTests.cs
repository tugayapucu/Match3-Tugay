using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class BoardTests
    {
        [TestCase(8, 8)]
        [TestCase(2, 5)]
        [TestCase(5, 2)]
        [TestCase(1, 1)]
        public void Constructor_PositiveDimensions_ArePreserved(int width, int height)
        {
            var board = new Board(width, height);

            Assert.That(board.Width, Is.EqualTo(width));
            Assert.That(board.Height, Is.EqualTo(height));
        }

        [TestCase(8, 8)]
        [TestCase(2, 5)]
        [TestCase(5, 2)]
        [TestCase(1, 1)]
        public void Constructor_AllCellsStartEmpty(int width, int height)
        {
            var board = new Board(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Assert.That(board.GetTile(new GridPosition(x, y)), Is.EqualTo(TileType.Empty));
                }
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonpositiveWidth_Throws(int width)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Board(width, 3));

            Assert.That(exception.ParamName, Is.EqualTo("width"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonpositiveHeight_Throws(int height)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Board(3, height));

            Assert.That(exception.ParamName, Is.EqualTo("height"));
        }

        [TestCase(8, 8, 0, 0)]
        [TestCase(8, 8, 7, 7)]
        [TestCase(2, 5, 1, 4)]
        [TestCase(5, 2, 4, 1)]
        [TestCase(1, 1, 0, 0)]
        public void SetTile_ValidPosition_CanBeReadAndOverwritten(int width, int height, int x, int y)
        {
            var board = new Board(width, height);
            var position = new GridPosition(x, y);

            board.SetTile(position, TileType.Red);
            Assert.That(board.GetTile(position), Is.EqualTo(TileType.Red));

            board.SetTile(position, TileType.Blue);
            Assert.That(board.GetTile(position), Is.EqualTo(TileType.Blue));

            board.SetTile(position, TileType.Empty);
            Assert.That(board.GetTile(position), Is.EqualTo(TileType.Empty));
        }

        [Test]
        public void SetTile_ChangesOnlyTheSpecifiedCell()
        {
            var board = new Board(3, 2);
            var changedPosition = new GridPosition(1, 0);

            board.SetTile(changedPosition, TileType.Green);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    var expected = position == changedPosition ? TileType.Green : TileType.Empty;
                    Assert.That(board.GetTile(position), Is.EqualTo(expected));
                }
            }
        }

        [Test]
        public void SeparateBoards_HaveIndependentStorage()
        {
            var first = new Board(2, 3);
            var second = new Board(2, 3);
            var position = new GridPosition(1, 2);

            first.SetTile(position, TileType.Purple);
            second.SetTile(position, TileType.Orange);

            Assert.That(first.GetTile(position), Is.EqualTo(TileType.Purple));
            Assert.That(second.GetTile(position), Is.EqualTo(TileType.Orange));
        }

        [TestCase(0, 0, true)]
        [TestCase(2, 4, true)]
        [TestCase(-1, 0, false)]
        [TestCase(0, -1, false)]
        [TestCase(3, 0, false)]
        [TestCase(0, 5, false)]
        public void IsInBounds_ChecksBothAxes(int x, int y, bool expected)
        {
            var board = new Board(3, 5);

            Assert.That(board.IsInBounds(new GridPosition(x, y)), Is.EqualTo(expected));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(3, 0)]
        [TestCase(0, 5)]
        public void GetTile_InvalidPosition_Throws(int x, int y)
        {
            var board = new Board(3, 5);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => board.GetTile(new GridPosition(x, y)));

            Assert.That(exception.ParamName, Is.EqualTo("position"));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(3, 0)]
        [TestCase(0, 5)]
        public void SetTile_InvalidPosition_ThrowsWithoutChangingExistingTiles(int x, int y)
        {
            var board = new Board(3, 5);
            var existingPosition = new GridPosition(1, 2);
            board.SetTile(existingPosition, TileType.Yellow);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => board.SetTile(new GridPosition(x, y), TileType.Red));

            Assert.That(exception.ParamName, Is.EqualTo("position"));
            for (int row = 0; row < board.Height; row++)
            {
                for (int column = 0; column < board.Width; column++)
                {
                    var position = new GridPosition(column, row);
                    var expected = position == existingPosition ? TileType.Yellow : TileType.Empty;
                    Assert.That(board.GetTile(position), Is.EqualTo(expected));
                }
            }
        }
    }
}
