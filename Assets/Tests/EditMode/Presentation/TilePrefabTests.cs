using Match3.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class TilePrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/Tile.prefab";
        private const string ArtFolder = "Assets/Art/Tiles/PrismaticEnergy/";

        // The real prefab shows each production PNG with one world unit per visual/collider cell.
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
                // The image includes transparent padding; its rectangle and collider cover one full cell.
                Assert.That(expected.bounds.size.x, Is.EqualTo(1f));
                Assert.That(expected.bounds.size.y, Is.EqualTo(1f));
                Assert.That(tile.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(tile.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(tile.GetComponent<BoxCollider2D>().size, Is.EqualTo(Vector2.one));
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tile), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }

        [Test]
        public void Prefab_HasNoTemporaryLabel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab.transform.childCount, Is.Zero, "The Phase 10 SpecialLabel child is gone.");
            Assert.That(prefab.GetComponentsInChildren<Component>(true).Length, Is.EqualTo(4),
                "Transform, SpriteRenderer, TileView and BoxCollider2D only.");
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab), Is.Zero);
        }
    }
}
