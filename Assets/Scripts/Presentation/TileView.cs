using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    // The visual for one board cell. Shows a TileType as a coloured sprite; holds no game rules.
    // RequireComponent makes Unity add a SpriteRenderer automatically, so GetComponent below cannot miss.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
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
            // Look up the colour first so an invalid type throws before any state changes.
            Color color = GetColor(tileType);
            // Lazy cache: Initialize can run before Awake when the prefab is inactive.
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Type = tileType;
            spriteRenderer.color = color;
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

        // TileType -> colour mapping lives here, not in Core, because colour is a presentation detail.
        private static Color GetColor(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Empty:
                    return Color.clear;
                case TileType.Red:
                    return Color.red;
                case TileType.Green:
                    return Color.green;
                case TileType.Blue:
                    return Color.blue;
                case TileType.Yellow:
                    return Color.yellow;
                case TileType.Purple:
                    return new Color(0.6f, 0.2f, 0.8f);
                case TileType.Orange:
                    return new Color(1f, 0.5f, 0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(tileType), tileType, "Unknown tile type.");
            }
        }
    }
}
