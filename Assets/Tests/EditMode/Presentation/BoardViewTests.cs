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
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));

                var local = view.transform.localPosition;
                Assert.That(local.x, Is.EqualTo(view.Position.X * 1.5f).Within(1e-5f));
                Assert.That(local.y, Is.EqualTo(view.Position.Y * 1.5f).Within(1e-5f));
            }
        }

        [Test]
        public void Render_CalledTwice_ReplacesThePreviousViews()
        {
            boardView.Render(CreateBoard(3, 2));
            var oldView = boardView.GetTileView(new GridPosition(0, 0));
            var secondBoard = CreateBoard(2, 2);

            boardView.Render(secondBoard);

            // Old tiles must not accumulate: only the second board's 4 views remain.
            var views = GetViews();
            Assert.That(views.Length, Is.EqualTo(4));
            Assert.That(oldView == null, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(new GridPosition(2, 1)));
            foreach (var view in views)
            {
                Assert.That(view.Type, Is.EqualTo(secondBoard.GetTile(view.Position)));
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
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

        [Test]
        public void GetTileView_UnrenderedPosition_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(new GridPosition(0, 0)));

            boardView.Render(CreateBoard(2, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(new GridPosition(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(new GridPosition(2, 0)));
        }

        [TestCase(1, 0)]
        [TestCase(0, 1)]
        public void AnimateSwap_ZeroDuration_UpdatesPositionsAndLookup(int secondX, int secondY)
        {
            var board = CreateBoard(2, 2);
            boardView.Render(board);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(secondX, secondY);
            var firstView = boardView.GetTileView(first);
            var secondView = boardView.GetTileView(second);
            Vector3 firstStart = firstView.transform.position;
            Vector3 secondStart = secondView.transform.position;

            Assert.That(board.TrySwapAdjacent(first, second), Is.True);
            Assert.That(boardView.AnimateSwap(first, second, 0f).MoveNext(), Is.False);

            Assert.That(boardView.GetTileView(first), Is.SameAs(secondView));
            Assert.That(boardView.GetTileView(second), Is.SameAs(firstView));
            Assert.That(firstView.Position, Is.EqualTo(second));
            Assert.That(secondView.Position, Is.EqualTo(first));
            Assert.That(firstView.transform.position, Is.EqualTo(secondStart));
            Assert.That(secondView.transform.position, Is.EqualTo(firstStart));
            foreach (var view in GetViews())
            {
                Assert.That(view.Type, Is.EqualTo(board.GetTile(view.Position)));
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
            }
        }

        [Test]
        public void AnimateSwap_RepeatedSwaps_KeepLookupAndModelSynchronized()
        {
            var board = CreateBoard(3, 1);
            boardView.Render(board);
            var first = new GridPosition(0, 0);
            var middle = new GridPosition(1, 0);
            var last = new GridPosition(2, 0);
            var originalFirstView = boardView.GetTileView(first);

            for (int i = 0; i < 3; i++)
            {
                Assert.That(board.TrySwapAdjacent(first, middle), Is.True);
                Assert.That(boardView.AnimateSwap(first, middle, 0f).MoveNext(), Is.False);
                Assert.That(board.TrySwapAdjacent(middle, last), Is.True);
                Assert.That(boardView.AnimateSwap(middle, last, 0f).MoveNext(), Is.False);
                foreach (var view in GetViews())
                {
                    Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
                    Assert.That(view.Type, Is.EqualTo(board.GetTile(view.Position)));
                }
            }

            Assert.That(boardView.GetTileView(first), Is.SameAs(originalFirstView));
            Assert.That(GetViews().Length, Is.EqualTo(3));

            boardView.Render(board);

            Assert.That(originalFirstView == null, Is.True);
            foreach (var view in GetViews())
            {
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
                Assert.That(view.Type, Is.EqualTo(board.GetTile(view.Position)));
            }
        }

        [Test]
        public void AnimateSwap_DoesNotChangeBoardContents()
        {
            var board = CreateBoard(2, 1);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            TileType firstType = board.GetTile(first);
            TileType secondType = board.GetTile(second);
            boardView.Render(board);

            // Deliberately animate without a model swap to verify this API never changes the Board.
            Assert.That(boardView.AnimateSwap(first, second, 0f).MoveNext(), Is.False);

            Assert.That(board.GetTile(first), Is.EqualTo(firstType));
            Assert.That(board.GetTile(second), Is.EqualTo(secondType));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void AnimateSwap_InvalidDuration_ThrowsBeforeChangingViews(float duration)
        {
            boardView.Render(CreateBoard(2, 1));
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var firstView = boardView.GetTileView(first);
            Vector3 start = firstView.transform.position;

            // Iterator code starts on MoveNext, rather than when the IEnumerator is created.
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                boardView.AnimateSwap(first, second, duration).MoveNext());

            Assert.That(exception.ParamName, Is.EqualTo("duration"));
            Assert.That(boardView.GetTileView(first), Is.SameAs(firstView));
            Assert.That(firstView.Position, Is.EqualTo(first));
            Assert.That(firstView.transform.position, Is.EqualTo(start));
        }

        [TestCase(-1, 0)]
        [TestCase(2, 0)]
        public void AnimateSwap_MissingView_ThrowsBeforeChangingViews(int x, int y)
        {
            boardView.Render(CreateBoard(2, 1));
            var first = new GridPosition(0, 0);
            var firstView = boardView.GetTileView(first);
            Vector3 start = firstView.transform.position;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                boardView.AnimateSwap(first, new GridPosition(x, y), 0f).MoveNext());

            Assert.That(boardView.GetTileView(first), Is.SameAs(firstView));
            Assert.That(firstView.transform.position, Is.EqualTo(start));
        }

        [Test]
        public void AnimateSwap_SameCell_ThrowsWithoutChangingItsView()
        {
            boardView.Render(CreateBoard(1, 1));
            var position = new GridPosition(0, 0);
            var view = boardView.GetTileView(position);

            Assert.Throws<ArgumentException>(() => boardView.AnimateSwap(position, position, 0f).MoveNext());

            Assert.That(boardView.GetTileView(position), Is.SameAs(view));
            Assert.That(view.Position, Is.EqualTo(position));
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
