using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Presentation
{
    // Shows a planned special-effect chain one effect at a time, in Core's order. It knows no footprint, targeting
    // or scoring rules: BoardView supplies each effect's own view and the rendered views its Footprint addresses.
    internal static class SpecialEffectFeedback
    {
        // One effect to show. An empty Highlighted array means "pulse the special only" (Wisp / Spectrum Core until 7b).
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

        // The same gold as the Phase 10 activation pulse, so the visual language stays consistent.
        private static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
        private const float OriginGrowth = 0.15f;

        // BoardView validates the duration and resolves every view before calling this helper.
        public static IEnumerator Animate(Step[] steps, float durationPerStep)
        {
            if (steps.Length == 0 || durationPerStep <= 0f)
            {
                yield break;
            }

            // Snapshot every involved view once, before any change: overlapping footprints never stack tint,
            // and every effect animates from (and restores to) the original appearance.
            var colors = new Dictionary<TileView, Color>();
            var scales = new Dictionary<TileView, Vector3>();
            var renderers = new Dictionary<TileView, SpriteRenderer>();
            foreach (var step in steps)
            {
                Remember(step.Origin, colors, scales, renderers);
                foreach (var view in step.Highlighted)
                {
                    Remember(view, colors, scales, renderers);
                }
            }

            foreach (var step in steps)
            {
                float elapsed = 0f;
                while (elapsed < durationPerStep)
                {
                    // Start fully lit, then settle; even a one-frame effect is visible.
                    float strength = 1f - elapsed / durationPerStep;
                    foreach (var view in step.Highlighted)
                    {
                        renderers[view].color = Color.Lerp(colors[view], Gold, strength);
                    }

                    renderers[step.Origin].color = Color.Lerp(colors[step.Origin], Gold, strength);
                    step.Origin.transform.localScale = scales[step.Origin] * (1f + OriginGrowth * strength);
                    yield return null;
                    elapsed += Time.deltaTime;
                }

                // Restore this effect's views before the next effect starts from the snapshot.
                Restore(step.Origin, colors, scales, renderers);
                foreach (var view in step.Highlighted)
                {
                    Restore(view, colors, scales, renderers);
                }
            }
        }

        private static void Remember(TileView view, Dictionary<TileView, Color> colors,
            Dictionary<TileView, Vector3> scales, Dictionary<TileView, SpriteRenderer> renderers)
        {
            if (renderers.ContainsKey(view))
            {
                return;
            }

            var renderer = view.GetComponent<SpriteRenderer>();
            renderers.Add(view, renderer);
            colors.Add(view, renderer.color);
            scales.Add(view, view.transform.localScale);
        }

        private static void Restore(TileView view, Dictionary<TileView, Color> colors,
            Dictionary<TileView, Vector3> scales, Dictionary<TileView, SpriteRenderer> renderers)
        {
            renderers[view].color = colors[view];
            view.transform.localScale = scales[view];
        }
    }
}
