using System;

namespace Match3.Core
{
    // The authoritative game state: which TileType sits in each cell. No Unity code, so it is easy to unit test.
    public sealed class Board
    {
        // Private so all access goes through bounds-checked GetTile/SetTile; indexed as [x, y].
        private readonly TileType[,] tiles;

        public int Width { get; }
        public int Height { get; }

        public Board(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            }

            Width = width;
            Height = height;
            // C# zero-initialises arrays, so every cell starts as TileType.Empty.
            tiles = new TileType[width, height];
        }

        // (0, 0) is the bottom-left cell; X grows right, Y grows up.
        public bool IsInBounds(GridPosition position)
        {
            return position.X >= 0 && position.X < Width
                && position.Y >= 0 && position.Y < Height;
        }

        public TileType GetTile(GridPosition position)
        {
            ValidatePosition(position);
            return tiles[position.X, position.Y];
        }

        public void SetTile(GridPosition position, TileType tileType)
        {
            ValidatePosition(position);
            tiles[position.X, position.Y] = tileType;
        }

        // Fail loudly on bad coordinates instead of silently ignoring them.
        private void ValidatePosition(GridPosition position)
        {
            if (!IsInBounds(position))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position), position, "Position must be within the board bounds.");
            }
        }
    }
}
