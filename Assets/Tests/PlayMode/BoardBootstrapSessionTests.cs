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
        public IEnumerator StartLevel_SuppliedDefinitionsReplaceSessionAndClearSelectionAndGesture()
        {
            yield return StartBootstrap();
            var first = new LevelDefinition(7, TileType.Red, 1000, 42);
            bootstrap.StartLevel(first, _ => { });
            yield return PlayValidMove();
            var oldBoard = CurrentBoard();
            var oldTiles = ReadTiles();
            var oldProgress = bootstrap.LevelProgress;
            var oldSnapshot = StartingSnapshot();
            var oldRefill = typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap);
            Click(new GridPosition(0, 0));
            Vector2 point = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(1, 1)).transform.position);
            controller.HandlePointerDown(point);

            var second = new LevelDefinition(11, TileType.Blue, 18, 123);
            bootstrap.StartLevel(second, _ => { });
            controller.HandlePointerUp(point);
            yield return null;

            Assert.That(CurrentBoard(), Is.Not.SameAs(oldBoard));
            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(oldProgress));
            Assert.That(StartingSnapshot(), Is.Not.SameAs(oldSnapshot));
            Assert.That(typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap), Is.Not.SameAs(oldRefill));
            Assert.That(ReadTiles(oldBoard), Is.EqualTo(oldTiles));
            Assert.That(ReadTiles(), Is.EqualTo(ReadTiles(BoardGenerator.GeneratePlayable(Size, Size,
                new[] { TileType.Red, TileType.Green, TileType.Blue }, second.Seed))));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(11));
            Assert.That(bootstrap.LevelProgress.GoalTileType, Is.EqualTo(TileType.Blue));
            Assert.That(bootstrap.LevelProgress.GoalTarget, Is.EqualTo(18));
            Assert.That(bootstrap.LevelProgress.Score, Is.Zero);
            Assert.That(bootstrap.LevelProgress.GoalProgress, Is.Zero);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(hud.Goal.text, Is.EqualTo("0/18"));
            AssertNoSelectionHighlights();
            AssertViewsMatchBoard();

            bootstrap.StartLevel(first, _ => { });
            yield return null;
            Assert.That(ReadTiles(), Is.EqualTo(ReadTiles(BoardGenerator.GeneratePlayable(Size, Size,
                new[] { TileType.Red, TileType.Green, TileType.Blue }, first.Seed))));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(7));
            Assert.That(bootstrap.LevelProgress.GoalTileType, Is.EqualTo(TileType.Red));
        }

        [UnityTest]
        public IEnumerator StartLevel_BeforeUnityStart_IsNotReplacedByStandaloneStartup()
        {
            var supplied = new LevelDefinition(9, TileType.Green, 17, 123);
            bootstrap.StartLevel(supplied, _ => { });
            var progress = bootstrap.LevelProgress;
            var board = CurrentBoard();
            yield return StartBootstrap();
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(progress.MovesRemaining, Is.EqualTo(9));
            Assert.That(progress.GoalTileType, Is.EqualTo(TileType.Green));
        }

        [Test]
        public void StartLevel_NullArguments_DoNotCreateASession()
        {
            Assert.Throws<ArgumentNullException>(() => bootstrap.StartLevel(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() => bootstrap.StartLevel(definition, null));
            Assert.That(bootstrap.LevelProgress, Is.Null);
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true), Is.Empty);
        }

        [UnityTest]
        public IEnumerator FlowGate_ClosedBeforeSession_IsHonouredByStartAndReopensWithoutReplacingIt()
        {
            bootstrap.SetInputAvailable(false);
            Assert.That(controller.enabled, Is.False);
            yield return StartBootstrap();
            var board = CurrentBoard();
            var progress = bootstrap.LevelProgress;
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.Null);
            bootstrap.SetInputAvailable(true);
            Assert.That(controller.enabled, Is.True);
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [UnityTest]
        public IEnumerator FlowGate_DisableDuringMove_DoesNotStopCleanupOrReopenItOnBusyFalse()
        {
            ConfigureLevel(goalTarget: 1000);
            yield return StartBootstrap();
            Assert.That(TryFindMove(CurrentBoard(), true, out var first, out var second), Is.True);
            Click(first); Click(second);
            Assert.That(bootstrap.IsBusy, Is.True);
            bootstrap.SetInputAvailable(false);
            Assert.That(controller.enabled, Is.False);
            Assert.That(bootstrap.IsBusy, Is.True);
            yield return WaitUntilUnlocked();
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(19));
            Assert.That(hud.Restart.interactable, Is.True, "The busy gate reports completion independently of the board flow gate.");
            Assert.That(hud.PlayAgain.interactable, Is.True);
            Assert.That(controller.enabled, Is.False);
            AssertViewsMatchBoard();
            bootstrap.SetInputAvailable(true);
            Assert.That(controller.enabled, Is.True);
            Assert.That(hud.Restart.interactable, Is.True);
            Assert.That(controller.SelectedPosition, Is.Null);
        }

        [UnityTest]
        public IEnumerator Outcome_CallbackReportsOnceWhileLocked_AndButtonsWaitForCleanup()
        {
            yield return StartBootstrap();
            Assert.That(TryFindMove(CurrentBoard(), true, out var first, out var second), Is.True);
            var preview = new Board(Size, Size);
            preview.CopyTilesFrom(CurrentBoard());
            preview.TrySwapAdjacent(first, second);
            TileType matchedColour = preview.GetTile(MatchFinder.Analyze(preview).MatchedPositions[0]);
            foreach (bool win in new[] { true, false })
            {
                int calls = 0;
                LevelStatus reported = LevelStatus.Playing;
                bool observedLocked = false, observedUnavailable = false, observedVisible = false;
                bootstrap.StartLevel(new LevelDefinition(1, matchedColour, win ? 1 : 1000, 42), status =>
                {
                    calls++;
                    reported = status;
                    observedLocked = bootstrap.IsBusy;
                    observedUnavailable = !hud.Restart.interactable && !hud.PlayAgain.interactable;
                    observedVisible = hud.OutcomePanel.activeSelf;
                    bootstrap.SetInputAvailable(true);
                });
                var board = CurrentBoard();
                var progress = bootstrap.LevelProgress;
                Click(first); Click(second);
                yield return WaitUntilUnlocked();
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(reported, Is.EqualTo(win ? LevelStatus.Won : LevelStatus.Lost));
                Assert.That(observedLocked && observedUnavailable && observedVisible, Is.True);
                Assert.That(CurrentBoard(), Is.SameAs(board), "No navigation occurs in the outcome callback.");
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(controller.enabled, Is.False, "The flow gate cannot revive terminal input.");
                Assert.That(bootstrap.IsBusy, Is.False);
                Assert.That(hud.Restart.interactable && hud.PlayAgain.interactable, Is.True);
                bootstrap.SetInputAvailable(false);
                bootstrap.SetInputAvailable(true);
                Assert.That(controller.enabled, Is.False);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void FlowGate_SameUpdateUiTapIsIgnored_AfterCancelOrSessionReplacement(bool mouseInput, bool replaceSession)
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
                Pointer pointer = mouseInput ? (Pointer)mouse : touch;
                _ = pointer.press.wasPressedThisFrame;
                StartConfiguredLevel();
                var cell = new GridPosition(0, 0);
                Click(cell);
                var oldBoard = CurrentBoard();
                bootstrap.SetInputAvailable(false);
                Vector2 point = inputCamera.WorldToScreenPoint(boardView.GetTileView(cell).transform.position);
                QueueUiTap(touch, mouse, point, 1);
                InputSystem.Update();
                Assert.That(pointer.press.wasPressedThisFrame && pointer.press.wasReleasedThisFrame, Is.True);
                if (replaceSession) StartConfiguredLevel();
                bootstrap.SetInputAvailable(true);
                PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(replaceSession ? null : (GridPosition?)cell));
                Assert.That(bootstrap.IsBusy, Is.False);
                Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(20));
                Assert.That(CurrentBoard(), replaceSession ? Is.Not.SameAs(oldBoard) : Is.SameAs(oldBoard));

                // Same UI press/release cannot be replayed by another poll in the same input update.
                PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(replaceSession ? null : (GridPosition?)cell));
                QueueUiTap(touch, mouse, point, 2);
                InputSystem.Update();
                PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(replaceSession ? (GridPosition?)cell : null));
            }
            finally { input.TearDown(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartLevel_FromEnabledSession_DiscardsCurrentUiTapAndAcceptsNextPress(bool mouseInput)
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
                Pointer pointer = mouseInput ? (Pointer)mouse : touch;
                _ = pointer.press.wasPressedThisFrame;
                StartConfiguredLevel();
                Assert.That(controller.enabled, Is.True);
                var oldBoard = CurrentBoard();
                var cell = new GridPosition(0, 0);
                Vector2 point = inputCamera.WorldToScreenPoint(boardView.GetTileView(cell).transform.position);
                QueueUiTap(touch, mouse, point, 1);
                InputSystem.Update();
                Assert.That(pointer.press.wasPressedThisFrame && pointer.press.wasReleasedThisFrame, Is.True);

                // Unlike the flow-gate cases, input is still enabled when the UI asks for replacement.
                // Calling the button callback models EventSystem running before this controller's poll.
                hud.Restart.onClick.Invoke();
                Assert.That(controller.enabled, Is.True);
                Assert.That(CurrentBoard(), Is.Not.SameAs(oldBoard));
                PollInput();
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(bootstrap.IsBusy, Is.False);
                Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(20));
                AssertNoSelectionHighlights();

                QueueUiTap(touch, mouse, point, 2);
                InputSystem.Update(); PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(cell));
            }
            finally { input.TearDown(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlowGate_UiPressHeldPastEnable_DoesNotArmUntilAFreshPress(bool mouseInput)
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
                Pointer pointer = mouseInput ? (Pointer)mouse : touch;
                _ = pointer.press.wasPressedThisFrame;
                StartConfiguredLevel();
                bootstrap.SetInputAvailable(false);
                Vector2 point = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(0, 0)).transform.position);
                if (mouseInput) InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
                else InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Began, position = point });
                InputSystem.Update();
                bootstrap.SetInputAvailable(true);
                PollInput();
                InputSystem.Update();
                PollInput();
                Vector2 dragged = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(1, 0)).transform.position);
                if (mouseInput) InputSystem.QueueStateEvent(mouse, new MouseState { position = dragged, buttons = 1 });
                else InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Moved, position = dragged });
                InputSystem.Update(); PollInput();
                if (mouseInput) InputSystem.QueueStateEvent(mouse, new MouseState { position = dragged });
                else InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Ended, position = dragged });
                InputSystem.Update(); PollInput();
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(bootstrap.IsBusy, Is.False);
                Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(20));
                Assert.That(bootstrap.LevelProgress.Score, Is.Zero);
                QueueUiTap(touch, mouse, point, 2);
                InputSystem.Update(); PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
            }
            finally { input.TearDown(); }
        }

        [UnityTest]
        public IEnumerator Outcome_PlayingDoesNotReport_AndReplacingTheSessionReplacesTheCallback()
        {
            yield return StartBootstrap();
            int firstCalls = 0, secondCalls = 0;
            bootstrap.StartLevel(new LevelDefinition(3, TileType.Red, 1000, 42), _ => firstCalls++);
            yield return PlayValidMove();
            Assert.That(firstCalls, Is.Zero);
            bootstrap.StartLevel(new LevelDefinition(1, TileType.Red, 1000, 123), _ => secondCalls++);
            yield return PlayValidMove();
            Assert.That(firstCalls, Is.Zero);
            Assert.That(secondCalls, Is.EqualTo(1));
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Lost));
        }

        [UnityTest]
        public IEnumerator StartLevel_InvalidDefinitionKeepsTheCurrentSessionAndOutcomeCallback()
        {
            yield return StartBootstrap();
            int acceptedCalls = 0, rejectedCalls = 0;
            bootstrap.StartLevel(new LevelDefinition(1, TileType.Red, 1000, 42), _ => acceptedCalls++);
            var board = CurrentBoard();
            var progress = bootstrap.LevelProgress;
            Assert.Throws<ArgumentOutOfRangeException>(() => bootstrap.StartLevel(
                new LevelDefinition(0, TileType.Red, 12, 123), _ => rejectedCalls++));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(controller.enabled, Is.True);
            yield return PlayValidMove();
            Assert.That(acceptedCalls, Is.EqualTo(1));
            Assert.That(rejectedCalls, Is.Zero);
        }

        private static void QueueUiTap(Touchscreen touch, Mouse mouse, Vector2 point, int id)
        {
            if (mouse != null)
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            }
            else
            {
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = TouchPhase.Began, position = point });
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = TouchPhase.Ended, position = point });
            }
        }

        private void PollInput()
        {
            typeof(BoardInputController).GetMethod("Update", Fields).Invoke(controller, null);
        }
    }
}
