using System;
using UnityEngine;

namespace Match3.Presentation
{
    // Keeps the existing HUD inside the device's usable screen area.
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform restartButton;
        [SerializeField] private float sideMargin = 48f;

        private RectTransform safeAreaRoot;
        private Canvas canvas;
        private float preferredTopBarWidth;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float lastScaleFactor;
        private bool hasLayout;

        // Step 4 can use these same on-screen UI edges when fitting the camera.
        public RectTransform SafeAreaRoot => safeAreaRoot;
        public RectTransform TopBar => topBar;
        public RectTransform RestartButton => restartButton;
        public float SideMargin => sideMargin;
        public const float BoardControlGap = 32f;
        public int LayoutVersion { get; private set; }

        private void Awake()
        {
            safeAreaRoot = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null || topBar == null || restartButton == null)
            {
                throw new InvalidOperationException("Assign a Canvas, TopBar and RestartButton to SafeAreaLayout.");
            }

            preferredTopBarWidth = topBar.sizeDelta.x;
        }

        private void OnEnable()
        {
            RefreshIfNeeded();
        }

        private void Update()
        {
            // Screen dimensions, insets and Canvas scale usually stay unchanged; only readjust when one changes.
            RefreshIfNeeded();
        }

        private void RefreshIfNeeded()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            float scaleFactor = canvas.scaleFactor;
            if (screenSize.x <= 0 || screenSize.y <= 0 || scaleFactor <= 0f)
            {
                return;
            }

            if (hasLayout && lastScreenSize == screenSize && lastSafeArea == safeArea
                && Mathf.Approximately(lastScaleFactor, scaleFactor))
            {
                return;
            }

            ApplyLayout(safeArea, screenSize, scaleFactor);
        }

        // The same calculation can be checked with simulated notch and gesture-bar rectangles.
        public void ApplyLayout(Rect safeArea, Vector2Int screenSize, float scaleFactor)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0 || scaleFactor <= 0f
                || float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor)
                || safeArea.width < 0f || safeArea.height < 0f
                || safeArea.xMin < 0f || safeArea.yMin < 0f
                || safeArea.xMax > screenSize.x || safeArea.yMax > screenSize.y)
            {
                throw new ArgumentOutOfRangeException(nameof(safeArea), "Safe area must fit inside a positive screen and Canvas scale.");
            }

            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            // Keep the approved 900-unit bar unless horizontal insets require a narrower one.
            float availableWidth = safeArea.width / scaleFactor - 2f * sideMargin;
            topBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Min(preferredTopBarWidth, Mathf.Max(0f, availableWidth)));

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastScaleFactor = scaleFactor;
            hasLayout = true;
            LayoutVersion++;
        }
    }
}
