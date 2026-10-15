using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    // The real composition root: configuration reaches LevelProgress, and its handler ends the level's input.
    // Exact move-by-move rules are covered with scripted boards in BoardInputControllerTests.
    public class BoardBootstrapTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        private GameObject tileTemplate;
        private GameObject boardObject;
        private GameObject cameraObject;
        private GameObject bootstrapObject;
        private BoardView boardView;
        private BoardInputController controller;
        private BoardBootstrap bootstrap;

        [SetUp]
        public void SetUp()
        {
            tileTemplate = new GameObject("BootstrapTestTileTemplate");
            tileTemplate.SetActive(false);
            var template = tileTemplate.AddComponent<TileView>();

            boardObject = new GameObject("BootstrapTestBoard");
            boardObject.transform.position = new Vector3(200f, 200f, 0f);
            boardView = boardObject.AddComponent<BoardView>();
            typeof(BoardView).GetField("tilePrefab", Fields).SetValue(boardView, template);
            controller = boardObject.AddComponent<BoardInputController>();

            cameraObject = new GameObject("BootstrapTestCamera");
            var camera = cameraObject.AddComponent<Camera>();

            // Inactive until configured, so Start runs only after every field below is set.
            bootstrapObject = new GameObject("BootstrapTest");
            bootstrapObject.SetActive(false);
            bootstrap = bootstrapObject.AddComponent<BoardBootstrap>();
            Set("boardView", boardView);
            Set("inputController", controller);
            Set("inputCamera", camera);
            Set("width", 4);
            Set("height", 4);
            Set("palette", new[] { TileType.Red, TileType.Green, TileType.Blue });
            Set("goalTileType", TileType.Red);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(bootstrapObject);
            Object.Destroy(boardObject);
            Object.Destroy(cameraObject);
            Object.Destroy(tileTemplate);
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

        private void Set(string field, object value)
        {
            typeof(BoardBootstrap).GetField(field, Fields).SetValue(bootstrap, value);
        }
    }
}
