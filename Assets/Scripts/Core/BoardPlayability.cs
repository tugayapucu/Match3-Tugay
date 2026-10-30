using System;

namespace Match3.Core
{
    // Answers whether the player has at least one swap the game would accept.
    // "Stable" (no match now) and "playable" (a legal move exists) are different questions; a dead board is stable but not playable.
    public static class BoardPlayability
    {
        // A legal move is an orthogonally adjacent swap that SwapValidator accepts (NormalMatch or SpecialSwap),
        // so this query never duplicates the match rules. Bounded work, performed only at settled-board checkpoints.
        public static bool HasAnyLegalMove(Board board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            // Every trial swap happens on this private copy, so the caller's Board is never touched.
            var scratch = new Board(board.Width, board.Height);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    scratch.SetTile(position, board.GetTile(position));
                }
            }

            // Checking only the right and upper neighbour visits each adjacent pair exactly once.
            for (int y = 0; y < scratch.Height; y++)
            {
                for (int x = 0; x < scratch.Width; x++)
                {
                    var cell = new GridPosition(x, y);
                    if (IsLegalSwap(scratch, cell, new GridPosition(x + 1, y))
                        || IsLegalSwap(scratch, cell, new GridPosition(x, y + 1)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // SwapValidator judges the board after the swap, so swap, ask, then swap back before the next candidate.
        private static bool IsLegalSwap(Board scratch, GridPosition first, GridPosition second)
        {
            if (!scratch.IsInBounds(second))
            {
                return false;
            }

            scratch.TrySwapAdjacent(first, second);
            SwapKind kind = SwapValidator.Analyze(scratch, first, second).Kind;
            scratch.TrySwapAdjacent(first, second);
            return kind != SwapKind.Invalid;
        }
    }
}
