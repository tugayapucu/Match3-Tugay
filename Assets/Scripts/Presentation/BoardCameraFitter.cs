using System;
using UnityEngine;

namespace Match3.Presentation
{
    // Frames the fixed world-space board between the safe-area-aware HUD controls.
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField] private SafeAreaLayout hudLayout;

        private Camera boardCamera;
        private Canvas canvas;
        private readonly Vector3[] corners = new Vector3[4];
        private Vector2Int lastScreenSize;
        private int lastLayoutVersion = -1;
        private float lastCanvasScale;
        private bool hasLayoutInputs;
        private bool warnedNoRoom;

        private void Awake()
        {
            boardCamera = GetComponent<Camera>();
            if (hudLayout == null || !boardCamera.orthographic)
            {
                throw new InvalidOperationException("Assign SafeAreaLayout to an orthographic board camera.");
            }

            canvas = hudLayout.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                throw new InvalidOperationException("SafeAreaLayout must be inside a Canvas.");
            }
        }

        private void LateUpdate()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            // SafeAreaLayout updates in Update; its version tells us when the UI edges can have moved.
            float canvasScale = canvas.scaleFactor;
            if (hasLayoutInputs && lastScreenSize == screenSize && lastLayoutVersion == hudLayout.LayoutVersion
                && Mathf.Approximately(lastCanvasScale, canvasScale))
            {
                return;
            }

            Rect safeArea = ScreenRect(hudLayout.SafeAreaRoot);
            Rect topBar = ScreenRect(hudLayout.TopBar);
            Rect restart = ScreenRect(hudLayout.RestartButton);
            float sideMargin = hudLayout.SideMargin * canvasScale;
            float controlGap = SafeAreaLayout.BoardControlGap * canvasScale;
            if (safeArea.xMax - sideMargin <= safeArea.xMin + sideMargin
                || topBar.yMin - controlGap <= restart.yMax + controlGap)
            {
                // Remember failed attempts too; the same unusable layout must not retry every frame.
                RememberLayoutInputs(screenSize, canvasScale);
                if (!warnedNoRoom)
                {
                    Debug.LogWarning("Board camera fitting paused: the HUD and Restart leave no room. Keeping the current camera until the layout changes.", this);
                    warnedNoRoom = true;
                }

                return;
            }

            Rect region = BoardCameraFraming.ContentRegion(safeArea, topBar.yMin, restart.yMax,
                sideMargin, controlGap);
            // The fixed 8x8 grid starts at BoardRoot (-3.5, -3.5), so its world centre is (0, 0).
            BoardCameraFraming.CalculateCamera(region, screenSize, Vector2.zero,
                BoardCameraFraming.BoardEnvelopeSize, out Vector2 center, out float size);

            boardCamera.orthographicSize = size;
            Vector3 position = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(center.x, center.y, position.z);
            RememberLayoutInputs(screenSize, canvasScale);
            warnedNoRoom = false;
        }

        private void RememberLayoutInputs(Vector2Int screenSize, float canvasScale)
        {
            lastScreenSize = screenSize;
            lastLayoutVersion = hudLayout.LayoutVersion;
            lastCanvasScale = canvasScale;
            hasLayoutInputs = true;
        }

        private Rect ScreenRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
        }
    }
}
