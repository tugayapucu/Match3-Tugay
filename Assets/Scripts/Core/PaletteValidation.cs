using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // The game's playable-palette rule, shared by everything that creates tiles (generation and refill).
    internal static class PaletteValidation
    {
        // Needs 3+ distinct, defined, non-Empty types. Generation relies on 3+ so its no-match exclusion
        // always leaves a choice; refill uses the same palette, so it follows the same rule.
        public static void Validate(IReadOnlyList<TileType> palette)
        {
            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            if (palette.Count < 3)
            {
                throw new ArgumentException("Palette must contain at least three distinct playable types.", nameof(palette));
            }

            var seenTypes = new HashSet<TileType>();
            for (int i = 0; i < palette.Count; i++)
            {
                var tileType = palette[i];
                // Rejects Empty and casted garbage like (TileType)99.
                if (tileType == TileType.Empty || !Enum.IsDefined(typeof(TileType), tileType))
                {
                    throw new ArgumentException("Palette entries must be defined playable tile types.", nameof(palette));
                }

                // HashSet.Add returns false when the value is already present.
                if (!seenTypes.Add(tileType))
                {
                    throw new ArgumentException("Palette entries must be distinct.", nameof(palette));
                }
            }
        }
    }
}
