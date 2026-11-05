using System;
using System.Collections;
using Match3.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Match3.Presentation
{
    public sealed class BoardInputController : MonoBehaviour
    {
        [SerializeField] private float swapDuration = 0.2f;
        [SerializeField] private float swipeThresholdFraction = 0.25f;

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
        // A visual cache only; never used to decide which cell is selected.
        private TileView highlightedView;
        private bool isInputLocked;
        // A gesture is distinct from the tap selection. Null means no held gesture to finish.
        private GridPosition? gestureStartPosition;
        private Vector2 gestureStartScreenPosition;
        private Pointer gesturePointer;
        private float gestureThresholdPixels;
        private bool gestureRejectedUntilRelease;

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
            ValidateSwipeThresholdFraction();
            this.board = board;
            this.boardView = boardView;
            inputCamera = camera;
            this.resolution = resolution;
            this.onValidMoveResolved = onValidMoveResolved;
            this.onBusyChanged = onBusyChanged;
            selectedPosition = null;
            ClearGesture();
            RefreshSelectionCue();
        }

        private void OnEnable()
        {
            // ButtonControl initializes frame-edge tracking lazily. Warm the current pointer before its first tap.
            var pointer = Pointer.current;
            if (pointer != null)
            {
                _ = pointer.press.wasPressedThisFrame;
            }
            RefreshSelectionCue();
        }

        private void OnDisable()
        {
            // A disabled component's running move still owns its lock and finishes its normal cleanup.
            ClearGesture();
            RefreshSelectionCue();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ClearGesture();
            }
        }

        private void Update()
        {
            if (board == null || isInputLocked)
            {
                return;
            }

            var pointer = Pointer.current;
            if (gestureStartPosition.HasValue && pointer != gesturePointer)
            {
                ClearGesture();
                return;
            }

            if (pointer == null)
            {
                return;
            }

            var touchscreen = pointer as Touchscreen;
            if (touchscreen != null && touchscreen.primaryTouch.phase.ReadValue() == TouchPhase.Canceled)
            {
                ClearGesture();
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                // Touch keeps the true origin even when press and release arrive in one input update.
                // Mouse uses the position at the press poll; movement wholly between polls is not reconstructed.
                Vector2 origin = touchscreen != null
                    ? touchscreen.primaryTouch.startPosition.ReadValue() : pointer.position.ReadValue();
                HandlePointerDown(origin);
            }

            // Independent from the press check: a very quick tap can have both edges in the same frame.
            // Primary touch stays pressed until all fingers lift; extra fingers are not new taps.
            if (pointer.press.wasReleasedThisFrame)
            {
                HandlePointerUp(pointer.position.ReadValue());
            }
            else if (pointer.press.isPressed)
            {
                HandlePointerMove(pointer.position.ReadValue());
            }
        }

        public void HandlePointerDown(Vector2 screenPosition)
        {
            // Guard the reusable entry point too, so every input source obeys the same lock.
            if (!isActiveAndEnabled || board == null || isInputLocked)
            {
                return;
            }

            ClearGesture();
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

            ValidateSwipeThresholdFraction();
            Vector2 center = inputCamera.WorldToScreenPoint(boardView.GetCellWorldPosition(position));
            Vector2 neighbour = inputCamera.WorldToScreenPoint(
                boardView.GetCellWorldPosition(new GridPosition(position.X + 1, position.Y)));
            float spacing = Vector2.Distance(center, neighbour);
            if (spacing <= 0f || float.IsNaN(spacing) || float.IsInfinity(spacing))
            {
                throw new InvalidOperationException("Screen-space cell spacing must be finite and positive.");
            }

            gestureStartPosition = position;
            gestureStartScreenPosition = screenPosition;
            gesturePointer = Pointer.current;
            gestureThresholdPixels = spacing * swipeThresholdFraction;
        }

        public void HandlePointerMove(Vector2 screenPosition)
        {
            if (!isActiveAndEnabled || board == null || isInputLocked
                || !gestureStartPosition.HasValue || gestureRejectedUntilRelease)
            {
                return;
            }

            Vector2 delta = screenPosition - gestureStartScreenPosition;
            if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < gestureThresholdPixels)
            {
                return;
            }

            GridPosition first = gestureStartPosition.Value;
            GridPosition second = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new GridPosition(first.X + (delta.x > 0f ? 1 : -1), first.Y)
                : new GridPosition(first.X, first.Y + (delta.y > 0f ? 1 : -1));
            if (!TryStartSwap(first, second))
            {
                // Bounds/Empty rejection keeps the old tap selection, with no retries or tap on release.
                gestureRejectedUntilRelease = true;
            }
        }

        public void HandlePointerUp(Vector2 screenPosition)
        {
            if (!isActiveAndEnabled || board == null || isInputLocked || !gestureStartPosition.HasValue)
            {
                return;
            }

            // A release can be the first sample that crosses the threshold.
            HandlePointerMove(screenPosition);
            if (gestureStartPosition.HasValue && !gestureRejectedUntilRelease)
            {
                GridPosition tapped = gestureStartPosition.Value;
                if (!selectedPosition.HasValue)
                {
                    selectedPosition = tapped;
                }
                else
                {
                    TryStartSwap(selectedPosition.Value, tapped);
                }
            }

            ClearGesture();
            RefreshSelectionCue();
        }

        private void RefreshSelectionCue()
        {
            // A re-render may have deactivated/replaced or destroyed the old visual. Always discard the
            // cache, even when Unity's destroyed-object null comparison is true; it is not selection state.
            if (highlightedView != null)
            {
                highlightedView.SetSelected(false);
            }
            highlightedView = null;

            if (boardView != null && selectedPosition.HasValue && !isInputLocked && isActiveAndEnabled)
            {
                var view = boardView.GetTileView(selectedPosition.Value);
                view.SetSelected(true);
                highlightedView = view;
            }
        }

        private void ClearGesture()
        {
            gestureStartPosition = null;
            gestureStartScreenPosition = default;
            gesturePointer = null;
            gestureThresholdPixels = 0f;
            gestureRejectedUntilRelease = false;
        }

        // True means an animated attempt started, including one that will roll back.
        // Rejected requests preserve the old selection and never acquire another move's lock.
        private bool TryStartSwap(GridPosition first, GridPosition second)
        {
            if (!isActiveAndEnabled || board == null || isInputLocked)
            {
                return false;
            }

            // Core throws for off-board coordinates. Swipes derive their neighbour without picking it.
            if (!board.IsInBounds(first) || !board.IsInBounds(second)
                || board.GetTile(first) == TileType.Empty || board.GetTile(second) == TileType.Empty)
            {
                return false;
            }

            ValidateSwapDuration();
            // Lock before the model changes; Core decides whether these cells may swap.
            isInputLocked = true;
            if (!board.TrySwapAdjacent(first, second))
            {
                isInputLocked = false;
                return false;
            }

            // The Board now holds the attempted result; Core decides whether it is a real Match-3 move.
            SwapAnalysis move = SwapValidator.Analyze(board, first, second);
            selectedPosition = first;
            ClearGesture();
            RefreshSelectionCue();
            // Only an accepted adjacent swap is a move; rejected clicks above never reach this point.
            onBusyChanged(true);
            StartCoroutine(FinishSwap(move));
            return true;
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
            RefreshSelectionCue();
            // Last, so whoever is told "not busy" sees a fully settled controller.
            onBusyChanged(false);
        }

        private void ValidateSwipeThresholdFraction()
        {
            if (swipeThresholdFraction <= 0f || float.IsNaN(swipeThresholdFraction)
                || float.IsInfinity(swipeThresholdFraction))
            {
                throw new InvalidOperationException("Swipe threshold fraction must be finite and positive.");
            }
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
