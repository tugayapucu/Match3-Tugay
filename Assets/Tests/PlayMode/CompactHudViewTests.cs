#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Match3.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    public class CompactHudViewTests
    {
        private GameObject canvasObject;
        private RectTransform root;
        private RectTransform content;
        private HudStatsView stats;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("CompactTestCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<Canvas>().referencePixelsPerUnit = 100;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CompactHud.prefab");
            root = Object.Instantiate(prefab, canvasObject.transform).GetComponent<RectTransform>();
            content = (RectTransform)root.Find("Content");
            stats = root.GetComponent<HudStatsView>();
            stats.ValidateConfiguration();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(canvasObject);
            yield return null;
        }

        [TestCase(722f)]
        [TestCase(803.12195f)]
        [TestCase(1008f)]
        public void ApprovedWorstCases_AllTextRolesFitAfterLayoutRebuild(float width)
        {
            Size(width);
            stats.ShowLevel(22, 22);
            var progress = new LevelProgress(23, TileType.Red, 22, 1);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red }));
            stats.Show(progress, TestTiles.SpriteFor(TileType.Red));
            FindText("Goal/Data/Values/Count").text = "22";
            FindText("Score/Value").text = "222222";
            Rebuild();

            Assert.That(content.GetComponentsInChildren<TMP_Text>().Length, Is.EqualTo(9));
            foreach (var text in content.GetComponentsInChildren<TMP_Text>())
            {
                Assert.That(text.isTextOverflowing, Is.False, text.name);
                Assert.That(text.textInfo.lineCount, Is.EqualTo(1), text.name);
                Assert.That(text.textInfo.characterCount, Is.EqualTo(text.text.Length), text.name);
                AssertVisibleInkInside(text, text.rectTransform);
                var column = text.transform;
                while (column.parent != content) column = column.parent;
                AssertVisibleInkInside(text, (RectTransform)column);
                AssertVisibleInkInside(text, root);
            }

            var score = FindText("Score/Value");
            Assert.That(score.fontSize, Is.InRange(40f, 48f));
            AssertSameBaseline(FindText("Level/Values/Number"), FindText("Level/Values/Total"));
            AssertSameBaseline(FindText("Goal/Data/Values/Count"), FindText("Goal/Data/Values/Total"));
            if (width == 722)
            {
                string[] columns = { "Level", "Goal", "Moves", "Score" };
                float[] minimums = { 140, 276, 130, 150 };
                for (int i = 0; i < columns.Length; i++)
                    Assert.That(((RectTransform)content.Find(columns[i])).rect.width, Is.EqualTo(minimums[i]).Within(.01f));
                Assert.That(FindText("Moves/Label").text, Is.EqualTo("MOVES"));
            }
        }

        [TestCase("22222")]
        [TestCase("999999")]
        public void OtherApprovedScores_StayReadableWithoutTruncation(string value)
        {
            Size(722);
            var score = FindText("Score/Value");
            score.text = value;
            Rebuild();
            Assert.That(score.fontSize, Is.InRange(40f, 48f));
            Assert.That(score.isTextOverflowing, Is.False);
            Assert.That(score.textInfo.characterCount, Is.EqualTo(value.Length));
            AssertVisibleInkInside(score, score.rectTransform);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(15)]
        [TestCase(30)]
        public void Fill_UsesTheRealFractionAtMinimumWidth_AndResets(int collected)
        {
            Size(722);
            var progress = new LevelProgress(22, TileType.Red, 30, 100);
            var tiles = new List<TileType>();
            for (int i = 0; i < collected; i++) tiles.Add(TileType.Red);
            if (collected > 0) progress.ApplyValidMove(new ResolutionResult(tiles));
            stats.Show(progress, TestTiles.SpriteFor(TileType.Red));
            Rebuild();
            var fill = content.Find("Goal/Data/Bar/Fill").GetComponent<Image>();
            float expected = collected / 30f;
            Assert.That(fill.rectTransform.anchorMax, Is.EqualTo(new Vector2(expected, 1)));
            Assert.That(fill.rectTransform.rect.width, Is.EqualTo(142 * expected).Within(.001f));
            Assert.That(fill.rectTransform.rect.height, Is.EqualTo(14));
            Assert.That(fill.enabled, Is.EqualTo(collected > 0));
            if (collected > 0)
            {
                var mesh = fill.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), "A tiny sliced fill must still draw.");
                foreach (var vertex in mesh.vertices)
                    Assert.That(float.IsNaN(vertex.x) || float.IsInfinity(vertex.x), Is.False);
            }
            stats.Show(new LevelProgress(22, TileType.Blue, 25, 100), TestTiles.SpriteFor(TileType.Blue));
            Assert.That(fill.enabled, Is.False);
            Assert.That(fill.rectTransform.anchorMax.x, Is.Zero);
        }

        [Test]
        public void Prefab_UsesApprovedSpritesAndLayout_AndHasNoRaycastTargets()
        {
            Assert.That(root.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<ContentSizeFitter>(true), Is.Empty);
            var groups = root.GetComponentsInChildren<HorizontalLayoutGroup>(true);
            Assert.That(groups.Length, Is.EqualTo(1));
            Assert.That(groups[0].padding.horizontal, Is.EqualTo(20));
            Assert.That(groups[0].spacing, Is.Zero);
            Assert.That(groups[0].childForceExpandWidth, Is.False);
            var movesLabel = FindText("Moves/Label");
            Assert.That(movesLabel.characterSpacing, Is.EqualTo(5));
            Assert.That(movesLabel.fontSize, Is.EqualTo(32));
            Assert.That(content.Find("Moves").GetComponent<LayoutElement>().minWidth, Is.EqualTo(130));
            Assert.That(FindText("Level/Label").characterSpacing, Is.EqualTo(10));
            Assert.That(FindText("Score/Label").characterSpacing, Is.EqualTo(10));
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            string[] paths = { "Shell", "ShellShadow", "Content/Goal/Data/Bar/Track", "Content/Goal/Data/Bar/Fill" };
            string[] sprites = { "HudShell", "HudShellShadow", "GoalTrack", "GoalFill" };
            for (int i = 0; i < paths.Length; i++)
            {
                var image = root.Find(paths[i]).GetComponent<Image>();
                Assert.That(image.sprite, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Hud/Compact/" + sprites[i] + ".png")));
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1));
                Assert.That(image.pixelsPerUnit, Is.EqualTo(2));
                Assert.That(image.color, Is.EqualTo(Color.white));
            }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool total = text.name == "Total";
                Assert.That(text.font, Is.SameAs(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Fonts/Fredoka/Fredoka-" + (total ? "Medium" : "SemiBold") + " SDF.asset")));
                Assert.That(text.fontStyle, Is.EqualTo(FontStyles.Normal));
                Assert.That(text.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));
            }
            Assert.That(root.Find("ShellShadow").GetComponent<RectTransform>().offsetMin, Is.EqualTo(new Vector2(-60, -78)));
            Assert.That(root.Find("ShellShadow").GetComponent<RectTransform>().offsetMax, Is.EqualTo(new Vector2(60, 42)));
        }

        private void Size(float width)
        {
            root.sizeDelta = content.sizeDelta = new Vector2(width, 112);
            content.localScale = Vector3.one;
        }

        private TMP_Text FindText(string path) => content.Find(path).GetComponent<TMP_Text>();

        private void Rebuild()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
        }

        private static void AssertSameBaseline(TMP_Text a, TMP_Text b)
        {
            float yA = a.transform.TransformPoint(new Vector3(0, a.textInfo.characterInfo[0].baseLine)).y;
            float yB = b.transform.TransformPoint(new Vector3(0, b.textInfo.characterInfo[0].baseLine)).y;
            Assert.That(yA, Is.EqualTo(yB).Within(.01f));
        }

        // These texts have no outline, underlay, synthetic bold or italic. Glyph metrics describe ink;
        // TMP's vertex quads additionally include transparent SDF padding and are not an ink-fit test.
        private static void AssertVisibleInkInside(TMP_Text text, RectTransform container)
        {
            foreach (var character in text.textInfo.characterInfo)
            {
                if (!character.isVisible) continue;
                var metrics = character.textElement.glyph.metrics;
                float left = character.origin + metrics.horizontalBearingX * character.scale;
                float top = character.baseLine + metrics.horizontalBearingY * character.scale;
                var min = new Vector3(left, top - metrics.height * character.scale);
                var max = new Vector3(left + metrics.width * character.scale, top);
                var localMin = container.InverseTransformPoint(text.transform.TransformPoint(min));
                var localMax = container.InverseTransformPoint(text.transform.TransformPoint(max));
                Assert.That(localMin.x, Is.GreaterThanOrEqualTo(container.rect.xMin - .1f), text.name + " left in " + container.name);
                Assert.That(localMax.x, Is.LessThanOrEqualTo(container.rect.xMax + .1f), text.name + " right in " + container.name);
                Assert.That(localMin.y, Is.GreaterThanOrEqualTo(container.rect.yMin - .1f), text.name + " bottom in " + container.name);
                Assert.That(localMax.y, Is.LessThanOrEqualTo(container.rect.yMax + .1f), text.name + " top in " + container.name);
            }
        }
    }
}
#endif
