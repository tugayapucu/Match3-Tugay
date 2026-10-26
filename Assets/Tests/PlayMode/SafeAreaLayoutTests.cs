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
            topBar.sizeDelta = new Vector2(900f, 96f);

            var restart = CreateRect("RestartButton", hud);
            restart.anchorMin = restart.anchorMax = new Vector2(0.5f, 0f);
            restart.pivot = new Vector2(0.5f, 0f);
            restart.anchoredPosition = new Vector2(0f, 48f);
            restart.sizeDelta = new Vector2(300f, 128f);

            var outcome = CreateRect("OutcomePanel", hud);
            var card = CreateRect("Card", outcome);
            card.sizeDelta = new Vector2(640f, 400f);
            var playAgain = CreateRect("PlayAgainButton", card);
            playAgain.anchoredPosition = new Vector2(0f, 40f);
            playAgain.sizeDelta = new Vector2(300f, 84f);

            layout = safeRoot.gameObject.AddComponent<SafeAreaLayout>();
            typeof(SafeAreaLayout).GetField("topBar", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(layout, topBar);
            typeof(SafeAreaLayout).GetField("restartButton", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(layout, restart);
            canvasObject.SetActive(true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(canvasObject);
            yield return null;
        }

        [TestCase(0, 0, 1080, 1920, 900, TestName = "NoCutout")]
        [TestCase(0, 0, 1080, 1820, 900, TestName = "TopCutout")]
        [TestCase(0, 80, 1080, 1840, 900, TestName = "BottomGestureInset")]
        [TestCase(60, 70, 940, 1750, 844, TestName = "AsymmetricInsetsShrinkTopBar")]
        public void ApprovedPortraitLayout_StaysInsideSafeArea(
            int x, int y, int width, int height, int expectedBarWidth)
        {
            var safeArea = new Rect(x, y, width, height);
            layout.ApplyLayout(safeArea, new Vector2Int(1080, 1920), 1f);

            var root = layout.SafeAreaRoot;
            var topBar = layout.TopBar;
            var restart = layout.RestartButton;
            var card = (RectTransform)root.Find("LevelHud/OutcomePanel/Card");
            var playAgain = (RectTransform)card.Find("PlayAgainButton");

            Assert.That(root.anchorMin.x, Is.EqualTo(safeArea.xMin / 1080f).Within(0.0001f));
            Assert.That(root.anchorMin.y, Is.EqualTo(safeArea.yMin / 1920f).Within(0.0001f));
            Assert.That(root.anchorMax.x, Is.EqualTo(safeArea.xMax / 1080f).Within(0.0001f));
            Assert.That(root.anchorMax.y, Is.EqualTo(safeArea.yMax / 1920f).Within(0.0001f));
            Assert.That(root.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(root.offsetMax, Is.EqualTo(Vector2.zero));

            Assert.That(topBar.sizeDelta.x, Is.EqualTo(expectedBarWidth).Within(0.01f));
            Assert.That(topBar.sizeDelta.y, Is.EqualTo(96f));
            Assert.That(topBar.anchoredPosition, Is.EqualTo(new Vector2(0f, -24f)));
            Assert.That(restart.sizeDelta, Is.EqualTo(new Vector2(300f, 128f)));
            Assert.That(restart.anchoredPosition, Is.EqualTo(new Vector2(0f, 48f)));
            Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(640f, 400f)));

            float centerX = safeArea.center.x;
            float barLeft = centerX - topBar.rect.width / 2f;
            float barRight = centerX + topBar.rect.width / 2f;
            float barBottom = safeArea.yMax - 24f - topBar.rect.height;
            float restartLeft = centerX - restart.rect.width / 2f;
            float restartRight = centerX + restart.rect.width / 2f;
            float restartTop = safeArea.yMin + 48f + restart.rect.height;

            Assert.That(barLeft, Is.GreaterThanOrEqualTo(safeArea.xMin + layout.SideMargin));
            Assert.That(barRight, Is.LessThanOrEqualTo(safeArea.xMax - layout.SideMargin));
            Assert.That(restartLeft, Is.GreaterThanOrEqualTo(safeArea.xMin + layout.SideMargin));
            Assert.That(restartRight, Is.LessThanOrEqualTo(safeArea.xMax - layout.SideMargin));
            Assert.That(barBottom - SafeAreaLayout.BoardControlGap,
                Is.GreaterThan(restartTop + SafeAreaLayout.BoardControlGap),
                "A non-overlapping board region must remain between the controls.");

            Assert.That(card.rect.width, Is.LessThan(safeArea.width));
            Assert.That(card.rect.height, Is.LessThan(safeArea.height));
            Assert.That(playAgain.rect.xMin, Is.GreaterThan(card.rect.xMin));
            Assert.That(playAgain.rect.xMax, Is.LessThan(card.rect.xMax));
            Assert.That(playAgain.rect.yMin, Is.GreaterThan(card.rect.yMin));
            Assert.That(playAgain.rect.yMax, Is.LessThan(card.rect.yMax));
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
