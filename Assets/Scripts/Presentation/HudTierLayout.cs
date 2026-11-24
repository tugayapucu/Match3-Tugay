using System;
using UnityEngine;

namespace Match3.Presentation
{
    public enum HudTier
    {
        Compact,
        Regular,
        Tall,
    }

    // One tier's HUD and control geometry, in Canvas units of the 1080-wide reference.
    public readonly struct HudTierMetrics
    {
        public HudTierMetrics(HudTier tier, float topOffset, float hudHeight,
            float buttonWidth, float buttonHeight, float buttonGap, float bottomOffset)
        {
            Tier = tier;
            TopOffset = topOffset;
            HudHeight = hudHeight;
            ButtonWidth = buttonWidth;
            ButtonHeight = buttonHeight;
            ButtonGap = buttonGap;
            BottomOffset = bottomOffset;
        }

        public HudTier Tier { get; }
        // From the SafeArea top to the top of the HUD.
        public float TopOffset { get; }
        public float HudHeight { get; }
        public float ButtonWidth { get; }
        // The whole button rect, lip included; it is also the BottomControls height.
        public float ButtonHeight { get; }
        public float ButtonGap { get; }
        // From the SafeArea bottom to the bottom of the button rect (the BottomControls anchor offset).
        public float BottomOffset { get; }

        public float ControlsWidth => 2f * ButtonWidth + ButtonGap;

        // SafeArea height the board cannot use: HUD and controls with their offsets, plus the
        // board-to-UI gap that BoardCameraFitter keeps on both sides.
        public float ReservedHeight => TopOffset + HudHeight + BottomOffset + ButtonHeight
            + 2f * SafeAreaLayout.BoardControlGap;
    }

    // Chooses the largest HUD that keeps today's board size. Pure layout math; it reads no scene objects.
    //
    // BoardCameraFitter sizes a cell as min(regionWidth, regionHeight) / BoardEnvelopeSize. The region width
    // depends only on the SafeArea width and the board margin, never on the HUD. So a taller HUD keeps the
    // cell size exactly as long as the board stays limited by width (regionHeight >= regionWidth); the
    // envelope size cancels out of that comparison. Compact reserves exactly the height today's 96 u bar
    // and 128 u buttons do (24 + 112 + 48 + 112 = 24 + 96 + 48 + 128), so it never changes the cell either.
    public static class HudTierLayout
    {
        // HUD-only side margin. BoardCameraFitter keeps its own board margin (SafeAreaLayout.SideMargin, 48 u).
        public const float HudSideMargin = 36f;

        public static readonly HudTierMetrics Compact = new HudTierMetrics(HudTier.Compact,
            topOffset: 24f, hudHeight: 112f, buttonWidth: 340f, buttonHeight: 112f, buttonGap: 32f, bottomOffset: 48f);
        public static readonly HudTierMetrics Regular = new HudTierMetrics(HudTier.Regular,
            topOffset: 24f, hudHeight: 440f, buttonWidth: 340f, buttonHeight: 136f, buttonGap: 40f, bottomOffset: 72f);
        public static readonly HudTierMetrics Tall = new HudTierMetrics(HudTier.Tall,
            topOffset: 24f, hudHeight: 700f, buttonWidth: 360f, buttonHeight: 144f, buttonGap: 40f, bottomOffset: 120f);

        public static HudTierMetrics Metrics(HudTier tier)
        {
            switch (tier)
            {
                case HudTier.Compact: return Compact;
                case HudTier.Regular: return Regular;
                case HudTier.Tall: return Tall;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown HUD tier.");
            }
        }

        // safeSize is the SafeArea in Canvas units; boardSideMargin is the margin BoardCameraFitter uses.
        // When even Compact leaves no room, it is still Compact: BoardCameraFitter then pauses as it does today.
        public static HudTier Select(Vector2 safeSize, float boardSideMargin)
        {
            if (!IsFiniteNonNegative(safeSize.x) || !IsFiniteNonNegative(safeSize.y) || !IsFiniteNonNegative(boardSideMargin))
            {
                throw new ArgumentOutOfRangeException(nameof(safeSize), "The SafeArea size and board margin must be finite and non-negative.");
            }

            if (KeepsBoardWidthLimited(Tall, safeSize, boardSideMargin)) return HudTier.Tall;
            if (KeepsBoardWidthLimited(Regular, safeSize, boardSideMargin)) return HudTier.Regular;
            return HudTier.Compact;
        }

        // The HUD spans the SafeArea width minus its own margins; the board region is unaffected.
        public static float HudWidth(float safeWidth)
        {
            if (!IsFiniteNonNegative(safeWidth))
            {
                throw new ArgumentOutOfRangeException(nameof(safeWidth), "The SafeArea width must be finite and non-negative.");
            }

            return Mathf.Max(0f, safeWidth - 2f * HudSideMargin);
        }

        private static bool KeepsBoardWidthLimited(HudTierMetrics tier, Vector2 safeSize, float boardSideMargin)
        {
            float regionWidth = safeSize.x - 2f * boardSideMargin;
            float regionHeight = safeSize.y - tier.ReservedHeight;
            return regionWidth > 0f && regionHeight >= regionWidth;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
