using System;
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

        // Track only our own clones so cleanup does not remove unrelated objects.
        private readonly List<TileView> tileViews = new List<TileView>();

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
                    // Remember the clone so the next render can remove it.
                    tileViews.Add(view);

                    // Give the view the cell address and type it should display.
                    view.Initialize(position, tileType);
                    // Turn grid indices into an offset relative to the root.
                    var localPosition = new Vector3(x * tileSpacing, y * tileSpacing, 0f);
                    // Apply the root's position, rotation, and scale to get a world position.
                    view.SetWorldPosition(root.TransformPoint(localPosition));
                    // Activate inactive-template clones; TileView hides Empty through its renderer.
                    view.gameObject.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up owned tiles when Unity invokes this component's destruction callback.
            ClearViews();
        }

        private void ClearViews()
        {
            foreach (var view in tileViews)
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

            // Forget removed views; clearing a list alone does not destroy GameObjects.
            tileViews.Clear();
        }
    }
}
