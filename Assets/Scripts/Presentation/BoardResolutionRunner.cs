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
        // When the board is stable, `onResolved` receives every tile type cleared by this whole resolution
        // (all cascade rounds together), exactly once. The callback is checked now; the work runs when iterated.
        public IEnumerator ResolveUntilStable(Action<ResolutionResult> onResolved)
        {
            if (onResolved == null)
            {
                throw new ArgumentNullException(nameof(onResolved));
            }

            return ResolveUntilStableRoutine(null, onResolved);
        }

        // Player context applies to the first scan only; the callback-only overload resolves standalone cascades.
        public IEnumerator ResolveUntilStable(SwapAnalysis move, Action<ResolutionResult> onResolved)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (move.Kind == SwapKind.Invalid)
            {
                throw new ArgumentException("An invalid swap cannot be resolved.", nameof(move));
            }

            if (move.Kind != SwapKind.NormalMatch || move.InitialActivations.Count != 0)
            {
                throw new NotSupportedException("Special activation is not implemented yet.");
            }

            if (onResolved == null)
            {
                throw new ArgumentNullException(nameof(onResolved));
            }

            return ResolveUntilStableRoutine(move, onResolved);
        }

        private IEnumerator ResolveUntilStableRoutine(SwapAnalysis move, Action<ResolutionResult> onResolved)
        {
            // Collected across every round, so one player move produces one result.
            var cleared = new List<TileType>();
            bool firstScan = true;
            while (true)
            {
                var analysis = MatchFinder.Analyze(board);
                var creations = SpecialCreationRules.FindCreations(analysis,
                    firstScan ? move?.First : null, firstScan ? move?.Second : null);
                // Expire preference after the first scan, including an empty scan.
                firstScan = false;
                if (analysis.MatchedPositions.Count == 0)
                {
                    break;
                }

                // Each pair: Core changes the Board first, then BoardView catches up.
                var removed = BoardResolution.ClearCells(board, analysis.MatchedPositions);
                foreach (TileType type in removed)
                {
                    if (TileRules.IsNormalMatchItem(type))
                    {
                        cleared.Add(type);
                    }
                }
                BoardResolution.PlaceSpecials(board, creations);
                boardView.RemoveViews(analysis.MatchedPositions);
                boardView.PlaceSpecialViews(creations);

                var moves = BoardResolution.ApplyGravity(board);
                yield return boardView.AnimateFalls(moves, fallDuration);

                var spawns = BoardResolution.Refill(board, palette, random);
                yield return boardView.AnimateRefill(spawns, refillDuration);

                // Refill may have created new patterns; the same loop handles every cascade.
            }

            // Stable: hand over the facts. Scoring and goals are decided by whoever receives them.
            onResolved(new ResolutionResult(cleared));
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
