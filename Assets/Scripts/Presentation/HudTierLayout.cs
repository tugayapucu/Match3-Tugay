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

    // How the Compact HUD's width relates to the board panel below it.
    public enum CompactHudMode
    {
        // The board panel is at least the HUD's minimum content width: the HUD matches the panel width.
        Aligned,
        // The board is small (short or no-room SafeArea): the HUD keeps its minimum width, wider than the panel.
        Expanded,
        // The SafeArea is narrower than the minimum: the content keeps its minimum layout and is scaled down.
        Scaled,
    }

    // The Compact HUD's horizontal geometry, in Canvas units. Its height is always the tier's HudHeight.
    public readonly struct CompactHudGeometry
    {
        public CompactHudGeometry(CompactHudMode mode, float width, float contentWidth, float contentScale)
        {
            Mode = mode;
            Width = width;
            ContentWidth = contentWidth;
            ContentScale = contentScale;
        }

        public CompactHudMode Mode { get; }
        // The width the HUD occupies on screen (the TopBar width).
        public float Width { get; }
        // The width the content is laid out at before scaling; never below the minimum content width.
        public float ContentWidth { get; }
        // Uniform scale applied to the content only; 1 unless Scaled. ContentWidth * ContentScale == Width.
        public float ContentScale { get; }
    }

    // The bottom button row, in Canvas units: two equal buttons with the tier's gap between them.
    public readonly struct ControlsGeometry
    {
        public ControlsGeometry(float buttonWidth, float buttonHeight, float gap)
        {
            ButtonWidth = buttonWidth;
            ButtonHeight = buttonHeight;
            Gap = gap;
        }

        public float ButtonWidth { get; }
        // Never shrinks: it is the touch target height and the BottomControls height the fitter reads.
        public float ButtonHeight { get; }
        public float Gap { get; }
        public float RowWidth => 2f * ButtonWidth + Gap;
        // Below this the buttons still fit while the SafeArea allows, but their labels may not stay readable.
        public bool MeetsMinimumWidth => ButtonWidth >= HudTierLayout.MinButtonWidth;
    }

    // Chooses the largest HUD that keeps today's board size, and sizes the Compact HUD and the button row.
    // Pure layout math; it reads no scene objects.
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

        // The board panel spans the 8 board cells plus 0.2 cell of padding on each side.
        public const float BoardPanelSize = 8.4f;

        // The narrowest width the Compact content fits at with its worst-case values (measured from the
        // Fredoka assets): padding 10 + 10, three 2 u dividers, and the Level 140, Goal 276, Moves 130 and
        // Score 150 column minimums.
        public const float CompactMinContentWidth = 722f;

        // The narrowest button whose label stays at a readable size.
        public const float MinButtonWidth = 220f;

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

        // The Compact HUD width: the board panel width when the panel is wide enough for the content, the
        // minimum content width otherwise, and never wider than the SafeArea minus the HUD margins.
        //
        // Plain arithmetic, the same cell formula BoardCameraFitter uses. When the HUD and controls leave no
        // room for the board, the cell is 0 instead of calling BoardCameraFraming.ContentRegion (which would
        // throw), so the HUD is simply Expanded and the fitter pauses exactly as it does today.
        public static CompactHudGeometry CompactHud(Vector2 safeSize, float boardSideMargin)
        {
            if (!IsFiniteNonNegative(safeSize.x) || !IsFiniteNonNegative(safeSize.y) || !IsFiniteNonNegative(boardSideMargin))
            {
                throw new ArgumentOutOfRangeException(nameof(safeSize), "The SafeArea size and board margin must be finite and non-negative.");
            }

            float regionWidth = safeSize.x - 2f * boardSideMargin;
            float regionHeight = safeSize.y - Compact.ReservedHeight;
            float cell = regionWidth > 0f && regionHeight > 0f
                ? Mathf.Min(regionWidth, regionHeight) / BoardCameraFraming.BoardEnvelopeSize
                : 0f;
            float panelWidth = BoardPanelSize * cell;
            float maxWidth = HudWidth(safeSize.x);

            if (maxWidth < CompactMinContentWidth)
            {
                // Lay the content out at its minimum and scale it to fill what the SafeArea allows.
                return new CompactHudGeometry(CompactHudMode.Scaled, maxWidth, CompactMinContentWidth,
                    maxWidth / CompactMinContentWidth);
            }

            if (panelWidth >= CompactMinContentWidth)
            {
                // The Canvas is always 1080 u wide, so the panel never exceeds maxWidth by more than float
                // rounding; Min keeps the HUD inside the SafeArea margins regardless.
                float width = Mathf.Min(panelWidth, maxWidth);
                return new CompactHudGeometry(CompactHudMode.Aligned, width, width, 1f);
            }

            return new CompactHudGeometry(CompactHudMode.Expanded, CompactMinContentWidth, CompactMinContentWidth, 1f);
        }

        // The button row for a tier: its design size when it fits inside the HUD margins, otherwise two equal
        // narrower buttons. The gap and the button height never change. While the available width is at least
        // the gap the row fits; below that only the buttons reach 0 width and the gap alone is wider.
        public static ControlsGeometry FitControls(HudTier tier, float safeWidth)
        {
            HudTierMetrics metrics = Metrics(tier);
            float available = HudWidth(safeWidth);
            float buttonWidth = metrics.ControlsWidth <= available
                ? metrics.ButtonWidth
                : Mathf.Max(0f, (available - metrics.ButtonGap) * 0.5f);
            return new ControlsGeometry(buttonWidth, metrics.ButtonHeight, metrics.ButtonGap);
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
