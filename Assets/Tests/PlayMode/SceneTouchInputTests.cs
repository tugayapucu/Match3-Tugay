using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Match3.Presentation.Tests
{
    // Uses the real scene, colliders and UI module. All input goes through queued touchscreen events;
    // neither HandlePointerDown nor Button.onClick is called directly.
    public class SceneTouchInputTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private InputTestFixture input;
        private Touchscreen touchscreen;
        private Scene scene;
        private BoardBootstrap bootstrap;
        private BoardInputController controller;
        private BoardView boardView;
        private LevelHudView hud;
        private Camera inputCamera;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            input = new InputTestFixture();
            input.Setup();
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            bootstrap = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BoardBootstrap>(true)).Single();
            controller = Read<BoardInputController>(bootstrap, "inputController");
            boardView = Read<BoardView>(bootstrap, "boardView");
            hud = Read<LevelHudView>(bootstrap, "levelHud");
            inputCamera = Read<Camera>(bootstrap, "inputCamera");
            yield return null;
            Physics2D.SyncTransforms();
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            input?.TearDown();
        }

        [UnityTest]
        public IEnumerator Restart_TouchPreservesSelectionOnPress_ThenRestartsOnRelease()
        {
            var first = new GridPosition(0, 0);
            Vector2 tilePoint = inputCamera.WorldToScreenPoint(boardView.GetTileView(first).transform.position);
            QueuePress(1, tilePoint);
            yield return null;
            yield return null;
            Assert.That(controller.SelectedPosition, Is.EqualTo(first), "The scene must receive real touch input.");
            QueueRelease(1, tilePoint);
            yield return null;

            var before = bootstrap.LevelProgress;
            var button = Read<Button>(hud, "restartButton");
            Vector2 point = ButtonPoint(button);
            Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(point)).collider, Is.Null,
                "The current Restart layout keeps the touch outside board colliders.");
            QueuePress(2, point);
            yield return null;
            yield return null;

            Assert.That(bootstrap.LevelProgress, Is.SameAs(before), "UI clicks fire on release.");
            Assert.That(controller.SelectedPosition, Is.EqualTo(first), "UI press must not change board selection.");
            QueueRelease(2, point);
            yield return null;
            yield return null;
            AssertFreshLevel(before);
        }

        [UnityTest]
        public IEnumerator PlayAgain_TouchAfterWon_RestartsWithoutSelectingTheBoardUnderneath()
        {
            // Outcome calculation is already tested elsewhere. Supply the same resolved-move facts that
            // the controller reports to Bootstrap, then test the real touchscreen/UI restart path.
            var progress = bootstrap.LevelProgress;
            ReportMove(new ResolutionResult(Enumerable.Repeat(progress.GoalTileType, progress.GoalTarget)));
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Won));
            yield return VerifyPlayAgainTouch();
        }

        [UnityTest]
        public IEnumerator PlayAgain_TouchAfterLost_RestartsWithoutSelectingTheBoardUnderneath()
        {
            int moves = bootstrap.LevelProgress.MovesRemaining;
            for (int i = 0; i < moves; i++)
            {
                ReportMove(new ResolutionResult(Array.Empty<TileType>()));
            }
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Lost));
            yield return VerifyPlayAgainTouch();
        }

        private IEnumerator VerifyPlayAgainTouch()
        {
            var before = bootstrap.LevelProgress;
            Assert.That(controller.enabled, Is.False);
            yield return null;
            var button = Read<Button>(hud, "playAgainButton");
            Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
            Vector2 point = ButtonPoint(button);
            QueuePress(1, point);
            yield return null;
            yield return null;

            Assert.That(bootstrap.LevelProgress, Is.SameAs(before));
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            QueueRelease(1, point);
            yield return null;
            yield return null;
            AssertFreshLevel(before);
        }

        private void AssertFreshLevel(LevelProgress before)
        {
            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(before), "The real UI button must request restart.");
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(Read<int>(bootstrap, "startingMoveCount")));
            Assert.That(bootstrap.LevelProgress.Score, Is.Zero);
            Assert.That(bootstrap.LevelProgress.GoalProgress, Is.Zero);
            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(Read<GameObject>(hud, "outcomePanel").activeSelf, Is.False);
        }

        private Vector2 ButtonPoint(Button button)
        {
            var rect = (RectTransform)button.transform;
            // Prefer a point also occupied by a tile, when the outcome button overlays the board.
            // This exercises terminal-state protection rather than relying on an empty gap between tiles.
            foreach (var view in boardView.GetComponentsInChildren<TileView>())
            {
                Vector2 point = inputCamera.WorldToScreenPoint(view.transform.position);
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, point))
                {
                    return point;
                }
            }
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private void QueuePress(int id, Vector2 position)
        {
            // Let the next real player-loop update process both the controller and InputSystemUIInputModule.
            input.BeginTouch(id, position, queueEventOnly: true, screen: touchscreen);
        }

        private void QueueRelease(int id, Vector2 position)
        {
            input.EndTouch(id, position, queueEventOnly: true, screen: touchscreen);
        }

        private void ReportMove(ResolutionResult result)
        {
            typeof(BoardBootstrap).GetMethod("HandleValidMoveResolved", Fields)
                .Invoke(bootstrap, new object[] { result });
        }

        private static T Read<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, Fields).GetValue(target);
        }
    }
}
