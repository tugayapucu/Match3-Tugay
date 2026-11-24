using System;
using System.Collections;
using System.Collections.Generic;
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
        private GameFlowController flow;
        private string progressKey;
        private List<string> openedUrls;
        private UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoaded;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            input = new InputTestFixture();
            input.Setup();
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            progressKey = "Match3.Tests.SceneFlow." + Guid.NewGuid().ToString("N");
            openedUrls = new List<string>();
            // sceneLoaded runs before Start: inject an isolated store before production startup can save.
            sceneLoaded = (loaded, mode) =>
            {
                if (loaded.name != "SampleScene") return;
                flow = loaded.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameFlowController>(true)).Single();
                flow.Initialize(new ProgressionStore(Read<LevelCatalog>(flow, "catalog").Count, progressKey),
                    url => openedUrls.Add(url));
            };
            SceneManager.sceneLoaded += sceneLoaded;
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            SceneManager.sceneLoaded -= sceneLoaded;
            scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            bootstrap = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BoardBootstrap>(true)).Single();
            controller = Read<BoardInputController>(bootstrap, "inputController");
            boardView = Read<BoardView>(bootstrap, "boardView");
            hud = Read<LevelHudView>(bootstrap, "levelHud");
            inputCamera = Read<Camera>(bootstrap, "inputCamera");
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(bootstrap.LevelProgress, Is.Null, "Bootstrap must not autostart a session.");
            Assert.That(controller.enabled, Is.False);
            Assert.That(hud.GetComponentInParent<SafeAreaLayout>().TopBar.gameObject.activeInHierarchy, Is.True,
                "Hidden flow HUD must retain active layout objects for camera framing.");
            Assert.That(flow.Play(), Is.True);
            yield return null;
            Physics2D.SyncTransforms();
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= sceneLoaded;
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            input?.TearDown();
            if (progressKey != null) { PlayerPrefs.DeleteKey(progressKey); PlayerPrefs.Save(); }
        }

        [UnityTest]
        public IEnumerator MainMenu_CoversScreen_BlocksBoardTouches_AndPlayStartsFreshWithoutLeakingATap()
        {
            var before = bootstrap.LevelProgress;
            Assert.That(flow.ReturnToMenu(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            var menu = Read<MainMenuView>(flow, "mainMenu");
            var controls = Read<GameObject>(menu, "controls");
            var button = Read<Button>(menu, "playButton");
            yield return null;
            Rect background = ScreenRect((RectTransform)menu.transform);
            Assert.That(background.xMin, Is.EqualTo(0f).Within(0.1f));
            Assert.That(background.yMin, Is.EqualTo(0f).Within(0.1f));
            Assert.That(background.width, Is.EqualTo(Screen.width).Within(0.1f));
            Assert.That(background.height, Is.EqualTo(Screen.height).Within(0.1f));
            Assert.That(menu.GetComponent<Image>().color.a, Is.EqualTo(1f));
            var safe = hud.GetComponentInParent<SafeAreaLayout>();
            Rect safeRect = ScreenRect(safe.SafeAreaRoot);
            Rect buttonRect = ScreenRect((RectTransform)button.transform);
            Assert.That(safeRect.Contains(buttonRect.min) && safeRect.Contains(buttonRect.max), Is.True);
            Assert.That(controls.activeInHierarchy, Is.True);
            Assert.That(controller.gameObject.activeInHierarchy, Is.True);

            // A press on a covered tile outside the menu button must not select or spend a move.
            Vector2 coveredTile = inputCamera.WorldToScreenPoint(boardView.GetTileView(new GridPosition(0, 0)).transform.position);
            Assert.That(buttonRect.Contains(coveredTile), Is.False);
            QueuePress(10, coveredTile); yield return null; yield return null;
            QueueRelease(10, coveredTile); yield return null; yield return null;
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(bootstrap.LevelProgress, Is.SameAs(before));
            Assert.That(before.MovesRemaining, Is.EqualTo(20));

            Vector2 buttonPoint = ButtonPoint(button);
            QueuePress(11, buttonPoint); yield return null; yield return null;
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            QueueRelease(11, buttonPoint); yield return null; yield return null;
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(before));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(20));
            Assert.That(controller.SelectedPosition, Is.Null, "The menu release must not select the board behind it.");
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(menu.gameObject.activeSelf || controls.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator MainMenu_Title_ReadsGalaxplode_OnOneLine_WithoutOverflow()
        {
            Assert.That(flow.ReturnToMenu(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            var menu = Read<MainMenuView>(flow, "mainMenu");
            var controls = Read<GameObject>(menu, "controls");
            yield return null;
            Assert.That(controls.activeInHierarchy, Is.True);
            Transform titleTransform = controls.transform.Find("Title");
            Assert.That(titleTransform, Is.Not.Null);
            var title = titleTransform.GetComponent<TMPro.TMP_Text>();

            // Measure the laid-out result, not a stale mesh from before the menu was shown.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)controls.transform);
            title.ForceMeshUpdate();

            Assert.That(title.text, Is.EqualTo("GALAXPLODE"));
            Assert.That(title.isTextOverflowing, Is.False);
            Assert.That(title.textInfo.lineCount, Is.EqualTo(1), "The longer title must not wrap.");
            Assert.That(title.textInfo.characterCount, Is.EqualTo("GALAXPLODE".Length));
            Assert.That(title.textBounds.size.x, Is.LessThanOrEqualTo(title.rectTransform.rect.width + 0.5f));
            Rect safeRect = ScreenRect(hud.GetComponentInParent<SafeAreaLayout>().SafeAreaRoot);
            Rect titleRect = ScreenRect(title.rectTransform);
            Assert.That(safeRect.Contains(titleRect.min) && safeRect.Contains(titleRect.max), Is.True);
        }

        [UnityTest]
        public IEnumerator MainMenu_PrivacyTouch_OpensOnceOnRelease_KeepsBoardGated_AndPlayStillWorks(
            [Values(false, true)] bool asymmetricSafeArea)
        {
            Assert.That(flow.ReturnToMenu(), Is.True);
            Assert.That(flow.Confirm(), Is.True);
            var menu = Read<MainMenuView>(flow, "mainMenu");
            var privacy = Read<Button>(menu, "privacyButton");
            var play = Read<Button>(menu, "playButton");
            var safe = hud.GetComponentInParent<SafeAreaLayout>();
            if (asymmetricSafeArea)
            {
                safe.enabled = false;
                safe.ApplyLayout(new Rect(120f, 90f, Screen.width - 200f, Screen.height - 190f),
                    new Vector2Int(Screen.width, Screen.height), safe.GetComponentInParent<Canvas>().scaleFactor);
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            Rect safeRect = ScreenRect(safe.SafeAreaRoot);
            Rect privacyRect = ScreenRect((RectTransform)privacy.transform);
            Rect playRect = ScreenRect((RectTransform)play.transform);
            Assert.That(safeRect.Contains(privacyRect.min) && safeRect.Contains(privacyRect.max), Is.True);
            Assert.That(safeRect.Contains(playRect.min) && safeRect.Contains(playRect.max), Is.True);
            Assert.That(privacyRect.Overlaps(playRect), Is.False);
            Assert.That(privacyRect.yMax, Is.LessThan(playRect.yMin), "Privacy is secondary, below Play.");
            float scale = safe.GetComponentInParent<Canvas>().scaleFactor;
            Assert.That(privacyRect.height / scale, Is.GreaterThanOrEqualTo(128f - 0.1f));
            Assert.That(privacy.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("Privacy Policy"));
            Assert.That(privacy.gameObject.activeInHierarchy && privacy.interactable, Is.True);
            var progress = bootstrap.LevelProgress;
            var types = boardView.GetComponentsInChildren<TileView>().Select(tile => tile.Type).ToArray();
            string label = Read<TMPro.TMP_Text>(menu, "playLabel").text;
            bool hadSave = PlayerPrefs.HasKey(progressKey);

            Vector2 point = ButtonPoint(privacy);
            QueuePress(20, point); yield return null; yield return null;
            Assert.That(openedUrls, Is.Empty, "The URL opens on release, not press.");
            QueueRelease(20, point); yield return null; yield return null;

            Assert.That(openedUrls, Is.EqualTo(new[] { GameFlowController.PrivacyPolicyUrl }));
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(boardView.GetComponentsInChildren<TileView>().Select(tile => tile.Type), Is.EqualTo(types));
            Assert.That(progress.MovesRemaining, Is.EqualTo(20));
            Assert.That(progress.Score, Is.Zero);
            Assert.That(PlayerPrefs.HasKey(progressKey), Is.EqualTo(hadSave));
            Assert.That(controller.enabled, Is.False);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(Read<TMPro.TMP_Text>(menu, "playLabel").text, Is.EqualTo(label));
            Assert.That(menu.gameObject.activeInHierarchy, Is.True);

            // A fresh press after the browser request follows the unchanged primary-action path.
            point = ButtonPoint(play);
            QueuePress(21, point); yield return null; yield return null;
            QueueRelease(21, point); yield return null; yield return null;
            AssertFreshLevel(progress);
            Assert.That(openedUrls.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Start_InitialDefinitionPreservesLevel1ConfigurationAndBoard()
        {
#if UNITY_EDITOR
            Assert.That(Read<int>(bootstrap, "width"), Is.EqualTo(BoardBootstrapTests.CatalogBoardWidth),
                "The catalog session fixture must match production board dimensions.");
            Assert.That(Read<int>(bootstrap, "height"), Is.EqualTo(BoardBootstrapTests.CatalogBoardHeight));
            Assert.That(Read<TileType[]>(bootstrap, "palette"), Is.EqualTo(BoardBootstrapTests.CatalogPalette),
                "Palette order affects seeded generation; catalog tests must use the production order.");
#endif
            var definition = Read<LevelCatalog>(flow, "catalog")[0];
            Assert.That(definition.Seed, Is.EqualTo(42));
            Assert.That(definition.StartingMoveCount, Is.EqualTo(20));
            Assert.That(definition.GoalTileType, Is.EqualTo(TileType.Red));
            Assert.That(definition.GoalTarget, Is.EqualTo(25));
            Assert.That(bootstrap.LevelProgress.MovesRemaining, Is.EqualTo(20));
            Assert.That(bootstrap.LevelProgress.GoalTileType, Is.EqualTo(TileType.Red));
            Assert.That(bootstrap.LevelProgress.GoalTarget, Is.EqualTo(25));
            var expected = BoardGenerator.GeneratePlayable(8, 8, new[]
            {
                TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange
            }, 42);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    var cell = new GridPosition(x, y);
                    Assert.That(boardView.GetTileView(cell).Type, Is.EqualTo(expected.GetTile(cell)), cell.ToString());
                }
            yield return null;
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
                GridPosition? previousSelection = controller.SelectedPosition;
                QueuePress(touchId, point);
                yield return null;
                yield return null;
                Assert.That(controller.SelectedPosition, Is.EqualTo(previousSelection), "Press alone does not commit a tap.");
                Assert.That(controller.IsInputLocked, Is.False, "Repeated taps on one tile must not start a move.");
                QueueRelease(touchId++, point);
                yield return null;
                yield return null;
                GridPosition? expectedAfterTap = previousSelection.HasValue ? (GridPosition?)null : cell;
                Assert.That(controller.SelectedPosition, Is.EqualTo(expectedAfterTap),
                    "Each corner tap must pick this cell: select it, then cancel it on the next tap.");
                Assert.That(view.transform.Find("SelectionHighlight").gameObject.activeInHierarchy,
                    Is.EqualTo(expectedAfterTap.HasValue));
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
                Assert.That(flow.Restart(), Is.True);
                Assert.That(flow.Confirm(), Is.True);
                TileView origin = boardView.GetTileView(new GridPosition(0, 0));
                Vector2 point = inputCamera.WorldToScreenPoint(origin.transform.position + offsets[i]);
                QueuePress(i + 1, point);
                yield return null;
                yield return null;
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(controller.IsInputLocked, Is.False);
                QueueRelease(i + 1, point);
                yield return null;
                yield return null;
                Assert.That(controller.SelectedPosition, Is.EqualTo(expected[i]));
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
                "Board camera fitting paused: the HUD and bottom controls leave no room. Keeping the current camera until the layout changes.");
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
        public IEnumerator GameplayConfirmation_TouchOpensOnRelease_ThenCancelsOrConfirmsWithoutLeaking(
            [Values(ConfirmationIntent.Restart, ConfirmationIntent.Leave)] ConfirmationIntent intent,
            [Values(false, true)] bool confirm)
        {
            var first = new GridPosition(0, 0);
            Vector2 tilePoint = inputCamera.WorldToScreenPoint(boardView.GetTileView(first).transform.position);
            QueuePress(1, tilePoint);
            yield return null;
            yield return null;
            Assert.That(controller.SelectedPosition, Is.Null, "Press must not commit selection.");
            QueueRelease(1, tilePoint);
            yield return null;
            yield return null;
            Assert.That(controller.SelectedPosition, Is.EqualTo(first), "The scene must receive the completed real touch tap.");

            var before = bootstrap.LevelProgress;
            var button = Read<Button>(hud, intent == ConfirmationIntent.Restart ? "restartButton" : "gameplayMenuButton");
            Vector2 point = ButtonPoint(button);
            Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(point)).collider, Is.Null,
                "Camera fitting must leave both gameplay controls clear of board colliders.");
            QueuePress(2, point);
            yield return null;
            yield return null;

            Assert.That(bootstrap.LevelProgress, Is.SameAs(before), "UI clicks fire on release.");
            Assert.That(controller.SelectedPosition, Is.EqualTo(first), "UI press must not change board selection.");
            QueueRelease(2, point);
            yield return null;
            yield return null;
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation));
            Assert.That(flow.PendingConfirmation, Is.EqualTo(intent));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(before));
            Assert.That(controller.enabled, Is.False);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(boardView.GetTileView(first).transform.Find("SelectionHighlight").gameObject.activeSelf, Is.False);
            Assert.That(Read<Button>(hud, "restartButton").interactable
                || Read<Button>(hud, "gameplayMenuButton").interactable, Is.False);
            var prompt = Read<ConfirmationView>(flow, "confirmation");
            Rect dim = ScreenRect((RectTransform)prompt.transform);
            Assert.That(dim.min, Is.EqualTo(Vector2.zero));
            Assert.That(dim.width, Is.EqualTo(Screen.width).Within(0.1f));
            Assert.That(dim.height, Is.EqualTo(Screen.height).Within(0.1f));
            var card = Read<GameObject>(prompt, "card");
            var safe = hud.GetComponentInParent<SafeAreaLayout>();
            Rect safeRect = ScreenRect(safe.SafeAreaRoot);
            Rect cardRect = ScreenRect((RectTransform)card.transform);
            Assert.That(safeRect.Contains(cardRect.min) && safeRect.Contains(cardRect.max), Is.True);

            // The raw board picker must stay gated even where the full-screen dimmer covers a collider.
            QueuePress(3, tilePoint); yield return null; yield return null;
            QueueRelease(3, tilePoint); yield return null; yield return null;
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(before));
            var decision = Read<Button>(prompt, confirm ? "confirmButton" : "cancelButton");
            Vector2 decisionPoint = ButtonPoint(decision);
            QueuePress(4, decisionPoint); yield return null; yield return null;
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Confirmation), "A decision is also made on release.");
            QueueRelease(4, decisionPoint); yield return null; yield return null;
            Assert.That(prompt.gameObject.activeSelf || card.activeSelf, Is.False);
            Assert.That(flow.PendingConfirmation, Is.Null);
            if (!confirm)
            {
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Playing));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(before));
                Assert.That(controller.enabled, Is.True);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(boardView.GetTileView(first).transform.Find("SelectionHighlight").gameObject.activeSelf, Is.True);
                Assert.That(controller.IsInputLocked, Is.False);
            }
            else if (intent == ConfirmationIntent.Restart) AssertFreshLevel(before);
            else
            {
                Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
                Assert.That(bootstrap.LevelProgress, Is.SameAs(before));
                Assert.That(controller.enabled, Is.False);
                Assert.That(Read<MainMenuView>(flow, "mainMenu").gameObject.activeInHierarchy, Is.True);
            }
        }

        private void AssertCameraFitAndPicking()
        {
            var layout = hud.GetComponentInParent<SafeAreaLayout>();
            var canvas = layout.GetComponentInParent<Canvas>();
            Rect safe = ScreenRect(layout.SafeAreaRoot);
            Rect top = ScreenRect(layout.TopBar);
            Rect restart = ScreenRect(layout.BottomControls);
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
            foreach (string field in new[] { "restartButton", "gameplayMenuButton" })
            {
                Rect control = ScreenRect((RectTransform)Read<Button>(hud, field).transform);
                Assert.That(control.xMin, Is.GreaterThanOrEqualTo(restart.xMin - 0.1f));
                Assert.That(control.xMax, Is.LessThanOrEqualTo(restart.xMax + 0.1f));
                Assert.That(control.yMax, Is.LessThanOrEqualTo(restart.yMax + 0.1f));
                Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(control.center)).collider, Is.Null);
            }
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
        public IEnumerator OutcomeAction_TouchAfterWon_StartsNextWithoutSelectingTheBoardUnderneath()
        {
            // Outcome calculation is already tested elsewhere. Supply the same resolved-move facts that
            // the controller reports to Bootstrap, then test the real touchscreen/UI restart path.
            var progress = bootstrap.LevelProgress;
            ReportMove(new ResolutionResult(Enumerable.Repeat(progress.GoalTileType, progress.GoalTarget)));
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Won));
            yield return VerifyOutcomeTouch(1);
        }

        [UnityTest]
        public IEnumerator Retry_TouchAfterLost_RestartsWithoutSelectingTheBoardUnderneath()
        {
            int moves = bootstrap.LevelProgress.MovesRemaining;
            for (int i = 0; i < moves; i++)
            {
                ReportMove(new ResolutionResult(Array.Empty<TileType>()));
            }
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Lost));
            yield return VerifyOutcomeTouch(0);
        }

        [UnityTest]
        public IEnumerator FinalOutcome_PlayAgainTouch_StartsLevelOneAndKeepsCompletedProgress()
        {
            int count = Read<LevelCatalog>(flow, "catalog").Count;
            for (int index = 0; index < count; index++)
            {
                var progress = bootstrap.LevelProgress;
                ReportMove(new ResolutionResult(Enumerable.Repeat(progress.GoalTileType, progress.GoalTarget)));
                if (index + 1 < count) Assert.That(flow.Next(), Is.True);
            }
            yield return null;
            Assert.That(Read<TMPro.TMP_Text>(hud, "outcomeTitleText").text, Is.EqualTo("All Levels Complete"));
            Assert.That(Read<TMPro.TMP_Text>(hud, "outcomeActionText").text, Is.EqualTo("Play Again"));
            Assert.That(flow.Next(), Is.False);
            yield return VerifyOutcomeTouch(0);
            Assert.That(PlayerPrefs.GetInt(progressKey), Is.EqualTo(count));
        }

        [UnityTest]
        public IEnumerator OutcomeMenuTouch_ReturnsToMenuWithoutLeakingInputOrChangingTheSession(
            [Values(false, true)] bool won)
        {
            var progress = bootstrap.LevelProgress;
            if (won) ReportMove(new ResolutionResult(Enumerable.Repeat(progress.GoalTileType, progress.GoalTarget)));
            else
                for (int i = 0, moves = progress.MovesRemaining; i < moves; i++)
                    ReportMove(new ResolutionResult(Array.Empty<TileType>()));
            yield return null;
            var button = Read<Button>(hud, "outcomeMenuButton");
            var primary = Read<Button>(hud, "playAgainButton");
            var safe = hud.GetComponentInParent<SafeAreaLayout>();
            Rect safeRect = ScreenRect(safe.SafeAreaRoot);
            Rect buttonRect = ScreenRect((RectTransform)button.transform);
            Assert.That(safeRect.Contains(buttonRect.min) && safeRect.Contains(buttonRect.max), Is.True);
            Assert.That(buttonRect.Overlaps(ScreenRect((RectTransform)primary.transform)), Is.False);
            Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True);
            Assert.That(Read<Button>(hud, "restartButton").interactable, Is.False);
            int saved = PlayerPrefs.GetInt(progressKey, 0);
            Vector2 point = ButtonPoint(button);
            QueuePress(1, point); yield return null; yield return null;
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome), "Menu acts on release.");
            QueueRelease(1, point); yield return null; yield return null;

            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));
            Assert.That(bootstrap.LevelProgress, Is.SameAs(progress));
            Assert.That(PlayerPrefs.GetInt(progressKey, 0), Is.EqualTo(saved));
            Assert.That(controller.enabled, Is.False);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(controller.SelectedPosition, Is.Null);
            var menu = Read<MainMenuView>(flow, "mainMenu");
            Assert.That(menu.gameObject.activeInHierarchy, Is.True);
            Assert.That(Read<TMPro.TMP_Text>(menu, "playLabel").text, Is.EqualTo(won ? "Continue" : "Play"));
            Assert.That(controller.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator OutcomeDimmer_CoversTheScreen_WhileTheCardAndButtonsStayInSafeArea(
            [Values("Won", "Lost", "AllLevelsComplete")] string outcome, [Values(false, true)] bool asymmetricSafeArea)
        {
            string expectedTitle = ReachOutcome(outcome);
            var safe = hud.GetComponentInParent<SafeAreaLayout>();
            if (asymmetricSafeArea)
            {
                safe.enabled = false;
                safe.ApplyLayout(new Rect(120f, 90f, Screen.width - 200f, Screen.height - 190f),
                    new Vector2Int(Screen.width, Screen.height), safe.GetComponentInParent<Canvas>().scaleFactor);
            }
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.Outcome));
            Assert.That(Read<TMPro.TMP_Text>(hud, "outcomeTitleText").text, Is.EqualTo(expectedTitle));
            var dimmer = Read<GameObject>(hud, "outcomeDimmer");
            Assert.That(dimmer.activeInHierarchy, Is.True);
            Rect dim = ScreenRect((RectTransform)dimmer.transform);
            Assert.That(dim.xMin, Is.EqualTo(0f).Within(0.1f));
            Assert.That(dim.yMin, Is.EqualTo(0f).Within(0.1f));
            Assert.That(dim.width, Is.EqualTo(Screen.width).Within(0.1f));
            Assert.That(dim.height, Is.EqualTo(Screen.height).Within(0.1f));

            Rect safeRect = ScreenRect(safe.SafeAreaRoot);
            if (asymmetricSafeArea) Assert.That(safeRect.width, Is.LessThan(Screen.width), "The inset must be in effect.");
            var card = Read<GameObject>(hud, "outcomePanel").transform.Find("Card");
            Rect cardRect = ScreenRect((RectTransform)card);
            Assert.That(safeRect.Contains(cardRect.min) && safeRect.Contains(cardRect.max), Is.True);
            foreach (string field in new[] { "playAgainButton", "outcomeMenuButton" })
            {
                Rect button = ScreenRect((RectTransform)Read<Button>(hud, field).transform);
                Assert.That(safeRect.Contains(button.min) && safeRect.Contains(button.max), Is.True, field);
            }
            // The card's own sorting Canvas draws it, and takes its touches, above the later full-screen dimmer.
            var cardCanvas = card.GetComponent<Canvas>();
            Assert.That(cardCanvas.overrideSorting, Is.True);
            Assert.That(cardCanvas.sortingOrder, Is.GreaterThan(dimmer.GetComponentInParent<Canvas>().sortingOrder));
            Assert.That(card.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            foreach (string field in new[] { "playAgainButton", "outcomeMenuButton" })
            {
                var button = Read<Button>(hud, field);
                var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    { position = CenterOf(button) };
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty, field);
                Assert.That(hits[0].gameObject.transform.IsChildOf(button.transform), Is.True,
                    field + ": the top UI hit is the card's button, not the dimmer.");
            }
        }

        [UnityTest]
        public IEnumerator OutcomeThenMenu_HiddenCardAndDimmerDoNotInterceptMenuTouches([Values(false, true)] bool won)
        {
            ReachOutcome(won ? "Won" : "Lost");
            yield return null;
            Assert.That(flow.ReturnToMenu(), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var menu = Read<MainMenuView>(flow, "mainMenu");
            var play = Read<Button>(menu, "playButton");
            var privacy = Read<Button>(menu, "privacyButton");
            var card = Read<GameObject>(hud, "outcomePanel").transform.Find("Card");

            Assert.That(Read<GameObject>(hud, "outcomeDimmer").activeInHierarchy, Is.False, "The menu is not dimmed.");
            Assert.That(card.gameObject.activeInHierarchy, Is.False, "The outcome card is not drawn on the menu.");
            // Precondition: the card's area overlaps the menu buttons, so a stray card raycast would be caught.
            Assert.That(ScreenRect((RectTransform)card).Overlaps(ScreenRect((RectTransform)play.transform)), Is.True);
            // The same UI raycast the input module uses must not reach anything on the hidden card,
            // including where its own buttons are (a tap there would otherwise be swallowed silently).
            foreach (string field in new[] { "playAgainButton", "outcomeMenuButton" })
            {
                var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    { position = CenterOf(Read<Button>(hud, field)) };
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Any(hit => hit.gameObject.transform.IsChildOf(card)), Is.False, field);
            }

            var progress = bootstrap.LevelProgress;
            Vector2 point = CenterOf(privacy);
            QueuePress(30, point); yield return null; yield return null;
            QueueRelease(30, point); yield return null; yield return null;
            Assert.That(openedUrls, Is.EqualTo(new[] { GameFlowController.PrivacyPolicyUrl }));
            Assert.That(flow.Mode, Is.EqualTo(GameFlowMode.MainMenu));

            point = CenterOf(play);
            QueuePress(31, point); yield return null; yield return null;
            QueueRelease(31, point); yield return null; yield return null;
            AssertFreshLevel(progress);
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(won ? 1 : 0));
            Assert.That(Read<GameObject>(hud, "outcomeDimmer").activeInHierarchy, Is.False);
        }

        // Reaches an outcome through the same resolved-move facts the controller reports; returns its title.
        private string ReachOutcome(string outcome)
        {
            if (outcome == "Lost")
            {
                for (int i = 0, moves = bootstrap.LevelProgress.MovesRemaining; i < moves; i++)
                    ReportMove(new ResolutionResult(Array.Empty<TileType>()));
                return "Out of Moves";
            }
            int count = outcome == "AllLevelsComplete" ? Read<LevelCatalog>(flow, "catalog").Count : 1;
            for (int index = 0; index < count; index++)
            {
                var progress = bootstrap.LevelProgress;
                ReportMove(new ResolutionResult(Enumerable.Repeat(progress.GoalTileType, progress.GoalTarget)));
                if (index + 1 < count) Assert.That(flow.Next(), Is.True);
            }
            return outcome == "AllLevelsComplete" ? "All Levels Complete" : "Level Complete!";
        }

        private static Vector2 CenterOf(Button button)
        {
            var rect = (RectTransform)button.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private IEnumerator VerifyOutcomeTouch(int expectedLevelIndex)
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
            Assert.That(flow.CurrentLevelIndex, Is.EqualTo(expectedLevelIndex));
        }

        private void AssertFreshLevel(LevelProgress before)
        {
            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(before), "The real UI button must request a fresh session.");
            Assert.That(bootstrap.LevelProgress.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(bootstrap.LevelProgress.MovesRemaining,
                Is.EqualTo(Read<LevelCatalog>(flow, "catalog")[flow.CurrentLevelIndex].StartingMoveCount));
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

        [UnityTest]
        public IEnumerator Scene_StatsViewIsTheTopBarAndShowsTheSessionValues()
        {
            var stats = Read<HudStatsView>(hud, "stats");
            Transform topBar = hud.GetComponentInParent<SafeAreaLayout>().TopBar;
            Assert.That(stats.transform, Is.SameAs(topBar));
            var moves = Read<TMPro.TMP_Text>(stats, "movesText");
            var score = Read<TMPro.TMP_Text>(stats, "scoreText");
            var goal = Read<TMPro.TMP_Text>(stats, "goalText");
            var icon = Read<Image>(stats, "goalIcon");
            Assert.That(moves.transform, Is.SameAs(topBar.Find("MovesGroup/MovesText")));
            Assert.That(score.transform, Is.SameAs(topBar.Find("ScoreGroup/ScoreText")));
            Assert.That(goal.transform, Is.SameAs(topBar.Find("GoalGroup/GoalRow/GoalText")));
            Assert.That(icon.transform, Is.SameAs(topBar.Find("GoalGroup/GoalRow/GoalIcon")));

            LevelProgress progress = bootstrap.LevelProgress;
            Assert.That(moves.text, Is.EqualTo(progress.MovesRemaining.ToString()));
            Assert.That(score.text, Is.EqualTo(progress.Score.ToString()));
            Assert.That(goal.text, Is.EqualTo($"{progress.GoalProgress}/{progress.GoalTarget}"));
            Assert.That(icon.sprite, Is.SameAs(Read<Sprite>(hud, progress.GoalTileType.ToString().ToLowerInvariant() + "GoalSprite")));
            yield break;
        }

        private static T Read<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, Fields).GetValue(target);
        }
    }
}
