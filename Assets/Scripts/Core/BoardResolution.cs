using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // The logical steps that resolve matches on a Board. Pure data changes: no visuals, no animation.
    public static class BoardResolution
    {
        // Sets every supplied cell to Empty (typically the cells MatchFinder found).
        // All positions are checked first, so invalid input leaves the Board unchanged.
        public static void ClearCells(Board board, IReadOnlyCollection<GridPosition> positions)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            foreach (GridPosition position in positions)
            {
                if (!board.IsInBounds(position))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(positions), position, "Every position must be within the board bounds.");
                }
            }

            // Empty marks a hole that gravity and refill will deal with in later steps.
            foreach (GridPosition position in positions)
            {
                board.SetTile(position, TileType.Empty);
            }
        }

        // Drops every tile as far down its column as possible, keeping the tiles' order; Empty cells end up on top.
        // Returns one move per tile that changed cell (column by column, bottom to top). Stationary tiles are not listed.
        public static IReadOnlyList<TileMove> ApplyGravity(Board board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var moves = new List<TileMove>();
            for (int x = 0; x < board.Width; x++)
            {
                // Write pointer: the lowest cell in this column that the next surviving tile should land on.
                int writeY = 0;
                for (int readY = 0; readY < board.Height; readY++)
                {
                    var from = new GridPosition(x, readY);
                    TileType tile = board.GetTile(from);
                    if (tile == TileType.Empty)
                    {
                        continue;
                    }

                    if (readY != writeY)
                    {
                        // There is a gap below: the tile falls to writeY and leaves a hole behind.
                        var to = new GridPosition(x, writeY);
                        board.SetTile(to, tile);
                        board.SetTile(from, TileType.Empty);
                        moves.Add(new TileMove(from, to, tile));
                    }

                    writeY++;
                }
            }

            return moves;
        }

        // Fills every Empty cell with a random palette type and returns one spawn per filled cell.
        // Order: columns left to right, cells bottom to top. Matches are NOT prevented: they cause cascades.
        // The caller owns `random`, so repeated refills continue one reproducible sequence.
        public static IReadOnlyList<TileSpawn> Refill(Board board, IReadOnlyList<TileType> palette, Random random)
        {
            // Check every input before touching the Board, so invalid input changes nothing.
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            PaletteValidation.Validate(palette);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var spawns = new List<TileSpawn>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var position = new GridPosition(x, y);
                    if (board.GetTile(position) != TileType.Empty)
                    {
                        continue;
                    }

                    TileType type = palette[random.Next(palette.Count)];
                    board.SetTile(position, type);
                    spawns.Add(new TileSpawn(position, type));
                }
            }

            return spawns;
        }
    }
}
