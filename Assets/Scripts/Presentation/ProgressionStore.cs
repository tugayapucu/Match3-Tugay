using System;
using UnityEngine;

namespace Match3.Presentation
{
    // Stores only the next catalog index, or levelCount after final completion. No active session state.
    public sealed class ProgressionStore
    {
        public const string DefaultKey = "Match3.Progression.v1";

        private readonly int levelCount;
        private readonly string key;

        // A separate key lets tests use real PlayerPrefs without touching player progress.
        public ProgressionStore(int levelCount, string key = DefaultKey)
        {
            if (levelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelCount), "The catalog must contain at least one level.");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Assign a nonempty progression key.", nameof(key));
            }

            this.levelCount = levelCount;
            this.key = key;
        }

        public int Load()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, levelCount);
        }

        // Call only for accepted completed-level progress. Flow decides when a win has been accepted.
        public int Save(int progression)
        {
            if (progression < 0 || progression > levelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(progression), "Progression must be between zero and levelCount.");
            }

            int saved = Math.Max(Load(), progression);
            PlayerPrefs.SetInt(key, saved);
            PlayerPrefs.Save();
            return saved;
        }
    }
}
