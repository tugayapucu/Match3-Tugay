using System.Reflection;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    // Builds an inactive TileView template wired like Tile.prefab, so fixtures can render boards without loading assets.
    internal static class TestTiles
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        public static TileView CreateTemplate(string name)
        {
            var root = new GameObject(name);
            // Inactive so clones start inactive too; BoardView activates them after Initialize.
            root.SetActive(false);
            var view = root.AddComponent<TileView>();

            var labelObject = new GameObject("SpecialLabel");
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TMPro.TextMeshPro>();
            typeof(TileView).GetField("specialLabel", Fields).SetValue(view, label);
            return view;
        }
    }
}
