using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class BoardCameraFramingTests
    {
        [TestCase(1080, 1920, 0, 0, 0, 0)]
        [TestCase(1080, 2160, 0, 0, 0, 0)]
        [TestCase(1080, 2340, 0, 0, 0, 0)]
        [TestCase(1080, 2400, 0, 0, 0, 0)]
        [TestCase(1440, 2960, 0, 0, 0, 0)]
        [TestCase(1080, 1440, 0, 0, 0, 0)]
        [TestCase(1080, 1200, 0, 0, 0, 0)]
        [TestCase(1080, 1920, 60, 70, 80, 100)]
        public void CameraFramesEnvelopeInsideTheAvailableRegion(int width, int height,
            int leftInset, int bottomInset, int rightInset, int topInset)
        {
            float uiScale = width / 1080f;
            var safe = new Rect(leftInset, bottomInset,
                width - leftInset - rightInset, height - bottomInset - topInset);
            float topBarBottom = safe.yMax - (24f + 96f) * uiScale;
            float restartTop = safe.yMin + (48f + 128f) * uiScale;
            Rect region = BoardCameraFraming.ContentRegion(safe, topBarBottom, restartTop,
                48f * uiScale, SafeAreaLayout.BoardControlGap * uiScale);

            BoardCameraFraming.CalculateCamera(region, new Vector2Int(width, height), Vector2.zero,
                BoardCameraFraming.BoardEnvelopeSize, out Vector2 cameraCenter, out float size);

            float pixelsPerWorld = height / (2f * size);
            Vector2 projectedCenter = Project(Vector2.zero, cameraCenter, width, height, pixelsPerWorld);
            Assert.That(projectedCenter.x, Is.EqualTo(region.center.x).Within(0.01f));
            Assert.That(projectedCenter.y, Is.EqualTo(region.center.y).Within(0.01f));

            float edge = BoardCameraFraming.BoardEnvelopeSize * 0.5f;
            Vector2 low = Project(new Vector2(-edge, -edge), cameraCenter, width, height, pixelsPerWorld);
            Vector2 high = Project(new Vector2(edge, edge), cameraCenter, width, height, pixelsPerWorld);
            Assert.That(low.x, Is.GreaterThanOrEqualTo(region.xMin - 0.02f));
            Assert.That(low.y, Is.GreaterThanOrEqualTo(region.yMin - 0.02f));
            Assert.That(high.x, Is.LessThanOrEqualTo(region.xMax + 0.02f));
            Assert.That(high.y, Is.LessThanOrEqualTo(region.yMax + 0.02f));
            Assert.That(region.yMax + 32f * uiScale, Is.LessThanOrEqualTo(topBarBottom));
            Assert.That(region.yMin - 32f * uiScale, Is.GreaterThanOrEqualTo(restartTop));
        }

        [Test]
        public void ReferencePortrait_UsesTheApprovedBoundsAndCamera()
        {
            Rect region = BoardCameraFraming.ContentRegion(new Rect(0f, 0f, 1080f, 1920f),
                1800f, 176f, 48f, 32f);
            BoardCameraFraming.CalculateCamera(region, new Vector2Int(1080, 1920), Vector2.zero,
                8.2f, out Vector2 center, out float size);

            Assert.That(region, Is.EqualTo(new Rect(48f, 208f, 984f, 1560f)));
            Assert.That(size, Is.EqualTo(8f).Within(0.0001f));
            Assert.That(center.x, Is.Zero.Within(0.0001f));
            Assert.That(center.y, Is.EqualTo(-28f / 120f).Within(0.0001f));
        }

        [TestCase(360f)]
        [TestCase(300f)]
        public void ContentRegion_NoRoom_StillRejectsTheInput(float screenHeight)
        {
            Assert.Throws<System.ArgumentException>(() => BoardCameraFraming.ContentRegion(
                new Rect(0f, 0f, 1080f, screenHeight), screenHeight - 120f, 176f, 48f, 32f));
        }

        private static Vector2 Project(Vector2 worldPoint, Vector2 cameraCenter,
            int screenWidth, int screenHeight, float pixelsPerWorld)
        {
            return new Vector2(screenWidth * 0.5f, screenHeight * 0.5f)
                + (worldPoint - cameraCenter) * pixelsPerWorld;
        }
    }
}
