using System;
using Match3.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class BoardViewTests
    {
        private GameObject boardObject;
        private GameObject tileTemplate;
        private BoardView boardView;

        [SetUp]
        public void SetUp()
        {
            // Inactive stand-in for the Tile prefab, so the template itself is never shown.
            tileTemplate = new GameObject("TileTemplate");
            tileTemplate.SetActive(false);
            var template = tileTemplate.AddComponent<TileView>();

            boardObject = new GameObject("Board");
            boardView = boardObject.AddComponent<BoardView>();

            // Private [SerializeField]s are set the way the Inspector does it; tileRoot stays null,
            // so tiles are parented under boardObject itself.
            var serialized = new SerializedObject(boardView);
            serialized.FindProperty("tilePrefab").objectReferenceValue = template;
            serialized.FindProperty("tileSpacing").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            // Destroying the board object also destroys every tile parented under it.
            UnityEngine.Object.DestroyImmediate(boardObject);
            UnityEngine.Object.DestroyImmediate(tileTemplate);
        }

        [Test]
        public void Render_CreatesOneViewPerCell_MatchingTheBoard()
        {
            var board = CreateBoard(3, 2);

            boardView.Render(board);

            var views = GetViews();
            Assert.That(views.Length, Is.EqualTo(6));
            foreach (var view in views)
            {
                Assert.That(view.Type, Is.EqualTo(board.GetTile(view.Position)));
                Assert.That(view.gameObject.activeSelf, Is.True);

                var local = view.transform.localPosition;
                Assert.That(local.x, Is.EqualTo(view.Position.X * 1.5f).Within(1e-5f));
                Assert.That(local.y, Is.EqualTo(view.Position.Y * 1.5f).Within(1e-5f));
            }
        }

        [Test]
        public void Render_CalledTwice_ReplacesThePreviousViews()
        {
            boardView.Render(CreateBoard(3, 2));
            var secondBoard = CreateBoard(2, 2);

            boardView.Render(secondBoard);

            // Old tiles must not accumulate: only the second board's 4 views remain.
            var views = GetViews();
            Assert.That(views.Length, Is.EqualTo(4));
            foreach (var view in views)
            {
                Assert.That(view.Type, Is.EqualTo(secondBoard.GetTile(view.Position)));
            }
        }

        [Test]
        public void Render_EmptyCell_HidesOnlyThatTilesRenderer()
        {
            var board = CreateBoard(2, 1);
            var emptyPosition = new GridPosition(1, 0);
            board.SetTile(emptyPosition, TileType.Empty);

            boardView.Render(board);

            // Empty cells keep a GameObject but draw nothing.
            foreach (var view in GetViews())
            {
                bool shouldDraw = view.Position != emptyPosition;
                Assert.That(view.GetComponent<SpriteRenderer>().enabled, Is.EqualTo(shouldDraw));
            }
        }

        [Test]
        public void Render_NullBoard_ThrowsAndKeepsTheCurrentViews()
        {
            boardView.Render(CreateBoard(3, 2));

            Assert.Throws<ArgumentNullException>(() => boardView.Render(null));

            // Inputs are validated before clearing, so a bad call does not blank the board.
            Assert.That(GetViews().Length, Is.EqualTo(6));
        }

        private TileView[] GetViews()
        {
            return boardObject.GetComponentsInChildren<TileView>(true);
        }

        // Hand-built board (not BoardGenerator) so the test controls every cell.
        private static Board CreateBoard(int width, int height)
        {
            var types = new[] { TileType.Red, TileType.Green, TileType.Blue };
            var board = new Board(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    board.SetTile(new GridPosition(x, y), types[(x + y) % types.Length]);
                }
            }

            return board;
        }
    }
}
