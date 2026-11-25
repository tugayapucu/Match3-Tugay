using System;
using System.Globalization;

namespace Match3.Presentation
{
    // The HUD's text and fill values. Pure formatting, so the views only assign what these return.
    //
    // Every number uses the invariant culture: the HUD shows plain digits on every device language,
    // and a test can compare exact strings. Counts are never negative (LevelProgress guarantees it), so a
    // negative value is a caller bug and fails loudly instead of showing a minus sign.
    public static class HudFormat
    {
        // Moves, score, goal count and the level number: digits only, no grouping separators.
        public static string Count(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "HUD counts cannot be negative.");
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        // The smaller total beside a value, such as the "/ 6" in "Level 2 / 6" or the "/ 22" beside a goal count.
        public static string OutOf(int total)
        {
            if (total <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(total), total, "A HUD total must be positive.");
            }

            return "/ " + Count(total);
        }

        // How full the goal track is, from 0 to 1. Clamped, so a count past the target never overdraws the bar.
        public static float GoalFill(int progress, int target)
        {
            if (target <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, "The goal target must be positive.");
            }
            if (progress < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(progress), progress, "Goal progress cannot be negative.");
            }

            return progress >= target ? 1f : (float)progress / target;
        }
    }
}
