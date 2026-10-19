using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Describes an already-applied adjacent swap: a normal match, special activation, or invalid move.
    public static class SwapValidator
    {
        // Contract: `board` already contains the result of swapping `first` and `second`.
        // This only inspects that result; it never changes the Board.
        public static bool IsValidResult(Board board, GridPosition first, GridPosition second)
        {
            return Analyze(board, first, second).Kind != SwapKind.Invalid;
        }

        public static SwapAnalysis Analyze(Board board, GridPosition first, GridPosition second)
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

            TileType firstType = board.GetTile(first);
            TileType secondType = board.GetTile(second);
            if (Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) != 1
                || TileRules.IsEmpty(firstType) || TileRules.IsEmpty(secondType))
            {
                return new SwapAnalysis(SwapKind.Invalid, first, second);
            }

            if (TileRules.IsSpecial(firstType) || TileRules.IsSpecial(secondType))
            {
                var activations = new List<SpecialActivation>(2);
                foreach (GridPosition position in new[] { first, second })
                {
                    TileType type = board.GetTile(position);
                    if (!TileRules.IsSpecial(type)) continue;

                    // Positions are read after the swap; only a Light Ball uses its normal partner's color.
                    TileType partner = position == first ? secondType : firstType;
                    TileType? color = type == TileType.LightBall && TileRules.IsNormalMatchItem(partner)
                        ? partner : (TileType?)null;
                    activations.Add(new SpecialActivation(position, color));
                }
                return new SwapAnalysis(SwapKind.SpecialSwap, first, second, activations);
            }

            // A match elsewhere on the board is not enough; it must contain a swapped cell.
            foreach (GridPosition matched in MatchFinder.FindMatches(board))
            {
                if (matched == first || matched == second)
                {
                    return new SwapAnalysis(SwapKind.NormalMatch, first, second);
                }
            }

            return new SwapAnalysis(SwapKind.Invalid, first, second);
        }
    }
}
