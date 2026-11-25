using System;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    // All sizes are Canvas units of the 1080-wide reference. The boundaries are derived from the production
    // constants, so a changed constant moves the tests with it instead of silently testing old numbers.
    public class CompactHudGeometryTests
    {
        private const float BoardMargin = 48f;
        private const float Tolerance = 0.001f;
        private const float Step = 0.01f;
        private const float TallHeight = 3000f;

        // The board side length (in Canvas units) at which the panel is exactly the minimum content width.
        private static float AlignedRegionSide =>
            HudTierLayout.CompactMinContentWidth * BoardCameraFraming.BoardEnvelopeSize / HudTierLayout.BoardPanelSize;

        // 800.8095238... u
        private static float AlignedWidthBoundary => 2f * BoardMargin + AlignedRegionSide;

        // 1064.8095238... u
        private static float AlignedHeightBoundary => HudTierLayout.Compact.ReservedHeight + AlignedRegionSide;

        // 794 u: below it even the minimum content does not fit inside the HUD margins.
        private static float ScaledWidthBoundary => HudTierLayout.CompactMinContentWidth + 2f * HudTierLayout.HudSideMargin;

        [Test]
        public void Boundaries_AreTheDerivedValues()
        {
            Assert.That(AlignedWidthBoundary, Is.EqualTo(800.8095238f).Within(Tolerance));
            Assert.That(AlignedHeightBoundary, Is.EqualTo(1064.8095238f).Within(Tolerance));
            Assert.That(ScaledWidthBoundary, Is.EqualTo(794f));
        }

        [TestCase(1080f, 1920f, CompactHudMode.Aligned, 1008f, TestName = "Reference_1080x1920_and_720x1280")]
        [TestCase(1080f, 2340f, CompactHudMode.Aligned, 1008f, TestName = "S23_1080x2340")]
        [TestCase(1080f, 1200f, CompactHudMode.Aligned, 860.48780f, TestName = "Short_1080x1200")]
        [TestCase(940f, 2170f, CompactHudMode.Aligned, 864.58537f, TestName = "S23Insets_L60T100R80B70")]
        [TestCase(940f, 1750f, CompactHudMode.Aligned, 864.58537f, TestName = "SafeAreaLayoutTestsAsymmetricInsets")]
        [TestCase(880f, 1730f, CompactHudMode.Aligned, 803.12195f, TestName = "SceneTouchInputTestsInsets")]
        [TestCase(1080f, 1000f, CompactHudMode.Expanded, 722f, TestName = "ShortBoard_1080x1000")]
        [TestCase(1080f, 220f, CompactHudMode.Expanded, 722f, TestName = "NoRoom_1080x220")]
        [TestCase(1080f, 200f, CompactHudMode.Expanded, 722f, TestName = "NoRoom_1080x200")]
        [TestCase(700f, 1920f, CompactHudMode.Scaled, 628f, TestName = "Narrow_700x1920")]
        public void ApprovedMatrix_PicksTheModeAndWidth(float safeWidth, float safeHeight, CompactHudMode mode, float width)
        {
            CompactHudGeometry hud = HudTierLayout.CompactHud(new Vector2(safeWidth, safeHeight), BoardMargin);

            Assert.That(hud.Mode, Is.EqualTo(mode));
            Assert.That(hud.Width, Is.EqualTo(width).Within(Tolerance));
            AssertConsistent(new Vector2(safeWidth, safeHeight), hud);
        }

        [Test]
        public void Scaled_LaysOutAtTheMinimumAndScalesToTheMargins()
        {
            CompactHudGeometry hud = HudTierLayout.CompactHud(new Vector2(700f, 1920f), BoardMargin);

            Assert.That(hud.ContentWidth, Is.EqualTo(HudTierLayout.CompactMinContentWidth));
            Assert.That(hud.ContentScale, Is.EqualTo(628f / 722f).Within(0.00001f));
        }

        [Test]
        public void AlignedWidthBoundary_BelowAtAndAbove()
        {
            float boundary = AlignedWidthBoundary;

            CompactHudGeometry below = HudTierLayout.CompactHud(new Vector2(boundary - Step, TallHeight), BoardMargin);
            Assert.That(below.Mode, Is.EqualTo(CompactHudMode.Expanded));
            Assert.That(below.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth));

            AssertAtBoundary(new Vector2(boundary, TallHeight));

            var aboveSize = new Vector2(boundary + Step, TallHeight);
            CompactHudGeometry above = HudTierLayout.CompactHud(aboveSize, BoardMargin);
            Assert.That(above.Mode, Is.EqualTo(CompactHudMode.Aligned));
            Assert.That(above.Width, Is.EqualTo(BoardPanelWidth(aboveSize)).Within(Tolerance));
            Assert.That(above.Width, Is.GreaterThan(HudTierLayout.CompactMinContentWidth));
        }

        [Test]
        public void AlignedHeightBoundary_BelowAtAndAbove()
        {
            float boundary = AlignedHeightBoundary;

            CompactHudGeometry below = HudTierLayout.CompactHud(new Vector2(1080f, boundary - Step), BoardMargin);
            Assert.That(below.Mode, Is.EqualTo(CompactHudMode.Expanded));
            Assert.That(below.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth));

            AssertAtBoundary(new Vector2(1080f, boundary));

            var aboveSize = new Vector2(1080f, boundary + Step);
            CompactHudGeometry above = HudTierLayout.CompactHud(aboveSize, BoardMargin);
            Assert.That(above.Mode, Is.EqualTo(CompactHudMode.Aligned));
            Assert.That(above.Width, Is.EqualTo(BoardPanelWidth(aboveSize)).Within(Tolerance));
            Assert.That(above.Width, Is.GreaterThan(HudTierLayout.CompactMinContentWidth));
        }

        [Test]
        public void ScaledWidthBoundary_BelowAtAndAbove()
        {
            float boundary = ScaledWidthBoundary;

            CompactHudGeometry below = HudTierLayout.CompactHud(new Vector2(boundary - Step, TallHeight), BoardMargin);
            Assert.That(below.Mode, Is.EqualTo(CompactHudMode.Scaled));
            Assert.That(below.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth - Step).Within(Tolerance));
            Assert.That(below.ContentScale, Is.LessThan(1f));

            // Both sides of this boundary give 722 u at scale 1, so the width is what is checked here.
            CompactHudGeometry at = HudTierLayout.CompactHud(new Vector2(boundary, TallHeight), BoardMargin);
            Assert.That(at.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth).Within(Tolerance));
            Assert.That(at.ContentScale, Is.EqualTo(1f).Within(0.00001f));

            CompactHudGeometry above = HudTierLayout.CompactHud(new Vector2(boundary + Step, TallHeight), BoardMargin);
            Assert.That(above.Mode, Is.EqualTo(CompactHudMode.Expanded));
            Assert.That(above.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth));
            Assert.That(above.ContentScale, Is.EqualTo(1f));
        }

        [Test]
        public void NoRoomHeights_AreExpandedWithoutThrowing()
        {
            for (float height = 0f; height <= HudTierLayout.Compact.ReservedHeight; height += 1f)
            {
                CompactHudGeometry hud = HudTierLayout.CompactHud(new Vector2(1080f, height), BoardMargin);
                Assert.That(hud.Mode, Is.EqualTo(CompactHudMode.Expanded), $"height {height}");
                Assert.That(hud.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth), $"height {height}");
            }
        }

        // Every SafeArea from nothing to the full Canvas width: finite values, inside the HUD margins, and
        // aligned with the real board panel whenever the mode says so.
        [Test]
        public void EverySafeArea_StaysInsideTheMarginsAndMatchesItsMode()
        {
            for (float width = 0f; width <= 1080f; width += 2f)
            {
                for (float height = 0f; height <= 3200f; height += 4f)
                {
                    var safe = new Vector2(width, height);
                    AssertConsistent(safe, HudTierLayout.CompactHud(safe, BoardMargin));
                }
            }
        }

        [Test]
        public void WiderThanTheCanvas_StillStaysInsideTheMargins()
        {
            var safe = new Vector2(1200f, TallHeight);
            CompactHudGeometry hud = HudTierLayout.CompactHud(safe, BoardMargin);

            Assert.That(hud.Mode, Is.EqualTo(CompactHudMode.Aligned));
            Assert.That(hud.Width, Is.EqualTo(HudTierLayout.HudWidth(1200f)));
        }

        [TestCase(float.NaN, 1920f, 48f)]
        [TestCase(1080f, float.PositiveInfinity, 48f)]
        [TestCase(-1f, 1920f, 48f)]
        [TestCase(1080f, -1f, 48f)]
        [TestCase(1080f, 1920f, -1f)]
        public void CompactHud_InvalidInput_Throws(float width, float height, float margin)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.CompactHud(new Vector2(width, height), margin));
        }

        [TestCase(HudTier.Compact, 784f)]
        [TestCase(HudTier.Regular, 792f)]
        [TestCase(HudTier.Tall, 832f)]
        public void Controls_KeepTheDesignSizeExactlyWhileItFits(HudTier tier, float designBoundary)
        {
            HudTierMetrics metrics = HudTierLayout.Metrics(tier);
            float boundary = metrics.ControlsWidth + 2f * HudTierLayout.HudSideMargin;
            Assert.That(boundary, Is.EqualTo(designBoundary));

            Assert.That(HudTierLayout.FitControls(tier, boundary + Step).ButtonWidth, Is.EqualTo(metrics.ButtonWidth));
            Assert.That(HudTierLayout.FitControls(tier, boundary).ButtonWidth, Is.EqualTo(metrics.ButtonWidth));

            ControlsGeometry below = HudTierLayout.FitControls(tier, boundary - Step);
            Assert.That(below.ButtonWidth, Is.LessThan(metrics.ButtonWidth));
            Assert.That(below.RowWidth, Is.LessThanOrEqualTo(HudTierLayout.HudWidth(boundary - Step) + 0.0001f));
        }

        [TestCase(HudTier.Compact, 544f)]
        [TestCase(HudTier.Regular, 552f)]
        [TestCase(HudTier.Tall, 552f)]
        public void Controls_MeetTheMinimumButtonWidthDownToTheFloor(HudTier tier, float floorBoundary)
        {
            HudTierMetrics metrics = HudTierLayout.Metrics(tier);
            float boundary = 2f * HudTierLayout.MinButtonWidth + metrics.ButtonGap + 2f * HudTierLayout.HudSideMargin;
            Assert.That(boundary, Is.EqualTo(floorBoundary));

            ControlsGeometry at = HudTierLayout.FitControls(tier, boundary);
            Assert.That(at.ButtonWidth, Is.EqualTo(HudTierLayout.MinButtonWidth));
            Assert.That(at.MeetsMinimumWidth, Is.True);
            Assert.That(HudTierLayout.FitControls(tier, boundary + Step).MeetsMinimumWidth, Is.True);
            Assert.That(HudTierLayout.FitControls(tier, boundary - Step).MeetsMinimumWidth, Is.False);
        }

        // The row fits only while the available width is at least the gap; that is the whole claim.
        [Test]
        public void Controls_FitWheneverTheAvailableWidthCoversTheGap()
        {
            foreach (HudTier tier in new[] { HudTier.Compact, HudTier.Regular, HudTier.Tall })
            {
                HudTierMetrics metrics = HudTierLayout.Metrics(tier);
                for (float width = 0f; width <= 1080f; width += 0.5f)
                {
                    ControlsGeometry row = HudTierLayout.FitControls(tier, width);
                    float available = HudTierLayout.HudWidth(width);
                    string at = $"{tier} at {width}";

                    Assert.That(row.ButtonWidth, Is.GreaterThanOrEqualTo(0f), at);
                    Assert.That(row.ButtonHeight, Is.EqualTo(metrics.ButtonHeight), at);
                    Assert.That(row.Gap, Is.EqualTo(metrics.ButtonGap), at);
                    if (available >= metrics.ButtonGap)
                    {
                        Assert.That(row.RowWidth, Is.LessThanOrEqualTo(available + 0.0001f), at);
                    }
                    if (available >= metrics.ControlsWidth)
                    {
                        Assert.That(row.ButtonWidth, Is.EqualTo(metrics.ButtonWidth), at);
                    }
                }
            }
        }

        // Narrower than the gap: the buttons collapse to 0 and the gap alone is wider than the SafeArea allows.
        // Nothing throws and the height stays, but the row is not claimed to fit.
        [TestCase(HudTier.Compact, 82f)]
        [TestCase(HudTier.Regular, 100f)]
        [TestCase(HudTier.Tall, 0f)]
        public void Controls_NarrowerThanTheGap_CollapseTheButtonsOnly(HudTier tier, float safeWidth)
        {
            HudTierMetrics metrics = HudTierLayout.Metrics(tier);
            ControlsGeometry row = HudTierLayout.FitControls(tier, safeWidth);

            Assert.That(HudTierLayout.HudWidth(safeWidth), Is.LessThan(metrics.ButtonGap), "Test data must be below the gap.");
            Assert.That(row.ButtonWidth, Is.Zero);
            Assert.That(row.RowWidth, Is.EqualTo(metrics.ButtonGap));
            Assert.That(row.ButtonHeight, Is.EqualTo(metrics.ButtonHeight));
            Assert.That(row.MeetsMinimumWidth, Is.False);
        }

        [Test]
        public void Controls_InvalidInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.FitControls(HudTier.Compact, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.FitControls(HudTier.Compact, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.FitControls((HudTier)99, 1080f));
        }

        // At the exact boundary float rounding may land on either side, and both sides give 722 u at scale 1,
        // so the width is asserted, not the mode.
        private static void AssertAtBoundary(Vector2 safe)
        {
            CompactHudGeometry at = HudTierLayout.CompactHud(safe, BoardMargin);
            Assert.That(at.Mode, Is.Not.EqualTo(CompactHudMode.Scaled));
            Assert.That(at.Width, Is.EqualTo(HudTierLayout.CompactMinContentWidth).Within(Tolerance));
            Assert.That(at.ContentScale, Is.EqualTo(1f));
        }

        private static void AssertConsistent(Vector2 safe, CompactHudGeometry hud)
        {
            string at = $"{safe.x}x{safe.y} as {hud.Mode}";
            float maxWidth = HudTierLayout.HudWidth(safe.x);
            float min = HudTierLayout.CompactMinContentWidth;

            if (!IsFinite(hud.Width) || !IsFinite(hud.ContentWidth) || !IsFinite(hud.ContentScale))
            {
                Assert.Fail($"{at}: non-finite geometry");
            }
            if (hud.Width > maxWidth + Tolerance)
            {
                Assert.Fail($"{at}: width {hud.Width} exceeds the HUD margins ({maxWidth})");
            }
            if (hud.ContentWidth < min - Tolerance || Math.Abs(hud.ContentWidth * hud.ContentScale - hud.Width) > Tolerance)
            {
                Assert.Fail($"{at}: content {hud.ContentWidth} x {hud.ContentScale} does not give width {hud.Width}");
            }
            if (hud.ContentScale < 0f || hud.ContentScale > 1f || (hud.Mode != CompactHudMode.Scaled && hud.ContentScale != 1f))
            {
                Assert.Fail($"{at}: scale {hud.ContentScale}");
            }
            if ((hud.Mode == CompactHudMode.Scaled) != (maxWidth < min))
            {
                Assert.Fail($"{at}: Scaled must mean the minimum content does not fit ({maxWidth})");
            }

            float panel = BoardPanelWidth(safe);
            if (hud.Mode == CompactHudMode.Aligned
                && (Math.Abs(hud.Width - panel) > Tolerance || panel < min - Tolerance))
            {
                Assert.Fail($"{at}: width {hud.Width} is not the board panel {panel}");
            }
            if (hud.Mode == CompactHudMode.Expanded && (hud.Width != min || panel >= min + Tolerance))
            {
                Assert.Fail($"{at}: width {hud.Width} with board panel {panel}");
            }
        }

        // The board panel BoardCameraFitter would produce with the Compact HUD and controls: 8.4 cells of the
        // real ContentRegion, or 0 when the fitter would pause for lack of room.
        private static float BoardPanelWidth(Vector2 safe)
        {
            HudTierMetrics compact = HudTierLayout.Compact;
            float gap = SafeAreaLayout.BoardControlGap;
            float hudBottomFromTop = compact.TopOffset + compact.HudHeight;
            float controlsTop = compact.BottomOffset + compact.ButtonHeight;
            if (safe.x - 2f * BoardMargin <= 0f || safe.y - hudBottomFromTop - controlsTop - 2f * gap <= 0f)
            {
                return 0f;
            }

            Rect region = BoardCameraFraming.ContentRegion(new Rect(0f, 0f, safe.x, safe.y),
                safe.y - hudBottomFromTop, controlsTop, BoardMargin, gap);
            return HudTierLayout.BoardPanelSize * Mathf.Min(region.width, region.height) / BoardCameraFraming.BoardEnvelopeSize;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
