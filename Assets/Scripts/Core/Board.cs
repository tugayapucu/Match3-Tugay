using System;

namespace Match3.Core
{
    public sealed class Board
    {
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
            tiles = new TileType[width, height];
        }

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
