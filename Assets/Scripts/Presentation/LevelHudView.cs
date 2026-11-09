using System;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    // Shows the current level's numbers and outcome, and forwards navigation clicks. It owns no rules:
    // LevelProgress calculates values, Bootstrap shows them, and GameFlow supplies navigation callbacks.
    // The scene owns the static labels (MOVES, GOAL, SCORE, FINAL SCORE); this view writes only the changing values.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LevelHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private Button restartButton;

        // One shared panel for both outcomes: hidden while Playing, titled by LevelProgress's status otherwise.
        [SerializeField] private GameObject outcomePanel;
        [SerializeField] private TMP_Text outcomeTitleText;
        [SerializeField] private TMP_Text outcomeScoreText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private TMP_Text outcomeActionText;
        [SerializeField] private Button outcomeMenuButton;

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
        private Action onOutcomeRequested;
        private Action onMenuRequested;
        private CanvasGroup visibility;
        private bool actionsAvailable = true;
        private bool levelOver;

        public void Initialize(Action onRestartRequested, Action onOutcomeRequested, Action onMenuRequested)
        {
            if (onRestartRequested == null)
            {
                throw new ArgumentNullException(nameof(onRestartRequested));
            }
            if (onOutcomeRequested == null)
            {
                throw new ArgumentNullException(nameof(onOutcomeRequested));
            }
            if (onMenuRequested == null)
            {
                throw new ArgumentNullException(nameof(onMenuRequested));
            }
            ValidateConfiguration();
            this.onRestartRequested = onRestartRequested;
            this.onOutcomeRequested = onOutcomeRequested;
            this.onMenuRequested = onMenuRequested;
        }

        public void ValidateConfiguration()
        {
            if (movesText == null || scoreText == null || goalText == null || restartButton == null || goalIcon == null
                || outcomePanel == null || outcomeTitleText == null || outcomeScoreText == null || playAgainButton == null
                || outcomeActionText == null || outcomeMenuButton == null)
            {
                throw new InvalidOperationException(
                    "Assign every HUD text, the goal icon, all three buttons and the outcome panel before initializing.");
            }

            // Every goal color is checked now, so a missing sprite fails at startup rather than on some later level.
            if (redGoalSprite == null || greenGoalSprite == null || blueGoalSprite == null
                || yellowGoalSprite == null || purpleGoalSprite == null || orangeGoalSprite == null)
            {
                throw new InvalidOperationException("Assign all six goal sprites before initializing.");
            }
        }

        // Unity calls Awake once per component, so each button listener is added exactly once,
        // however many times the level restarts. Restart and the contextual outcome action are separate routes.
        private void Awake()
        {
            visibility = GetComponent<CanvasGroup>();
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(HandleOutcomeClicked);
            }
            if (outcomeMenuButton != null)
            {
                outcomeMenuButton.onClick.AddListener(HandleMenuClicked);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(HandleOutcomeClicked);
            }
            if (outcomeMenuButton != null)
            {
                outcomeMenuButton.onClick.RemoveListener(HandleMenuClicked);
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

            levelOver = progress.Status != LevelStatus.Playing;
            if (levelOver)
            {
                outcomeTitleText.text = OutcomeTitle(progress.Status);
                outcomeScoreText.text = progress.Score.ToString();
            }

            outcomePanel.SetActive(levelOver);
            RefreshActionAvailability();
        }

        // False while a move is animating, so a player click can never ask for a restart mid-move.
        // One busy gate covers every action; Restart also requires a Playing level.
        public void SetRestartAvailable(bool isAvailable)
        {
            actionsAvailable = isAvailable;
            RefreshActionAvailability();
        }

        private void RefreshActionAvailability()
        {
            restartButton.interactable = actionsAvailable && !levelOver;
            playAgainButton.interactable = actionsAvailable && levelOver;
            outcomeMenuButton.interactable = actionsAvailable && levelOver;
        }

        private void HandleRestartClicked()
        {
            onRestartRequested?.Invoke();
        }

        private void HandleOutcomeClicked()
        {
            onOutcomeRequested?.Invoke();
        }

        private void HandleMenuClicked()
        {
            onMenuRequested?.Invoke();
        }

        // Flow knows the catalog position; this view does not decide whether the final level was won.
        public void SetOutcomePresentation(string title, string actionLabel)
        {
            outcomeTitleText.text = title;
            outcomeActionText.text = actionLabel;
        }

        // Keep layout objects active: the camera fitter still reads their rectangles behind flow UI.
        public void SetVisible(bool visible)
        {
            visibility.alpha = visible ? 1f : 0f;
            visibility.interactable = visible;
            visibility.blocksRaycasts = visible;
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

        private static string OutcomeTitle(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Won: return "Level Complete!";
                case LevelStatus.Lost: return "Out of Moves";
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "Only a finished level has an outcome.");
            }
        }
    }
}
