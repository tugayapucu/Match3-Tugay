using UnityEngine;

namespace Match3.Presentation
{
    // Presentation-only timing curves for tile movement: maps linear progress (0..1) to eased progress (0..1).
    // Logical Board state never depends on these values; only where a visual is drawn mid-animation does.
    public static class TileEasing
    {
        // Smoothstep: starts and ends at zero speed and is fastest in the middle (1.5x linear speed at t = 0.5).
        // Symmetric, so EaseInOut(1 - t) == 1 - EaseInOut(t), and exact at both ends so final positions stay exact.
        public static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
