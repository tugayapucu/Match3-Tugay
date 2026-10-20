using Match3.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class TilePrefabTests
    {
        [TestCase(TileType.HorizontalRocket, "<->", 0f)]
        [TestCase(TileType.VerticalRocket, "<->", 90f)]
        [TestCase(TileType.Propeller, "P", 0f)]
        [TestCase(TileType.TNT, "TNT", 0f)]
        [TestCase(TileType.LightBall, "LB", 0f)]
        public void Prefab_SpecialHasWiredReadableSymbolAndOrientation(TileType type, string symbol, float angle)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tile.prefab");
            Assert.That(prefab, Is.Not.Null);
            var tile = Object.Instantiate(prefab);
            try
            {
                var view = tile.GetComponent<TileView>();
                view.Initialize(new GridPosition(0, 0), type);
                var label = tile.GetComponentInChildren<TMP_Text>(true);
                Assert.That(label, Is.Not.Null);
                Assert.That(label.font, Is.Not.Null);
                Assert.That(label.fontSharedMaterial, Is.Not.Null);
                Assert.That(label.fontSharedMaterial.shader, Is.Not.Null);
                Assert.That(label.gameObject.activeInHierarchy, Is.True);
                Assert.That(label.text, Is.EqualTo(symbol));
                Assert.That(label.richText, Is.False, "Rocket arrows must stay literal text.");
                Assert.That(Quaternion.Angle(label.transform.localRotation, Quaternion.Euler(0f, 0f, angle)), Is.LessThan(0.01f));
                var textRenderer = label.GetComponent<MeshRenderer>();
                var background = tile.GetComponent<SpriteRenderer>();
                Assert.That(textRenderer.sortingLayerID, Is.EqualTo(background.sortingLayerID));
                Assert.That(textRenderer.sortingOrder, Is.GreaterThan(background.sortingOrder));
                label.ForceMeshUpdate();
                Assert.That(label.textInfo.characterCount, Is.EqualTo(symbol.Length));
                Assert.That(label.textBounds.size.x, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.85f));
                Assert.That(label.textBounds.size.y, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.85f));
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tile), Is.Zero);
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(label.gameObject), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }
    }
}
