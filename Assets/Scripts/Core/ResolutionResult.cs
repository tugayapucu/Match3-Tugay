using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Match3.Core
{
    // Facts about one valid player move: the tile types its resolution cleared, cascades included.
    // Reports facts only; scoring and goals are decided by whoever reads it (LevelProgress).
    public sealed class ResolutionResult
    {
        private readonly ReadOnlyCollection<TileType> clearedTiles;

        public ResolutionResult(IEnumerable<TileType> clearedTiles)
        {
            if (clearedTiles == null)
            {
                throw new ArgumentNullException(nameof(clearedTiles));
            }

            // Copy, so later changes to the caller's collection cannot change this result.
            var copy = new List<TileType>(clearedTiles);
            foreach (TileType tile in copy)
            {
                // A cleared cell always held a real tile; Empty or casted garbage means a caller bug.
                if (tile == TileType.Empty || !Enum.IsDefined(typeof(TileType), tile))
                {
                    throw new ArgumentException("Cleared tiles must be defined, non-Empty tile types.", nameof(clearedTiles));
                }
            }

            this.clearedTiles = copy.AsReadOnly();
        }

        // No meaningful order: use the counts below.
        public IReadOnlyList<TileType> ClearedTiles => clearedTiles;

        public int ClearedCount => clearedTiles.Count;

        public int CountOf(TileType tileType)
        {
            int count = 0;
            foreach (TileType tile in clearedTiles)
            {
                if (tile == tileType)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
