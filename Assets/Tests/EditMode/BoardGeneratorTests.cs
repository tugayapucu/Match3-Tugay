using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class BoardGeneratorTests
    {
        [TestCase(8, 8)]
        [TestCase(2, 7)]
        [TestCase(7, 2)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(1, 8)]
        [TestCase(8, 1)]
        [TestCase(3, 3)]
        public void Generate_FillsEveryCellUsingThePalette(int width, int height)
        {
            var palette = CreatePalette();
            var board = BoardGenerator.Generate(width, height, palette, 123);

            Assert.That(board.Width, Is.EqualTo(width));
            Assert.That(board.Height, Is.EqualTo(height));
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Assert.That(palette, Does.Contain(board.GetTile(new GridPosition(x, y))));
                }
            }
        }

        [TestCase(0)]
        [TestCase(-123)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void Generate_SameInputs_ProduceTheSameBoard(int seed)
        {
            var palette = CreatePalette();
            var first = BoardGenerator.Generate(8, 8, palette, seed);
            var second = BoardGenerator.Generate(8, 8, palette, seed);

            Assert.That(HaveSameTiles(first, second), Is.True);
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceMoreThanOneLayout()
        {
            var palette = CreatePalette();
            var first = BoardGenerator.Generate(8, 8, palette, 0);
            bool foundDifferentLayout = false;

            for (int seed = 1; seed < 16; seed++)
            {
                var board = BoardGenerator.Generate(8, 8, palette, seed);
                if (!HaveSameTiles(first, board))
                {
                    foundDifferentLayout = true;
                    break;
                }
            }

            Assert.That(foundDifferentLayout, Is.True);
        }

        [Test]
        public void Generate_AllPlayableTypes_AreAcceptedWithoutModifyingThePalette()
        {
            var palette = new[]
            {
                TileType.Orange, TileType.Purple, TileType.Yellow,
                TileType.Blue, TileType.Green, TileType.Red
            };
            var original = (TileType[])palette.Clone();
            var board = BoardGenerator.Generate(8, 8, palette, 42);

            Assert.That(palette, Is.EqualTo(original));
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(palette, Does.Contain(board.GetTile(new GridPosition(x, y))));
                }
            }
        }

        [Test]
        public void Generate_SelectedPalette_RestrictsTheGeneratedTypes()
        {
            var palette = new[] { TileType.Yellow, TileType.Purple, TileType.Orange };
            var board = BoardGenerator.Generate(5, 7, palette, 42);

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Assert.That(palette, Does.Contain(board.GetTile(new GridPosition(x, y))));
                }
            }
        }

        [TestCase(8, 8)]
        [TestCase(2, 7)]
        [TestCase(7, 2)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(1, 8)]
        [TestCase(8, 1)]
        [TestCase(3, 3)]
        public void Generate_MultipleSeeds_HaveNoHorizontalMatches(int width, int height)
        {
            var palette = CreatePalette();
            for (int seed = -25; seed < 25; seed++)
            {
                var board = BoardGenerator.Generate(width, height, palette, seed);
                for (int y = 0; y < height; y++)
                {
                    TileType previous = TileType.Empty;
                    int runLength = 0;
                    for (int x = 0; x < width; x++)
                    {
                        var current = board.GetTile(new GridPosition(x, y));
                        runLength = current == previous ? runLength + 1 : 1;
                        Assert.That(runLength, Is.LessThan(3), $"Horizontal run at ({x}, {y}), seed {seed}.");
                        previous = current;
                    }
                }
            }
        }

        [TestCase(8, 8)]
        [TestCase(2, 7)]
        [TestCase(7, 2)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(1, 8)]
        [TestCase(8, 1)]
        [TestCase(3, 3)]
        public void Generate_MultipleSeeds_HaveNoVerticalMatches(int width, int height)
        {
            var palette = CreatePalette();
            for (int seed = -25; seed < 25; seed++)
            {
                var board = BoardGenerator.Generate(width, height, palette, seed);
                for (int x = 0; x < width; x++)
                {
                    TileType previous = TileType.Empty;
                    int runLength = 0;
                    for (int y = 0; y < height; y++)
                    {
                        var current = board.GetTile(new GridPosition(x, y));
                        runLength = current == previous ? runLength + 1 : 1;
                        Assert.That(runLength, Is.LessThan(3), $"Vertical run at ({x}, {y}), seed {seed}.");
                        previous = current;
                    }
                }
            }
        }

        [Test]
        public void Generate_NullPalette_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BoardGenerator.Generate(8, 8, null, 42));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Generate_FewerThanThreePaletteEntries_Throws(int count)
        {
            var palette = new TileType[count];
            Array.Copy(CreatePalette(), palette, count);

            Assert.Throws<ArgumentException>(() => BoardGenerator.Generate(8, 8, palette, 42));
        }

        [Test]
        public void Generate_EmptyInPalette_Throws()
        {
            var palette = new[] { TileType.Red, TileType.Green, TileType.Empty };

            Assert.Throws<ArgumentException>(() => BoardGenerator.Generate(8, 8, palette, 42));
        }

        [Test]
        public void Generate_DuplicatePaletteEntry_Throws()
        {
            var palette = new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Red };

            Assert.Throws<ArgumentException>(() => BoardGenerator.Generate(8, 8, palette, 42));
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void Generate_UndefinedPaletteEntry_Throws(int value)
        {
            var palette = new[] { TileType.Red, TileType.Green, (TileType)value };

            Assert.Throws<ArgumentException>(() => BoardGenerator.Generate(8, 8, palette, 42));
        }

        [TestCase(0, 8, "width")]
        [TestCase(-1, 8, "width")]
        [TestCase(8, 0, "height")]
        [TestCase(8, -1, "height")]
        public void Generate_InvalidDimensions_Throws(int width, int height, string parameterName)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => BoardGenerator.Generate(width, height, CreatePalette(), 42));

            Assert.That(exception.ParamName, Is.EqualTo(parameterName));
        }

        private static TileType[] CreatePalette()
        {
            return new[] { TileType.Red, TileType.Green, TileType.Blue };
        }

        private static bool HaveSameTiles(Board first, Board second)
        {
            if (first.Width != second.Width || first.Height != second.Height)
            {
                return false;
            }

            for (int y = 0; y < first.Height; y++)
            {
                for (int x = 0; x < first.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (first.GetTile(position) != second.GetTile(position))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
