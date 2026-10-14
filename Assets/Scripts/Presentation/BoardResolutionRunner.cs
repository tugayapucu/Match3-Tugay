using System;
using System.Collections;
using System.Collections.Generic;
using Match3.Core;

namespace Match3.Presentation
{
    // Sequences the resolution of a valid move: clear → fall → refill, repeated while matches remain (cascades).
    // It owns no rules: MatchFinder and BoardResolution decide what happens, BoardView shows it.
    // This class only decides the order, so each logical step is followed by its visual step.
    public sealed class BoardResolutionRunner
    {
        private readonly Board board;
        private readonly BoardView boardView;
        private readonly TileType[] palette;
        private readonly Random random;
        private readonly float fallDuration;
        private readonly float refillDuration;

        // `random` is owned by the caller, so every refill in the game continues one reproducible sequence.
        // The palette itself is validated by Core (generation at startup and Refill on use).
        public BoardResolutionRunner(
            Board board, BoardView boardView, IReadOnlyList<TileType> palette, Random random,
            float fallDuration, float refillDuration)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (boardView == null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            ValidateDuration(fallDuration, nameof(fallDuration));
            ValidateDuration(refillDuration, nameof(refillDuration));

            this.board = board;
            this.boardView = boardView;
            // Copy, so later edits to the caller's array (e.g. in the Inspector) cannot change a game in progress.
            this.palette = new TileType[palette.Count];
            for (int i = 0; i < palette.Count; i++)
            {
                this.palette[i] = palette[i];
            }

            this.random = random;
            this.fallDuration = fallDuration;
            this.refillDuration = refillDuration;
        }

        // Runs until MatchFinder finds nothing. The caller keeps input locked for the whole time.
        public IEnumerator ResolveUntilStable()
        {
            var matches = MatchFinder.FindMatches(board);
            while (matches.Count > 0)
            {
                // Each pair: Core changes the Board first, then BoardView catches up.
                BoardResolution.ClearCells(board, matches);
                boardView.RemoveViews(matches);

                var moves = BoardResolution.ApplyGravity(board);
                yield return boardView.AnimateFalls(moves, fallDuration);

                var spawns = BoardResolution.Refill(board, palette, random);
                yield return boardView.AnimateRefill(spawns, refillDuration);

                // Refill may have created new matches: if so, the loop runs again (a cascade).
                matches = MatchFinder.FindMatches(board);
            }
        }

        private static void ValidateDuration(float duration, string parameterName)
        {
            if (duration < 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                throw new ArgumentOutOfRangeException(parameterName, duration, "Duration must be finite and nonnegative.");
            }
        }
    }
}
