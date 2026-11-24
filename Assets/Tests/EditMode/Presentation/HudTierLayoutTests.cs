using System;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    // All sizes are Canvas units of the 1080-wide reference (screen pixels * 1080 / screen width).
    public class HudTierLayoutTests
    {
        // SafeAreaLayout's board margin in the scene, which BoardCameraFitter uses.
        private const float BoardMargin = 48f;
        // Today's layout: TopBar 96 u at 24 u from the SafeArea top, BottomControls 128 u at 48 u from its bottom.
        private const float TodayTopBarBottom = 24f + 96f;
        private const float TodayControlsTop = 48f + 128f;

        [TestCase(1080f, 2340f, HudTier.Tall, 120f, TestName = "S23_1080x2340")]
        [TestCase(1080f, 2400f, HudTier.Tall, 120f, TestName = "Tall_1080x2400")]
        [TestCase(1080f, 2220f, HudTier.Tall, 120f, TestName = "Screen1440x2960")]
        [TestCase(1080f, 2160f, HudTier.Tall, 120f, TestName = "Tall_1080x2160")]
        [TestCase(1080f, 1920f, HudTier.Regular, 120f, TestName = "Reference_1080x1920_and_720x1280")]
        [TestCase(1080f, 1840f, HudTier.Regular, 120f, TestName = "BottomGestureInset_1080x1920")]
        [TestCase(1080f, 1820f, HudTier.Regular, 120f, TestName = "TopCutout_1080x1920")]
        [TestCase(1080f, 1440f, HudTier.Compact, 120f, TestName = "Short_1080x1440")]
        [TestCase(1080f, 1200f, HudTier.Compact, 102.439026f, TestName = "Short_1080x1200")]
        [TestCase(940f, 2170f, HudTier.Tall, 102.926834f, TestName = "S23Insets_L60T100R80B70")]
        [TestCase(940f, 1750f, HudTier.Regular, 102.926834f, TestName = "SafeAreaLayoutTestsAsymmetricInsets")]
        [TestCase(880f, 1730f, HudTier.Regular, 95.609756f, TestName = "SceneTouchInputTestsInsets")]
        public void ApprovedMatrix_PicksTheTierAndKeepsTodaysCellSize(float safeWidth, float safeHeight,
            HudTier expectedTier, float expectedCell)
        {
            var safe = new Vector2(safeWidth, safeHeight);
            HudTier tier = HudTierLayout.Select(safe, BoardMargin);
            HudTierMetrics metrics = HudTierLayout.Metrics(tier);

            Assert.That(tier, Is.EqualTo(expectedTier));
            Assert.That(TodayCell(safe), Is.EqualTo(expectedCell).Within(0.0001f), "Test data must match today's layout.");
            Assert.That(TierCell(safe, metrics), Is.EqualTo(expectedCell).Within(0.0001f));
            Assert.That(metrics.ControlsWidth, Is.LessThanOrEqualTo(safeWidth - 2f * BoardMargin),
                "Both buttons stay inside the board margins.");
        }

        // A tier fits while safe height - reserved height >= board region width (safe width - 96).
        // Reserved: Tall 1052 u, Regular 736 u, so Tall needs height >= width + 956 and Regular >= width + 640.
        [TestCase(1080f, 2036f, HudTier.Tall)]
        [TestCase(1080f, 2036.1f, HudTier.Tall)]
        [TestCase(1080f, 2035.9f, HudTier.Regular)]
        [TestCase(1080f, 1720.1f, HudTier.Regular)]
        [TestCase(1080f, 1720f, HudTier.Regular)]
        [TestCase(1080f, 1719.9f, HudTier.Compact)]
        [TestCase(940f, 1896f, HudTier.Tall)]
        [TestCase(940f, 1895.9f, HudTier.Regular)]
        [TestCase(940f, 1580f, HudTier.Regular)]
        [TestCase(940f, 1579.9f, HudTier.Compact)]
        [TestCase(880f, 1836f, HudTier.Tall)]
        [TestCase(880f, 1835.9f, HudTier.Regular)]
        [TestCase(880f, 1520f, HudTier.Regular)]
        [TestCase(880f, 1519.9f, HudTier.Compact)]
        public void Boundaries_SwitchExactlyWhereTheBoardStopsBeingWidthLimited(float safeWidth, float safeHeight, HudTier expected)
        {
            Assert.That(HudTierLayout.Select(new Vector2(safeWidth, safeHeight), BoardMargin), Is.EqualTo(expected));
        }

        [Test]
        public void Boundaries_FollowTheBoardRegionWidthAtEveryWidth()
        {
            for (float width = 700f; width <= 1080f; width += 1f)
            {
                float regionWidth = width - 2f * BoardMargin;
                foreach (var tier in new[] { HudTierLayout.Tall, HudTierLayout.Regular })
                {
                    float threshold = regionWidth + tier.ReservedHeight;
                    Assert.That(HudTierLayout.Select(new Vector2(width, threshold), BoardMargin), Is.EqualTo(tier.Tier),
                        $"width {width}");
                    Assert.That(HudTierLayout.Select(new Vector2(width, threshold - 0.01f), BoardMargin), Is.Not.EqualTo(tier.Tier),
                        $"width {width}");
                }
            }
        }

        [Test]
        public void EverySafeArea_KeepsTodaysCellSize_OrHasNoRoomExactlyAsToday()
        {
            foreach (float width in new[] { 600f, 700f, 784f, 808f, 844f, 880f, 940f, 1000f, 1080f })
            {
                for (float height = 300f; height <= 3200f; height += 1f)
                {
                    var safe = new Vector2(width, height);
                    HudTierMetrics metrics = HudTierLayout.Metrics(HudTierLayout.Select(safe, BoardMargin));
                    float today = TodayCell(safe);
                    float now = TierCell(safe, metrics);
                    if (float.IsNaN(today))
                    {
                        Assert.That(metrics.Tier, Is.EqualTo(HudTier.Compact), $"{width}x{height}");
                        Assert.That(float.IsNaN(now), Is.True, $"{width}x{height}");
                    }
                    else
                    {
                        Assert.That(now, Is.EqualTo(today).Within(0.0001f), $"{width}x{height} as {metrics.Tier}");
                    }
                }
            }
        }

        [Test]
        public void Compact_ReservesExactlyTodaysHeight()
        {
            float today = TodayTopBarBottom + TodayControlsTop + 2f * SafeAreaLayout.BoardControlGap;
            Assert.That(HudTierLayout.Compact.ReservedHeight, Is.EqualTo(today));
            Assert.That(HudTierLayout.Compact.BottomOffset, Is.EqualTo(48f), "Approved Compact anchor offset.");
        }

        [Test]
        public void Metrics_MatchTheApprovedDesign()
        {
            AssertMetrics(HudTierLayout.Metrics(HudTier.Tall), HudTier.Tall, 24f, 700f, 360f, 144f, 40f, 120f);
            AssertMetrics(HudTierLayout.Metrics(HudTier.Regular), HudTier.Regular, 24f, 440f, 340f, 136f, 40f, 72f);
            AssertMetrics(HudTierLayout.Metrics(HudTier.Compact), HudTier.Compact, 24f, 112f, 340f, 112f, 32f, 48f);
        }

        [TestCase(1080f, 1008f, 984f)]
        [TestCase(940f, 868f, 844f)]
        [TestCase(880f, 808f, 784f)]
        public void HudWidth_UsesItsOwnMargin_WhileTheBoardKeeps48(float safeWidth, float hudWidth, float boardRegionWidth)
        {
            Assert.That(HudTierLayout.HudSideMargin, Is.EqualTo(36f));
            Assert.That(HudTierLayout.HudWidth(safeWidth), Is.EqualTo(hudWidth));
            Rect region = BoardCameraFraming.ContentRegion(new Rect(0f, 0f, safeWidth, 1920f),
                1920f - TodayTopBarBottom, TodayControlsTop, BoardMargin, SafeAreaLayout.BoardControlGap);
            Assert.That(region.width, Is.EqualTo(boardRegionWidth));
        }

        [Test]
        public void HudWidth_NeverNegative()
        {
            Assert.That(HudTierLayout.HudWidth(50f), Is.Zero);
            Assert.That(HudTierLayout.HudWidth(0f), Is.Zero);
        }

        [Test]
        public void NoRoom_IsCompact_SoTheFitterPausesAsToday()
        {
            Assert.That(HudTierLayout.Select(Vector2.zero, BoardMargin), Is.EqualTo(HudTier.Compact));
            Assert.That(HudTierLayout.Select(new Vector2(80f, 3000f), BoardMargin), Is.EqualTo(HudTier.Compact));
            Assert.That(HudTierLayout.Select(new Vector2(1080f, 300f), BoardMargin), Is.EqualTo(HudTier.Compact));
        }

        [TestCase(float.NaN, 1920f, 48f)]
        [TestCase(1080f, float.PositiveInfinity, 48f)]
        [TestCase(-1f, 1920f, 48f)]
        [TestCase(1080f, -1f, 48f)]
        [TestCase(1080f, 1920f, -1f)]
        [TestCase(1080f, 1920f, float.NaN)]
        public void InvalidInput_Throws(float width, float height, float margin)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.Select(new Vector2(width, height), margin));
        }

        [Test]
        public void InvalidTierOrWidth_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.Metrics((HudTier)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.HudWidth(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudTierLayout.HudWidth(-1f));
        }

        private static float TodayCell(Vector2 safe)
        {
            return Cell(safe, TodayTopBarBottom, TodayControlsTop);
        }

        private static float TierCell(Vector2 safe, HudTierMetrics tier)
        {
            return Cell(safe, tier.TopOffset + tier.HudHeight, tier.BottomOffset + tier.ButtonHeight);
        }

        // The same region and scale BoardCameraFitter computes, in Canvas units; NaN when it would pause.
        private static float Cell(Vector2 safe, float hudBottomFromTop, float controlsTopFromBottom)
        {
            float gap = SafeAreaLayout.BoardControlGap;
            if (safe.x - 2f * BoardMargin <= 0f || safe.y - hudBottomFromTop - controlsTopFromBottom - 2f * gap <= 0f)
            {
                return float.NaN;
            }

            Rect region = BoardCameraFraming.ContentRegion(new Rect(0f, 0f, safe.x, safe.y),
                safe.y - hudBottomFromTop, controlsTopFromBottom, BoardMargin, gap);
            return Mathf.Min(region.width, region.height) / BoardCameraFraming.BoardEnvelopeSize;
        }

        private static void AssertMetrics(HudTierMetrics metrics, HudTier tier, float top, float hud,
            float buttonWidth, float buttonHeight, float gap, float bottom)
        {
            Assert.That(metrics.Tier, Is.EqualTo(tier));
            Assert.That(metrics.TopOffset, Is.EqualTo(top));
            Assert.That(metrics.HudHeight, Is.EqualTo(hud));
            Assert.That(metrics.ButtonWidth, Is.EqualTo(buttonWidth));
            Assert.That(metrics.ButtonHeight, Is.EqualTo(buttonHeight));
            Assert.That(metrics.ButtonGap, Is.EqualTo(gap));
            Assert.That(metrics.BottomOffset, Is.EqualTo(bottom));
        }
    }
}
