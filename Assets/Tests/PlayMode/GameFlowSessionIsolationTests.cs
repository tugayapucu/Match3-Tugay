#if UNITY_EDITOR
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Random = System.Random;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Match3.Presentation.Tests
{
    public partial class BoardBootstrapTests
    {
        [UnityTest]
        public IEnumerator SessionIsolation_RealWinThenNext_ReplacesBoardProgressViewsAndBothRandomStreams()
        {
            PrepareFlow();
            SetIsolationLevels();
            InitializeFlow(); flow.Play();
            yield return null;
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition.HasValue, Is.True);
            Click(new GridPosition(0, 0)); // Cancel before choosing the winning swap's origin.
            yield return WinIsolationFirstLevel();

            var oldBoard = CurrentBoard();
            var oldTiles = ReadTiles();
            var oldProgress = bootstrap.LevelProgress;
            var oldSnapshot = StartingSnapshot();
            var oldRunner = IsolationRunner();
            var oldRefill = IsolationRandom(oldRunner, "random");
            var oldTargeting = IsolationRandom(oldRunner, "targetingRandom");
            var oldViews = boardObject.GetComponentsInChildren<TileView>(true);
            // Deliberately advance the finished session's streams, so reuse cannot look like a fresh seed.
            oldRefill.Next(); oldTargeting.Next();

            hud.PlayAgain.onClick.Invoke();
            yield return null; // Render deactivates old views immediately; Destroy completes at frame end.
            AssertIsolatedStart(1, oldBoard, oldProgress, oldViews);
            Assert.That(StartingSnapshot(), Is.Not.SameAs(oldSnapshot));
            Assert.That(ReadTiles(StartingSnapshot()), Is.EqualTo(ReadTiles()));
            Assert.That(ReadTiles(oldBoard), Is.EqualTo(oldTiles));
            var runner = IsolationRunner();
            var refill = IsolationRandom(runner, "random");
            var targeting = IsolationRandom(runner, "targetingRandom");
            Assert.That(runner, Is.Not.SameAs(oldRunner));
            Assert.That(refill, Is.Not.SameAs(oldRefill));
            Assert.That(targeting, Is.Not.SameAs(oldTargeting));
            Assert.That(refill, Is.Not.SameAs(targeting));
            Assert.That(typeof(BoardBootstrap).GetField("refillRandom", Fields).GetValue(bootstrap), Is.SameAs(refill));
            // Inspect the live streams only at the end of this test; these reads advance them.
            Assert.That(IsolationDraws(refill), Is.EqualTo(IsolationDraws(new Random(flowCatalog[1].Seed))));
            Assert.That(IsolationDraws(targeting), Is.EqualTo(IsolationDraws(
                new Random(unchecked(flowCatalog[1].Seed ^ 0x5EED1234)))));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SessionIsolation_MenuReentry_DiscardsHeldTouchAndPartialProgress(
            [Values(false, true)] bool completedReplay)
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var touch = InputSystem.AddDevice<Touchscreen>();
                _ = touch.press.wasPressedThisFrame;
                PrepareFlow(completedReplay ? 2 : 1);
                SetIsolationLevels();
                InitializeFlow(); flow.Play();
                yield return null;
                if (completedReplay)
                {
                    yield return WinIsolationFirstLevel();
                    hud.PlayAgain.onClick.Invoke();
                    yield return null;
                }
                Assert.That(flow.CurrentLevelIndex, Is.EqualTo(1));
                yield return PlayValidMove();
                var oldBoard = CurrentBoard();
                var oldTiles = ReadTiles();
                var oldProgress = bootstrap.LevelProgress;
                var oldViews = boardObject.GetComponentsInChildren<TileView>(true);
                Assert.That(oldProgress.Score, Is.GreaterThan(0));
                Assert.That(oldProgress.MovesRemaining, Is.EqualTo(6));
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Click(new GridPosition(0, 0));
                Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
                Vector2 point = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(1, 0)).transform.position);
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Began, position = point });
                InputSystem.Update(); PollInput();
                Assert.That(touch.press.isPressed, Is.True);

                hud.GameplayMenu.onClick.Invoke();
                Assert.That(flow.PendingConfirmation, Is.EqualTo(ConfirmationIntent.Leave));
                prompt.Confirm.onClick.Invoke();
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
                Assert.That(CurrentBoard(), Is.SameAs(oldBoard), "The menu covers, but does not replace, the old session.");
                Assert.That(controller.enabled, Is.False);
                Assert.That(menu.Label.text, Is.EqualTo(completedReplay ? "Play Again" : "Continue"));
                menu.Play.onClick.Invoke();
                // The old touch lifts after re-entry. It must neither select nor attempt a swap on the new board.
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Ended, position = point });
                InputSystem.Update(); PollInput();
                yield return null;
                AssertIsolatedStart(completedReplay ? 0 : 1, oldBoard, oldProgress, oldViews);
                Assert.That(ReadTiles(oldBoard), Is.EqualTo(oldTiles));
                Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(completedReplay ? 2 : 1));

                Vector2 freshPoint = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(0, 0)).transform.position);
                QueueUiTap(touch, null, freshPoint, 2); InputSystem.Update(); PollInput();
                Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)), "A fresh press still works.");
                Assert.That(bootstrap.IsBusy, Is.False);
            }
            finally
            {
                if (flow != null) flow.enabled = false;
                input.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator SessionIsolation_RestartCurrentLevel_ReplaysWispResolutionWithFreshIndependentStreams()
        {
            PrepareFlow(1); SetIsolationLevels(); InitializeFlow(); flow.Play();
            yield return null;
            var initial = ReadTiles();
            var firstRunner = IsolationRunner();
            var firstRefill = IsolationRandom(firstRunner, "random");
            var firstTargeting = IsolationRandom(firstRunner, "targetingRandom");
            var firstSnapshot = StartingSnapshot();
            yield return PlayIsolationWisp();
            var oldBoard = CurrentBoard();
            var resolved = ReadTiles();
            var oldProgress = bootstrap.LevelProgress;
            var oldViews = boardObject.GetComponentsInChildren<TileView>(true);
            int score = oldProgress.Score, goal = oldProgress.GoalProgress;
            // Save the continuation of each used stream, then restart before any more gameplay.
            var refillAfterMove = IsolationDraws(firstRefill);
            var targetingAfterMove = IsolationDraws(firstTargeting);
            Click(new GridPosition(0, 0));
            hud.Restart.onClick.Invoke();
            Assert.That(flow.PendingConfirmation, Is.EqualTo(ConfirmationIntent.Restart));
            prompt.Confirm.onClick.Invoke();
            yield return null;
            AssertIsolatedStart(1, oldBoard, oldProgress, oldViews);
            Assert.That(ReadTiles(), Is.EqualTo(initial));
            Assert.That(StartingSnapshot(), Is.Not.SameAs(firstSnapshot));
            Assert.That(ReadTiles(StartingSnapshot()), Is.EqualTo(initial));
            var secondRunner = IsolationRunner();
            var secondRefill = IsolationRandom(secondRunner, "random");
            var secondTargeting = IsolationRandom(secondRunner, "targetingRandom");
            Assert.That(secondRunner, Is.Not.SameAs(firstRunner));
            Assert.That(secondRefill, Is.Not.SameAs(firstRefill));
            Assert.That(secondTargeting, Is.Not.SameAs(firstTargeting));
            Assert.That(secondRefill, Is.Not.SameAs(secondTargeting));
            yield return PlayIsolationWisp();
            Assert.That(ReadTiles(), Is.EqualTo(resolved), "Same level and move must replay targeting, refill and cascades.");
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(6));
            Assert.That(bootstrap.LevelProgress.Score, Is.EqualTo(score));
            Assert.That(bootstrap.LevelProgress.GoalProgress, Is.EqualTo(goal));
            Assert.That(ReadTiles(oldBoard), Is.EqualTo(resolved), "New gameplay must not mutate the replaced board.");
            Assert.That(IsolationDraws(secondRefill), Is.EqualTo(refillAfterMove));
            Assert.That(IsolationDraws(secondTargeting), Is.EqualTo(targetingAfterMove));
            Assert.That(PlayerPrefs.GetInt(flowKey), Is.EqualTo(1));
            AssertNoSelectionHighlights();
            AssertViewsMatchBoard();
        }

        private void SetIsolationLevels()
        {
            typeof(LevelCatalog).GetField("levels", Fields).SetValue(flowCatalog, new[]
            {
                new LevelDefinition(4, TileType.Red, 1, 42),
                new LevelDefinition(7, TileType.Blue, 1000, 73)
            });
        }

        private IEnumerator WinIsolationFirstLevel()
        {
            // Controlled special fixture, not a content/difficulty claim. The move uses the real runner and callback.
            var board = CurrentBoard();
            board.SetTile(new GridPosition(1, 1), TileType.LightBall);
            board.SetTile(new GridPosition(2, 1), TileType.Red);
            boardView.Render(board);
            yield return null;
            Click(new GridPosition(1, 1)); Click(new GridPosition(2, 1));
            Assert.That(bootstrap.IsBusy, Is.True);
            yield return WaitUntilUnlocked();
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(bootstrap.LevelProgress.Score, Is.GreaterThan(0));
            Assert.That(hud.OutcomePanel.activeSelf && hud.PlayAgain.interactable, Is.True);
        }

        private IEnumerator PlayIsolationWisp()
        {
            var board = CurrentBoard();
            board.SetTile(new GridPosition(2, 2), TileType.Propeller);
            board.SetTile(new GridPosition(3, 2), TileType.Red);
            boardView.Render(board);
            yield return null;
            Click(new GridPosition(2, 2)); Click(new GridPosition(3, 2));
            Assert.That(bootstrap.IsBusy, Is.True);
            yield return WaitUntilUnlocked();
            yield return null;
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(bootstrap.LevelProgress.Score, Is.GreaterThan(0));
            AssertViewsMatchBoard();
        }

        private void AssertIsolatedStart(int index, Board oldBoard, LevelProgress oldProgress, TileView[] oldViews)
        {
            var level = flowCatalog[index];
            var board = CurrentBoard();
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(index));
            Assert.That(board, Is.Not.SameAs(oldBoard));
            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(oldProgress));
            Assert.That(ReadTiles(), Is.EqualTo(ReadTiles(BoardGenerator.GeneratePlayable(Size, Size,
                new[] { TileType.Red, TileType.Green, TileType.Blue }, level.Seed))));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(level.StartingMoveCount));
            Assert.That(bootstrap.LevelProgress.GoalTileType, Is.EqualTo(level.GoalTileType));
            Assert.That(bootstrap.LevelProgress.GoalTarget, Is.EqualTo(level.GoalTarget));
            Assert.That(bootstrap.LevelProgress.Score, Is.Zero);
            Assert.That(bootstrap.LevelProgress.GoalProgress, Is.Zero);
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(hud.Moves.text, Is.EqualTo(level.StartingMoveCount.ToString()));
            Assert.That(hud.Goal.text, Is.EqualTo($"0/{level.GoalTarget}"));
            Assert.That(hud.Score.text, Is.EqualTo("0"));
            Assert.That(hud.OutcomePanel.activeSelf, Is.False);
            Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
            Assert.That(menu.Background.activeSelf || menu.Controls.activeSelf, Is.False);
            Assert.That(hud.Root.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(hud.Restart.interactable && hud.GameplayMenu.interactable, Is.True);
            Assert.That(hud.PlayAgain.interactable || hud.OutcomeMenu.interactable, Is.False);
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(bootstrap.IsBusy, Is.False);
            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.True);
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(Size * Size));
            foreach (var view in oldViews) Assert.That(view == null, Is.True, "Old Unity tile instances must be destroyed.");
            AssertNoSelectionHighlights();
            AssertViewsMatchBoard();
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var cell = new GridPosition(x, y);
                    Assert.That(boardView.GetTileView(cell).transform.position, Is.EqualTo(boardView.GetCellWorldPosition(cell)));
                }
        }

        private BoardResolutionRunner IsolationRunner() =>
            (BoardResolutionRunner)typeof(BoardInputController).GetField("resolution", Fields).GetValue(controller);

        private static Random IsolationRandom(BoardResolutionRunner runner, string field) =>
            (Random)typeof(BoardResolutionRunner).GetField(field, Fields).GetValue(runner);

        private static int[] IsolationDraws(Random random)
        {
            var values = new int[8];
            for (int i = 0; i < values.Length; i++) values[i] = random.Next();
            return values;
        }
    }
}
#endif
