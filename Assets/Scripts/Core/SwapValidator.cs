using System;

namespace Match3.Core
{
    // The Match-3 move rule: a swap counts only if it creates a match that includes one of the two swapped cells.
    public static class SwapValidator
    {
        // Contract: `board` already contains the result of swapping `first` and `second`.
        // This only inspects that result; it never changes the Board.
        public static bool IsValidResult(Board board, GridPosition first, GridPosition second)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            // Off-board positions are a caller bug; fail loudly instead of quietly returning false.
            if (!board.IsInBounds(first))
            {
                throw new ArgumentOutOfRangeException(nameof(first), first, "Position must be within the board bounds.");
            }

            if (!board.IsInBounds(second))
            {
                throw new ArgumentOutOfRangeException(nameof(second), second, "Position must be within the board bounds.");
            }

            // A match elsewhere on the board is not enough; it must contain a swapped cell.
            foreach (GridPosition matched in MatchFinder.FindMatches(board))
            {
                if (matched == first || matched == second)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
