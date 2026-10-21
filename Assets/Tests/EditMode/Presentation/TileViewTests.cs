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

        [Test]
        public void Initialize_SetsPositionAndShowsTheTypesSprite()
        {
            var view = TestTiles.CreateTemplate("InitializeTestTile");
            try
            {
                view.Initialize(new GridPosition(2, 3), TileType.VerticalRocket);

                Assert.That(view.Position, Is.EqualTo(new GridPosition(2, 3)));
                Assert.That(view.Type, Is.EqualTo(TileType.VerticalRocket));
                Assert.That(view.GetComponent<SpriteRenderer>().sprite, Is.SameAs(TestTiles.SpriteFor(TileType.VerticalRocket)));
                Assert.That(view.transform.childCount, Is.Zero, "No temporary label child remains.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view.gameObject);
            }
        }
    }
}
