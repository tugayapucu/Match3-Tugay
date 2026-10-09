using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    // Composition root: the one place that wires the logical board to its view at startup.
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

        // The view that will display the generated board; assign it in the Inspector.
        [SerializeField] private BoardView boardView;

        // Kept so later gameplay systems can work on this same authoritative instance.
        private Board board;

        // Unity calls Start once, before the first frame, after every object's Awake.
        private void Start()
        {
            if (boardView == null)
            {
                throw new InvalidOperationException("Assign a BoardView before starting.");
            }

            board = BoardGenerator.Generate(width, height, palette, seed);
            boardView.Render(board);
        }
    }
}
