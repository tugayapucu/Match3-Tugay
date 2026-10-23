using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3.Presentation
{
    // Shows a planned special-effect chain one effect at a time, in Core's order. It knows no footprint, targeting
    // or scoring rules: BoardView supplies each effect's own view, the rendered views its Footprint addresses and,
    // for a Wisp, where Core's RemoteTarget is.
    internal static class SpecialEffectFeedback
    {
        // One effect to show. The origin is never in Highlighted, so it gets only its own, stronger pop.
        internal sealed class Step
        {
            public readonly TileView Origin;
            public readonly TileView[] Highlighted;
            // Where the Wisp flies (Core's RemoteTarget), or null for no flight.
            public readonly Vector3? FlightEnd;
            // The visible view at the RemoteTarget, given the strong pop on landing; null if that cell shows nothing.
            public readonly TileView Target;

            public Step(TileView origin, TileView[] highlighted, Vector3? flightEnd = null, TileView target = null)
            {
                Origin = origin;
                Highlighted = highlighted;
                FlightEnd = flightEnd;
                Target = target;
            }
        }

        // Scale only: a renderer color multiplies the pre-colored artwork, so a tint would darken some gems and
        // shift their hue. Growing reads the same on every color and leaves RGB and alpha untouched.
        private const float OriginGrowth = 0.2f;
        private const float FootprintGrowth = 0.1f;

        // BoardView validates the durations and resolves every view before calling this helper.
        // One flat iterator (no nested coroutines), so each MoveNext is exactly one frame.
        public static IEnumerator Animate(Step[] steps, float durationPerStep, float flightDuration)
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

                if (step.Target != null)
                {
                    Remember(step.Target, scales);
                }
            }

            foreach (var step in steps)
            {
                // Local burst: start fully grown, then settle; even a one-frame effect is visible.
                for (float elapsed = 0f; elapsed < durationPerStep; elapsed += Time.deltaTime)
                {
                    float strength = 1f - elapsed / durationPerStep;
                    foreach (var view in step.Highlighted)
                    {
                        view.transform.localScale = scales[view] * (1f + FootprintGrowth * strength);
                    }

                    step.Origin.transform.localScale = scales[step.Origin] * (1f + OriginGrowth * strength);
                    yield return null;
                }

                // Restore this burst's views before the flight or the next effect starts from the snapshot.
                step.Origin.transform.localScale = scales[step.Origin];
                foreach (var view in step.Highlighted)
                {
                    view.transform.localScale = scales[view];
                }

                if (!step.FlightEnd.HasValue)
                {
                    continue;
                }

                // Flight: a temporary copy of the Wisp's picture travels to Core's RemoteTarget at the Wisp's popped
                // size, so it reads as launched. The real Wisp view, the Board and the lookup are untouched.
                GameObject flyer = CreateFlyer(step.Origin, scales[step.Origin] * (1f + OriginGrowth));
                Vector3 start = step.Origin.transform.position;
                for (float elapsed = 0f; elapsed < flightDuration; elapsed += Time.deltaTime)
                {
                    float progress = TileEasing.EaseInOut(elapsed / flightDuration);
                    flyer.transform.position = Vector3.Lerp(start, step.FlightEnd.Value, progress);
                    yield return null;
                }

                // Landing: the copy sits on the target and shrinks into it, while the target takes the strong pop
                // (the Wisp's actual hit), so the impact is visible.
                Vector3 flyerScale = flyer.transform.localScale;
                flyer.transform.position = step.FlightEnd.Value;
                for (float elapsed = 0f; elapsed < durationPerStep; elapsed += Time.deltaTime)
                {
                    float strength = 1f - elapsed / durationPerStep;
                    flyer.transform.localScale = flyerScale * strength;
                    if (step.Target != null)
                    {
                        step.Target.transform.localScale = scales[step.Target] * (1f + OriginGrowth * strength);
                    }

                    yield return null;
                }

                // Hide at once; Destroy itself only happens at the end of the frame.
                flyer.SetActive(false);
                Object.Destroy(flyer);
                if (step.Target != null)
                {
                    step.Target.transform.localScale = scales[step.Target];
                }
            }
        }

        // Picture only: no TileView, no collider, never registered with BoardView. A sibling of the tiles, so it
        // shares their space and is destroyed with the board if the feedback never finishes.
        private static GameObject CreateFlyer(TileView origin, Vector3 scale)
        {
            var source = origin.GetComponent<SpriteRenderer>();
            var flyer = new GameObject("WispFlight");
            flyer.transform.SetParent(origin.transform.parent, false);
            flyer.transform.localRotation = origin.transform.localRotation;
            flyer.transform.localScale = scale;
            flyer.transform.position = origin.transform.position;
            var renderer = flyer.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.color = source.color;
            renderer.sortingLayerID = source.sortingLayerID;
            // Drawn above the tiles it passes over.
            renderer.sortingOrder = source.sortingOrder + 1;
            return flyer;
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
