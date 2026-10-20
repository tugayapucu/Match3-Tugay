using System;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class TileViewTests
    {
        [TestCase(TileType.HorizontalRocket, "<->", 0f)]
        [TestCase(TileType.VerticalRocket, "<->", 90f)]
        [TestCase(TileType.Propeller, "P", 0f)]
        [TestCase(TileType.TNT, "TNT", 0f)]
        [TestCase(TileType.LightBall, "LB", 0f)]
        public void Initialize_Special_IsVisibleAndRetainsItsLogicalType(TileType type, string symbol, float angle)
        {
            var tile = new GameObject("SpecialTestTile");
            try
            {
                var view = tile.AddComponent<TileView>();
                var labelObject = new GameObject("SpecialLabel");
                labelObject.transform.SetParent(tile.transform, false);
                var label = labelObject.AddComponent<TMPro.TextMeshPro>();
                var serialized = new UnityEditor.SerializedObject(view);
                serialized.FindProperty("specialLabel").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
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
                Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.red));
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
            var tile = new GameObject("MissingLabelTile");
            try
            {
                var view = tile.AddComponent<TileView>();
                view.SetTileType(TileType.Red);
                Assert.Throws<InvalidOperationException>(() => view.SetTileType(TileType.TNT));
                Assert.That(view.Type, Is.EqualTo(TileType.Red));
                Assert.That(tile.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.red));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tile);
            }
        }
    }
}
