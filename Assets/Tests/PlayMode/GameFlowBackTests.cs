#if UNITY_EDITOR
using System;
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Match3.Presentation.Tests
{
    public partial class BoardBootstrapTests
    {
        private void WithBackKeyboard(Action<InputTestFixture, Keyboard> exercise, int saved = 0)
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                PrepareFlow(saved); InitializeFlow();
                exercise(input, keyboard);
            }
            finally
            {
                // Stop the owned action before the fixture restores its separate Input System state.
                if (flow != null) flow.enabled = false;
                input.TearDown();
            }
        }

        [Test]
        public void Back_UninitializedOrMainMenu_DoesNotStartOrNavigateASession()
        {
            PrepareFlow();
            Assert.That(flow.HandleBack(), Is.False);
            InitializeFlow();
            Assert.That(flow.HandleBack(), Is.False, "Main Menu delegates native Android Back; it does not quit the Editor.");
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(bootstrap.LevelProgress, Is.Null);
            Assert.That(controller.enabled, Is.False);
        }

        [Test]
        public void Escape_SettledPlaying_RequestsLeave_AndHeldPressDoesNotCancelIt()
        {
            WithBackKeyboard((input, keyboard) =>
            {
                flow.Play();
                var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
                input.Press(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
                Assert.That(flow.PendingConfirmation, Is.EqualTo(ConfirmationIntent.Leave));
                Assert.That(controller.enabled, Is.False);
                InputSystem.Update(); InputSystem.Update();
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation), "A held Back is not a second request.");
                input.Release(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
                input.Press(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(controller.enabled, Is.True);
            });
        }

        [Test]
        public void Escape_PressAndReleaseInOneInputUpdate_HandlesTheFirstQuickBackOnce()
        {
            WithBackKeyboard((input, keyboard) =>
            {
                flow.Play();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
                Assert.That(flow.PendingConfirmation, Is.EqualTo(ConfirmationIntent.Leave));
                Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(2));
            });
        }

        [TestCase(ConfirmationIntent.Restart)]
        [TestCase(ConfirmationIntent.Leave)]
        public void Escape_OnEitherConfirmation_CancelsAndRestoresSelection(ConfirmationIntent intent)
        {
            WithBackKeyboard((input, keyboard) =>
            {
                flow.Play();
                var selected = new GridPosition(0, 0);
                Click(selected);
                var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
                var view = boardView.GetTileView(selected);
                if (intent == ConfirmationIntent.Restart) flow.Restart(); else flow.ReturnToMenu();
                input.Press(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Assert.That(flow.PendingConfirmation, Is.Null);
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(controller.SelectedPosition, Is.EqualTo(selected));
                Assert.That(view.transform.Find("SelectionHighlight").gameObject.activeSelf, Is.True);
                Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
                Assert.That(controller.enabled, Is.True);
                Assert.That(PlayerPrefs.GetInt(flowKey), Is.Zero);
            });
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Escape_OnSettledLossWinOrFinalWin_ReturnsToMenuWithoutAnotherSave(int ending)
        {
            WithBackKeyboard((input, keyboard) =>
            {
                flow.Play();
                if (ending == 0) { ReportValidMove(TileType.Green); ReportValidMove(TileType.Green); }
                else ReportValidMove(ending == 2 ? TileType.Blue : TileType.Red);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
                int saved = PlayerPrefs.GetInt(flowKey);
                var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
                input.Press(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(saved));
                Assert.That(controller.enabled, Is.False);
                Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
                Assert.That(menu.Background.activeInHierarchy && menu.Controls.activeInHierarchy, Is.True);
            }, ending == 2 ? 1 : 0);
        }

        [Test]
        public void Escape_DisablingFlowStopsItsListener_AndReenablingListensAgain()
        {
            WithBackKeyboard((input, keyboard) =>
            {
                flow.Play();
                flow.enabled = false;
                input.Press(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                input.Release(keyboard.escapeKey);
                flow.enabled = true;
                input.Press(keyboard.escapeKey);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
                Assert.That(flow.PendingConfirmation, Is.EqualTo(ConfirmationIntent.Leave));
            });
        }

        [UnityTest]
        public IEnumerator Escape_DuringResolution_IsDiscardedEvenIfHeldPastCleanup()
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                PrepareFlow(); InitializeFlow(); flow.Play();
                yield return null;
                var board = CurrentBoard(); var progress = bootstrap.LevelProgress;
                Assert.That(TryFindMove(board, true, out var first, out var second), Is.True);
                Click(first); Click(second);
                Assert.That(bootstrap.IsBusy, Is.True);
                input.Press(keyboard.escapeKey);
                // In a UnityTest, the fixture queues input; the player loop processes it next frame.
                yield return null;
                Assert.That(bootstrap.IsBusy, Is.True);
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Assert.That(flow.PendingConfirmation, Is.Null);
                yield return WaitUntilUnlocked();
                yield return null;
                Assert.That(flow.Mode, Is.EqualTo(progress.Status == LevelStatus.Playing ? GameFlowMode.Playing : GameFlowMode.Outcome));
                Assert.That(flow.PendingConfirmation, Is.Null, "No Back request is deferred past cleanup.");
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(progress.MovesRemaining, Is.EqualTo(1));
                Assert.That(controller.IsInputLocked, Is.False);
                yield return null;
                AssertViewsMatchBoard();
                input.Release(keyboard.escapeKey);
                yield return null;
                input.Press(keyboard.escapeKey);
                yield return null;
                Assert.That(flow.Mode, Is.EqualTo(progress.Status == LevelStatus.Playing ? GameFlowMode.Confirmation : GameFlowMode.MainMenu));
            }
            finally
            {
                if (flow != null) flow.enabled = false;
                input.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator Back_WhileOutcomeIsVisibleButLocked_IsIgnoredUntilCleanup()
        {
            PrepareFlow(); InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard();
            board.SetTile(new GridPosition(1, 1), TileType.LightBall);
            board.SetTile(new GridPosition(2, 1), TileType.Red);
            boardView.Render(board);
            yield return null;
            var callback = typeof(BoardBootstrap).GetField("onLevelEnded", Fields);
            var original = (Action<LevelStatus>)callback.GetValue(bootstrap);
            bool observedLockedOutcome = false;
            callback.SetValue(bootstrap, new Action<LevelStatus>(status =>
            {
                original(status);
                observedLockedOutcome = bootstrap.IsBusy && flow.Mode == GameFlowMode.Outcome
                    && !flow.HandleBack() && flow.Mode == GameFlowMode.Outcome;
            }));
            Click(new GridPosition(1, 1)); Click(new GridPosition(2, 1));
            yield return WaitUntilUnlocked();
            Assert.That(observedLockedOutcome, Is.True);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(flow.HandleBack(), Is.True);
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
        }
    }
}
#endif
