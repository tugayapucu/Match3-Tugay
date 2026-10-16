using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    // The real composition root: configuration reaches LevelProgress, its handler ends the level's input,
    // and restart rebuilds the whole session. Exact move-by-move rules are covered with scripted boards
    // in BoardInputControllerTests.
    public class BoardBootstrapTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int Size = 5;

        private GameObject tileTemplate;
        private GameObject boardObject;
        private GameObject cameraObject;
        private GameObject bootstrapObject;
        private BoardView boardView;
        private BoardInputController controller;
        private BoardBootstrap bootstrap;
        private Camera inputCamera;

        [SetUp]
        public void SetUp()
        {
            tileTemplate = new GameObject("BootstrapTestTileTemplate");
            tileTemplate.SetActive(false);
            var template = tileTemplate.AddComponent<TileView>();
            // Sprite-free test tiles need an explicit size; Unity otherwise autosizes this collider to zero.
            tileTemplate.AddComponent<BoxCollider2D>().size = Vector2.one;

            boardObject = new GameObject("BootstrapTestBoard");
            boardObject.transform.position = new Vector3(200f, 200f, 0f);
            boardView = boardObject.AddComponent<BoardView>();
            typeof(BoardView).GetField("tilePrefab", Fields).SetValue(boardView, template);
            controller = boardObject.AddComponent<BoardInputController>();

            // Centred on the 5x5 board (spacing 1), far from anything in the project's scene.
            cameraObject = new GameObject("BootstrapTestCamera");
            cameraObject.transform.position = new Vector3(202f, 202f, -10f);
            inputCamera = cameraObject.AddComponent<Camera>();
            inputCamera.orthographic = true;
            inputCamera.orthographicSize = 4f;

            // Inactive until configured, so Start runs only after every field below is set.
            bootstrapObject = new GameObject("BootstrapTest");
            bootstrapObject.SetActive(false);
            bootstrap = bootstrapObject.AddComponent<BoardBootstrap>();
            Set("boardView", boardView);
            Set("inputController", controller);
            Set("inputCamera", inputCamera);
            Set("width", Size);
            Set("height", Size);
            Set("palette", new[] { TileType.Red, TileType.Green, TileType.Blue });
            Set("goalTileType", TileType.Red);
            Set("fallDuration", 0.05f);
            Set("refillDuration", 0.05f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(bootstrapObject);
            Object.Destroy(boardObject);
            Object.Destroy(cameraObject);
            Object.Destroy(tileTemplate);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Start_CreatesALevelFromTheConfiguration()
        {
            Set("startingMoveCount", 7);
            Set("goalTarget", 12);

            yield return StartBootstrap();

            var level = bootstrap.LevelProgress;
            Assert.That(level.MovesRemaining, Is.EqualTo(7));
            Assert.That(level.Score, Is.Zero);
            Assert.That(level.GoalTileType, Is.EqualTo(TileType.Red));
            Assert.That(level.GoalTarget, Is.EqualTo(12));
            Assert.That(level.GoalProgress, Is.Zero);
            Assert.That(level.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(controller.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Start_GoalTypeNotInPalette_FailsBeforeCreatingTheBoard()
        {
            Set("goalTileType", TileType.Purple);
            LogAssert.Expect(LogType.Exception, new Regex("Purple is not in the palette"));

            yield return StartBootstrap();

            Assert.That(bootstrap.LevelProgress, Is.Null);
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ValidMoveResolved_LevelStillPlaying_KeepsInputEnabled()
        {
            Set("startingMoveCount", 2);
            Set("goalTarget", 5);
            yield return StartBootstrap();

            ReportValidMove(TileType.Red, TileType.Red, TileType.Red);

            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(bootstrap.LevelProgress.GoalProgress, Is.EqualTo(3));
            Assert.That(controller.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ValidMoveResolved_GoalReached_WinsAndDisablesInput()
        {
            Set("startingMoveCount", 2);
            Set("goalTarget", 3);
            yield return StartBootstrap();

            ReportValidMove(TileType.Red, TileType.Red, TileType.Red);

            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(controller.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator ValidMoveResolved_LastMoveWithoutGoal_LosesAndDisablesInput()
        {
            Set("startingMoveCount", 1);
            Set("goalTarget", 3);
            yield return StartBootstrap();

            ReportValidMove(TileType.Green, TileType.Green, TileType.Green);

            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Lost));
            Assert.That(controller.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator RestartLevel_WhilePlaying_ResetsBoardAndProgress()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();
            var firstLevel = bootstrap.LevelProgress;

            yield return PlayValidMove();
            Assert.That(firstLevel.MovesRemaining, Is.EqualTo(4));
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.Not.Null);

            bootstrap.RestartLevel();
            yield return null;

            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(firstLevel));
            AssertFreshLevel(startingTiles, 5);
        }

        [UnityTest]
        public IEnumerator RestartLevel_AfterWon_StartsPlayableAgain()
        {
            Set("startingMoveCount", 2);
            Set("goalTarget", 3);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();
            ReportValidMove(TileType.Red, TileType.Red, TileType.Red);
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Won));

            bootstrap.RestartLevel();
            yield return null;

            AssertFreshLevel(startingTiles, 2);
        }

        [UnityTest]
        public IEnumerator RestartLevel_AfterLost_StartsPlayableAgain()
        {
            Set("startingMoveCount", 1);
            Set("goalTarget", 3);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();
            ReportValidMove(TileType.Green, TileType.Green, TileType.Green);
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Lost));

            bootstrap.RestartLevel();
            yield return null;

            AssertFreshLevel(startingTiles, 1);
        }

        [UnityTest]
        public IEnumerator RestartLevel_Twice_StaysCleanAndReportsEachMoveOnce()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();

            bootstrap.RestartLevel();
            bootstrap.RestartLevel();
            yield return null;
            AssertFreshLevel(startingTiles, 5);

            // A callback registered twice would apply this move twice and leave 3 moves.
            yield return PlayValidMove();
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(4));
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator RestartLevel_DuringAMove_ThrowsAndTheMoveFinishesNormally()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            var level = bootstrap.LevelProgress;
            var board = CurrentBoard();
            Assert.That(TryFindValidMove(board, out var first, out var second), Is.True, "Expected a valid move.");
            Click(first);
            Click(second);
            Assert.That(controller.IsInputLocked, Is.True);

            Assert.Throws<InvalidOperationException>(() => bootstrap.RestartLevel());

            // Nothing was replaced: the running move still owns the same level and board.
            Assert.That(bootstrap.LevelProgress, Is.SameAs(level));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(controller.IsInputLocked, Is.True);

            yield return WaitUntilUnlocked();
            Assert.That(level.MovesRemaining, Is.EqualTo(4));
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator RestartLevel_InvalidConfiguration_ThrowsAndKeepsTheCurrentLevel()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            var level = bootstrap.LevelProgress;
            var board = CurrentBoard();
            var tiles = ReadTiles();
            Set("startingMoveCount", 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => bootstrap.RestartLevel());

            Assert.That(bootstrap.LevelProgress, Is.SameAs(level));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(ReadTiles(), Is.EqualTo(tiles));
            AssertViewsMatchBoard();
        }

        private IEnumerator StartBootstrap()
        {
            bootstrapObject.SetActive(true);
            // Unity calls Start before the next frame's updates.
            yield return null;
        }

        // Calls the same handler the controller receives in Initialize, as if a valid move had just resolved.
        private void ReportValidMove(params TileType[] cleared)
        {
            typeof(BoardBootstrap).GetMethod("HandleValidMoveResolved", Fields)
                .Invoke(bootstrap, new object[] { new ResolutionResult(cleared) });
        }

        // Restart contract: same starting board, fresh progress, playable input, one up-to-date view per cell.
        // Call one frame after RestartLevel, so the old views' deferred Destroy has happened.
        private void AssertFreshLevel(TileType[,] startingTiles, int startingMoveCount)
        {
            var level = bootstrap.LevelProgress;
            Assert.That(level.MovesRemaining, Is.EqualTo(startingMoveCount));
            Assert.That(level.Score, Is.Zero);
            Assert.That(level.GoalProgress, Is.Zero);
            Assert.That(level.Status, Is.EqualTo(LevelStatus.Playing));

            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(controller.SelectedPosition, Is.Null);

            Assert.That(ReadTiles(), Is.EqualTo(startingTiles), "Same seed, same starting board.");
            Assert.That(MatchFinder.FindMatches(CurrentBoard()), Is.Empty);
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(Size * Size),
                "Exactly one view per cell: the previous level's views are gone.");
            AssertViewsMatchBoard();

            // Playable: a click selects.
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        private void AssertViewsMatchBoard()
        {
            var board = CurrentBoard();
            foreach (var view in boardObject.GetComponentsInChildren<TileView>(true))
            {
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
                Assert.That(view.Type, Is.EqualTo(board.GetTile(view.Position)));
            }
        }

        // Plays the first valid move on the current board and waits until it has fully resolved.
        private IEnumerator PlayValidMove()
        {
            Assert.That(TryFindValidMove(CurrentBoard(), out var first, out var second), Is.True, "Expected a valid move.");
            Click(first);
            Click(second);
            Assert.That(controller.IsInputLocked, Is.True, "An adjacent valid second click should start a move.");
            yield return WaitUntilUnlocked();
        }

        private IEnumerator WaitUntilUnlocked()
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "The move did not finish.");
                yield return null;
            }
        }

        // Finds the first adjacent swap that SwapValidator accepts, trying it on a copy so the real Board is untouched.
        private static bool TryFindValidMove(Board board, out GridPosition first, out GridPosition second)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var origin = new GridPosition(x, y);
                    foreach (var neighbour in new[] { new GridPosition(x + 1, y), new GridPosition(x, y + 1) })
                    {
                        if (!board.IsInBounds(neighbour))
                        {
                            continue;
                        }

                        var copy = new Board(board.Width, board.Height);
                        for (int cy = 0; cy < board.Height; cy++)
                        {
                            for (int cx = 0; cx < board.Width; cx++)
                            {
                                var cell = new GridPosition(cx, cy);
                                copy.SetTile(cell, board.GetTile(cell));
                            }
                        }

                        copy.TrySwapAdjacent(origin, neighbour);
                        if (SwapValidator.IsValidResult(copy, origin, neighbour))
                        {
                            first = origin;
                            second = neighbour;
                            return true;
                        }
                    }
                }
            }

            first = default;
            second = default;
            return false;
        }

        private void Click(GridPosition position)
        {
            Physics2D.SyncTransforms();
            controller.HandlePointerDown(inputCamera.WorldToScreenPoint(boardView.GetTileView(position).transform.position));
        }

        private Board CurrentBoard()
        {
            return (Board)typeof(BoardBootstrap).GetField("board", Fields).GetValue(bootstrap);
        }

        private TileType[,] ReadTiles()
        {
            var board = CurrentBoard();
            var tiles = new TileType[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    tiles[x, y] = board.GetTile(new GridPosition(x, y));
                }
            }

            return tiles;
        }

        private void Set(string field, object value)
        {
            typeof(BoardBootstrap).GetField(field, Fields).SetValue(bootstrap, value);
        }
    }
}
