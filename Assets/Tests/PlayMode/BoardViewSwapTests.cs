using System.Collections;
using System.Reflection;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Match3.Presentation.Tests
{
    public class BoardViewSwapTests
    {
        private GameObject boardObject;
        private GameObject tileTemplate;
        private BoardView boardView;

        [SetUp]
        public void SetUp()
        {
            tileTemplate = new GameObject("TileTemplate");
            tileTemplate.SetActive(false);
            var template = tileTemplate.AddComponent<TileView>();

            boardObject = new GameObject("SwapTestBoard");
            boardObject.transform.position = new Vector3(4f, -2f, 0f);
            boardObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            boardObject.transform.localScale = new Vector3(2f, 3f, 1f);
            boardView = boardObject.AddComponent<BoardView>();

            // Configure the Inspector fields without introducing a runtime configuration API for tests.
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BoardView).GetField("tilePrefab", fields).SetValue(boardView, template);
            typeof(BoardView).GetField("tileSpacing", fields).SetValue(boardView, 1.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(boardObject);
            Object.Destroy(tileTemplate);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimateSwap_MovesTogether_ThenRepeatedSwapRestoresViews()
        {
            var board = new Board(3, 1);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var untouched = new GridPosition(2, 0);
            board.SetTile(first, TileType.Red);
            board.SetTile(second, TileType.Blue);
            board.SetTile(untouched, TileType.Green);
            boardView.Render(board);
            var firstView = boardView.GetTileView(first);
            var secondView = boardView.GetTileView(second);
            var untouchedView = boardView.GetTileView(untouched);
            Vector3 firstStart = firstView.transform.position;
            Vector3 secondStart = secondView.transform.position;
            Vector3 untouchedStart = untouchedView.transform.position;

            // The model changes immediately; the visuals still represent their original cells.
            Assert.That(board.TrySwapAdjacent(first, second), Is.True);
            Assert.That(board.GetTile(first), Is.EqualTo(TileType.Blue));
            Assert.That(firstView.Type, Is.EqualTo(TileType.Red));
            var animation = boardView.AnimateSwap(first, second, 1f);
            Assert.That(animation.MoveNext(), Is.True);
            Assert.That(firstView.transform.position, Is.EqualTo(firstStart));

            yield return null;
            Assert.That(animation.MoveNext(), Is.True);

            // Check both sprites at the same intermediate progress, not just their final positions.
            float progress = Vector3.Distance(firstStart, firstView.transform.position)
                / Vector3.Distance(firstStart, secondStart);
            Assert.That(progress, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(Vector3.Distance(secondView.transform.position,
                Vector3.Lerp(secondStart, firstStart, progress)), Is.LessThan(1e-5f));
            Assert.That(boardView.GetTileView(first), Is.SameAs(firstView));
            Assert.That(firstView.Position, Is.EqualTo(first));

            yield return animation;

            Assert.That(firstView.transform.position, Is.EqualTo(secondStart));
            Assert.That(secondView.transform.position, Is.EqualTo(firstStart));
            Assert.That(firstView.Position, Is.EqualTo(second));
            Assert.That(secondView.Position, Is.EqualTo(first));
            Assert.That(boardView.GetTileView(first), Is.SameAs(secondView));
            Assert.That(boardView.GetTileView(second), Is.SameAs(firstView));
            Assert.That(boardView.GetTileView(first).Type, Is.EqualTo(board.GetTile(first)));
            Assert.That(boardView.GetTileView(second).Type, Is.EqualTo(board.GetTile(second)));

            Assert.That(board.TrySwapAdjacent(first, second), Is.True);
            yield return boardView.AnimateSwap(first, second, 0.05f);

            Assert.That(boardView.GetTileView(first), Is.SameAs(firstView));
            Assert.That(boardView.GetTileView(second), Is.SameAs(secondView));
            Assert.That(firstView.Position, Is.EqualTo(first));
            Assert.That(secondView.Position, Is.EqualTo(second));
            Assert.That(firstView.transform.position, Is.EqualTo(firstStart));
            Assert.That(secondView.transform.position, Is.EqualTo(secondStart));
            Assert.That(board.GetTile(first), Is.EqualTo(TileType.Red));
            Assert.That(board.GetTile(second), Is.EqualTo(TileType.Blue));
            Assert.That(boardView.GetTileView(untouched), Is.SameAs(untouchedView));
            Assert.That(untouchedView.transform.position, Is.EqualTo(untouchedStart));
            Assert.That(untouchedView.Type, Is.EqualTo(board.GetTile(untouched)));
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(3));
        }
    }
}
