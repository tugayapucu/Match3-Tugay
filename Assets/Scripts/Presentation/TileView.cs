using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        // These describe what the view represents; the Board remains authoritative.
        public GridPosition Position { get; private set; }
        public TileType Type { get; private set; }

        private void Awake()
        {
            SetTileType(Type);
        }

        public void Initialize(GridPosition position, TileType tileType)
        {
            SetTileType(tileType);
            Position = position;
        }

        public void SetTileType(TileType tileType)
        {
            Color color = GetColor(tileType);
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Type = tileType;
            spriteRenderer.color = color;
            spriteRenderer.enabled = tileType != TileType.Empty;
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

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
