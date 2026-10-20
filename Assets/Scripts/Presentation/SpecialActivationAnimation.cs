using System.Collections;
using UnityEngine;

namespace Match3.Presentation
{
    // One small visual pulse for supplied views. It knows no Board, footprints, targeting or scoring rules.
    internal static class SpecialActivationAnimation
    {
        // BoardView validates the duration and resolves the view references before calling this helper.
        public static IEnumerator Animate(TileView[] views, float duration)
        {
            if (views.Length == 0)
            {
                yield break;
            }

            var scales = new Vector3[views.Length];
            var colors = new Color[views.Length];
            var renderers = new SpriteRenderer[views.Length];
            for (int i = 0; i < views.Length; i++)
            {
                scales[i] = views[i].transform.localScale;
                renderers[i] = views[i].GetComponent<SpriteRenderer>();
                colors[i] = renderers[i].color;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Start visibly expanded and gold, then settle back; even one-frame pulses remain visible.
                float strength = 1f - elapsed / duration;
                for (int i = 0; i < views.Length; i++)
                {
                    views[i].transform.localScale = scales[i] * (1f + 0.15f * strength);
                    renderers[i].color = Color.Lerp(colors[i], new Color(1f, 0.8f, 0.2f), strength);
                }
                yield return null;
                elapsed += Time.deltaTime;
            }

            for (int i = 0; i < views.Length; i++)
            {
                views[i].transform.localScale = scales[i];
                renderers[i].color = colors[i];
            }
        }
    }
}
