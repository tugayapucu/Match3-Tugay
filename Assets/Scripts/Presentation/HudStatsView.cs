using System;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    // Displays one HUD layout's statistics: moves, score, goal count and goal icon. It writes values only;
    // LevelHudView decides when to show them and owns the outcome panel, visibility and every button.
    public sealed class HudStatsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private Image goalIcon;

        public void ValidateConfiguration()
        {
            if (movesText == null || scoreText == null || goalText == null || goalIcon == null)
            {
                throw new InvalidOperationException("Assign the moves, score and goal texts and the goal icon to HudStatsView.");
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

            movesText.text = progress.MovesRemaining.ToString();
            scoreText.text = progress.Score.ToString();
            goalText.text = $"{progress.GoalProgress}/{progress.GoalTarget}";
            goalIcon.sprite = goalSprite;
        }
    }
}
