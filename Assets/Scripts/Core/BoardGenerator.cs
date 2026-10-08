using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Builds a new, fully filled Board that starts with no 3-in-a-row matches.
    public static class BoardGenerator
    {
        // Same width/height/palette order/seed always produce the same board (reproducible for tests and bugs).
        public static Board Generate(int width, int height, IReadOnlyList<TileType> palette, int seed)
        {
            ValidatePalette(palette);

            var board = new Board(width, height);
            // Seeded System.Random (not UnityEngine.Random) keeps Core engine-free and deterministic.
            var random = new Random(seed);
            // Reused scratch buffer for the allowed types of each cell, avoiding per-cell allocations.
            var candidates = new TileType[palette.Count];

            // Fill bottom-to-top, left-to-right, so the cells left of and below (x, y) are already set.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // If the two cells to the left share a type, placing that type here would make a horizontal 3.
                    TileType excludedHorizontal = TileType.Empty;
                    if (x >= 2)
                    {
                        var previous = board.GetTile(new GridPosition(x - 1, y));
                        if (previous == board.GetTile(new GridPosition(x - 2, y)))
                        {
                            excludedHorizontal = previous;
                        }
                    }

                    // Same rule for the two cells below, preventing a vertical 3.
                    TileType excludedVertical = TileType.Empty;
                    if (y >= 2)
                    {
                        var previous = board.GetTile(new GridPosition(x, y - 1));
                        if (previous == board.GetTile(new GridPosition(x, y - 2)))
                        {
                            excludedVertical = previous;
                        }
                    }

                    // Collect every palette type that is not excluded.
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

        // Needs 3+ distinct playable types, otherwise the exclusion rule could leave no valid choice.
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
                // Rejects Empty and casted garbage like (TileType)99.
                if (tileType == TileType.Empty || !Enum.IsDefined(typeof(TileType), tileType))
                {
                    throw new ArgumentException("Palette entries must be defined playable tile types.", nameof(palette));
                }

                // HashSet.Add returns false when the value is already present.
                if (!seenTypes.Add(tileType))
                {
                    throw new ArgumentException("Palette entries must be distinct.", nameof(palette));
                }
            }
        }
    }
}
