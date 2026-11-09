#if UNITY_EDITOR
using System;
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Match3.Presentation.Tests
{
    public partial class BoardBootstrapTests
    {
        private GameFlowController flow;
        private LevelCatalog flowCatalog;
        private string flowKey;
        private TestMainMenu menu;
        private TestConfirmation prompt;
        private static readonly int[] StoredFlowProgress = { -1, 0, 1, 2, 99 };

        private void PrepareFlow(int saved = 0, bool emptyCatalog = false)
        {
            flowKey = "Match3.Tests.Flow." + Guid.NewGuid().ToString("N");
            PlayerPrefs.SetInt(flowKey, saved);
            flowCatalog = ScriptableObject.CreateInstance<LevelCatalog>();
            typeof(LevelCatalog).GetField("levels", Fields).SetValue(flowCatalog, emptyCatalog
                ? new LevelDefinition[0] : new[]
                {
                    new LevelDefinition(2, TileType.Red, 1, 42),
                    new LevelDefinition(1, TileType.Blue, 1, 73)
                });
            var go = new GameObject("TestGameFlow");
            go.transform.SetParent(bootstrapObject.transform, false);
            flow = go.AddComponent<GameFlowController>();
            typeof(GameFlowController).GetField("catalog", Fields).SetValue(flow, flowCatalog);
            typeof(GameFlowController).GetField("bootstrap", Fields).SetValue(flow, bootstrap);
            typeof(GameFlowController).GetField("hud", Fields).SetValue(flow, hud.View);
            menu = TestMainMenu.Create(bootstrapObject.transform);
            typeof(GameFlowController).GetField("mainMenu", Fields).SetValue(flow, menu.View);
            prompt = TestConfirmation.Create(bootstrapObject.transform);
            typeof(GameFlowController).GetField("confirmation", Fields).SetValue(flow, prompt.View);
        }

        private void InitializeFlow()
        {
            flow.Initialize(new ProgressionStore(flowCatalog.Count, flowKey));
            bootstrapObject.SetActive(true);
        }

        [TearDown]
        public void CleanFlowPreferences()
        {
            if (flowKey != null) { PlayerPrefs.DeleteKey(flowKey); PlayerPrefs.Save(); flowKey = null; }
            if (flowCatalog != null) UnityEngine.Object.DestroyImmediate(flowCatalog);
        }

        [UnityTest]
        public IEnumerator GameFlow_MenuLoadsProgress_AndPlayStartsOnlyTheChosenLevel(
            [ValueSource(nameof(StoredFlowProgress))] int saved)
        {
            PrepareFlow(saved); InitializeFlow();
            yield return null;
            int expected = Mathf.Clamp(saved, 0, 2);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(flow.Progression, Is.EqualTo(expected));
            Assert.That(flow.MainMenuActionLabel, Is.EqualTo(expected == 0 ? "Play" : expected == 2 ? "Play Again" : "Continue"));
            Assert.That(bootstrap.LevelProgress, Is.Null);
            Assert.That(controller.enabled, Is.False);
            Assert.That(hud.Root.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(boardObject.activeInHierarchy, Is.True);
            Assert.That(menu.Background.activeInHierarchy && menu.Controls.activeInHierarchy, Is.True);
            Assert.That(menu.Label.text, Is.EqualTo(flow.MainMenuActionLabel));
            menu.Play.onClick.Invoke();
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(menu.Background.activeSelf || menu.Controls.activeSelf, Is.False);
            var board = CurrentBoard();
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(expected == 2 ? 0 : expected));
            Assert.That(bootstrap.LevelProgress.GoalTileType, Is.EqualTo(flowCatalog[flow.CurrentLevelIndex].GoalTileType));
            Assert.That(flow.Play(), Is.False);
            Assert.That(CurrentBoard(), Is.SameAs(board));
            yield return null;
            AssertViewsMatchBoard();
        }

        [Test]
        public void GameFlow_EmptyCatalog_FailsBeforeCreatingASessionOrChangingStoredProgress()
        {
            PrepareFlow(1, emptyCatalog: true);
            Assert.Throws<InvalidOperationException>(() => flow.Initialize(new ProgressionStore(2, flowKey)));
            Assert.That(bootstrap.LevelProgress, Is.Null);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Uninitialized));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GameFlow_RealWin_SavesOnceWhileLocked_AndCannotNavigateBeforeCleanup()
        {
            PrepareFlow(); InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard();
            // A controlled earned-special fixture; resolution and scoring use the real production runner.
            board.SetTile(new GridPosition(1, 1), TileType.LightBall);
            board.SetTile(new GridPosition(2, 1), TileType.Red);
            boardView.Render(board);
            yield return null;
            var callbackField = typeof(BoardBootstrap).GetField("onLevelEnded", Fields);
            var original = (Action<LevelStatus>)callbackField.GetValue(bootstrap);
            int callbacks = 0;
            bool observedLockedOutcome = false;
            callbackField.SetValue(bootstrap, new Action<LevelStatus>(status =>
            {
                callbacks++;
                original(status);
                Assert.That(bootstrap.IsBusy, Is.True);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
                Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(hud.OutcomeAction.text, Is.EqualTo("Next Level"));
                Assert.That(hud.OutcomeTitle.text, Is.EqualTo("Level Complete!"));
                Assert.That(hud.PlayAgain.interactable, Is.False);
                Assert.That(hud.OutcomeMenu.interactable, Is.False);
                Assert.That(hud.Restart.interactable, Is.False);
                Assert.That(flow.Next() || flow.Retry() || flow.PlayAgain() || flow.ReturnToMenu() || flow.Restart(), Is.False);
                observedLockedOutcome = true;
            }));
            Click(new GridPosition(1, 1)); Click(new GridPosition(2, 1));
            Assert.That(flow.ReturnToMenu(), Is.False);
            Assert.That(flow.Restart(), Is.False, "A Playing session still rejects restart while its move is busy.");
            yield return WaitUntilUnlocked();
            Assert.That(observedLockedOutcome, Is.True, "Assertions in a coroutine callback must actually be reached.");
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(hud.PlayAgain.interactable, Is.True);
            Assert.That(hud.OutcomeMenu.interactable, Is.True);
            Assert.That(hud.Restart.interactable, Is.False);
            // A duplicate terminal callback cannot advance twice or reinterpret the outcome.
            original(LevelStatus.Won);
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
            hud.PlayAgain.onClick.Invoke();
            var nextBoard = CurrentBoard();
            hud.PlayAgain.onClick.Invoke();
            Assert.That(CurrentBoard(), Is.SameAs(nextBoard), "A duplicate outcome click cannot start another session.");
            Assert.That(flow.Next(), Is.False);
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(CurrentBoard(), Is.Not.SameAs(board));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1), "Next navigates; it does not save.");
        }

        [UnityTest]
        public IEnumerator GameFlow_LossRetryRestartAndMenu_NeverAdvanceProgress_AndRebuildDeterministically()
        {
            PrepareFlow(1); InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard(); var tiles = ReadTiles();
            ReportValidMove(TileType.Green);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(hud.OutcomeAction.text, Is.EqualTo("Retry"));
            Assert.That(flow.Next(), Is.False);
            Assert.That(flow.PlayAgain(), Is.False);
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
            Assert.That(flow.Retry(), Is.True);
            Assert.That(flow.Retry(), Is.False);
            Assert.That(CurrentBoard(), Is.Not.SameAs(board));
            Assert.That(ReadTiles(), Is.EqualTo(tiles));
            var retry = CurrentBoard();
            Assert.That(flow.Restart(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            Assert.That(CurrentBoard(), Is.Not.SameAs(retry));
            Assert.That(ReadTiles(), Is.EqualTo(tiles));
            Assert.That(flow.ReturnToMenu(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            Assert.That(controller.enabled, Is.False);
            Assert.That(boardObject.activeInHierarchy, Is.True);
            Assert.That(menu.Background.activeInHierarchy && menu.Controls.activeInHierarchy, Is.True);
            Assert.That(menu.Label.text, Is.EqualTo("Continue"));
            Assert.That(flow.ReturnToMenu(), Is.False);
            Assert.That(flow.Play(), Is.True);
            Assert.That(ReadTiles(), Is.EqualTo(tiles));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
            yield return null;
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator GameFlow_FreshLossRetryRestartAndMenu_DoNotEvenCreateASaveKey()
        {
            PrepareFlow();
            PlayerPrefs.DeleteKey(flowKey);
            InitializeFlow(); flow.Play();
            yield return null;
            ReportValidMove(TileType.Green); ReportValidMove(TileType.Green);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(PlayerPrefs.HasKey(flowKey), Is.False, "Loss must not call Save, even with zero progress.");
            Assert.That(flow.Retry(), Is.True);
            Assert.That(PlayerPrefs.HasKey(flowKey), Is.False);
            Assert.That(flow.Restart(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            Assert.That(PlayerPrefs.HasKey(flowKey), Is.False);
            Assert.That(flow.ReturnToMenu(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            Assert.That(PlayerPrefs.HasKey(flowKey), Is.False);
        }

        [UnityTest]
        public IEnumerator GameFlow_CompletedReplay_NextUsesCurrentIndex_AndFinalPlayAgainKeepsCompletion()
        {
            PrepareFlow(2); InitializeFlow(); flow.Play();
            yield return null;
            Assert.That(flow.CurrentLevelIndex, Is.Zero);
            ReportValidMove(TileType.Red);
            Assert.That(flow.Progression, Is.EqualTo(2));
            Assert.That(flow.Next(), Is.True);
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(1));
            ReportValidMove(TileType.Blue);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(hud.OutcomeAction.text, Is.EqualTo("Play Again"));
            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("All Levels Complete"));
            Assert.That(flow.Next(), Is.False);
            hud.PlayAgain.onClick.Invoke();
            Assert.That(flow.PlayAgain(), Is.False);
            Assert.That(flow.CurrentLevelIndex, Is.Zero);
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(2));
            Assert.That(flow.ReturnToMenu(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            Assert.That(flow.MainMenuActionLabel, Is.EqualTo("Play Again"));
            Assert.That(flow.Play(), Is.True);
            Assert.That(flow.CurrentLevelIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GameFlow_OutcomeMenu_ReturnsWithoutSavingOrReplacingTheSession(
            [Values(0, 1, 2)] int ending)
        {
            // Loss, non-final Win, final Win: the same Menu button has no level-specific navigation rule.
            PrepareFlow(ending == 2 ? 1 : 0); InitializeFlow(); flow.Play();
            yield return null;
            if (ending == 0) { ReportValidMove(TileType.Green); ReportValidMove(TileType.Green); }
            else ReportValidMove(ending == 2 ? TileType.Blue : TileType.Red);

            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(hud.OutcomeAction.text, Is.EqualTo(ending == 0 ? "Retry" : ending == 1 ? "Next Level" : "Play Again"));
            Assert.That(hud.OutcomeMenu.interactable, Is.True);
            Assert.That(hud.Restart.interactable, Is.False);
            var finishedBoard = CurrentBoard();
            var finishedProgress = bootstrap.LevelProgress;
            int saved = PlayerPrefs.GetInt(flowKey);
            hud.OutcomeMenu.onClick.Invoke();
            hud.OutcomeMenu.onClick.Invoke();

            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(controller.enabled, Is.False);
            Assert.That(CurrentBoard(), Is.SameAs(finishedBoard));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(finishedProgress));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(saved));
            Assert.That(menu.Label.text, Is.EqualTo(ending == 0 ? "Play" : ending == 1 ? "Continue" : "Play Again"));
            Assert.That(menu.Background.activeInHierarchy && menu.Controls.activeInHierarchy, Is.True);
            Assert.That(boardObject.activeInHierarchy, Is.True);
            Assert.That(hud.Root.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(flow.Retry() || flow.Next() || flow.PlayAgain(), Is.False);
        }

        [TestCase(false, "Play")]
        [TestCase(true, "Play")]
        [TestCase(false, "Restart")]
        [TestCase(true, "Restart")]
        [TestCase(false, "Next")]
        [TestCase(true, "Next")]
        [TestCase(false, "Retry")]
        [TestCase(true, "Retry")]
        [TestCase(false, "PlayAgain")]
        [TestCase(true, "PlayAgain")]
        public void GameFlow_SameUpdateUiGesture_NeverLeaksToNewSession(bool mouseInput, string action)
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
                Pointer pointer = mouseInput ? (Pointer)mouse : touch;
                _ = pointer.press.wasPressedThisFrame;
                PrepareFlow(action == "PlayAgain" ? 1 : 0); InitializeFlow();
                if (action != "Play")
                {
                    flow.Play();
                    if (action == "Next") ReportValidMove(TileType.Red);
                    if (action == "PlayAgain") ReportValidMove(TileType.Blue);
                    if (action == "Retry") { ReportValidMove(TileType.Green); ReportValidMove(TileType.Green); }
                    if (action == "Restart") Assert.That(flow.Restart(), Is.True);
                }
                // All fixture levels have the same board geometry, before and after session replacement.
                Vector2 point = inputCamera.WorldToScreenPoint(boardObject.transform.position);
                QueueUiTap(touch, mouse, point, 1); InputSystem.Update();
                bool started = action == "Play" ? flow.Play() : action == "Restart" ? flow.Confirm()
                    : action == "Next" ? flow.Next() : action == "Retry" ? flow.Retry() : flow.PlayAgain();
                Assert.That(started, Is.True);
                PollInput(); PollInput();
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(bootstrap.IsBusy, Is.False);
                QueueUiTap(touch, mouse, point, 2); InputSystem.Update(); PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
            }
            finally { input.TearDown(); }
        }
    }
}
#endif
