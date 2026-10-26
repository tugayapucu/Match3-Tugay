using System;
using UnityEngine;

namespace Match3.Presentation
{
    // Screen-space layout and orthographic framing math; it reads no scene objects.
    public static class BoardCameraFraming
    {
        public const float BoardEnvelopeSize = 8.2f;

        public static Rect ContentRegion(Rect safeArea, float topBarBottom, float restartTop,
            float sideMargin, float controlGap)
        {
            if (safeArea.width <= 0f || safeArea.height <= 0f || sideMargin < 0f || controlGap < 0f
                || float.IsNaN(topBarBottom) || float.IsNaN(restartTop)
                || float.IsNaN(sideMargin) || float.IsNaN(controlGap))
            {
                throw new ArgumentOutOfRangeException(nameof(safeArea), "The safe area and margins must be finite and usable.");
            }

            Rect region = Rect.MinMaxRect(safeArea.xMin + sideMargin, restartTop + controlGap,
                safeArea.xMax - sideMargin, topBarBottom - controlGap);
            if (region.width <= 0f || region.height <= 0f)
            {
                throw new ArgumentException("The HUD and Restart leave no room for the board.", nameof(safeArea));
            }

            return region;
        }

        public static void CalculateCamera(Rect region, Vector2Int screenSize, Vector2 boardCenter,
            float envelopeSize, out Vector2 cameraCenter, out float orthographicSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0 || region.width <= 0f || region.height <= 0f
                || envelopeSize <= 0f || float.IsNaN(envelopeSize) || float.IsInfinity(envelopeSize))
            {
                throw new ArgumentOutOfRangeException(nameof(region), "The screen, region and board envelope must be positive.");
            }

            // One scale fits both dimensions; the smaller available dimension limits the board.
            float pixelsPerWorldUnit = Mathf.Min(region.width, region.height) / envelopeSize;
            orthographicSize = screenSize.y / (2f * pixelsPerWorldUnit);

            // Move the camera so the fixed board centre appears at the region's screen centre.
            Vector2 screenCenter = new Vector2(screenSize.x * 0.5f, screenSize.y * 0.5f);
            cameraCenter = boardCenter - (region.center - screenCenter) / pixelsPerWorldUnit;
        }
    }
}
