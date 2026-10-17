using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    // Builds Boards from text so test layouts read like the board on screen.
    internal static class TestBoards
    {
        // Rows are written top-down as they appear on screen: rows[0] is the highest y.
        // R/G/B/Y/P/O are colors, H/V/F/T/L are specials, '.' is Empty. P stays Purple.
        public static Board Create(params string[] rows)
        {
            var board = new Board(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
            {
                Assert.That(rows[row].Length, Is.EqualTo(board.Width), "All rows must have the same length.");
                int y = board.Height - 1 - row;
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), ToTileType(rows[row][x]));
                }
            }

            return board;
        }

        private static TileType ToTileType(char symbol)
        {
            switch (symbol)
            {
                case '.': return TileType.Empty;
                case 'R': return TileType.Red;
                case 'G': return TileType.Green;
                case 'B': return TileType.Blue;
                case 'Y': return TileType.Yellow;
                case 'P': return TileType.Purple;
                case 'O': return TileType.Orange;
                case 'H': return TileType.HorizontalRocket;
                case 'V': return TileType.VerticalRocket;
                case 'F': return TileType.Propeller;
                case 'T': return TileType.TNT;
                case 'L': return TileType.LightBall;
                default: throw new ArgumentException($"Unknown tile symbol '{symbol}'.", nameof(symbol));
            }
        }
    }
}
