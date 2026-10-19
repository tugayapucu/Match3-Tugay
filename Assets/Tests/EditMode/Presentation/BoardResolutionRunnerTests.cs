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

            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(null, boardView, Palette, random, 0f, 0f, new System.Random(2)));
            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(board, null, Palette, random, 0f, 0f, new System.Random(2)));
            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(board, boardView, null, random, 0f, 0f, new System.Random(2)));
            Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(board, boardView, Palette, null, 0f, 0f, new System.Random(2)));
        }

        [Test]
        public void Constructor_NullTargetingStream_ThrowsBeforeAnyMove()
        {
            var error = Assert.Throws<ArgumentNullException>(() => new BoardResolutionRunner(
                new Board(3, 3), boardView, Palette, new System.Random(1), 0f, 0f, null));
            Assert.That(error.ParamName, Is.EqualTo("targetingRandom"));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Constructor_InvalidDuration_Throws(float duration)
        {
            var board = new Board(3, 3);
            var random = new System.Random(1);

            var fall = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardResolutionRunner(board, boardView, Palette, random, duration, 0f, new System.Random(2)));
            var refill = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BoardResolutionRunner(board, boardView, Palette, random, 0f, duration, new System.Random(2)));

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
            var runner = new BoardResolutionRunner(board, boardView, Palette, new System.Random(1), 0f, 0f, new System.Random(2));
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
            var runner = new BoardResolutionRunner(board, boardView, Palette, new System.Random(1), 0f, 0f, new System.Random(2));

            // Thrown on the call itself, not later when the coroutine is first stepped.
            Assert.Throws<ArgumentNullException>(() => runner.ResolveUntilStable(null));
        }

        [Test]
        public void ResolveUntilStable_PlayerOverload_RejectsMissingOrInvalidContextImmediately()
        {
            var board = new Board(3, 1);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var invalid = SwapValidator.Analyze(board, first, second);
            var runner = new BoardResolutionRunner(board, boardView, Palette, new System.Random(1), 0f, 0f, new System.Random(2));

            Assert.Throws<ArgumentNullException>(() => runner.ResolveUntilStable(null, r => { }));
            Assert.Throws<ArgumentException>(() => runner.ResolveUntilStable(invalid, r => { }));
            for (int x = 0; x < 3; x++) board.SetTile(new GridPosition(x, 0), TileType.Red);
            var valid = SwapValidator.Analyze(board, first, second);
            Assert.Throws<ArgumentNullException>(() => runner.ResolveUntilStable(valid, null));
        }
    }
}
