using System;
using Match3.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    public class SelectionHighlightTests
    {
        [Test]
        public void ProductionPrefab_HighlightIsInactiveVisualOnly_WithApprovedImportSettings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tile.prefab");
            var child = prefab.transform.Find("SelectionHighlight");
            Assert.That(child, Is.Not.Null);
            Assert.That(child.gameObject.activeSelf, Is.False);
            Assert.That(child.GetComponents<Component>().Length, Is.EqualTo(2));
            Assert.That(child.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(child.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(child.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(child.localScale, Is.EqualTo(Vector3.one));
            var renderer = child.GetComponent<SpriteRenderer>();
            var main = prefab.GetComponent<SpriteRenderer>();
            Assert.That(renderer.sortingLayerID, Is.EqualTo(main.sortingLayerID));
            Assert.That(main.sortingOrder, Is.Zero);
            Assert.That(renderer.sortingOrder, Is.EqualTo(1));
            Assert.That(renderer.sharedMaterial, Is.SameAs(main.sharedMaterial));
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(renderer.sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(512f));
            Assert.That(renderer.sprite.rect.size, Is.EqualTo(new Vector2(512f, 512f)));
            Assert.That(renderer.sprite.pivot, Is.EqualTo(new Vector2(256f, 256f)));
            Assert.That(importer.mipmapEnabled, Is.True);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            var android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.RGBA32));
        }

        [Test]
        public void SetSelected_RepeatedlyTogglesOnlyTheChild_ForEveryVisibleType()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tile.prefab"));
            try
            {
                var view = root.GetComponent<TileView>();
                var child = root.transform.Find("SelectionHighlight").gameObject;
                var renderer = root.GetComponent<SpriteRenderer>();
                var collider = root.GetComponent<BoxCollider2D>();
                foreach (var type in TestTiles.VisibleTypes)
                {
                    view.Initialize(new GridPosition(2, 3), type);
                    root.transform.localScale = new Vector3(0.8f, 1.1f, 1f);
                    renderer.color = new Color(0.7f, 0.8f, 0.9f, 0.6f);
                    Physics2D.SyncTransforms();
                    var bounds = collider.bounds;
                    var sprite = renderer.sprite;
                    for (int i = 0; i < 6; i++)
                    {
                        bool selected = i % 2 == 0;
                        view.SetSelected(selected);
                        Physics2D.SyncTransforms();
                        Assert.That(child.activeSelf, Is.EqualTo(selected));
                        Assert.That(view.Type, Is.EqualTo(type));
                        Assert.That(view.Position, Is.EqualTo(new GridPosition(2, 3)));
                        Assert.That(root.transform.localScale, Is.EqualTo(new Vector3(0.8f, 1.1f, 1f)));
                        Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
                        Assert.That(renderer.sprite, Is.SameAs(sprite));
                        Assert.That(renderer.color, Is.EqualTo(new Color(0.7f, 0.8f, 0.9f, 0.6f)));
                        Assert.That(renderer.enabled, Is.True);
                        Assert.That(collider.enabled, Is.True);
                        Assert.That(collider.size, Is.EqualTo(Vector2.one));
                        Assert.That(collider.bounds, Is.EqualTo(bounds));
                    }
                    view.SetSelected(true);
                    view.Initialize(new GridPosition(2, 3), type);
                    Assert.That(child.activeSelf, Is.False, "Reinitialization clears the cue.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("missing")]
        [TestCase("sprite")]
        [TestCase("root")]
        public void MissingOrIncorrectHighlight_FailsBothStartupValidationAndSetSelected(string fault)
        {
            var view = TestTiles.CreateTemplate("InvalidHighlight");
            var boardObject = new GameObject("InvalidHighlightBoard");
            try
            {
                var child = view.transform.Find("SelectionHighlight");
                if (fault == "sprite") { child.GetComponent<SpriteRenderer>().sprite = null; }
                var serialized = new SerializedObject(view);
                if (fault == "missing") { serialized.FindProperty("selectionHighlight").objectReferenceValue = null; }
                if (fault == "root") { serialized.FindProperty("selectionHighlight").objectReferenceValue = view.GetComponent<SpriteRenderer>(); }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var boardView = boardObject.AddComponent<BoardView>();
                var boardSetup = new SerializedObject(boardView);
                boardSetup.FindProperty("tilePrefab").objectReferenceValue = view;
                boardSetup.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => view.SetSelected(true));
                Assert.Throws<InvalidOperationException>(() => view.SetSelected(false));
                Assert.Throws<InvalidOperationException>(() => boardView.ValidateConfiguration(new[] { TileType.Red }));
                Assert.That(child.gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(view.gameObject); Object.DestroyImmediate(boardObject); }
        }
    }
}
