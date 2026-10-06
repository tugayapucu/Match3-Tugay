using System;
using System.Collections.Generic;

namespace Match3.Core
{
    public static class BoardGenerator
    {
        public static Board Generate(int width, int height, IReadOnlyList<TileType> palette, int seed)
        {
            ValidatePalette(palette);

            var board = new Board(width, height);
            var random = new Random(seed);
            var candidates = new TileType[palette.Count];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TileType excludedHorizontal = TileType.Empty;
                    if (x >= 2)
                    {
                        var previous = board.GetTile(new GridPosition(x - 1, y));
                        if (previous == board.GetTile(new GridPosition(x - 2, y)))
                        {
                            excludedHorizontal = previous;
                        }
                    }

                    TileType excludedVertical = TileType.Empty;
                    if (y >= 2)
                    {
                        var previous = board.GetTile(new GridPosition(x, y - 1));
                        if (previous == board.GetTile(new GridPosition(x, y - 2)))
                        {
                            excludedVertical = previous;
                        }
                    }

                    int candidateCount = 0;
                    for (int i = 0; i < palette.Count; i++)
                    {
                        var tileType = palette[i];
                        if (tileType != excludedHorizontal && tileType != excludedVertical)
                        {
                            candidates[candidateCount] = tileType;
                            candidateCount++;
                        }
                    }

                    // At most two types are excluded, so the validated palette always leaves a choice.
                    var chosenType = candidates[random.Next(candidateCount)];
                    board.SetTile(new GridPosition(x, y), chosenType);
                }
            }

            return board;
        }

        private static void ValidatePalette(IReadOnlyList<TileType> palette)
        {
            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            if (palette.Count < 3)
            {
                throw new ArgumentException("Palette must contain at least three distinct playable types.", nameof(palette));
            }

            var seenTypes = new HashSet<TileType>();
            for (int i = 0; i < palette.Count; i++)
            {
                var tileType = palette[i];
                if (tileType == TileType.Empty || !Enum.IsDefined(typeof(TileType), tileType))
                {
                    throw new ArgumentException("Palette entries must be defined playable tile types.", nameof(palette));
                }

                if (!seenTypes.Add(tileType))
                {
                    throw new ArgumentException("Palette entries must be distinct.", nameof(palette));
                }
            }
        }
    }
}
