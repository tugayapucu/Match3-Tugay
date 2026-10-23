using System;
using System.Collections;
using System.Reflection;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    // The HUD only formats values LevelProgress already calculated, and forwards Restart clicks.
    // No pixels or layout are checked: only the values it writes, the goal icon and the button it controls.
    // Static labels (MOVES, GOAL, SCORE) live in the scene, so the texts here hold values only.
    public class LevelHudViewTests
    {
        private TestHud hud;

        [SetUp]
        public void SetUp()
        {
            hud = TestHud.Create();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(hud.Root);
            yield return null;
        }

        [Test]
        public void Show_NewLevel_DisplaysStartingValuesAndNoStatus()
        {
            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));

            Assert.That(hud.Moves.text, Is.EqualTo("20"));
            Assert.That(hud.Score.text, Is.EqualTo("0"));
            Assert.That(hud.Goal.text, Is.EqualTo("0/25"));
            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
            Assert.That(hud.Status.text, Is.Empty);
        }

        [Test]
        public void Show_AfterAMove_DisplaysTheNewValues()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Green }));

            hud.View.Show(progress);

            Assert.That(hud.Moves.text, Is.EqualTo("19"));
            Assert.That(hud.Score.text, Is.EqualTo("300"));
            Assert.That(hud.Goal.text, Is.EqualTo("2/25"));
            Assert.That(hud.Status.text, Is.Empty);
        }

        [Test]
        public void Show_Won_DisplaysTheWinMessage()
        {
            var progress = new LevelProgress(5, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Red }));

            hud.View.Show(progress);

            Assert.That(hud.Status.text, Is.EqualTo("You Win!"));
            Assert.That(hud.Goal.text, Is.EqualTo("3/3"));
        }

        [Test]
        public void Show_Lost_DisplaysTheOutOfMovesMessage()
        {
            var progress = new LevelProgress(1, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Green, TileType.Green, TileType.Green }));

            hud.View.Show(progress);

            Assert.That(hud.Status.text, Is.EqualTo("Out of Moves"));
            Assert.That(hud.Moves.text, Is.EqualTo("0"));
        }

        [TestCase(TileType.Red)]
        [TestCase(TileType.Green)]
        [TestCase(TileType.Blue)]
        [TestCase(TileType.Yellow)]
        [TestCase(TileType.Purple)]
        [TestCase(TileType.Orange)]
        public void Show_GoalIcon_IsTheGoalColorsOwnSprite(TileType goal)
        {
            hud.View.Show(new LevelProgress(20, goal, 25, 100));

            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(goal)));
        }

        [Test]
        public void Show_MissingGoalSprite_ThrowsBeforeChangingAnyValue()
        {
            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));
            typeof(LevelHudView).GetField("blueGoalSprite", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hud.View, null);

            Assert.Throws<InvalidOperationException>(() => hud.View.Show(new LevelProgress(9, TileType.Blue, 5, 100)));
            Assert.That(hud.Moves.text, Is.EqualTo("20"));
            Assert.That(hud.Goal.text, Is.EqualTo("0/25"));
            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
        }

        [Test]
        public void RestartClick_AfterReinitializing_CallsOnlyTheLatestHandlerExactlyOnce()
        {
            int oldCalls = 0;
            int newCalls = 0;
            hud.View.Initialize(() => oldCalls++);
            hud.View.Initialize(() => newCalls++);

            hud.Restart.onClick.Invoke();

            Assert.That(oldCalls, Is.Zero);
            Assert.That(newCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetRestartAvailable_ControlsWhetherTheButtonIsInteractable()
        {
            hud.View.SetRestartAvailable(false);
            Assert.That(hud.Restart.interactable, Is.False);

            hud.View.SetRestartAvailable(true);
            Assert.That(hud.Restart.interactable, Is.True);
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => hud.View.Initialize(null));
            Assert.Throws<ArgumentNullException>(() => hud.View.Show(null));
        }

        [TestCase("goalText")]
        [TestCase("goalIcon")]
        [TestCase("orangeGoalSprite")]
        public void Initialize_MissingReference_Throws(string field)
        {
            typeof(LevelHudView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hud.View, null);

            Assert.Throws<InvalidOperationException>(() => hud.View.Initialize(() => { }));
        }
    }
}
