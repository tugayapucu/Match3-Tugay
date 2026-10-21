using System;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class TileViewTests
    {
        [Test]
        public void SetTileType_EveryVisibleType_ShowsItsMappedSpriteUntinted()
        {
            var view = TestTiles.CreateTemplate("MappingTestTile");
            try
            {
                var renderer = view.GetComponent<SpriteRenderer>();
                foreach (TileType type in TestTiles.VisibleTypes)
                {
                    view.SetTileType(type);

                    Assert.That(view.Type, Is.EqualTo(type));
                    Assert.That(renderer.sprite, Is.SameAs(TestTiles.SpriteFor(type)), type.ToString());
                    Assert.That(renderer.color, Is.EqualTo(Color.white), "Pre-colored art rests untinted.");
                    Assert.That(renderer.enabled, Is.True);
                    // Orientation comes from the sprite itself; the tile and its collider never rotate.
                    Assert.That(view.transform.localRotation, Is.EqualTo(Quaternion.identity));
                }

                view.SetTileType(TileType.Empty);
                Assert.That(renderer.enabled, Is.False, "Empty cells draw nothing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void SetTileType_InvalidType_FailsBeforeChangingTheView()
        {
            var view = TestTiles.CreateTemplate("InvalidTypeTestTile");
            try
            {
                var renderer = view.GetComponent<SpriteRenderer>();
                view.SetTileType(TileType.Blue);

                Assert.Throws<ArgumentOutOfRangeException>(() => view.SetTileType((TileType)99));

                Assert.That(view.Type, Is.EqualTo(TileType.Blue));
                Assert.That(renderer.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Blue)));
                Assert.That(renderer.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void SetTileType_MissingSprite_FailsBeforeChangingTheView()
        {
            var tile = new GameObject("MissingSpriteTile");
            try
            {
                // No sprites assigned: an Empty view is fine, a visible type is a setup error.
                var view = tile.AddComponent<TileView>();
                var renderer = tile.GetComponent<SpriteRenderer>();
                // EditMode does not run Awake on AddComponent, so set the starting state explicitly.
                view.SetTileType(TileType.Empty);

                Assert.Throws<InvalidOperationException>(() => view.SetTileType(TileType.Red));

                Assert.That(view.Type, Is.EqualTo(TileType.Empty));
                Assert.That(renderer.sprite, Is.Null);
                Assert.That(renderer.enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tile);
            }
        }

        [TestCase(TileType.HorizontalRocket, "<->", 0f)]
        [TestCase(TileType.VerticalRocket, "<->", 90f)]
        [TestCase(TileType.Propeller, "P", 0f)]
        [TestCase(TileType.TNT, "TNT", 0f)]
        [TestCase(TileType.LightBall, "LB", 0f)]
        public void Initialize_Special_IsVisibleAndRetainsItsLogicalType(TileType type, string symbol, float angle)
        {
            var view = TestTiles.CreateTemplate("SpecialTestTile");
            var tile = view.gameObject;
            try
            {
                var label = tile.GetComponentInChildren<TMPro.TMP_Text>(true);
                view.Initialize(new GridPosition(2, 3), type);

                Assert.That(view.Type, Is.EqualTo(type));
                Assert.That(view.Position, Is.EqualTo(new GridPosition(2, 3)));
                Assert.That(tile.GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
                Assert.That(label.gameObject.activeSelf, Is.True);
                Assert.That(label.text, Is.EqualTo(symbol));
                Assert.That(Quaternion.Angle(label.transform.localRotation, Quaternion.Euler(0f, 0f, angle)), Is.LessThan(0.01f));
                Assert.That(tile.transform.localRotation, Is.EqualTo(Quaternion.identity));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.SetTileType((TileType)99));
                Assert.That(view.Type, Is.EqualTo(type), "An invalid type must leave the view unchanged.");
                Assert.That(label.text, Is.EqualTo(symbol));
                view.SetTileType(TileType.Red);
                Assert.That(tile.GetComponent<SpriteRenderer>().sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
                Assert.That(label.gameObject.activeSelf, Is.False);
                Assert.That(label.text, Is.Empty);
                Assert.That(label.transform.localRotation, Is.EqualTo(Quaternion.identity));
                view.SetTileType(TileType.Empty);
                Assert.That(tile.GetComponent<SpriteRenderer>().enabled, Is.False);
                Assert.That(label.gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tile);
            }
        }

        [Test]
        public void SetTileType_MissingSpecialLabel_FailsBeforeChangingTheView()
        {
            var view = TestTiles.CreateTemplate("MissingLabelTile");
            var tile = view.gameObject;
            try
            {
                typeof(TileView).GetField("specialLabel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(view, null);
                view.SetTileType(TileType.Red);
                Assert.Throws<InvalidOperationException>(() => view.SetTileType(TileType.TNT));
                Assert.That(view.Type, Is.EqualTo(TileType.Red));
                Assert.That(tile.GetComponent<SpriteRenderer>().sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tile);
            }
        }
    }
}
