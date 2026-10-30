using System;
using System.Collections;
using System.Collections.Generic;
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

        // Seed 93 on the shipped 8x8 six-colour level: ten normal moves (first click, second click) that leave a stable
        // board with no legal move while the level is still Playing.
        private static readonly int[,] Seed93Moves =
        {
            { 5, 7, 6, 7 }, { 2, 7, 3, 7 }, { 5, 6, 5, 7 }, { 1, 6, 1, 7 }, { 0, 7, 1, 7 },
            { 3, 7, 4, 7 }, { 2, 6, 3, 6 }, { 3, 6, 3, 7 }, { 1, 6, 1, 7 }, { 4, 5, 5, 5 }
        };

        private static readonly string[] Seed93DeadRows =
        {
            "PGGOYYOP", "BRYBGBGY", "OYBPYRPY", "PGRPRBGP",
            "PGOGBOYO", "OYRGGPPY", "BYPRYORR", "GRPBGPYR"
        };

        private GameObject tileTemplate;
        private GameObject boardObject;
        private GameObject cameraObject;
        private GameObject bootstrapObject;
        private BoardView boardView;
        private BoardInputController controller;
        private BoardBootstrap bootstrap;
        private Camera inputCamera;
        private TestHud hud;

        [SetUp]
        public void SetUp()
        {
            var template = TestTiles.CreateTemplate("BootstrapTestTileTemplate");
            tileTemplate = template.gameObject;
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

            hud = TestHud.Create();

            // Inactive until configured, so Start runs only after every field below is set.
            bootstrapObject = new GameObject("BootstrapTest");
            bootstrapObject.SetActive(false);
            bootstrap = bootstrapObject.AddComponent<BoardBootstrap>();
            Set("boardView", boardView);
            Set("inputController", controller);
            Set("inputCamera", inputCamera);
            Set("levelHud", hud.View);
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
            Object.Destroy(hud.Root);
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
            Assert.That(hud.Moves.text, Is.EqualTo("7"));
            Assert.That(hud.Score.text, Is.EqualTo("0"));
            Assert.That(hud.Goal.text, Is.EqualTo("0/12"));
            Assert.That(hud.OutcomePanel.activeSelf, Is.False);
            Assert.That(hud.Restart.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator ValidMove_HudUpdatesWhenTheMoveFinishes_AndRestartIsUnavailableUntilThen()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            Assert.That(TryFindMove(CurrentBoard(), true, out var first, out var second), Is.True, "Expected a valid move.");

            Click(first);
            Click(second);

            Assert.That(controller.IsInputLocked, Is.True);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "The move did not finish.");
                // Busy: no restart offered, and the HUD still shows the values from before the move.
                Assert.That(hud.Restart.interactable, Is.False);
                Assert.That(hud.PlayAgain.interactable, Is.False, "Play Again follows the same busy gate.");
                Assert.That(hud.Moves.text, Is.EqualTo("5"));
                yield return null;
            }

            var level = bootstrap.LevelProgress;
            Assert.That(hud.Restart.interactable, Is.True);
            Assert.That(hud.Moves.text, Is.EqualTo("4"));
            Assert.That(level.Score, Is.GreaterThan(0));
            Assert.That(hud.Score.text, Is.EqualTo(level.Score.ToString()));
            Assert.That(hud.Goal.text, Is.EqualTo($"{level.GoalProgress}/50"));
        }

        [UnityTest]
        public IEnumerator InvalidMove_LeavesTheHudUnchanged()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            Assert.That(TryFindMove(CurrentBoard(), false, out var first, out var second), Is.True, "Expected an invalid swap.");

            Click(first);
            Click(second);
            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(hud.Restart.interactable, Is.False, "Busy during the rollback too.");
            yield return WaitUntilUnlocked();

            Assert.That(hud.Restart.interactable, Is.True);
            Assert.That(hud.Moves.text, Is.EqualTo("5"));
            Assert.That(hud.Score.text, Is.EqualTo("0"));
            Assert.That(hud.Goal.text, Is.EqualTo("0/50"));
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
            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("Level Complete!"));
            Assert.That(hud.Goal.text, Is.EqualTo("3/3"));
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
            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("Out of Moves"));
            Assert.That(hud.Moves.text, Is.EqualTo("0"));
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
            Assert.That(hud.Restart.interactable, Is.True, "Settled while Playing: restart is offered.");

            hud.Restart.onClick.Invoke();
            yield return null;

            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(firstLevel));
            AssertFreshLevel(startingTiles, 5);
        }

        [UnityTest]
        public IEnumerator Start_StartingSnapshot_IsAnIndependentCopyThatPlayDoesNotChange()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();

            Assert.That(StartingSnapshot(), Is.Not.SameAs(CurrentBoard()), "A separate Board, not another reference.");
            Assert.That(ReadTiles(StartingSnapshot()), Is.EqualTo(startingTiles));

            yield return PlayValidMove();

            Assert.That(ReadTiles(), Is.Not.EqualTo(startingTiles), "The move changed the authoritative board.");
            Assert.That(ReadTiles(StartingSnapshot()), Is.EqualTo(startingTiles), "The snapshot did not change.");
        }

        [UnityTest]
        public IEnumerator RestartLevel_RecreatesTheSameStartingBoardAndANewSnapshot()
        {
            Set("startingMoveCount", 5);
            Set("goalTarget", 50);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();
            var firstSnapshot = StartingSnapshot();

            yield return PlayValidMove();
            hud.Restart.onClick.Invoke();
            yield return null;

            Assert.That(StartingSnapshot(), Is.Not.SameAs(firstSnapshot), "Every session gets its own snapshot.");
            Assert.That(StartingSnapshot(), Is.Not.SameAs(CurrentBoard()));
            Assert.That(ReadTiles(StartingSnapshot()), Is.EqualTo(startingTiles));
            Assert.That(ReadTiles(), Is.EqualTo(startingTiles), "Same seed, same starting board.");
        }

        [UnityTest]
        public IEnumerator Seed93Route_LastMoveEndsTheLevel_LeavesTheDeadBoardBecauseLossIsNeverRecovered()
        {
            // Ten moves only: the tenth move loses, so recovery must not run. This also shows the real input,
            // resolution runner and bootstrap reach the dead board.
            ConfigureSeed93Level(10, 25);
            yield return StartBootstrap();

            for (int move = 0; move < 10; move++)
            {
                yield return PlaySeed93Move(move);
            }

            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Lost));
            Assert.That(ReadTiles(), Is.EqualTo(TilesFromRows(Seed93DeadRows)), "The real runner reaches the dead board.");
            Assert.That(BoardPlayability.HasAnyLegalMove(CurrentBoard()), Is.False);
        }

        [UnityTest]
        public IEnumerator Seed93Route_StillPlaying_RecoversTheDeadBoardInPlaceBeforeUnlocking()
        {
            ConfigureSeed93Level(20, 25);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();
            var authoritativeBoard = CurrentBoard();

            for (int move = 0; move < 9; move++)
            {
                yield return PlaySeed93Move(move);
            }

            // The tenth move would leave a dead board. Recovery runs before the controller unlocks,
            // so the first unlocked frame already shows a playable board.
            Click(new GridPosition(Seed93Moves[9, 0], Seed93Moves[9, 1]));
            Click(new GridPosition(Seed93Moves[9, 2], Seed93Moves[9, 3]));
            Assert.That(controller.IsInputLocked, Is.True);
            yield return WaitUntilUnlocked();

            Assert.That(CurrentBoard(), Is.SameAs(authoritativeBoard), "Recovery keeps the same Board object.");
            Assert.That(ReadTiles(), Is.Not.EqualTo(TilesFromRows(Seed93DeadRows)), "The dead board was replaced.");
            Assert.That(MatchFinder.FindMatches(CurrentBoard()), Is.Empty);
            Assert.That(BoardPlayability.HasAnyLegalMove(CurrentBoard()), Is.True);

            // Exactly the values the ten moves earn; recovery adds nothing.
            var level = bootstrap.LevelProgress;
            Assert.That(level.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(level.MovesRemaining, Is.EqualTo(10));
            Assert.That(level.GoalProgress, Is.EqualTo(3));
            Assert.That(level.Score, Is.EqualTo(3300));
            Assert.That(controller.enabled, Is.True);

            // One frame later the replaced views' deferred Destroy has run.
            yield return null;
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(64));
            AssertViewsMatchBoard();

            // Play continues on the recovered board through the same controller path: one more legal move.
            yield return PlayValidMove();
            Assert.That(level.MovesRemaining, Is.EqualTo(9), "Exactly one more move was consumed.");
            Assert.That(level.Score, Is.GreaterThan(3300), "The move scored its cleared tiles.");
            Assert.That(level.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(MatchFinder.FindMatches(CurrentBoard()), Is.Empty, "The board is stable again.");
            Assert.That(BoardPlayability.HasAnyLegalMove(CurrentBoard()), Is.True);
            yield return null;
            AssertViewsMatchBoard();
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)), "The next click is accepted normally.");

            hud.Restart.onClick.Invoke();
            yield return null;
            Assert.That(ReadTiles(), Is.EqualTo(startingTiles), "Restart still starts from the seed's starting board.");
            Assert.That(ReadTiles(StartingSnapshot()), Is.EqualTo(startingTiles));
        }

        [UnityTest]
        public IEnumerator ValidMoveResolved_DeadBoardWhilePlaying_RecoversWithoutChangingProgress()
        {
            ConfigureSeed93Level(20, 25);
            yield return StartBootstrap();
            CurrentBoard().CopyTilesFrom(BoardFromRows(Seed93DeadRows));

            ReportValidMove(TileType.Green);

            // Only the reported move counts: one move, one tile's score, no goal tiles.
            var level = bootstrap.LevelProgress;
            Assert.That(level.MovesRemaining, Is.EqualTo(19));
            Assert.That(level.Score, Is.EqualTo(100));
            Assert.That(level.GoalProgress, Is.Zero);
            Assert.That(BoardPlayability.HasAnyLegalMove(CurrentBoard()), Is.True);
            Assert.That(MatchFinder.FindMatches(CurrentBoard()), Is.Empty);

            yield return null;
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator ValidMoveResolved_DeadBoardOnWin_IsNotRecovered()
        {
            ConfigureSeed93Level(20, 3);
            yield return StartBootstrap();
            CurrentBoard().CopyTilesFrom(BoardFromRows(Seed93DeadRows));

            ReportValidMove(TileType.Red, TileType.Red, TileType.Red);

            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(ReadTiles(), Is.EqualTo(TilesFromRows(Seed93DeadRows)), "No recovery after the level is won.");
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
            Assert.That(hud.Restart.interactable, Is.True);

            hud.Restart.onClick.Invoke();
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
            Assert.That(hud.Restart.interactable, Is.True);

            hud.Restart.onClick.Invoke();
            yield return null;

            AssertFreshLevel(startingTiles, 1);
        }

        [UnityTest]
        public IEnumerator PlayAgain_AfterWon_UsesTheSameRestartPath()
        {
            Set("startingMoveCount", 2);
            Set("goalTarget", 3);
            yield return StartBootstrap();
            var startingTiles = ReadTiles();
            ReportValidMove(TileType.Red, TileType.Red, TileType.Red);
            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
            Assert.That(hud.PlayAgain.interactable, Is.True, "Settled after the last move: Play Again is offered.");

            hud.PlayAgain.onClick.Invoke();
            yield return null;

            AssertFreshLevel(startingTiles, 2);
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
            Assert.That(TryFindMove(board, true, out var first, out var second), Is.True, "Expected a valid move.");
            Click(first);
            Click(second);
            Assert.That(controller.IsInputLocked, Is.True);
            // The HUD never offers it; calling it anyway is a programming error and throws.
            Assert.That(hud.Restart.interactable, Is.False);

            Assert.Throws<InvalidOperationException>(() => bootstrap.RestartLevel());

            // Nothing was replaced: the running move still owns the same level and board.
            Assert.That(bootstrap.LevelProgress, Is.SameAs(level));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(controller.IsInputLocked, Is.True);

            yield return WaitUntilUnlocked();
            Assert.That(level.MovesRemaining, Is.EqualTo(4));
            Assert.That(hud.Restart.interactable, Is.True);
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

        [UnityTest]
        public IEnumerator RestartLevel_ReplaysPropellerTargetsAndRefillAcrossTwoPlayerMoves()
        {
            var colors = new[] { TileType.Red, TileType.Green, TileType.Blue,
                TileType.Yellow, TileType.Purple, TileType.Orange };
            Set("palette", colors);
            Set("seed", 42);
            Set("startingMoveCount", 5);
            Set("goalTarget", 1000);
            yield return StartBootstrap();
            var previousBoards = new List<TileType[,]>();
            var previousScores = new List<int>();
            var first = new GridPosition(1, 1);
            var second = new GridPosition(2, 1);

            for (int session = 0; session < 2; session++)
            {
                if (session > 0)
                {
                    bootstrap.RestartLevel();
                    yield return null;
                    Assert.That(bootstrap.LevelProgress.Score, Is.Zero);
                }
                var expectedTargeting = new System.Random(unchecked(42 ^ 0x5EED1234));
                for (int move = 0; move < 2; move++)
                {
                    var board = CurrentBoard();
                    // Install a controlled earned-special fixture, keeping the production session's RNGs.
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                            board.SetTile(new GridPosition(x, y), colors[(x + 2 * y) % colors.Length]);
                    board.SetTile(first, TileType.Propeller);
                    boardView.Render(board);
                    yield return null; // Finish destroying the previous fixture's views.

                    var candidates = new List<GridPosition>();
                    var outsideViews = new List<TileView>();
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                            if (Math.Abs(x - second.X) + Math.Abs(y - second.Y) > 1)
                            {
                                var cell = new GridPosition(x, y);
                                candidates.Add(cell);
                                outsideViews.Add(boardView.GetTileView(cell));
                            }
                    GridPosition expectedTarget = candidates[expectedTargeting.Next(candidates.Count)];
                    Click(first);
                    Click(second);
                    Assert.That(controller.IsInputLocked, Is.True);
                    Assert.Throws<InvalidOperationException>(() => bootstrap.RestartLevel());

                    // During the first fall, only the remote target outside takeoff has lost its old view.
                    bool observedTarget = false;
                    float deadline = Time.realtimeSinceStartup + 10f;
                    while (controller.IsInputLocked)
                    {
                        Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                        Assert.That(hud.Restart.interactable, Is.False);
                        if (!observedTarget)
                        {
                            int removedOutside = 0;
                            for (int i = 0; i < outsideViews.Count; i++)
                                if (outsideViews[i] == null)
                                {
                                    removedOutside++;
                                    Assert.That(candidates[i], Is.EqualTo(expectedTarget));
                                }
                            Assert.That(removedOutside, Is.LessThanOrEqualTo(1));
                            observedTarget = removedOutside == 1;
                        }
                        yield return null;
                    }

                    Assert.That(observedTarget, Is.True, "Observe the actual target before later cascades can remove other views.");
                    Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(4 - move));
                    Assert.That(hud.Restart.interactable, Is.True);
                    Assert.That(controller.SelectedPosition, Is.Null);
                    Assert.That(MatchFinder.FindMatches(board), Is.Empty);
                    Assert.That(ReadTiles(), Has.No.Member(TileType.Empty));
                    AssertViewsMatchBoard();
                    if (session == 0)
                    {
                        previousBoards.Add(ReadTiles());
                        previousScores.Add(bootstrap.LevelProgress.Score);
                    }
                    else
                    {
                        Assert.That(ReadTiles(), Is.EqualTo(previousBoards[move]), "Refill must replay too.");
                        Assert.That(bootstrap.LevelProgress.Score, Is.EqualTo(previousScores[move]));
                    }
                }
            }
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

            Assert.That(hud.Moves.text, Is.EqualTo(startingMoveCount.ToString()));
            Assert.That(hud.Score.text, Is.EqualTo("0"));
            Assert.That(hud.Goal.text, Is.EqualTo($"0/{level.GoalTarget}"));
            Assert.That(hud.OutcomePanel.activeSelf, Is.False, "A fresh level hides the outcome panel.");
            Assert.That(hud.Restart.interactable, Is.True);

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
            Assert.That(TryFindMove(CurrentBoard(), true, out var first, out var second), Is.True, "Expected a valid move.");
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

        // Finds the first adjacent swap that SwapValidator accepts (or, when `valid` is false, rejects),
        // trying it on a copy so the real Board is untouched.
        private static bool TryFindMove(Board board, bool valid, out GridPosition first, out GridPosition second)
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
                        if (SwapValidator.IsValidResult(copy, origin, neighbour) == valid)
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
            controller.HandlePointerDown(inputCamera.WorldToScreenPoint(boardView.GetTileView(position).transform.position));
        }

        private Board CurrentBoard()
        {
            return (Board)typeof(BoardBootstrap).GetField("board", Fields).GetValue(bootstrap);
        }

        // The shipped level's board and palette, framed so every cell can be clicked.
        private void ConfigureSeed93Level(int startingMoveCount, int goalTarget)
        {
            Set("width", 8);
            Set("height", 8);
            Set("palette", new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange });
            Set("seed", 93);
            Set("startingMoveCount", startingMoveCount);
            Set("goalTarget", goalTarget);
            cameraObject.transform.position = new Vector3(203.5f, 203.5f, -10f);
            inputCamera.orthographicSize = 8f;
        }

        // Clicks one recorded move and waits until the controller unlocks.
        private IEnumerator PlaySeed93Move(int move)
        {
            Click(new GridPosition(Seed93Moves[move, 0], Seed93Moves[move, 1]));
            Click(new GridPosition(Seed93Moves[move, 2], Seed93Moves[move, 3]));
            Assert.That(controller.IsInputLocked, Is.True, $"Move {move + 1} should start.");
            yield return WaitUntilUnlocked();
        }

        // Rows top-down, as on screen: R/G/B/Y/P/O are the normal colours.
        private static TileType[,] TilesFromRows(params string[] rows)
        {
            const string symbols = ".RGBYPO";
            var tiles = new TileType[rows[0].Length, rows.Length];
            for (int row = 0; row < rows.Length; row++)
            {
                for (int x = 0; x < rows[row].Length; x++)
                {
                    tiles[x, rows.Length - 1 - row] = (TileType)symbols.IndexOf(rows[row][x]);
                }
            }

            return tiles;
        }

        private static Board BoardFromRows(params string[] rows)
        {
            var tiles = TilesFromRows(rows);
            var board = new Board(tiles.GetLength(0), tiles.GetLength(1));
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), tiles[x, y]);
                }
            }

            return board;
        }

        // The private copy of the starting tiles that dead-board recovery will fall back to.
        private Board StartingSnapshot()
        {
            return (Board)typeof(BoardBootstrap).GetField("startingBoard", Fields).GetValue(bootstrap);
        }

        private TileType[,] ReadTiles()
        {
            return ReadTiles(CurrentBoard());
        }

        private static TileType[,] ReadTiles(Board board)
        {
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
