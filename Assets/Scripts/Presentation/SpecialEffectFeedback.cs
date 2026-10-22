using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Presentation
{
    // Shows a planned special-effect chain one effect at a time, in Core's order. It knows no footprint, targeting
    // or scoring rules: BoardView supplies each effect's own view and the rendered views its Footprint addresses.
    internal static class SpecialEffectFeedback
    {
        // One effect to show. An empty Highlighted array means "pulse the special only" (Wisp, until its flight feedback).
        // The origin is never in Highlighted, so it gets only its own, stronger pop.
        internal sealed class Step
        {
            public readonly TileView Origin;
            public readonly TileView[] Highlighted;

            public Step(TileView origin, TileView[] highlighted)
            {
                Origin = origin;
                Highlighted = highlighted;
            }
        }

        // Scale only: a renderer color multiplies the pre-colored artwork, so a tint would darken some gems and
        // shift their hue. Growing reads the same on every color and leaves RGB and alpha untouched.
        private const float OriginGrowth = 0.2f;
        private const float FootprintGrowth = 0.1f;

        // BoardView validates the duration and resolves every view before calling this helper.
        public static IEnumerator Animate(Step[] steps, float durationPerStep)
        {
            if (steps.Length == 0 || durationPerStep <= 0f)
            {
                yield break;
            }

            // Snapshot every involved view once, before any change: overlapping footprints never stack growth,
            // and every effect animates from (and restores to) the original size.
            var scales = new Dictionary<TileView, Vector3>();
            foreach (var step in steps)
            {
                Remember(step.Origin, scales);
                foreach (var view in step.Highlighted)
                {
                    Remember(view, scales);
                }
            }

            foreach (var step in steps)
            {
                float elapsed = 0f;
                while (elapsed < durationPerStep)
                {
                    // Start fully grown, then settle; even a one-frame effect is visible.
                    float strength = 1f - elapsed / durationPerStep;
                    foreach (var view in step.Highlighted)
                    {
                        view.transform.localScale = scales[view] * (1f + FootprintGrowth * strength);
                    }

                    step.Origin.transform.localScale = scales[step.Origin] * (1f + OriginGrowth * strength);
                    yield return null;
                    elapsed += Time.deltaTime;
                }

                // Restore this effect's views before the next effect starts from the snapshot.
                step.Origin.transform.localScale = scales[step.Origin];
                foreach (var view in step.Highlighted)
                {
                    view.transform.localScale = scales[view];
                }
            }
        }

        private static void Remember(TileView view, Dictionary<TileView, Vector3> scales)
        {
            if (!scales.ContainsKey(view))
            {
                scales.Add(view, view.transform.localScale);
            }
        }
    }
}
