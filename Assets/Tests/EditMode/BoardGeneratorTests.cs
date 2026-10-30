using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class BoardGeneratorTests
    {
        [TestCase(1, 1)]
        [TestCase(1, 8)]
        [TestCase(8, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 8)]
        [TestCase(8, 2)]
        [TestCase(3, 3)]
        [TestCase(8, 8)]
        [TestCase(13, 11)]
        public void Generate_ThreeColorsAndManySeeds_HaveNoResolvablePatterns(int width, int height)
        {
            var palette = CreatePalette();
            for (int seed = -50; seed <= 50; seed++)
            {
                var board = BoardGenerator.Generate(width, height, palette, seed);
                Assert.That(MatchFinder.FindMatches(board), Is.Empty, $"Seed {seed}.");
                // Independent square check guards against matching and generation sharing the same mistake.
                for (int y = 0; y < height - 1; y++)
                {
                    for (int x = 0; x < width - 1; x++)
                    {
                        TileType color = board.GetTile(new GridPosition(x, y));
                        bool square = board.GetTile(new GridPosition(x + 1, y)) == color
                            && board.GetTile(new GridPosition(x, y + 1)) == color
                            && board.GetTile(new GridPosition(x + 1, y + 1)) == color;
                        Assert.That(square, Is.False, $"Square at ({x},{y}), seed {seed}.");
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
        public void Generate_AllNormalColors_AreAcceptedWithoutModifyingThePalette()
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

        [Test]
        public void GeneratePlayable_ShippedSeed42_KeepsTheExactGenerateLayout()
        {
            var palette = CreateShippedPalette();

            // Already playable, so the first attempt is used: the starting board players see does not change.
            Assert.That(HaveSameTiles(BoardGenerator.GeneratePlayable(8, 8, palette, 42), BoardGenerator.Generate(8, 8, palette, 42)), Is.True);
        }

        [TestCase(8, 42)]
        [TestCase(3, 0)]
        [TestCase(4, 81)]
        public void GeneratePlayable_AcceptedBoard_IsStableAndHasALegalMove(int size, int seed)
        {
            var board = BoardGenerator.GeneratePlayable(size, size, CreateShippedPalette(), seed);

            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.True);
        }

        [TestCase(8, 42)]
        [TestCase(3, 0)]
        public void GeneratePlayable_SameInputs_ProduceTheSameBoard(int size, int seed)
        {
            var palette = CreateShippedPalette();

            Assert.That(HaveSameTiles(
                BoardGenerator.GeneratePlayable(size, size, palette, seed),
                BoardGenerator.GeneratePlayable(size, size, palette, seed)), Is.True);
        }

        [Test]
        public void GeneratePlayable_DeadFirstCandidates_AdvanceToTheFirstPlayableDerivedSeed()
        {
            var palette = CreateShippedPalette();
            // Found by search: on 3x3 with six colours, seeds 0 to 3 generate dead boards and seed 4 is the first playable one.
            for (int seed = 0; seed < 4; seed++)
            {
                Assert.That(BoardPlayability.HasAnyLegalMove(BoardGenerator.Generate(3, 3, palette, seed)), Is.False, $"Seed {seed}.");
            }

            Assert.That(HaveSameTiles(BoardGenerator.GeneratePlayable(3, 3, palette, 0), BoardGenerator.Generate(3, 3, palette, 4)), Is.True);
        }

        [Test]
        public void GeneratePlayable_NoPlayableCandidateWithinTheBudget_ThrowsBudgetExhausted()
        {
            // A 1x1 board has no adjacent pair, so every attempt is dead.
            var exception = Assert.Throws<InvalidOperationException>(
                () => BoardGenerator.GeneratePlayable(1, 1, CreateShippedPalette(), 42));

            Assert.That(exception.Message, Does.Contain("attempt budget was exhausted"));
            Assert.That(exception.Message, Does.Contain("seeds 42 to 51"));
        }

        private static TileType[] CreateShippedPalette()
        {
            return new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange };
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
