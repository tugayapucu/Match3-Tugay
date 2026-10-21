using System.Collections.Generic;
using System.Reflection;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    // Builds an inactive TileView template wired like Tile.prefab, so fixtures can render boards without loading assets.
    // The PlayMode test assembly has its own copy: the two test assemblies cannot share code.
    internal static class TestTiles
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        // The TileView sprite slot for every visible TileType.
        private static readonly Dictionary<TileType, string> SpriteFields = new Dictionary<TileType, string>
        {
            { TileType.Red, "redSprite" },
            { TileType.Green, "greenSprite" },
            { TileType.Blue, "blueSprite" },
            { TileType.Yellow, "yellowSprite" },
            { TileType.Purple, "purpleSprite" },
            { TileType.Orange, "orangeSprite" },
            { TileType.HorizontalRocket, "horizontalRocketSprite" },
            { TileType.VerticalRocket, "verticalRocketSprite" },
            { TileType.Propeller, "propellerSprite" },
            { TileType.TNT, "tntSprite" },
            { TileType.LightBall, "lightBallSprite" }
        };

        private static readonly Dictionary<TileType, Sprite> Sprites = new Dictionary<TileType, Sprite>();

        public static IEnumerable<TileType> VisibleTypes => SpriteFields.Keys;

        // One distinct generated sprite per visible TileType, shared by every template, so tests can compare by reference.
        public static Sprite SpriteFor(TileType type)
        {
            // Unity's == also catches a sprite destroyed since it was cached.
            if (!Sprites.TryGetValue(type, out var sprite) || sprite == null)
            {
                sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                sprite.name = type + "TestSprite";
                sprite.hideFlags = HideFlags.HideAndDontSave;
                Sprites[type] = sprite;
            }

            return sprite;
        }

        public static TileView CreateTemplate(string name)
        {
            var root = new GameObject(name);
            // Inactive so clones start inactive too; BoardView activates them after Initialize.
            root.SetActive(false);
            var view = root.AddComponent<TileView>();
            foreach (var slot in SpriteFields)
            {
                typeof(TileView).GetField(slot.Value, Fields).SetValue(view, SpriteFor(slot.Key));
            }

            return view;
        }
    }
}
