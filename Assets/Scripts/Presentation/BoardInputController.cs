using System;
using System.Collections;
using Match3.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Match3.Presentation
{
    public sealed class BoardInputController : MonoBehaviour
    {
        [SerializeField] private float swapDuration = 0.2f;

        private Board board;
        private BoardView boardView;
        private Camera inputCamera;
        private BoardResolutionRunner resolution;
        // Receives the facts of each finished valid move; the receiver decides what they mean (moves, score, goal).
        private Action<ResolutionResult> onValidMoveResolved;
        // Told true when an accepted move starts and false once it has completely finished (e.g. for a restart button).
        private Action<bool> onBusyChanged;
        // Null means no selection; the controller remembers at most one logical cell.
        private GridPosition? selectedPosition;
        private bool isInputLocked;

        public GridPosition? SelectedPosition => selectedPosition;
        public bool IsInputLocked => isInputLocked;

        // Startup wiring supplies the existing model, its rendered view, the picking camera,
        // the runner that resolves matches after a valid move, who to tell when such a move has finished,
        // and who to tell when a move starts and ends.
        public void Initialize(
            Board board, BoardView boardView, Camera camera, BoardResolutionRunner resolution,
            Action<ResolutionResult> onValidMoveResolved, Action<bool> onBusyChanged)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (boardView == null)
            {
                throw new ArgumentNullException(nameof(boardView));
            }

            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (onValidMoveResolved == null)
            {
                throw new ArgumentNullException(nameof(onValidMoveResolved));
            }

            if (onBusyChanged == null)
            {
                throw new ArgumentNullException(nameof(onBusyChanged));
            }

            if (isInputLocked)
            {
                throw new InvalidOperationException("Wait for the current swap before reinitializing input.");
            }

            ValidateSwapDuration();
            this.board = board;
            this.boardView = boardView;
            inputCamera = camera;
            this.resolution = resolution;
            this.onValidMoveResolved = onValidMoveResolved;
            this.onBusyChanged = onBusyChanged;
            selectedPosition = null;
        }

        private void Update()
        {
            if (board == null || isInputLocked)
            {
                return;
            }

            // Mouse left-click and the touchscreen's primary touch share this press/position pair.
            // Primary touch stays pressed until all fingers lift, so extra fingers are not new taps.
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                HandlePointerDown(pointer.position.ReadValue());
            }
        }

        public void HandlePointerDown(Vector2 screenPosition)
        {
            // Guard the reusable entry point too, so every input source obeys the same lock.
            if (!isActiveAndEnabled || board == null || isInputLocked)
            {
                return;
            }

            // A tile may have landed since the last physics step; pick its current transform.
            Physics2D.SyncTransforms();
            // The ray converts screen pixels into a world-space query against 2D tile colliders.
            Ray ray = inputCamera.ScreenPointToRay(screenPosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider == null)
            {
                return;
            }

            var tileView = hit.collider.GetComponent<TileView>();
            if (tileView == null || !board.IsInBounds(tileView.Position))
            {
                return;
            }

            GridPosition position = tileView.Position;
            // Ignore hidden empty cells and visuals that belong to another board.
            if (board.GetTile(position) == TileType.Empty || boardView.GetTileView(position) != tileView)
            {
                return;
            }

            if (!selectedPosition.HasValue)
            {
                selectedPosition = position;
                return;
            }

            ValidateSwapDuration();
            GridPosition first = selectedPosition.Value;
            // Lock before the model changes; Core decides whether these cells may swap.
            isInputLocked = true;
            if (!board.TrySwapAdjacent(first, position))
            {
                isInputLocked = false;
                return;
            }

            // The Board now holds the attempted result; Core decides whether it is a real Match-3 move.
            SwapAnalysis move = SwapValidator.Analyze(board, first, position);
            // Only an accepted adjacent swap is a move; rejected clicks above never reach this point.
            onBusyChanged(true);
            StartCoroutine(FinishSwap(move));
        }

        private IEnumerator FinishSwap(SwapAnalysis move)
        {
            GridPosition first = move.First;
            GridPosition second = move.Second;
            // Both outcomes animate forward, so the player sees the attempted move.
            yield return boardView.AnimateSwap(first, second, swapDuration);

            if (move.Kind != SwapKind.Invalid)
            {
                // Clear, fall and refill until no matches remain, including every cascade.
                ResolutionResult result = null;
                yield return resolution.ResolveUntilStable(move, r => result = r);
                // The board is stable now: report the whole move exactly once, before the cleanup below.
                // The receiver may disable this component (level over); the cleanup still runs, because
                // a disabled MonoBehaviour's running coroutines continue.
                onValidMoveResolved(result);
            }
            else
            {
                // Restore the logical state first; the visuals catch up below.
                // Cannot fail: these same adjacent cells were just swapped.
                board.TrySwapAdjacent(first, second);
                // The same call sends both visuals home, because the lookup was updated by the forward swap.
                yield return boardView.AnimateSwap(first, second, swapDuration);
            }

            // Input stays locked until every animation for this move has finished:
            // the rollback for an invalid move, or the whole cascade chain for a valid one.
            selectedPosition = null;
            isInputLocked = false;
            // Last, so whoever is told "not busy" sees a fully settled controller.
            onBusyChanged(false);
        }

        private void ValidateSwapDuration()
        {
            if (swapDuration < 0f || float.IsNaN(swapDuration) || float.IsInfinity(swapDuration))
            {
                throw new InvalidOperationException("Swap duration must be finite and nonnegative.");
            }
        }
    }
}
