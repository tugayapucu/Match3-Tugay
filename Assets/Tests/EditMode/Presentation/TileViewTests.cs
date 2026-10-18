using System;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class TileViewTests
    {
        [TestCase(TileType.HorizontalRocket)]
        [TestCase(TileType.VerticalRocket)]
        [TestCase(TileType.Propeller)]
        [TestCase(TileType.TNT)]
        [TestCase(TileType.LightBall)]
        public void Initialize_Special_IsVisibleAndRetainsItsLogicalType(TileType type)
        {
            var tile = new GameObject("SpecialTestTile");
            try
            {
                var view = tile.AddComponent<TileView>();
                view.Initialize(new GridPosition(2, 3), type);

                Assert.That(view.Type, Is.EqualTo(type));
                Assert.That(view.Position, Is.EqualTo(new GridPosition(2, 3)));
                Assert.That(tile.GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));

                Assert.Throws<ArgumentOutOfRangeException>(() => view.SetTileType((TileType)99));
                Assert.That(view.Type, Is.EqualTo(type), "An invalid type must leave the view unchanged.");
                view.SetTileType(TileType.Red);
                Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.red));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tile);
            }
        }
    }
}
