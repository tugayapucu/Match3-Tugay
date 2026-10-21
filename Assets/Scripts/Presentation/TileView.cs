using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    // The visual for one board cell. Shows the TileType's pre-colored sprite; holds no game rules.
    // RequireComponent makes Unity add a SpriteRenderer automatically, so GetComponent below cannot miss.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        // One production sprite per visible TileType, assigned on Tile.prefab. Field names follow the Core
        // TileType names; the art itself uses the Prismatic Energy names (e.g. Rocket → Beam Crystal).
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite greenSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Sprite yellowSprite;
        [SerializeField] private Sprite purpleSprite;
        [SerializeField] private Sprite orangeSprite;
        [SerializeField] private Sprite horizontalRocketSprite;
        [SerializeField] private Sprite verticalRocketSprite;
        [SerializeField] private Sprite propellerSprite;
        [SerializeField] private Sprite tntSprite;
        [SerializeField] private Sprite lightBallSprite;

        // Cached to avoid repeated GetComponent calls.
        private SpriteRenderer spriteRenderer;

        // These describe what the view represents; the Board remains authoritative.
        public GridPosition Position { get; private set; }
        public TileType Type { get; private set; }

        // Unity lifecycle callback; makes a fresh instance render its default (Empty = hidden) state.
        private void Awake()
        {
            SetTileType(Type);
        }

        // Called by BoardView right after Instantiate to say which cell this view shows.
        public void Initialize(GridPosition position, TileType tileType)
        {
            SetTileType(tileType);
            Position = position;
        }

        public void SetTileType(TileType tileType)
        {
            // Look up the sprite first so an invalid type or an unassigned sprite throws before any state changes.
            Sprite sprite = GetSprite(tileType);
            if (tileType != TileType.Empty && sprite == null)
            {
                throw new InvalidOperationException($"Assign the {tileType} sprite before displaying that tile.");
            }

            // Lazy cache: Initialize can run before Awake when the prefab is inactive.
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Type = tileType;
            spriteRenderer.sprite = sprite;
            // The art is already colored, so it rests untinted; animations may tint briefly and restore white.
            spriteRenderer.color = Color.white;
            // Empty cells keep their GameObject but draw nothing.
            spriteRenderer.enabled = tileType != TileType.Empty;
        }

        // BoardView decides where tiles go; the tile just applies it.
        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        // Movement has finished; update the represented cell without changing the tile's appearance.
        public void SetGridPosition(GridPosition position)
        {
            Position = position;
        }

        // TileType -> sprite mapping lives here, not in Core, because appearance is a presentation detail.
        // Empty has no sprite: its cell keeps the view hidden.
        private Sprite GetSprite(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Empty: return null;
                case TileType.Red: return redSprite;
                case TileType.Green: return greenSprite;
                case TileType.Blue: return blueSprite;
                case TileType.Yellow: return yellowSprite;
                case TileType.Purple: return purpleSprite;
                case TileType.Orange: return orangeSprite;
                case TileType.HorizontalRocket: return horizontalRocketSprite;
                case TileType.VerticalRocket: return verticalRocketSprite;
                case TileType.Propeller: return propellerSprite;
                case TileType.TNT: return tntSprite;
                case TileType.LightBall: return lightBallSprite;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tileType), tileType, "Unknown tile type.");
            }
        }
    }
}
