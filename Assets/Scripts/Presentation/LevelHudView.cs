using System;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    // Shows the current level's numbers and forwards Restart clicks. It owns no rules:
    // LevelProgress calculates every value, and BoardBootstrap decides when to show it and what Restart does.
    public sealed class LevelHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button restartButton;

        // What a Restart click does; replaced (never added to) on every Initialize.
        private Action onRestartRequested;

        public void Initialize(Action onRestartRequested)
        {
            if (onRestartRequested == null)
            {
                throw new ArgumentNullException(nameof(onRestartRequested));
            }

            if (movesText == null || scoreText == null || goalText == null || statusText == null || restartButton == null)
            {
                throw new InvalidOperationException("Assign every HUD text and the restart button before initializing.");
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

            movesText.text = $"Moves: {progress.MovesRemaining}";
            scoreText.text = $"Score: {progress.Score}";
            goalText.text = $"Goal: {progress.GoalTileType} {progress.GoalProgress} / {progress.GoalTarget}";
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
