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

        // The view that will display the generated board; assign it in the Inspector.
        [SerializeField] private BoardView boardView;
        // Explicit scene references keep startup wiring here instead of using global lookups.
        [SerializeField] private BoardInputController inputController;
        [SerializeField] private Camera inputCamera;

        // Kept so later gameplay systems can work on this same authoritative instance.
        private Board board;

        // Unity calls Start once, before the first frame, after every object's Awake.
        private void Start()
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

            board = BoardGenerator.Generate(width, height, palette, seed);
            boardView.Render(board);
            // Refills use their own seeded System.Random, so a whole game is reproducible from `seed`.
            var resolution = new BoardResolutionRunner(
                board, boardView, palette, new System.Random(seed), fallDuration, refillDuration);
            // Pass the same authoritative instance after its tile visuals have been created.
            inputController.Initialize(board, boardView, inputCamera, resolution);
        }
    }
}
