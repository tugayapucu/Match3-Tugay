using System;
using UnityEngine;

namespace Match3.Presentation
{
    // Keeps the HUD and the bottom buttons inside the device's usable screen area, sized by the Compact tier.
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomControls;
        [SerializeField] private RectTransform menuButton;
        [SerializeField] private RectTransform restartButton;
        // The board's side margin, which BoardCameraFitter uses. The HUD has its own (HudTierLayout.HudSideMargin).
        [SerializeField] private float sideMargin = 48f;

        private RectTransform safeAreaRoot;
        private Canvas canvas;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float lastScaleFactor;
        private bool hasLayout;

        // Step 4 can use these same on-screen UI edges when fitting the camera.
        public RectTransform SafeAreaRoot => safeAreaRoot;
        public RectTransform TopBar => topBar;
        public RectTransform BottomControls => bottomControls;
        public float SideMargin => sideMargin;
        public const float BoardControlGap = 32f;
        public int LayoutVersion { get; private set; }

        private void Awake()
        {
            safeAreaRoot = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null || topBar == null || bottomControls == null)
            {
                throw new InvalidOperationException("Assign a Canvas, TopBar and BottomControls to SafeAreaLayout.");
            }

            // The buttons are sized here but must sit in the row the fitter reads, or the board could overlap them.
            if (menuButton == null || restartButton == null
                || menuButton.parent != bottomControls || restartButton.parent != bottomControls)
            {
                throw new InvalidOperationException("Assign the Menu and Restart buttons inside BottomControls to SafeAreaLayout.");
            }
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

            // Compact on every screen for now; tier selection is not active yet. The tier metrics also set the
            // vertical offsets, so the height reserved for the HUD and buttons has a single source.
            Vector2 safeSize = safeArea.size / scaleFactor;
            HudTierMetrics tier = HudTierLayout.Compact;
            CompactHudGeometry hud = HudTierLayout.CompactHud(safeSize, sideMargin);
            ControlsGeometry row = HudTierLayout.FitControls(tier.Tier, safeSize.x);

            // Only the width varies; the height the fitter reads stays the tier's. hud.ContentScale has no content
            // to scale until the Compact HUD content exists.
            topBar.anchoredPosition = new Vector2(0f, -tier.TopOffset);
            topBar.sizeDelta = new Vector2(hud.Width, tier.HudHeight);

            // The scene anchors Menu to the row's left edge and Restart to its right edge, so the gap is what
            // remains between them.
            bottomControls.anchoredPosition = new Vector2(0f, tier.BottomOffset);
            bottomControls.sizeDelta = new Vector2(row.RowWidth, row.ButtonHeight);
            var buttonSize = new Vector2(row.ButtonWidth, row.ButtonHeight);
            menuButton.sizeDelta = buttonSize;
            restartButton.sizeDelta = buttonSize;

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastScaleFactor = scaleFactor;
            hasLayout = true;
            LayoutVersion++;
        }
    }
}
