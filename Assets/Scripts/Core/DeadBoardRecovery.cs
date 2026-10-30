using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Replaces a dead board (stable, but no legal move) with a playable one, in place, during play.
    // Unlike BoardGenerator.GeneratePlayable it never gives up by throwing: running out of attempts falls back
    // to a known playable board, because this runs while a move still holds the input lock.
    public static class DeadBoardRecovery
    {
        // Generation-seed draws before falling back.
        private const int GenerationAttempts = 10;

        // Returns false and draws nothing from `random` when the board already has a legal move.
        // Otherwise draws up to ten generation seeds from `random`; the first candidate with no match and a legal move
        // is copied into `board`, else `fallback` is. The Board object itself is kept, so every holder sees the new tiles.
        public static bool RecoverIfDead(Board board, IReadOnlyList<TileType> palette, Random random, Board fallback)
        {
            // Check every input before drawing any randomness or touching the board.
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            PaletteValidation.Validate(palette);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (fallback == null)
            {
                throw new ArgumentNullException(nameof(fallback));
            }

            if (fallback.Width != board.Width || fallback.Height != board.Height)
            {
                throw new ArgumentException("The fallback board must have the same width and height.", nameof(fallback));
            }

            if (BoardPlayability.HasAnyLegalMove(board))
            {
                return false;
            }

            for (int attempt = 0; attempt < GenerationAttempts; attempt++)
            {
                var candidate = BoardGenerator.Generate(board.Width, board.Height, palette, random.Next());
                // Validate before copying: the real board only ever receives a stable, playable layout.
                if (IsStableAndPlayable(candidate))
                {
                    board.CopyTilesFrom(candidate);
                    return true;
                }
            }

            // The fallback was validated when the level started; failing here means a programming error, not bad luck.
            if (!IsStableAndPlayable(fallback))
            {
                throw new InvalidOperationException("The fallback board must have no match and at least one legal move.");
            }

            board.CopyTilesFrom(fallback);
            return true;
        }

        private static bool IsStableAndPlayable(Board candidate)
        {
            return MatchFinder.FindMatches(candidate).Count == 0 && BoardPlayability.HasAnyLegalMove(candidate);
        }
    }
}
