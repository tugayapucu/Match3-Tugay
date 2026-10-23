using System;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    // Shows the current level's numbers and forwards Restart clicks. It owns no rules:
    // LevelProgress calculates every value, and BoardBootstrap decides when to show it and what Restart does.
    // The scene owns the static labels (MOVES, GOAL, SCORE); this view writes only the changing values.
    public sealed class LevelHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button restartButton;

        // Shows which gem to collect. The HUD keeps its own references to the same PNGs as the tiles,
        // so it does not depend on TileView just to borrow its sprite mapping.
        [SerializeField] private Image goalIcon;
        [SerializeField] private Sprite redGoalSprite;
        [SerializeField] private Sprite greenGoalSprite;
        [SerializeField] private Sprite blueGoalSprite;
        [SerializeField] private Sprite yellowGoalSprite;
        [SerializeField] private Sprite purpleGoalSprite;
        [SerializeField] private Sprite orangeGoalSprite;

        // What a Restart click does; replaced (never added to) on every Initialize.
        private Action onRestartRequested;

        public void Initialize(Action onRestartRequested)
        {
            if (onRestartRequested == null)
            {
                throw new ArgumentNullException(nameof(onRestartRequested));
            }

            if (movesText == null || scoreText == null || goalText == null || statusText == null || restartButton == null
                || goalIcon == null)
            {
                throw new InvalidOperationException("Assign every HUD text, the goal icon and the restart button before initializing.");
            }

            // Every goal color is checked now, so a missing sprite fails at startup rather than on some later level.
            if (redGoalSprite == null || greenGoalSprite == null || blueGoalSprite == null
                || yellowGoalSprite == null || purpleGoalSprite == null || orangeGoalSprite == null)
            {
                throw new InvalidOperationException("Assign all six goal sprites before initializing.");
            }

            this.onRestartRequested = onRestartRequested;
        }

        // Unity calls Awake once per component, so the button listener is added exactly once,
        // however many times the level restarts.
        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        // Reads the already-calculated values; never changes them.
        public void Show(LevelProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            // Checked first, so a missing sprite changes nothing on screen.
            Sprite goalSprite = GetGoalSprite(progress.GoalTileType);
            if (goalSprite == null)
            {
                throw new InvalidOperationException($"Assign the {progress.GoalTileType} goal sprite before showing it.");
            }

            movesText.text = progress.MovesRemaining.ToString();
            scoreText.text = progress.Score.ToString();
            goalText.text = $"{progress.GoalProgress}/{progress.GoalTarget}";
            goalIcon.sprite = goalSprite;
            statusText.text = StatusMessage(progress.Status);
        }

        // False while a move is animating, so a player click can never ask for a restart mid-move.
        public void SetRestartAvailable(bool isAvailable)
        {
            restartButton.interactable = isAvailable;
        }

        private void HandleRestartClicked()
        {
            onRestartRequested?.Invoke();
        }

        // LevelProgress only accepts a normal color as the goal, so anything else is a programming error.
        private Sprite GetGoalSprite(TileType goal)
        {
            switch (goal)
            {
                case TileType.Red: return redGoalSprite;
                case TileType.Green: return greenGoalSprite;
                case TileType.Blue: return blueGoalSprite;
                case TileType.Yellow: return yellowGoalSprite;
                case TileType.Purple: return purpleGoalSprite;
                case TileType.Orange: return orangeGoalSprite;
                default: throw new ArgumentOutOfRangeException(nameof(goal), goal, "The goal must be a normal color.");
            }
        }

        private static string StatusMessage(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Won: return "You Win!";
                case LevelStatus.Lost: return "Out of Moves";
                default: return string.Empty;
            }
        }
    }
}
