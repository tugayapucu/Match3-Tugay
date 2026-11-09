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
        [UnityTest]
        public IEnumerator Confirmation_CancelPreservesTheSessionAndRestoresTheSelectedCue(
            [Values(ConfirmationIntent.Restart, ConfirmationIntent.Leave)] ConfirmationIntent intent)
        {
            PrepareFlow(); InitializeFlow(); flow.Play();
            yield return null;
            ReportValidMove(TileType.Green);
            var board = CurrentBoard();
            var progress = bootstrap.LevelProgress;
            var tiles = ReadTiles();
            var snapshot = StartingSnapshot();
            var random = typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap);
            var selected = new GridPosition(0, 0);
            Click(selected);
            var selectedView = boardView.GetTileView(selected);
            var cue = selectedView.transform.Find("SelectionHighlight").gameObject;
            Assert.That(cue.activeSelf, Is.True);
            Assert.That(intent == ConfirmationIntent.Restart ? flow.Restart() : flow.ReturnToMenu(), Is.True);

            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
            Assert.That(flow.PendingConfirmation, Is.EqualTo(intent));
            Assert.That(controller.enabled, Is.False);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(controller.SelectedPosition, Is.EqualTo(selected));
            Assert.That(cue.activeSelf, Is.False);
            Assert.That(hud.Restart.interactable || hud.GameplayMenu.interactable, Is.False);
            Assert.That(flow.Restart() || flow.ReturnToMenu() || flow.Play() || flow.Next() || flow.Retry(), Is.False);
            Assert.That(prompt.Title.text, Is.EqualTo(intent == ConfirmationIntent.Restart ? "Restart level?" : "Leave level?"));
            Assert.That(prompt.Background.activeInHierarchy && prompt.Card.activeInHierarchy, Is.True);
            Assert.That(boardObject.activeInHierarchy, Is.True);
            var point = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(1, 0)).transform.position);
            controller.HandlePointerDown(point);
            Assert.That(controller.SelectedPosition, Is.EqualTo(selected));
            Assert.That(bootstrap.IsBusy, Is.False);
            prompt.Cancel.onClick.Invoke();
            prompt.Cancel.onClick.Invoke();

            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(flow.PendingConfirmation, Is.Null);
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(ReadTiles(), Is.EqualTo(tiles));
            Assert.That(StartingSnapshot(), Is.SameAs(snapshot));
            Assert.That(typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap), Is.SameAs(random));
            Assert.That(progress.MovesRemaining, Is.EqualTo(1));
            Assert.That(progress.Score, Is.EqualTo(100));
            Assert.That(progress.GoalProgress, Is.Zero);
            Assert.That(hud.Moves.text, Is.EqualTo("1"));
            Assert.That(hud.Score.text, Is.EqualTo("100"));
            Assert.That(hud.Goal.text, Is.EqualTo("0/1"));
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(selected));
            Assert.That(cue.activeSelf, Is.True);
            Assert.That(boardView.GetTileView(selected), Is.SameAs(selectedView));
            Assert.That(hud.Restart.interactable && hud.GameplayMenu.interactable, Is.True);
            Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.Zero);
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator Confirmation_ConfirmExecutesOnlyItsIntentOnce_WithoutSaving(
            [Values(ConfirmationIntent.Restart, ConfirmationIntent.Leave)] ConfirmationIntent intent)
        {
            PrepareFlow(1); InitializeFlow(); flow.Play();
            yield return null;
            var board = CurrentBoard(); var progress = bootstrap.LevelProgress; var tiles = ReadTiles();
            var snapshot = StartingSnapshot();
            var random = typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap);
            Click(new GridPosition(0, 0));
            if (intent == ConfirmationIntent.Restart) hud.Restart.onClick.Invoke();
            else hud.GameplayMenu.onClick.Invoke();
            Assert.That(flow.PendingConfirmation, Is.EqualTo(intent));
            Assert.That(CurrentBoard(), Is.SameAs(board));
            Assert.That(flow.CancelConfirmation(), Is.True);
            if (intent == ConfirmationIntent.Restart) hud.Restart.onClick.Invoke();
            else hud.GameplayMenu.onClick.Invoke();
            prompt.Confirm.onClick.Invoke();
            var afterConfirm = CurrentBoard();
            prompt.Confirm.onClick.Invoke();
            Assert.That(CurrentBoard(), Is.SameAs(afterConfirm));
            Assert.That(flow.Confirm() || flow.CancelConfirmation(), Is.False);
            Assert.That(flow.PendingConfirmation, Is.Null);
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
            Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
            if (intent == ConfirmationIntent.Restart)
            {
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Assert.That(CurrentBoard(), Is.Not.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(progress));
                Assert.That(StartingSnapshot(), Is.Not.SameAs(snapshot));
                Assert.That(typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap), Is.Not.SameAs(random));
                Assert.That(ReadTiles(), Is.EqualTo(tiles));
                Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(flowCatalog[1].StartingMoveCount));
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(controller.enabled, Is.True);
                Assert.That(menu.Background.activeSelf, Is.False);
            }
            else
            {
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
                Assert.That(CurrentBoard(), Is.SameAs(board));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
                Assert.That(ReadTiles(), Is.EqualTo(tiles));
                Assert.That(controller.enabled, Is.False);
                Assert.That(menu.Background.activeInHierarchy && menu.Controls.activeInHierarchy, Is.True);
                Assert.That(menu.Label.text, Is.EqualTo("Continue"));
            }
            yield return null;
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator Confirmation_RequestsDuringAMoveAreIgnored_AndDoNotQueueAPrompt()
        {
            PrepareFlow(); InitializeFlow(); flow.Play();
            yield return null;
            Assert.That(TryFindMove(CurrentBoard(), true, out var first, out var second), Is.True);
            Click(first); Click(second);
            Assert.That(bootstrap.IsBusy, Is.True);
            Assert.That(hud.Restart.interactable || hud.GameplayMenu.interactable, Is.False);
            Assert.That(flow.Restart() || flow.ReturnToMenu() || flow.Confirm() || flow.CancelConfirmation(), Is.False);
            Assert.That(flow.PendingConfirmation, Is.Null);
            Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
            yield return WaitUntilUnlocked();
            Assert.That(flow.Mode, Is.Not.EqualTo(GameFlowMode.Confirmation));
            Assert.That(flow.PendingConfirmation, Is.Null);
        }

        [TestCase(false, ConfirmationIntent.Restart)]
        [TestCase(true, ConfirmationIntent.Restart)]
        [TestCase(false, ConfirmationIntent.Leave)]
        [TestCase(true, ConfirmationIntent.Leave)]
        public void Confirmation_CancelSameUpdateUiTapNeedsAFreshBoardPress(bool mouseInput, ConfirmationIntent intent)
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
                Pointer pointer = mouseInput ? (Pointer)mouse : touch;
                _ = pointer.press.wasPressedThisFrame;
                PrepareFlow(); InitializeFlow(); flow.Play();
                if (intent == ConfirmationIntent.Restart) flow.Restart(); else flow.ReturnToMenu();
                Vector2 point = inputCamera.WorldToScreenPoint(boardObject.transform.position);
                QueueUiTap(touch, mouse, point, 1); InputSystem.Update();
                prompt.Cancel.onClick.Invoke();
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
