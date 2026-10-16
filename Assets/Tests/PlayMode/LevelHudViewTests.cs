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
    // No pixels or layout are checked: only the text it shows and the button it controls.
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

            Assert.That(hud.Moves.text, Is.EqualTo("Moves: 20"));
            Assert.That(hud.Score.text, Is.EqualTo("Score: 0"));
            Assert.That(hud.Goal.text, Is.EqualTo("Goal: Red 0 / 25"));
            Assert.That(hud.Status.text, Is.Empty);
        }

        [Test]
        public void Show_AfterAMove_DisplaysTheNewValues()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Green }));

            hud.View.Show(progress);

            Assert.That(hud.Moves.text, Is.EqualTo("Moves: 19"));
            Assert.That(hud.Score.text, Is.EqualTo("Score: 300"));
            Assert.That(hud.Goal.text, Is.EqualTo("Goal: Red 2 / 25"));
            Assert.That(hud.Status.text, Is.Empty);
        }

        [Test]
        public void Show_Won_DisplaysTheWinMessage()
        {
            var progress = new LevelProgress(5, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Red }));

            hud.View.Show(progress);

            Assert.That(hud.Status.text, Is.EqualTo("You Win!"));
            Assert.That(hud.Goal.text, Is.EqualTo("Goal: Red 3 / 3"));
        }

        [Test]
        public void Show_Lost_DisplaysTheOutOfMovesMessage()
        {
            var progress = new LevelProgress(1, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Green, TileType.Green, TileType.Green }));

            hud.View.Show(progress);

            Assert.That(hud.Status.text, Is.EqualTo("Out of Moves"));
            Assert.That(hud.Moves.text, Is.EqualTo("Moves: 0"));
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

        [Test]
        public void Initialize_MissingReference_Throws()
        {
            typeof(LevelHudView).GetField("goalText", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hud.View, null);

            Assert.Throws<InvalidOperationException>(() => hud.View.Initialize(() => { }));
        }
    }
}
