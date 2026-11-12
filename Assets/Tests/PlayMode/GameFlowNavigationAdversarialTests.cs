#if UNITY_EDITOR
using System;
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Match3.Presentation.Tests
{
    public partial class BoardBootstrapTests
    {
        [UnityTest]
        public IEnumerator NavigationAdversarial_RestartAndSwipe_FirstAcceptedActionOwnsTheFrame(
            [Values(false, true)] bool restartFirst)
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                _ = touch.press.wasPressedThisFrame;
                PrepareNavigationReview(); InitializeFlow(); flow.Play();
                yield return null;
                var board = CurrentBoard(); var tiles = ReadTiles(); var progress = bootstrap.LevelProgress;
                Assert.That(TryFindMove(board, true, out var first, out var second), Is.True);
                Vector2 start = inputCamera.WorldToScreenPoint(boardView.GetTileView(first).transform.position);
                Vector2 end = inputCamera.WorldToScreenPoint(boardView.GetTileView(second).transform.position);
                if (restartFirst) hud.Restart.onClick.Invoke();
                SendNavigationTouch(touch, TouchPhase.Began, start);
                SendNavigationTouch(touch, TouchPhase.Moved, end);
                if (!restartFirst) hud.Restart.onClick.Invoke();
                SendNavigationTouch(touch, TouchPhase.Ended, end);
                if (restartFirst)
                {
                    Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
                    Assert.That(flow.PendingConfirmation, Is.EqualTo(ConfirmationIntent.Restart));
                    Assert.That(bootstrap.IsBusy, Is.False);
                    Assert.That(ReadTiles(), Is.EqualTo(tiles));
                    Assert.That(progress.MovesRemaining, Is.EqualTo(20));
                    prompt.Cancel.onClick.Invoke();
                }
                else
                {
                    AssertBusyNavigationRefused();
                    yield return WaitUntilUnlocked();
                    Assert.That(progress.MovesRemaining, Is.EqualTo(19));
                    Assert.That(progress.Score, Is.GreaterThan(0));
                }
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Assert.That(flow.PendingConfirmation, Is.Null);
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(PlayerPrefs.GetInt(flowKey), Is.Zero);
                yield return null;
                AssertViewsMatchBoard();
            }
            finally
            {
                if (flow != null) flow.enabled = false;
                input.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator NavigationAdversarial_EveryBusyFrameRejectsNavigation_ThroughRollbackCascadeOrChain(
            [Values("Rollback", "Cascade", "Chain")] string scenario)
        {
            PrepareNavigationReview();
            Set("width", 3); Set("height", 3); Set("palette", CatalogPalette);
            // Seed 4 is a known playable 3x3/six-colour generation fixture. These are test-only level values.
            typeof(LevelCatalog).GetField("levels", Fields).SetValue(flowCatalog, new[]
            {
                new LevelDefinition(5, TileType.Blue, scenario == "Cascade" ? 3 : 1000, 4),
                new LevelDefinition(7, TileType.Red, 1000, 42)
            });
            InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
            board.CopyTilesFrom(BoardFromRows("GBY", "GPO", "RGG"));
            System.Random refill = scenario == "Cascade"
                ? new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5) : new System.Random(7);
            var first = new GridPosition(0, 0); var second = new GridPosition(1, 0);
            if (scenario == "Rollback") { first = new GridPosition(1, 0); second = new GridPosition(1, 1); }
            if (scenario == "Chain")
            {
                board.SetTile(first, TileType.LightBall);
                board.SetTile(second, TileType.Propeller);
                var copy = new Board(3, 3); copy.CopyTilesFrom(board); copy.TrySwapAdjacent(first, second);
                var move = SwapValidator.Analyze(copy, first, second);
                Assert.That(SpecialEffects.Plan(copy, move.InitialActivations, new System.Random(2)).Effects.Count, Is.GreaterThanOrEqualTo(2));
            }
            boardView.Render(board);
            var runner = new BoardResolutionRunner(board, boardView, CatalogPalette, refill, 0.08f, 0.08f, new System.Random(2));
            typeof(BoardInputController).GetField("resolution", Fields).SetValue(controller, runner);
            typeof(BoardBootstrap).GetField("refillRandom", Fields).SetValue(bootstrap, refill);
            yield return null;
            var before = ReadTiles(); var moving = boardView.GetTileView(first);
            var callbackField = typeof(BoardInputController).GetField("onValidMoveResolved", Fields);
            var original = (Action<ResolutionResult>)callbackField.GetValue(controller);
            ResolutionResult result = null;
            bool observedCallback = false;
            callbackField.SetValue(controller, new Action<ResolutionResult>(r =>
            {
                AssertBusyNavigationRefused();
                original(r);
                AssertBusyNavigationRefused(); // Includes the terminal-but-still-locked window for Cascade.
                result = r; observedCallback = true;
            }));
            Click(first); Click(second);
            Assert.That(bootstrap.IsBusy, Is.True);
            bool sawRollback = false, sawFlight = false;
            int busyFrames = 0;
            // Exercise the actual focus callback while a cascade owns the coroutine; this is not OS suspension.
            if (scenario == "Cascade") SendNavigationFocus(false);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (bootstrap.IsBusy)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                AssertBusyNavigationRefused();
                Assert.That(progress.MovesRemaining, Is.EqualTo(5));
                Assert.That(progress.Score, Is.Zero);
                sawRollback |= scenario == "Rollback" && moving != null && moving.Position == second;
                sawFlight |= boardObject.transform.Find("WispFlight") != null;
                busyFrames++;
                yield return null;
            }
            if (scenario == "Cascade") SendNavigationFocus(true);
            Assert.That(busyFrames, Is.GreaterThan(0));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(flow.PendingConfirmation, Is.Null, "Ignored requests must never appear after unlock.");
            if (scenario == "Rollback")
            {
                Assert.That(sawRollback, Is.True, "Navigation was challenged during the reverse animation too.");
                Assert.That(ReadTiles(), Is.EqualTo(before));
                Assert.That(observedCallback, Is.False);
                Assert.That(progress.MovesRemaining, Is.EqualTo(5));
                Assert.That(progress.Score, Is.Zero);
            }
            else
            {
                Assert.That(observedCallback, Is.True, "Callback assertions must have been reached.");
                Assert.That(progress.MovesRemaining, Is.EqualTo(4));
                Assert.That(progress.Score, Is.GreaterThan(0));
                if (scenario == "Cascade")
                {
                    Assert.That(result.ClearedCount, Is.EqualTo(9));
                    Assert.That(result.CountOf(TileType.Blue), Is.EqualTo(3));
                    Assert.That(progress.Score, Is.EqualTo(900));
                    Assert.That(progress.Status, Is.EqualTo(LevelStatus.Won));
                }
                else Assert.That(sawFlight, Is.True, "The Wisp phase of the chain must actually run.");
            }
            Assert.That(flow.Mode, Is.EqualTo(progress.Status == LevelStatus.Playing ? GameFlowMode.Playing : GameFlowMode.Outcome));
            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            yield return null;
            AssertNoSelectionHighlights(); AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator NavigationAdversarial_Seed93Recovery_RejectsRequestsOnBothSidesOfTheLockedRecovery()
        {
            PrepareNavigationReview(); ConfigureSeed93Level(20, 25);
            typeof(LevelCatalog).GetField("levels", Fields).SetValue(flowCatalog, new[]
            {
                new LevelDefinition(20, TileType.Red, 25, 93), new LevelDefinition(7, TileType.Blue, 1000, 42)
            });
            InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard();
            var callbackField = typeof(BoardInputController).GetField("onValidMoveResolved", Fields);
            var original = (Action<ResolutionResult>)callbackField.GetValue(controller);
            int reports = 0; bool observedRecovery = false;
            callbackField.SetValue(controller, new Action<ResolutionResult>(r =>
            {
                reports++;
                if (reports == 10)
                {
                    Assert.That(ReadTiles(), Is.EqualTo(TilesFromRows(Seed93DeadRows)));
                    AssertBusyNavigationRefused();
                }
                original(r);
                if (reports == 10)
                {
                    AssertBusyNavigationRefused();
                    Assert.That(ReadTiles(), Is.Not.EqualTo(TilesFromRows(Seed93DeadRows)));
                    Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.True);
                    observedRecovery = true;
                }
            }));
            for (int i = 0; i < Seed93Moves.GetLength(0); i++) yield return PlaySeed93Move(i);
            Assert.That(observedRecovery, Is.True);
            Assert.That(reports, Is.EqualTo(10));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(flow.PendingConfirmation, Is.Null);
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(10));
            Assert.That(bootstrap.LevelProgress.Score, Is.EqualTo(3300));
            Assert.That(bootstrap.LevelProgress.GoalProgress, Is.EqualTo(3));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.Zero);
            yield return null;
            AssertViewsMatchBoard();
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [UnityTest]
        public IEnumerator NavigationAdversarial_SingleLevel_LastMoveWinAndRepeatedActionsRespectFinalBoundary()
        {
            PrepareNavigationReview();
            typeof(LevelCatalog).GetField("levels", Fields).SetValue(flowCatalog,
                new[] { new LevelDefinition(1, TileType.Red, 1, 42) });
            InitializeFlow();
            menu.Play.onClick.Invoke(); var board = CurrentBoard();
            for (int i = 0; i < 5; i++) menu.Play.onClick.Invoke();
            Assert.That(CurrentBoard(), Is.SameAs(board), "Play spam starts only one session.");
            yield return null;
            board.SetTile(new GridPosition(1, 1), TileType.LightBall);
            board.SetTile(new GridPosition(2, 1), TileType.Red);
            boardView.Render(board);
            yield return null;
            var callbackField = typeof(BoardBootstrap).GetField("onLevelEnded", Fields);
            var original = (Action<LevelStatus>)callbackField.GetValue(bootstrap);
            int reports = 0; bool observedLockedFinal = false;
            callbackField.SetValue(bootstrap, new Action<LevelStatus>(status =>
            {
                reports++; original(status);
                Assert.That(status, Is.EqualTo(LevelStatus.Won));
                Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.Zero);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
                Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
                AssertBusyNavigationRefused();
                original(status); // A duplicated terminal notification is ignored while already in Outcome.
                Assert.That(CurrentBoard(), Is.SameAs(board));
                observedLockedFinal = true;
            }));
            Click(new GridPosition(1, 1)); Click(new GridPosition(2, 1));
            yield return WaitUntilUnlocked();
            Assert.That(observedLockedFinal, Is.True);
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("All Levels Complete"));
            Assert.That(hud.OutcomeAction.text, Is.EqualTo("Play Again"));
            Assert.That(flow.Next(), Is.False);
            Assert.That(flow.Retry(), Is.False);
            hud.PlayAgain.onClick.Invoke(); var replay = CurrentBoard();
            for (int i = 0; i < 5; i++) hud.PlayAgain.onClick.Invoke();
            Assert.That(CurrentBoard(), Is.SameAs(replay));
            Assert.That(replay, Is.Not.SameAs(board));
            Assert.That(flow.CurrentLevelIndex, Is.Zero);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(1));
            Assert.That(bootstrap.LevelProgress.Score, Is.Zero);
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
            yield return null;
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator NavigationAdversarial_FocusLossOnConfirmation_PreservesIntentAndCancelSelection(
            [Values(ConfirmationIntent.Restart, ConfirmationIntent.Leave)] ConfirmationIntent intent)
        {
            PrepareNavigationReview(); InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
            Click(new GridPosition(0, 0));
            var selected = boardView.GetTileView(new GridPosition(0, 0));
            if (intent == ConfirmationIntent.Restart) hud.Restart.onClick.Invoke();
            else hud.GameplayMenu.onClick.Invoke();
            SendNavigationFocus(false); SendNavigationFocus(true);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
            Assert.That(flow.PendingConfirmation, Is.EqualTo(intent));
            Assert.That(controller.enabled, Is.False);
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(prompt.Card.activeInHierarchy && prompt.Background.activeInHierarchy, Is.True);
            Assert.That(selected.transform.Find("SelectionHighlight").gameObject.activeSelf, Is.False);
            Assert.That(flow.HandleBack(), Is.True);
            Assert.That(flow.CancelConfirmation(), Is.False, "Cancel cannot run twice.");
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(selected.transform.Find("SelectionHighlight").gameObject.activeSelf, Is.True);
            Assert.That(progress.MovesRemaining, Is.EqualTo(20));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.Zero);
        }

        private void PrepareNavigationReview()
        {
            PrepareFlow();
            typeof(LevelCatalog).GetField("levels", Fields).SetValue(flowCatalog, new[]
            {
                new LevelDefinition(20, TileType.Red, 1000, 42),
                new LevelDefinition(7, TileType.Blue, 1000, 73)
            });
        }

        private void AssertBusyNavigationRefused()
        {
            Assert.That(bootstrap.IsBusy, Is.True);
            var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
            var mode = flow.Mode; int index = flow.CurrentLevelIndex, saved = PlayerPrefs.GetInt(flowKey);
            Assert.That(flow.Restart(), Is.False);
            Assert.That(flow.ReturnToMenu(), Is.False);
            Assert.That(flow.HandleBack(), Is.False);
            Assert.That(flow.Play(), Is.False);
            Assert.That(flow.Next(), Is.False);
            Assert.That(flow.Retry(), Is.False);
            Assert.That(flow.PlayAgain(), Is.False);
            Assert.That(flow.Confirm(), Is.False);
            Assert.That(flow.CancelConfirmation(), Is.False);
            Assert.That(hud.Restart.interactable || hud.GameplayMenu.interactable
                || hud.PlayAgain.interactable || hud.OutcomeMenu.interactable, Is.False);
            // Invoke bypasses Button.interactable: also prove the defensive command guards, not just UI styling.
            hud.Restart.onClick.Invoke(); hud.GameplayMenu.onClick.Invoke();
            hud.PlayAgain.onClick.Invoke(); hud.OutcomeMenu.onClick.Invoke();
            Assert.That(flow.Mode, Is.EqualTo(mode));
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(index));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(saved));
            Assert.That(flow.PendingConfirmation, Is.Null);
            Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
        }

        private void SendNavigationTouch(Touchscreen touch, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = phase, position = position });
            InputSystem.Update();
            if (controller.enabled) PollInput();
        }

        // A callback simulation, not an assertion about Android suspension or native foreground lifecycle.
        private void SendNavigationFocus(bool focused) => typeof(BoardInputController)
            .GetMethod("OnApplicationFocus", Fields).Invoke(controller, new object[] { focused });
    }
}
#endif
