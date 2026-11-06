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
        // Standalone startup only, until GameFlow owns launch in Step 8. No current-definition copy is kept.
        [SerializeField] private LevelDefinition initialLevel = new LevelDefinition(20, TileType.Red, 25, 42);
        [SerializeField] private TileType[] palette =
        {
            TileType.Red, TileType.Green, TileType.Blue,
            TileType.Yellow, TileType.Purple, TileType.Orange
        };

        // Seconds for the gravity and refill animations during match resolution.
        [SerializeField] private float fallDuration = 0.15f;
        [SerializeField] private float refillDuration = 0.15f;

        // Scoring remains a game-wide rule.
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
        // A private copy of the validated starting tiles, kept for dead-board recovery. It is never shown, never handed
        // to input or resolution, and never changed; every restart replaces it with a new copy.
        private Board startingBoard;
        // The session's refill stream, the same instance the resolution runner uses. Dead-board recovery draws from it
        // only when it actually replaces a board, so a game without recovery replays exactly as before.
        private System.Random refillRandom;
        private LevelProgress levelProgress;
        private Action<LevelStatus> onLevelEnded;
        private bool inputAvailable = true;

        // Null until a session is created; replaced on every successful StartLevel, including same-level retry.
        public LevelProgress LevelProgress => levelProgress;
        public bool IsBusy => inputController != null && inputController.IsInputLocked;

        // Unity calls Start once, before the first frame, after every object's Awake.
        private void Start()
        {
            // Android ignores vSyncCount; request 60 fps once, leaving Editor/desktop pacing unchanged.
            if (Application.platform == RuntimePlatform.Android)
            {
                Application.targetFrameRate = 60;
            }

            // A caller may already have supplied a session before this component's Start.
            if (levelProgress == null)
            {
                StartLevel(initialLevel, _ => { });
            }
        }

        // Flow availability is separate from the resolving move's lock. Disabling the component does not stop
        // its coroutine; a terminal level can never be re-enabled by this gate.
        public void SetInputAvailable(bool available)
        {
            if (inputController == null)
            {
                throw new InvalidOperationException("Assign a BoardInputController before gating input.");
            }

            inputAvailable = available;
            ApplyInputAvailability();
        }

        // The same supplied definition creates a deterministic fresh session. Navigation belongs to the caller.
        public void StartLevel(LevelDefinition definition, Action<LevelStatus> onLevelEnded)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (onLevelEnded == null)
            {
                throw new ArgumentNullException(nameof(onLevelEnded));
            }
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
            if (Array.IndexOf(palette, definition.GoalTileType) < 0)
            {
                throw new InvalidOperationException($"The goal tile type {definition.GoalTileType} is not in the palette.");
            }

            // Inspector values a move would otherwise read only after input locks: fail here, before the first move.
            boardView.ValidateConfiguration(palette);

            // Build and validate the new session first, so a bad setting fails before the current level is touched.
            var newProgress = new LevelProgress(definition.StartingMoveCount, definition.GoalTileType,
                definition.GoalTarget, pointsPerClearedTile);
            // Stable and playable: no match now and at least one legal move.
            var newBoard = BoardGenerator.GeneratePlayable(width, height, palette, definition.Seed);
            var newStartingBoard = new Board(newBoard.Width, newBoard.Height);
            newStartingBoard.CopyTilesFrom(newBoard);
            // Recreate both independent streams per session: targeting draws must never advance refill.
            var newRefillRandom = new System.Random(definition.Seed);
            var targetingRandom = new System.Random(unchecked(definition.Seed ^ 0x5EED1234));
            var resolution = new BoardResolutionRunner(
                newBoard, boardView, palette, newRefillRandom, fallDuration, refillDuration, targetingRandom);

            // Preserve today's standalone HUD actions until the approved flow UI replaces their routing.
            // Both supply the same definition again; callbacks are replaced, never accumulated.
            levelHud.Initialize(() => StartLevel(definition, onLevelEnded));
            // Cycle enabled input so OnEnable re-arms the same-update UI-tap guard for the new session.
            inputController.enabled = false;
            this.onLevelEnded = onLevelEnded;
            levelProgress = newProgress;
            board = newBoard;
            startingBoard = newStartingBoard;
            refillRandom = newRefillRandom;
            // Render destroys the previous level's tile views and registers new ones.
            boardView.Render(board);
            // Pass the same authoritative instance after its tile visuals have been created.
            // Initialize replaces the callback (it is never added twice) and clears any selection.
            inputController.Initialize(
                board, boardView, inputCamera, resolution, HandleValidMoveResolved, HandleBusyChanged);
            ApplyInputAvailability();
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
                ApplyInputAvailability();
                // Synchronous notification while FinishSwap still owns the lock. Never navigate here.
                onLevelEnded(levelProgress.Status);
                return;
            }

            // Still playing on a settled board: if no legal move is left, Core replaces the tiles in place.
            // Moves, score and goal are untouched. The controller still holds the input lock until this returns,
            // so the player never sees an unlocked dead board.
            if (DeadBoardRecovery.RecoverIfDead(board, palette, refillRandom, startingBoard))
            {
                boardView.Render(board);
            }
        }

        // Restart is offered only while no move is running: the button mirrors the controller's busy state,
        // pushed to us at the start and end of each move instead of polled every frame.
        private void HandleBusyChanged(bool isBusy)
        {
            levelHud.SetRestartAvailable(!isBusy);
        }

        private void ApplyInputAvailability()
        {
            inputController.enabled = inputAvailable && levelProgress != null
                && levelProgress.Status == LevelStatus.Playing;
        }
    }
}
