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

        [TestCase(0, 0, 1, 0)]
        [TestCase(1, 0, 0, 0)]
        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(2, 1, 1, 1)]
        public void TrySwapAdjacent_Neighbors_SwapsOnlyTheTwoCells(
            int firstX, int firstY, int secondX, int secondY)
        {
            var board = CreateSwapTestBoard();
            var before = ReadTiles(board);
            var first = new GridPosition(firstX, firstY);
            var second = new GridPosition(secondX, secondY);

            bool swapped = board.TrySwapAdjacent(first, second);

            Assert.That(swapped, Is.True);
            // Build the expected result from the original values, not the swapped board.
            TileType originalFirst = before[first.X, first.Y];
            before[first.X, first.Y] = before[second.X, second.Y];
            before[second.X, second.Y] = originalFirst;
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [TestCase(0, 0, 0, 0)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(1, 1, 0, 0)]
        [TestCase(0, 0, 2, 0)]
        [TestCase(2, 0, 0, 0)]
        [TestCase(0, 0, 2, 1)]
        public void TrySwapAdjacent_NonNeighbors_ReturnsFalseWithoutChangingTheBoard(
            int firstX, int firstY, int secondX, int secondY)
        {
            var board = CreateSwapTestBoard();
            var before = ReadTiles(board);

            bool swapped = board.TrySwapAdjacent(
                new GridPosition(firstX, firstY), new GridPosition(secondX, secondY));

            Assert.That(swapped, Is.False);
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [TestCase(-1, 0, true)]
        [TestCase(0, -1, true)]
        [TestCase(3, 0, true)]
        [TestCase(0, 2, true)]
        [TestCase(-1, 0, false)]
        [TestCase(0, -1, false)]
        [TestCase(3, 0, false)]
        [TestCase(0, 2, false)]
        [TestCase(int.MinValue, 0, true)]
        [TestCase(int.MaxValue, 0, false)]
        public void TrySwapAdjacent_InvalidPosition_ThrowsWithoutChangingTheBoard(
            int x, int y, bool invalidFirst)
        {
            var board = CreateSwapTestBoard();
            var before = ReadTiles(board);
            var invalid = new GridPosition(x, y);
            var valid = new GridPosition(1, 0);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                board.TrySwapAdjacent(invalidFirst ? invalid : valid, invalidFirst ? valid : invalid));

            Assert.That(exception.ParamName, Is.EqualTo("position"));
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [TestCase(2, 1, TileType.Red, TileType.Blue)]
        [TestCase(1, 2, TileType.Red, TileType.Blue)]
        [TestCase(2, 1, TileType.Red, TileType.Red)]
        [TestCase(2, 1, TileType.Red, TileType.Empty)]
        [TestCase(2, 1, TileType.Empty, TileType.Red)]
        [TestCase(2, 1, TileType.Empty, TileType.Empty)]
        public void TrySwapAdjacent_SmallBoard_AcceptsNeighborsRegardlessOfContents(
            int width, int height, TileType firstType, TileType secondType)
        {
            var board = new Board(width, height);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(width - 1, height - 1);
            board.SetTile(first, firstType);
            board.SetTile(second, secondType);

            Assert.That(board.TrySwapAdjacent(first, second), Is.True);
            Assert.That(board.GetTile(first), Is.EqualTo(secondType));
            Assert.That(board.GetTile(second), Is.EqualTo(firstType));
        }

        [Test]
        public void TrySwapAdjacent_DistantVerticalCells_ReturnsFalseWithoutChangingTheBoard()
        {
            var board = new Board(1, 3);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(0, 2);
            board.SetTile(first, TileType.Red);
            board.SetTile(second, TileType.Blue);
            var before = ReadTiles(board);

            Assert.That(board.TrySwapAdjacent(first, second), Is.False);
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [Test]
        public void TrySwapAdjacent_SingleCell_ReturnsFalseAndPreservesItsTile()
        {
            var board = new Board(1, 1);
            var position = new GridPosition(0, 0);
            board.SetTile(position, TileType.Red);

            Assert.That(board.TrySwapAdjacent(position, position), Is.False);
            Assert.That(board.GetTile(position), Is.EqualTo(TileType.Red));
        }

        private static Board CreateSwapTestBoard()
        {
            var board = new Board(3, 2);
            var types = new[]
            {
                TileType.Red, TileType.Green, TileType.Blue,
                TileType.Yellow, TileType.Purple, TileType.Orange
            };
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), types[x + y * board.Width]);
                }
            }

            return board;
        }

        private static TileType[,] ReadTiles(Board board)
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
