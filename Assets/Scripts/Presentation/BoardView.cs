using System;
using System.Collections;
using System.Collections.Generic;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        // The tile template to copy for each cell; assign it in the Inspector.
        [SerializeField] private TileView tilePrefab;
        // Optional parent and coordinate origin for the visual grid.
        [SerializeField] private Transform tileRoot;
        // Distance between cell centers in the root's local space.
        [SerializeField] private float tileSpacing = 1f;

        // Find a visual by cell address and track only our own clones for cleanup.
        private readonly Dictionary<GridPosition, TileView> tileViews = new Dictionary<GridPosition, TileView>();

        // Display the supplied board by reading its cells, without changing its data.
        public void Render(Board board)
        {
            // Validate inputs before removing the current display.
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (tilePrefab == null)
            {
                throw new InvalidOperationException("Assign a TileView prefab before rendering.");
            }

            if (tileSpacing <= 0f || float.IsNaN(tileSpacing) || float.IsInfinity(tileSpacing))
            {
                throw new InvalidOperationException("Tile spacing must be a positive finite value.");
            }

            // Remove the previous grid before creating its replacement.
            ClearViews();
            // If no root is assigned, parent tiles to this GameObject instead.
            Transform root = tileRoot != null ? tileRoot : transform;

            // Visit every cell one row at a time.
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    // This is a logical cell address, not a Unity world position.
                    var position = new GridPosition(x, y);
                    var tileType = board.GetTile(position);
                    // Clone under the root; false selects local-space parenting.
                    var view = Instantiate(tilePrefab, root, false);
                    // Register which visual currently represents this cell.
                    tileViews.Add(position, view);

                    // Give the view the cell address and type it should display.
                    view.Initialize(position, tileType);
                    view.SetWorldPosition(GetWorldPosition(position));
                    // Activate inactive-template clones; TileView hides Empty through its renderer.
                    view.gameObject.SetActive(true);
                }
            }
        }

        public TileView GetTileView(GridPosition position)
        {
            if (!tileViews.TryGetValue(position, out var view) || view == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position), position, "No tile view is rendered at this position.");
            }

            return view;
        }

        // The caller swaps the Board first, then waits for this coroutine before another swap or render.
        public IEnumerator AnimateSwap(GridPosition first, GridPosition second, float duration)
        {
            if (duration < 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be finite and nonnegative.");
            }

            var firstView = GetTileView(first);
            var secondView = GetTileView(second);
            if (first == second)
            {
                throw new ArgumentException("Swap animation requires two different positions.", nameof(second));
            }

            var firstStart = firstView.transform.position;
            var secondStart = secondView.transform.position;
            var firstDestination = GetWorldPosition(second);
            var secondDestination = GetWorldPosition(first);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Both visuals use the same progress so they travel together.
                float progress = elapsed / duration;
                firstView.SetWorldPosition(Vector3.Lerp(firstStart, firstDestination, progress));
                secondView.SetWorldPosition(Vector3.Lerp(secondStart, secondDestination, progress));
                yield return null;
                elapsed += Time.deltaTime;
            }

            // Snap exactly to the destinations, including when a zero duration skips movement.
            firstView.SetWorldPosition(firstDestination);
            secondView.SetWorldPosition(secondDestination);

            // The visuals keep their tile types; only the cells they represent change.
            tileViews[first] = secondView;
            tileViews[second] = firstView;
            firstView.SetGridPosition(second);
            secondView.SetGridPosition(first);
        }

        private Vector3 GetWorldPosition(GridPosition position)
        {
            Transform root = tileRoot != null ? tileRoot : transform;
            // Convert a cell address into an offset, then apply the root's position, rotation, and scale.
            var localPosition = new Vector3(position.X * tileSpacing, position.Y * tileSpacing, 0f);
            return root.TransformPoint(localPosition);
        }

        private void OnDestroy()
        {
            // Clean up owned tiles when Unity invokes this component's destruction callback.
            ClearViews();
        }

        private void ClearViews()
        {
            foreach (var view in tileViews.Values)
            {
                // Unity treats an already-destroyed tile object as null.
                if (view == null)
                {
                    continue;
                }

                // Runtime destruction is deferred, so hide old tiles before showing the replacement grid.
                view.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(view.gameObject);
                }
                else
                {
                    // Outside Play Mode, remove the object immediately for Editor validation.
                    DestroyImmediate(view.gameObject);
                }
            }

            // Forget removed views; clearing the dictionary alone does not destroy GameObjects.
            tileViews.Clear();
        }
    }
}
