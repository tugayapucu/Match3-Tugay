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
        public IEnumerator Picking_CellCornersAcceptTouch_WhileOutsideBoardDoesNot()
        {
            var cell = new GridPosition(0, 0);
            TileView view = boardView.GetTileView(cell);
            int movesBefore = bootstrap.LevelProgress.MovesRemaining;
            int touchId = 1;
            foreach (Vector3 offset in new[]
                { new Vector3(-0.51f, 0f, 0f), new Vector3(0f, -0.51f, 0f) })
            {
                Vector2 point = inputCamera.WorldToScreenPoint(view.transform.position + offset);
                QueuePress(touchId, point);
                yield return null;
                yield return null;
                Assert.That(controller.SelectedPosition, Is.Null, "Taps just outside the board must not pick a tile.");
                QueueRelease(touchId++, point);
                yield return null;
            }

            foreach (Vector2 corner in new[]
                { new Vector2(-0.49f, -0.49f), new Vector2(-0.49f, 0.49f),
                    new Vector2(0.49f, -0.49f), new Vector2(0.49f, 0.49f) })
            {
                // Inset 0.01 units from each cell edge, including transparent corners of the image.
                Vector3 world = view.transform.position + new Vector3(corner.x, corner.y, 0f);
                Vector2 point = inputCamera.WorldToScreenPoint(world);
                QueuePress(touchId, point);
                yield return null;
                yield return null;
                Assert.That(controller.SelectedPosition, Is.EqualTo(cell));
                Assert.That(controller.IsInputLocked, Is.False, "Repeated taps on one tile must not start a move.");
                QueueRelease(touchId++, point);
                yield return null;
            }

            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(movesBefore));
        }

        [UnityTest]
        public IEnumerator Picking_PointsEitherSideOfSharedBorders_SelectTheirOwnCell()
        {
            var offsets = new[]
            {
                new Vector3(0.49f, 0f, 0f), new Vector3(0.51f, 0f, 0f),
                new Vector3(0f, 0.49f, 0f), new Vector3(0f, 0.51f, 0f)
            };
            var expected = new[]
            {
                new GridPosition(0, 0), new GridPosition(1, 0),
                new GridPosition(0, 0), new GridPosition(0, 1)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                bootstrap.RestartLevel();
                TileView origin = boardView.GetTileView(new GridPosition(0, 0));
                Vector2 point = inputCamera.WorldToScreenPoint(origin.transform.position + offsets[i]);
                QueuePress(i + 1, point);
                yield return null;
                yield return null;
                Assert.That(controller.SelectedPosition, Is.EqualTo(expected[i]));
                Assert.That(controller.IsInputLocked, Is.False);
                QueueRelease(i + 1, point);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CameraFit_KeepsBoardBetweenControls_AndTilePickingWorks()
        {
            yield return null;
            AssertCameraFitAndPicking();
        }

        [UnityTest]
        public IEnumerator CameraFit_UpdatesAfterAnAsymmetricSafeAreaChange()
        {
            var layout = hud.GetComponentInParent<SafeAreaLayout>();
            var canvas = layout.GetComponentInParent<Canvas>();
            layout.enabled = false;
            layout.ApplyLayout(new Rect(30f, 20f, Screen.width - 70f, Screen.height - 90f),
                new Vector2Int(Screen.width, Screen.height), canvas.scaleFactor);

            yield return null;
            AssertCameraFitAndPicking();
        }

        [UnityTest]
        public IEnumerator CameraFit_NoRoom_WarnsOnceKeepsCamera_AndRecoversWhenLayoutChanges()
        {
            var layout = hud.GetComponentInParent<SafeAreaLayout>();
            float scale = layout.GetComponentInParent<Canvas>().scaleFactor;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            Vector3 previousPosition = inputCamera.transform.position;
            float previousSize = inputCamera.orthographicSize;
            layout.enabled = false;

            LogAssert.Expect(LogType.Warning,
                "Board camera fitting paused: the HUD and Restart leave no room. Keeping the current camera until the layout changes.");
            layout.ApplyLayout(new Rect(0f, 0f, Screen.width, 200f * scale), screenSize, scale);
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
                Assert.That(inputCamera.transform.position, Is.EqualTo(previousPosition));
                Assert.That(inputCamera.orthographicSize, Is.EqualTo(previousSize));
            }

            // Another unusable layout retries without warning again during the same no-room period.
            layout.ApplyLayout(new Rect(0f, 0f, Screen.width, 220f * scale), screenSize, scale);
            yield return null;
            yield return null;
            Assert.That(inputCamera.transform.position, Is.EqualTo(previousPosition));
            Assert.That(inputCamera.orthographicSize, Is.EqualTo(previousSize));
            LogAssert.NoUnexpectedReceived();

            // A changed usable layout must fit again rather than retaining a cached failed attempt.
            inputCamera.orthographicSize = 77f;
            inputCamera.transform.position = new Vector3(12f, 13f, previousPosition.z);
            layout.ApplyLayout(Screen.safeArea, screenSize, scale);
            yield return null;
            yield return null;
            AssertCameraFitAndPicking();
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
                "Camera fitting must leave Restart clear of board colliders.");
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

        private void AssertCameraFitAndPicking()
        {
            var layout = hud.GetComponentInParent<SafeAreaLayout>();
            var canvas = layout.GetComponentInParent<Canvas>();
            Rect safe = ScreenRect(layout.SafeAreaRoot);
            Rect top = ScreenRect(layout.TopBar);
            Rect restart = ScreenRect(layout.RestartButton);
            float margin = layout.SideMargin * canvas.scaleFactor;
            float gap = SafeAreaLayout.BoardControlGap * canvas.scaleFactor;

            Vector3 low = inputCamera.WorldToScreenPoint(new Vector3(-4.1f, -4.1f, 0f));
            Vector3 high = inputCamera.WorldToScreenPoint(new Vector3(4.1f, 4.1f, 0f));
            Assert.That(low.x, Is.GreaterThanOrEqualTo(safe.xMin + margin - 0.1f));
            Assert.That(high.x, Is.LessThanOrEqualTo(safe.xMax - margin + 0.1f));
            Assert.That(low.y, Is.GreaterThanOrEqualTo(restart.yMax + gap - 0.1f));
            Assert.That(high.y, Is.LessThanOrEqualTo(top.yMin - gap + 0.1f));
            Assert.That((low.x + high.x) * 0.5f, Is.EqualTo(safe.center.x).Within(0.1f));
            Assert.That((low.y + high.y) * 0.5f,
                Is.EqualTo((restart.yMax + top.yMin) * 0.5f).Within(0.1f));

            var restartCenter = new Vector2(restart.center.x, restart.center.y);
            Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(restartCenter)).collider, Is.Null);
            foreach (var cell in new[] { new GridPosition(0, 0), new GridPosition(7, 7) })
            {
                TileView view = boardView.GetTileView(cell);
                Vector2 point = inputCamera.WorldToScreenPoint(view.transform.position);
                Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(point)).collider,
                    Is.SameAs(view.GetComponent<Collider2D>()));
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
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
