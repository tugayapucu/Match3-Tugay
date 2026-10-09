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
        // Null means no selection; the controller remembers at most one logical cell.
        private GridPosition? selectedPosition;
        private bool isInputLocked;

        public GridPosition? SelectedPosition => selectedPosition;
        public bool IsInputLocked => isInputLocked;

        // Startup wiring supplies the existing model, its rendered view, and the picking camera.
        public void Initialize(Board board, BoardView boardView, Camera camera)
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

            if (isInputLocked)
            {
                throw new InvalidOperationException("Wait for the current swap before reinitializing input.");
            }

            ValidateSwapDuration();
            this.board = board;
            this.boardView = boardView;
            inputCamera = camera;
            selectedPosition = null;
        }

        private void Update()
        {
            if (board == null || isInputLocked)
            {
                return;
            }

            // Only the input source is mouse-specific; touch can reuse HandlePointerDown later.
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                HandlePointerDown(mouse.position.ReadValue());
            }
        }

        public void HandlePointerDown(Vector2 screenPosition)
        {
            // Guard the reusable entry point too, so every input source obeys the same lock.
            if (!isActiveAndEnabled || board == null || isInputLocked)
            {
                return;
            }

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

            StartCoroutine(FinishSwap(first, position));
        }

        private IEnumerator FinishSwap(GridPosition first, GridPosition second)
        {
            // Keep input locked until movement and the view lookup updates are both complete.
            yield return boardView.AnimateSwap(first, second, swapDuration);
            selectedPosition = null;
            isInputLocked = false;
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
