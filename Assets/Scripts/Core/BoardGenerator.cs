using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Builds a fully filled Board with no resolvable lines or same-color 2x2 squares.
    public static class BoardGenerator
    {
        // Total attempts for a playable board: seed + 0 through seed + 9.
        private const int PlayableAttempts = 10;

        // Like Generate, but the board also has at least one legal move. The first attempt is exactly Generate(seed),
        // so an already-playable board is returned unchanged; otherwise the derived seeds seed + 1, seed + 2, ... are tried.
        // Deterministic, and throws only at session setup (never during a move) if every attempt is dead.
        public static Board GeneratePlayable(int width, int height, IReadOnlyList<TileType> palette, int seed)
        {
            for (int attempt = 0; attempt < PlayableAttempts; attempt++)
            {
                var candidate = Generate(width, height, palette, unchecked(seed + attempt));
                if (BoardPlayability.HasAnyLegalMove(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"The playable-board generation attempt budget was exhausted: seeds {seed} to {unchecked(seed + PlayableAttempts - 1)} "
                + "produced no board with a legal move.");
        }

        // Same width/height/palette order/seed always produce the same board (reproducible for tests and bugs).
        public static Board Generate(int width, int height, IReadOnlyList<TileType> palette, int seed)
        {
            // 3+ types are required so the exclusion rule below always leaves a valid choice.
            PaletteValidation.Validate(palette);

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

                    // This cell is a square's top-right corner; its other three cells are already filled.
                    TileType excludedSquare = TileType.Empty;
                    if (x >= 1 && y >= 1)
                    {
                        var left = board.GetTile(new GridPosition(x - 1, y));
                        if (left == board.GetTile(new GridPosition(x, y - 1))
                            && left == board.GetTile(new GridPosition(x - 1, y - 1)))
                        {
                            excludedSquare = left;
                        }
                    }

                    // Collect every palette type that is not excluded.
                    int candidateCount = 0;
                    for (int i = 0; i < palette.Count; i++)
                    {
                        var tileType = palette[i];
                        if (tileType != excludedHorizontal && tileType != excludedVertical && tileType != excludedSquare)
                        {
                            candidates[candidateCount] = tileType;
                            candidateCount++;
                        }
                    }

                    // A square exclusion equals any line exclusion present here, so at most two colors are blocked.
                    var chosenType = candidates[random.Next(candidateCount)];
                    board.SetTile(new GridPosition(x, y), chosenType);
                }
            }

            return board;
        }
    }
}
