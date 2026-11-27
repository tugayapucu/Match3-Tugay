using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    public class SafeAreaLayoutTests
    {
        private GameObject canvasObject;
        private SafeAreaLayout layout;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.SetActive(false);
            var safeRoot = CreateRect("SafeArea", canvasObject.transform);
            var hud = CreateRect("LevelHud", safeRoot);
            var topBar = CreateRect("TopBar", hud);
            topBar.anchorMin = topBar.anchorMax = new Vector2(0.5f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = new Vector2(0f, -24f);
            topBar.sizeDelta = new Vector2(1008f, 112f);

            var controls = CreateRect("BottomControls", hud);
            controls.anchorMin = controls.anchorMax = new Vector2(0.5f, 0f);
            controls.pivot = new Vector2(0.5f, 0f);
            controls.anchoredPosition = new Vector2(0f, 48f);
            controls.sizeDelta = new Vector2(712f, 112f);
            // Like the scene: Menu fixed to the row's bottom-left corner, Restart to its bottom-right corner.
            var menu = CreateRect("MenuButton", controls);
            menu.anchorMin = menu.anchorMax = menu.pivot = new Vector2(0f, 0f);
            menu.sizeDelta = new Vector2(340f, 112f);
            var restart = CreateRect("RestartButton", controls);
            restart.anchorMin = restart.anchorMax = restart.pivot = new Vector2(1f, 0f);
            restart.sizeDelta = new Vector2(340f, 112f);

            var outcome = CreateRect("OutcomePanel", hud);
            var card = CreateRect("Card", outcome);
            card.sizeDelta = new Vector2(640f, 400f);
            var playAgain = CreateRect("PlayAgainButton", card);
            playAgain.anchoredPosition = new Vector2(0f, 40f);
            playAgain.sizeDelta = new Vector2(300f, 84f);

            layout = safeRoot.gameObject.AddComponent<SafeAreaLayout>();
            Set("topBar", topBar);
            Set("bottomControls", controls);
            Set("menuButton", menu);
            Set("restartButton", restart);
            canvasObject.SetActive(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(canvasObject);
            yield return null;
        }

        // The bar matches the board panel (8.4 cells): 1008 u while the board is 120 u cells, 864.59 u at 940 wide.
        [TestCase(0, 0, 1080, 1920, 1008f, TestName = "NoCutout")]
        [TestCase(0, 0, 1080, 1820, 1008f, TestName = "TopCutout")]
        [TestCase(0, 80, 1080, 1840, 1008f, TestName = "BottomGestureInset")]
        [TestCase(60, 70, 940, 1750, 864.58537f, TestName = "AsymmetricInsetsShrinkTopBar")]
        public void ApprovedPortraitLayout_StaysInsideSafeArea(
            int x, int y, int width, int height, float expectedBarWidth)
        {
            var safeArea = new Rect(x, y, width, height);
            layout.ApplyLayout(safeArea, new Vector2Int(1080, 1920), 1f);

            var root = layout.SafeAreaRoot;
            var topBar = layout.TopBar;
            var controls = layout.BottomControls;
            var card = (RectTransform)root.Find("LevelHud/OutcomePanel/Card");
            var playAgain = (RectTransform)card.Find("PlayAgainButton");

            Assert.That(root.anchorMin.x, Is.EqualTo(safeArea.xMin / 1080f).Within(0.0001f));
            Assert.That(root.anchorMin.y, Is.EqualTo(safeArea.yMin / 1920f).Within(0.0001f));
            Assert.That(root.anchorMax.x, Is.EqualTo(safeArea.xMax / 1080f).Within(0.0001f));
            Assert.That(root.anchorMax.y, Is.EqualTo(safeArea.yMax / 1920f).Within(0.0001f));
            Assert.That(root.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(root.offsetMax, Is.EqualTo(Vector2.zero));

            Assert.That(topBar.sizeDelta.x, Is.EqualTo(expectedBarWidth).Within(0.01f));
            Assert.That(topBar.sizeDelta.y, Is.EqualTo(112f));
            Assert.That(topBar.anchoredPosition, Is.EqualTo(new Vector2(0f, -24f)));
            Assert.That(controls.sizeDelta, Is.EqualTo(new Vector2(712f, 112f)));
            Assert.That(controls.anchoredPosition, Is.EqualTo(new Vector2(0f, 48f)));
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(640f, 400f)));

            float centerX = safeArea.center.x;
            float barLeft = centerX - topBar.rect.width / 2f;
            float barRight = centerX + topBar.rect.width / 2f;
            float barBottom = safeArea.yMax - 24f - topBar.rect.height;
            float controlsLeft = centerX - controls.rect.width / 2f;
            float controlsRight = centerX + controls.rect.width / 2f;
            float controlsTop = safeArea.yMin + 48f + controls.rect.height;

            // The HUD and buttons use the HUD margin (36 u); the board keeps its own 48 u margin.
            float hudMargin = HudTierLayout.HudSideMargin;
            Assert.That(barLeft, Is.GreaterThanOrEqualTo(safeArea.xMin + hudMargin - 0.001f));
            Assert.That(barRight, Is.LessThanOrEqualTo(safeArea.xMax - hudMargin + 0.001f));
            Assert.That(controlsLeft, Is.GreaterThanOrEqualTo(safeArea.xMin + hudMargin));
            Assert.That(controlsRight, Is.LessThanOrEqualTo(safeArea.xMax - hudMargin));
            Assert.That(barBottom - SafeAreaLayout.BoardControlGap,
                Is.GreaterThan(controlsTop + SafeAreaLayout.BoardControlGap),
                "A non-overlapping board region must remain between the controls.");

            Assert.That(card.rect.width, Is.LessThan(safeArea.width));
            Assert.That(card.rect.height, Is.LessThan(safeArea.height));
            Assert.That(playAgain.rect.xMin, Is.GreaterThan(card.rect.xMin));
            Assert.That(playAgain.rect.xMax, Is.LessThan(card.rect.xMax));
            Assert.That(playAgain.rect.yMin, Is.GreaterThan(card.rect.yMin));
            Assert.That(playAgain.rect.yMax, Is.LessThan(card.rect.yMax));
        }

        private void Set(string field, RectTransform value)
        {
            typeof(SafeAreaLayout).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(layout, value);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
