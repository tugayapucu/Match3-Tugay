using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
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
            var content = CreateRect("Content", topBar);
            content.sizeDelta = new Vector2(1008f, 112f);

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
            Set("compactContent", content);
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

        // Expanded (short board), Scaled-width bar (content scaling arrives with the Compact HUD content), and a
        // row below the 220 u button minimum that still fits.
        [TestCase(0, 460, 1080, 1000, 722f, 340f, TestName = "Expanded_1080x1000")]
        [TestCase(190, 0, 700, 1920, 628f, 298f, TestName = "NarrowBar_700x1920")]
        [TestCase(290, 0, 500, 1920, 428f, 198f, TestName = "BelowButtonMinimum_500x1920")]
        public void CompactModes_SizeTheBarAndTheRow(int x, int y, int width, int height, float barWidth, float buttonWidth)
        {
            var safeArea = new Rect(x, y, width, height);
            layout.ApplyLayout(safeArea, new Vector2Int(1080, 1920), 1f);

            Assert.That(layout.TopBar.sizeDelta, Is.EqualTo(new Vector2(barWidth, 112f)));
            Assert.That(layout.TopBar.anchoredPosition, Is.EqualTo(new Vector2(0f, -24f)));
            AssertRow(buttonWidth);
            Assert.That(barWidth, Is.LessThanOrEqualTo(width - 2f * HudTierLayout.HudSideMargin));
            Assert.That(layout.BottomControls.sizeDelta.x, Is.LessThanOrEqualTo(width - 2f * HudTierLayout.HudSideMargin));
        }

        [Test]
        public void ApplyLayout_AfterANarrowSafeArea_ReturnsToTheFullLayout()
        {
            layout.ApplyLayout(new Rect(290, 0, 500, 1920), new Vector2Int(1080, 1920), 1f);
            layout.ApplyLayout(new Rect(0, 0, 1080, 1920), new Vector2Int(1080, 1920), 1f);

            // 8.4 cells of 120 u: float rounding can give 1008.0001.
            Assert.That(layout.TopBar.sizeDelta.x, Is.EqualTo(1008f).Within(0.001f));
            Assert.That(layout.TopBar.sizeDelta.y, Is.EqualTo(112f));
            AssertRow(340f);
        }

        [TestCase("menuButton", true, TestName = "MissingMenu")]
        [TestCase("restartButton", true, TestName = "MissingRestart")]
        [TestCase("menuButton", false, TestName = "MenuOutsideTheRow")]
        [TestCase("restartButton", false, TestName = "RestartOutsideTheRow")]
        public void Awake_WithoutBothButtonsInsideTheRow_Throws(string field, bool missing)
        {
            var broken = new GameObject("BrokenLayout", typeof(RectTransform));
            broken.SetActive(false);
            broken.transform.SetParent(canvasObject.transform, false);
            var brokenLayout = broken.AddComponent<SafeAreaLayout>();
            foreach (string name in new[] { "topBar", "compactContent", "bottomControls", "menuButton", "restartButton" })
            {
                Copy(name, brokenLayout);
            }
            var outside = (RectTransform)new GameObject("Outside", typeof(RectTransform)).transform;
            outside.SetParent(broken.transform, false);
            typeof(SafeAreaLayout).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(brokenLayout, missing ? null : outside);

            LogAssert.Expect(LogType.Exception,
                new Regex("InvalidOperationException: Assign the Menu and Restart buttons inside BottomControls"));
            broken.SetActive(true);
            // Removed before the next frame, so a broken layout never runs Update.
            Object.DestroyImmediate(broken);
        }

        // Menu sits on the row's left edge and Restart on its right edge; the tier's gap is what is left between.
        private void AssertRow(float buttonWidth)
        {
            var controls = layout.BottomControls;
            var menu = (RectTransform)controls.Find("MenuButton");
            var restart = (RectTransform)controls.Find("RestartButton");
            float gap = HudTierLayout.Compact.ButtonGap;

            Assert.That(controls.sizeDelta, Is.EqualTo(new Vector2(2f * buttonWidth + gap, 112f)));
            Assert.That(controls.anchoredPosition, Is.EqualTo(new Vector2(0f, 48f)));
            Assert.That(menu.sizeDelta, Is.EqualTo(new Vector2(buttonWidth, 112f)));
            Assert.That(restart.sizeDelta, Is.EqualTo(new Vector2(buttonWidth, 112f)));

            Rect row = controls.rect;
            Rect left = LocalRect(menu, controls);
            Rect right = LocalRect(restart, controls);
            Assert.That(left.xMin, Is.EqualTo(row.xMin).Within(0.001f));
            Assert.That(right.xMax, Is.EqualTo(row.xMax).Within(0.001f));
            Assert.That(right.xMin - left.xMax, Is.EqualTo(gap).Within(0.001f));
            Assert.That(left.yMin, Is.EqualTo(row.yMin).Within(0.001f));
            Assert.That(right.yMin, Is.EqualTo(row.yMin).Within(0.001f));
        }

        [TestCase(1080, 1200, CompactHudMode.Aligned)]
        [TestCase(1080, 1000, CompactHudMode.Expanded)]
        [TestCase(700, 1920, CompactHudMode.Scaled)]
        [TestCase(1080, 200, CompactHudMode.Expanded)]
        public void Content_FollowsTheWidthContract_AndRecoversWithoutChangingReservedHeight(
            int width, int height, CompactHudMode expectedMode)
        {
            var screen = new Vector2Int(1080, 1920);
            var area = new Rect((1080 - width) * .5f, 0, width, height);
            layout.ApplyLayout(area, screen, 1);
            var content = (RectTransform)layout.TopBar.Find("Content");
            var geometry = HudTierLayout.CompactHud(area.size, layout.SideMargin);
            Assert.That(geometry.Mode, Is.EqualTo(expectedMode));
            Assert.That(content.sizeDelta, Is.EqualTo(new Vector2(geometry.ContentWidth, 112)));
            Assert.That(content.localScale, Is.EqualTo(new Vector3(geometry.ContentScale, geometry.ContentScale, 1)));
            Assert.That(content.rect.width * content.localScale.x, Is.EqualTo(layout.TopBar.rect.width).Within(.001f));
            Assert.That(layout.TopBar.rect.height, Is.EqualTo(112));

            layout.ApplyLayout(new Rect(0, 0, 1080, 1920), screen, 1);
            Assert.That(content.localScale, Is.EqualTo(Vector3.one));
            Assert.That(content.rect.width, Is.EqualTo(1008).Within(.001f));
            Assert.That(content.rect.height, Is.EqualTo(112));
            Assert.That(layout.TopBar.rect.height, Is.EqualTo(112));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Awake_WithoutContentInsideTopBar_Throws(bool missing)
        {
            var broken = new GameObject("BrokenContentLayout", typeof(RectTransform));
            broken.SetActive(false);
            broken.transform.SetParent(canvasObject.transform, false);
            var target = broken.AddComponent<SafeAreaLayout>();
            foreach (string field in new[] { "topBar", "bottomControls", "menuButton", "restartButton" }) Copy(field, target);
            typeof(SafeAreaLayout).GetField("compactContent", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, missing ? null : broken.transform);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Assign the Compact HUD content inside TopBar"));
            broken.SetActive(true);
            Object.DestroyImmediate(broken);
        }

        private void Copy(string field, SafeAreaLayout target)
        {
            FieldInfo info = typeof(SafeAreaLayout).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            info.SetValue(target, info.GetValue(layout));
        }

        // A child's rect in its parent's local space (no rotation or scale in this fixture).
        private static Rect LocalRect(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Vector3 min = parent.InverseTransformPoint(corners[0]);
            Vector3 max = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
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
