using System;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    public class BoardResolutionRunnerTests
    {
        private static readonly TileType[] Palette = { TileType.Red, TileType.Green, TileType.Blue };

        private GameObject boardObject;
        private BoardView boardView;

        [SetUp]
        public void SetUp()
        {
            boardObject = new GameObject("RunnerTestBoard");
            boardView = boardObject.AddComponent<BoardView>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(boardObject);
        }

        [Test]
        public void Constructor_MissingDependency_Throws()
        {
            var board = new Board(3, 3);
            var random = new System.Random(1);

            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(null, boardView, Palette, random, 0f, 0f));
            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(board, null, Palette, random, 0f, 0f));
            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(board, boardView, null, random, 0f, 0f));
            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(board, boardView, Palette, null, 0f, 0f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidDuration_Throws(float duration)
        {
            var board = new Board(3, 3);
            var random = new System.Random(1);

            var fall = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardResolutionRunner(board, boardView, Palette, random, duration, 0f));
            var refill = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardResolutionRunner(board, boardView, Palette, random, 0f, duration));

            Assert.That(fall.ParamName, Is.EqualTo("fallDuration"));
            Assert.That(refill.ParamName, Is.EqualTo("refillDuration"));
        }

        [Test]
        public void ResolveUntilStable_BoardWithoutMatches_FinishesImmediatelyWithoutChanges()
        {
            var board = new Board(3, 1);
            board.SetTile(new GridPosition(0, 0), TileType.Red);
            board.SetTile(new GridPosition(1, 0), TileType.Green);
            board.SetTile(new GridPosition(2, 0), TileType.Blue);
            var runner = new BoardResolutionRunner(board, boardView, Palette, new System.Random(1), 0f, 0f);
            int callbacks = 0;
            ResolutionResult result = null;

            // Nothing to resolve: the very first step already reports "finished", with an empty result.
            Assert.That(runner.ResolveUntilStable(r => { callbacks++; result = r; }).MoveNext(), Is.False);
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(result.ClearedCount, Is.Zero);
            Assert.That(board.GetTile(new GridPosition(0, 0)), Is.EqualTo(TileType.Red));
            Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.Green));
            Assert.That(board.GetTile(new GridPosition(2, 0)), Is.EqualTo(TileType.Blue));
        }

        [Test]
        public void ResolveUntilStable_NullCallback_ThrowsBeforeAnyWork()
        {
            var board = new Board(3, 3);
            var runner = new BoardResolutionRunner(board, boardView, Palette, new System.Random(1), 0f, 0f);

            // Thrown on the call itself, not later when the coroutine is first stepped.
            Assert.Throws<ArgumentNullException>(() => runner.ResolveUntilStable(null));
        }
    }
}
