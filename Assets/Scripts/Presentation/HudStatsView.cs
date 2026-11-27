using System;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    // Displays one HUD layout's values and goal fill. It writes values only;
    // LevelHudView decides when to show them and owns the outcome panel, visibility and every button.
    public sealed class HudStatsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goalCountText;
        [SerializeField] private TMP_Text goalTotalText;
        [SerializeField] private TMP_Text levelNumberText;
        [SerializeField] private TMP_Text levelTotalText;
        [SerializeField] private Image goalIcon;
        [SerializeField] private Image goalFill;

        public void ValidateConfiguration()
        {
            if (movesText == null || scoreText == null || goalCountText == null || goalTotalText == null
                || levelNumberText == null || levelTotalText == null || goalIcon == null || goalFill == null)
            {
                throw new InvalidOperationException("Assign every level, moves, score and goal text, the goal icon and fill to HudStatsView.");
            }
        }

        // goalSprite is resolved by LevelHudView first, so a missing sprite changes nothing on screen.
        public void Show(LevelProgress progress, Sprite goalSprite)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }
            if (goalSprite == null)
            {
                throw new ArgumentNullException(nameof(goalSprite));
            }

            // Format everything before writing: a rejected value must leave the entire display unchanged.
            string moves = HudFormat.Count(progress.MovesRemaining);
            string score = HudFormat.Count(progress.Score);
            string count = HudFormat.Count(progress.GoalProgress);
            string total = HudFormat.OutOf(progress.GoalTarget);
            float fraction = HudFormat.GoalFill(progress.GoalProgress, progress.GoalTarget);

            movesText.text = moves;
            scoreText.text = score;
            goalCountText.text = count;
            goalTotalText.text = total;
            goalIcon.sprite = goalSprite;
            // Anchors also follow later layout changes; no per-frame code or artificial minimum fill.
            goalFill.rectTransform.anchorMax = new Vector2(fraction, 1f);
            goalFill.enabled = fraction > 0f;
        }

        public void ShowLevel(int number, int count)
        {
            if (count <= 0 || number < 1 || number > count)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "The level number must be within a positive catalog count.");
            }

            string level = HudFormat.Count(number);
            string total = HudFormat.OutOf(count);
            levelNumberText.text = level;
            levelTotalText.text = total;
        }
    }
}
