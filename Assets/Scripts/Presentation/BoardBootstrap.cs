using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    // Composition root: the one place that wires the logical board to its view, input and resolution at startup.
    // BoardGenerator and BoardView never reference each other; this class connects them.
    public sealed class BoardBootstrap : MonoBehaviour
    {
        // Board generation settings, editable in the Inspector.
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        // Same seed + same palette order => same board every run.
        [SerializeField] private int seed = 42;
        [SerializeField] private TileType[] palette =
        {
            TileType.Red, TileType.Green, TileType.Blue,
            TileType.Yellow, TileType.Purple, TileType.Orange
        };

        // Seconds for the gravity and refill animations during match resolution.
        [SerializeField] private float fallDuration = 0.15f;
        [SerializeField] private float refillDuration = 0.15f;

        // Level rules: how many valid moves the player has, the collection goal, and the score per cleared tile.
        [SerializeField] private int startingMoveCount = 20;
        [SerializeField] private TileType goalTileType = TileType.Red;
        [SerializeField] private int goalTarget = 25;
        [SerializeField] private int pointsPerClearedTile = 100;

        // The view that will display the generated board; assign it in the Inspector.
        [SerializeField] private BoardView boardView;
        // Explicit scene references keep startup wiring here instead of using global lookups.
        [SerializeField] private BoardInputController inputController;
        [SerializeField] private Camera inputCamera;
        // Shows moves, score, goal and outcome, and offers Restart.
        [SerializeField] private LevelHudView levelHud;

        // The current session's authoritative instance, shared by the view, input and resolution runner.
        private Board board;
        private LevelProgress levelProgress;

        // The rules state of the current level; null until Start has run, replaced on every restart.
        public LevelProgress LevelProgress => levelProgress;

        // Unity calls Start once, before the first frame, after every object's Awake.
        private void Start()
        {
            StartLevel();
        }

        // Starts the level again in place: a fresh board from the same seed (so the same starting board),
        // fresh moves, score and goal, and input enabled again. Allowed whenever the board is settled,
        // in any status (Playing, Won or Lost); rejected while a move is still animating.
        public void RestartLevel()
        {
            StartLevel();
        }

        // Builds a complete level session. Used both at startup and on restart, so both follow the same path.
        private void StartLevel()
        {
            if (boardView == null)
            {
                throw new InvalidOperationException("Assign a BoardView before starting.");
            }

            if (inputController == null)
            {
                throw new InvalidOperationException("Assign a BoardInputController before starting.");
            }

            if (inputCamera == null)
            {
                throw new InvalidOperationException("Assign an input Camera before starting.");
            }

            if (levelHud == null)
            {
                throw new InvalidOperationException("Assign a LevelHudView before starting.");
            }

            // A running move still owns the board and its views; there is no safe way to swap them mid-animation.
            if (inputController.IsInputLocked)
            {
                throw new InvalidOperationException("Wait for the current move to finish before starting the level.");
            }

            // A cross-setting rule only this class can check: a goal colour that never spawns could never be won.
            if (Array.IndexOf(palette, goalTileType) < 0)
            {
                throw new InvalidOperationException($"The goal tile type {goalTileType} is not in the palette.");
            }

            // Restart clicks come back here. Assigned (not added), so restarting never stacks handlers.
            levelHud.Initialize(RestartLevel);

            // Build and validate the new session first, so a bad setting fails before the current level is touched.
            var newProgress = new LevelProgress(startingMoveCount, goalTileType, goalTarget, pointsPerClearedTile);
            var newBoard = BoardGenerator.Generate(width, height, palette, seed);
            // Recreate both independent streams per session: targeting draws must never advance refill.
            var refillRandom = new System.Random(seed);
            var targetingRandom = new System.Random(unchecked(seed ^ 0x5EED1234));
            var resolution = new BoardResolutionRunner(
                newBoard, boardView, palette, refillRandom, fallDuration, refillDuration, targetingRandom);

            levelProgress = newProgress;
            board = newBoard;
            // Render destroys the previous level's tile views and registers new ones.
            boardView.Render(board);
            // Pass the same authoritative instance after its tile visuals have been created.
            // Initialize replaces the callback (it is never added twice) and clears any selection.
            inputController.Initialize(
                board, boardView, inputCamera, resolution, HandleValidMoveResolved, HandleBusyChanged);
            // A finished level disabled input; the new level is playable again.
            inputController.enabled = true;
            levelHud.Show(levelProgress);
            levelHud.SetRestartAvailable(true);
        }

        // Called by the input controller once per valid move, after the board is stable again.
        private void HandleValidMoveResolved(ResolutionResult result)
        {
            levelProgress.ApplyValidMove(result);
            levelHud.Show(levelProgress);

            // Won or Lost: no new move may start. This is the level's outcome, not the controller's
            // per-move lock; the controller still finishes its own cleanup after this returns.
            if (levelProgress.Status != LevelStatus.Playing)
            {
                inputController.enabled = false;
            }
        }

        // Restart is offered only while no move is running: the button mirrors the controller's busy state,
        // pushed to us at the start and end of each move instead of polled every frame.
        private void HandleBusyChanged(bool isBusy)
        {
            levelHud.SetRestartAvailable(!isBusy);
        }
    }
}
