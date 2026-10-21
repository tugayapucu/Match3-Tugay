using Match3.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class TilePrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/Tile.prefab";
        private const string ArtFolder = "Assets/Art/Tiles/PrismaticEnergy/";

        // The real prefab must show each production PNG imported in Step 1, without changing size or picking.
        [TestCase(TileType.Red, "Normal/Red.png")]
        [TestCase(TileType.Green, "Normal/Green.png")]
        [TestCase(TileType.Blue, "Normal/Blue.png")]
        [TestCase(TileType.Yellow, "Normal/Yellow.png")]
        [TestCase(TileType.Purple, "Normal/Purple.png")]
        [TestCase(TileType.Orange, "Normal/Orange.png")]
        [TestCase(TileType.HorizontalRocket, "Specials/BeamCrystal_H.png")]
        [TestCase(TileType.VerticalRocket, "Specials/BeamCrystal_V.png")]
        [TestCase(TileType.Propeller, "Specials/Wisp.png")]
        [TestCase(TileType.TNT, "Specials/NovaGem.png")]
        [TestCase(TileType.LightBall, "Specials/SpectrumCore.png")]
        public void Prefab_EveryVisibleType_ShowsItsProductionSprite(TileType type, string artFile)
        {
            var expected = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + artFile);
            Assert.That(expected, Is.Not.Null, artFile);
            var tile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                tile.GetComponent<TileView>().Initialize(new GridPosition(0, 0), type);

                var renderer = tile.GetComponent<SpriteRenderer>();
                Assert.That(renderer.sprite, Is.SameAs(expected));
                Assert.That(renderer.color, Is.EqualTo(Color.white));
                Assert.That(renderer.enabled, Is.True);
                // One sprite fills one board cell, exactly like the old square: no root or collider changes.
                Assert.That(expected.bounds.size.x, Is.EqualTo(1f));
                Assert.That(expected.bounds.size.y, Is.EqualTo(1f));
                Assert.That(tile.transform.localScale, Is.EqualTo(new Vector3(0.9f, 0.9f, 1f)));
                Assert.That(tile.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(tile.GetComponent<BoxCollider2D>().size, Is.EqualTo(Vector2.one));
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tile), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }

        [TestCase(TileType.HorizontalRocket, "<->", 0f)]
        [TestCase(TileType.VerticalRocket, "<->", 90f)]
        [TestCase(TileType.Propeller, "P", 0f)]
        [TestCase(TileType.TNT, "TNT", 0f)]
        [TestCase(TileType.LightBall, "LB", 0f)]
        public void Prefab_SpecialHasWiredReadableSymbolAndOrientation(TileType type, string symbol, float angle)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
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
