using System;

namespace Match3.Core
{
    // Passive request data for the approved resolution contract; it performs no activation itself.
    public readonly struct SpecialActivation
    {
        public GridPosition Position { get; }
        public TileType? TargetColor { get; }

        public SpecialActivation(GridPosition position, TileType? targetColor = null)
        {
            if (targetColor.HasValue && !TileRules.IsNormalMatchItem(targetColor.Value))
            {
                throw new ArgumentException("An activation target color must be a normal color.", nameof(targetColor));
            }

            Position = position;
            TargetColor = targetColor;
        }
    }
}
