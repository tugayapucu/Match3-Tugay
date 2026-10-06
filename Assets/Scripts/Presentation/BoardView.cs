using System;
using System.Collections.Generic;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private float tileSpacing = 1f;

        private readonly List<TileView> tileViews = new List<TileView>();

        public void Render(Board board)
        {
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

            ClearViews();
            Transform root = tileRoot != null ? tileRoot : transform;

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    var tileType = board.GetTile(position);
                    var view = Instantiate(tilePrefab, root, false);
                    tileViews.Add(view);

                    view.Initialize(position, tileType);
                    var localPosition = new Vector3(x * tileSpacing, y * tileSpacing, 0f);
                    view.SetWorldPosition(root.TransformPoint(localPosition));
                    view.gameObject.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            ClearViews();
        }

        private void ClearViews()
        {
            foreach (var view in tileViews)
            {
                if (view == null)
                {
                    continue;
                }

                // Hide old views immediately because runtime destruction waits until the end of the frame.
                view.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(view.gameObject);
                }
                else
                {
                    DestroyImmediate(view.gameObject);
                }
            }

            tileViews.Clear();
        }
    }
}
