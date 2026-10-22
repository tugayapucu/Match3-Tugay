using System.Collections;
using UnityEngine;

namespace Match3.Presentation
{
    // One short shrink-and-fade for views that are about to be removed. It knows no Board, matching, scoring
    // or special rules: Core has already cleared these cells, and BoardView removes the views right afterwards.
    internal static class ClearAnimation
    {
        // BoardView validates the duration and resolves the views before calling this helper.
        public static IEnumerator Animate(TileView[] views, float duration)
        {
            // Zero duration keeps the old behaviour: nothing animates and removal happens in the same step.
            if (views.Length == 0 || duration <= 0f)
            {
                yield break;
            }

            // Each view's own starting scale and color, so nothing assumes the prefab's values.
            var scales = new Vector3[views.Length];
            var renderers = new SpriteRenderer[views.Length];
            var colors = new Color[views.Length];
            for (int i = 0; i < views.Length; i++)
            {
                scales[i] = views[i].transform.localScale;
                renderers[i] = views[i].GetComponent<SpriteRenderer>();
                colors[i] = renderers[i].color;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Same easing language as movement; 1 = untouched, 0 = gone.
                float remaining = 1f - TileEasing.EaseInOut(elapsed / duration);
                for (int i = 0; i < views.Length; i++)
                {
                    views[i].transform.localScale = scales[i] * remaining;
                    // Fade alpha only: the pre-colored sprite keeps its RGB appearance.
                    Color c = colors[i];
                    renderers[i].color = new Color(c.r, c.g, c.b, c.a * remaining);
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            // End fully shrunk and transparent; the caller destroys these views next.
            for (int i = 0; i < views.Length; i++)
            {
                views[i].transform.localScale = Vector3.zero;
                Color c = colors[i];
                renderers[i].color = new Color(c.r, c.g, c.b, 0f);
            }
        }
    }
}
