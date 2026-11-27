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
    // The HUD only formats values LevelProgress already calculated, and forwards Restart / Play Again clicks.
    // No pixels or layout are checked: only the values it writes, the goal icon, the outcome panel and its buttons.
    // Static labels (MOVES, GOAL, SCORE, FINAL SCORE) live in the scene, so the texts here hold values only.
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
        public void Show_NewLevel_DisplaysStartingValuesAndHidesTheOutcome()
        {
            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));

            Assert.That(hud.Moves.text, Is.EqualTo("20"));
            Assert.That(hud.Score.text, Is.EqualTo("0"));
            Assert.That(hud.GoalCount.text, Is.EqualTo("0"));
            Assert.That(hud.GoalTotal.text, Is.EqualTo("/ 25"));
            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
            Assert.That(hud.OutcomePanel.activeSelf, Is.False);
        }

        [Test]
        public void Show_AfterAMove_DisplaysTheNewValues()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Green }));

            hud.View.Show(progress);

            Assert.That(hud.Moves.text, Is.EqualTo("19"));
            Assert.That(hud.Score.text, Is.EqualTo("300"));
            Assert.That(hud.GoalCount.text, Is.EqualTo("2"));
            Assert.That(hud.GoalTotal.text, Is.EqualTo("/ 25"));
            Assert.That(hud.OutcomePanel.activeSelf, Is.False);
        }

        [Test]
        public void Show_Won_ShowsTheOutcomePanelWithTheFinalScore()
        {
            var progress = new LevelProgress(5, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Red }));

            hud.View.Show(progress);

            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("Level Complete!"));
            Assert.That(hud.OutcomeScore.text, Is.EqualTo("300"));
            Assert.That(hud.GoalCount.text, Is.EqualTo("3"));
            Assert.That(hud.GoalTotal.text, Is.EqualTo("/ 3"));
        }

        [Test]
        public void Show_Lost_ShowsTheOutcomePanelWithTheFinalScore()
        {
            var progress = new LevelProgress(1, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Green, TileType.Green, TileType.Green }));

            hud.View.Show(progress);

            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("Out of Moves"));
            Assert.That(hud.OutcomeScore.text, Is.EqualTo("300"));
            Assert.That(hud.Moves.text, Is.EqualTo("0"));
        }

        [Test]
        public void Show_PlayingAfterAnOutcome_HidesThePanelAgain()
        {
            var finished = new LevelProgress(1, TileType.Red, 3, 100);
            finished.ApplyValidMove(new ResolutionResult(new[] { TileType.Green }));
            hud.View.Show(finished);
            Assert.That(hud.OutcomePanel.activeSelf, Is.True);

            hud.View.Show(new LevelProgress(1, TileType.Red, 3, 100));

            Assert.That(hud.OutcomePanel.activeSelf, Is.False);
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
            Assert.That(hud.GoalCount.text, Is.EqualTo("0"));
            Assert.That(hud.GoalTotal.text, Is.EqualTo("/ 25"));
            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
        }

        [Test]
        public void RestartClick_AfterReinitializing_CallsOnlyTheLatestHandlerExactlyOnce()
        {
            int oldCalls = 0;
            int newCalls = 0;
            hud.View.Initialize(() => oldCalls++, () => { }, () => { });
            hud.View.Initialize(() => newCalls++, () => { }, () => { });

            hud.Restart.onClick.Invoke();

            Assert.That(oldCalls, Is.Zero);
            Assert.That(newCalls, Is.EqualTo(1));
        }

        [Test]
        public void OutcomeRestartAndMenuClicks_HaveSeparateHandlers_ReplacedExactlyOnce()
        {
            int oldCalls = 0, restartCalls = 0, outcomeCalls = 0, menuCalls = 0;
            hud.View.Initialize(() => oldCalls++, () => oldCalls++, () => oldCalls++);
            hud.View.Initialize(() => restartCalls++, () => outcomeCalls++, () => menuCalls++);
            hud.PlayAgain.onClick.Invoke();
            Assert.That(outcomeCalls, Is.EqualTo(1));
            Assert.That(restartCalls, Is.Zero);
            hud.Restart.onClick.Invoke();
            Assert.That(restartCalls, Is.EqualTo(1));
            Assert.That(outcomeCalls, Is.EqualTo(1));
            hud.OutcomeMenu.onClick.Invoke();
            Assert.That(menuCalls, Is.EqualTo(1));
            hud.GameplayMenu.onClick.Invoke();
            Assert.That(menuCalls, Is.EqualTo(2), "Both Menu buttons forward the same navigation request.");
            Assert.That(restartCalls, Is.EqualTo(1));
            Assert.That(outcomeCalls, Is.EqualTo(1));
            Assert.That(oldCalls, Is.Zero);
        }

        [Test]
        public void SetRestartAvailable_UsesOneBusyGate_AndOnlyOffersActionsForTheCurrentStatus()
        {
            hud.View.Show(new LevelProgress(2, TileType.Red, 3, 100));
            hud.View.SetRestartAvailable(false);
            Assert.That(hud.Restart.interactable, Is.False);
            Assert.That(hud.PlayAgain.interactable, Is.False);
            Assert.That(hud.OutcomeMenu.interactable, Is.False);

            hud.View.SetRestartAvailable(true);
            Assert.That(hud.Restart.interactable, Is.True);
            Assert.That(hud.PlayAgain.interactable, Is.False);
            Assert.That(hud.OutcomeMenu.interactable, Is.False);

            // The outcome can appear inside the locked callback, before cleanup releases the busy gate.
            hud.View.SetRestartAvailable(false);
            var finished = new LevelProgress(1, TileType.Red, 3, 100);
            finished.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Red }));
            hud.View.Show(finished);
            Assert.That(hud.Restart.interactable, Is.False);
            Assert.That(hud.PlayAgain.interactable, Is.False);
            Assert.That(hud.OutcomeMenu.interactable, Is.False);
            hud.View.SetRestartAvailable(true);
            Assert.That(hud.Restart.interactable, Is.False);
            Assert.That(hud.PlayAgain.interactable, Is.True);
            Assert.That(hud.OutcomeMenu.interactable, Is.True);

            hud.View.Show(new LevelProgress(2, TileType.Red, 3, 100));
            Assert.That(hud.Restart.interactable, Is.True);
            Assert.That(hud.PlayAgain.interactable, Is.False);
            Assert.That(hud.OutcomeMenu.interactable, Is.False);
        }

        [Test]
        public void SetOutcomePresentation_DisplaysFlowSuppliedFinalTitleAndAction_WithoutChangingTheScore()
        {
            var finished = new LevelProgress(1, TileType.Red, 3, 100);
            finished.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Red }));
            hud.View.Show(finished);
            hud.View.SetOutcomePresentation("All Levels Complete", "Play Again");

            Assert.That(hud.OutcomeTitle.text, Is.EqualTo("All Levels Complete"));
            Assert.That(hud.OutcomeAction.text, Is.EqualTo("Play Again"));
            Assert.That(hud.OutcomeScore.text, Is.EqualTo("300"));
            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
        }

        [Test]
        public void GameplayActions_PromptGateNeverOverridesBusyOrTerminalState()
        {
            hud.View.Show(new LevelProgress(2, TileType.Red, 1, 100));
            hud.View.SetRestartAvailable(true);
            Assert.That(hud.Restart.interactable && hud.GameplayMenu.interactable, Is.True);
            hud.View.SetGameplayActionsAvailable(false);
            hud.View.SetRestartAvailable(true);
            Assert.That(hud.Restart.interactable || hud.GameplayMenu.interactable, Is.False);
            hud.View.SetRestartAvailable(false);
            hud.View.SetGameplayActionsAvailable(true);
            Assert.That(hud.Restart.interactable || hud.GameplayMenu.interactable, Is.False);
            hud.View.SetRestartAvailable(true);
            Assert.That(hud.Restart.interactable && hud.GameplayMenu.interactable, Is.True);

            var won = new LevelProgress(2, TileType.Red, 1, 100);
            won.ApplyValidMove(new ResolutionResult(new[] { TileType.Red }));
            hud.View.Show(won);
            Assert.That(hud.Restart.interactable || hud.GameplayMenu.interactable, Is.False);
            Assert.That(hud.PlayAgain.interactable && hud.OutcomeMenu.interactable, Is.True);
        }

        // The full-screen dimmer sits outside the HUD's CanvasGroup, so the view applies
        // "level over AND HUD visible" itself, whichever of Show and SetVisible comes last.
        [Test]
        public void OutcomeDimmer_ShowThenHide_FollowsTheOutcomeAndTheHudVisibility()
        {
            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));
            Assert.That(hud.OutcomeDimmer.activeSelf, Is.False, "Off while playing.");

            hud.View.Show(Won());
            Assert.That(hud.OutcomeDimmer.activeSelf, Is.True, "On with the Won outcome.");

            hud.View.SetVisible(false);
            Assert.That(hud.OutcomeDimmer.activeSelf, Is.False, "Off when the HUD is hidden for the menu.");
            Assert.That(hud.OutcomePanel.activeSelf, Is.False, "The card is deactivated so it cannot take menu touches.");

            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));
            hud.View.SetVisible(true);
            Assert.That(hud.OutcomeDimmer.activeSelf, Is.False, "A new level starts without the dimmer.");
            Assert.That(hud.OutcomePanel.activeSelf, Is.False);
        }

        [Test]
        public void OutcomeDimmer_HiddenThenShown_AppearsOnlyWhenTheHudBecomesVisible()
        {
            hud.View.SetVisible(false);
            hud.View.Show(Lost());
            Assert.That(hud.OutcomeDimmer.activeSelf, Is.False, "A terminal Show while hidden leaves the dimmer off.");
            Assert.That(hud.OutcomePanel.activeSelf, Is.False);

            hud.View.SetVisible(true);
            Assert.That(hud.OutcomeDimmer.activeSelf, Is.True, "Showing the HUD with the outcome up turns it on.");
            Assert.That(hud.OutcomePanel.activeSelf, Is.True);
        }

        private static LevelProgress Won()
        {
            var progress = new LevelProgress(5, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Red, TileType.Red, TileType.Red }));
            return progress;
        }

        private static LevelProgress Lost()
        {
            var progress = new LevelProgress(1, TileType.Red, 3, 100);
            progress.ApplyValidMove(new ResolutionResult(new[] { TileType.Green, TileType.Green, TileType.Green }));
            return progress;
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => hud.View.Initialize(null, () => { }, () => { }));
            Assert.Throws<ArgumentNullException>(() => hud.View.Initialize(() => { }, null, () => { }));
            Assert.Throws<ArgumentNullException>(() => hud.View.Initialize(() => { }, () => { }, null));
            Assert.Throws<ArgumentNullException>(() => hud.View.Show(null));
        }

        [TestCase("stats")]
        [TestCase("orangeGoalSprite")]
        [TestCase("outcomePanel")]
        [TestCase("outcomeTitleText")]
        [TestCase("outcomeScoreText")]
        [TestCase("playAgainButton")]
        [TestCase("outcomeActionText")]
        [TestCase("outcomeMenuButton")]
        [TestCase("outcomeDimmer")]
        [TestCase("gameplayMenuButton")]
        public void Initialize_MissingReference_Throws(string field)
        {
            typeof(LevelHudView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hud.View, null);

            Assert.Throws<InvalidOperationException>(() => hud.View.Initialize(() => { }, () => { }, () => { }));
        }

        // The stats view's own references are checked at the same startup point as LevelHudView's.
        [TestCase("movesText")]
        [TestCase("scoreText")]
        [TestCase("goalCountText")]
        [TestCase("goalTotalText")]
        [TestCase("levelNumberText")]
        [TestCase("levelTotalText")]
        [TestCase("goalFill")]
        [TestCase("goalIcon")]
        public void Initialize_MissingStatsReference_Throws(string field)
        {
            typeof(HudStatsView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hud.Stats, null);

            Assert.Throws<InvalidOperationException>(() => hud.View.Initialize(() => { }, () => { }, () => { }));
        }

        [TestCase("MovesRemaining", -1)]
        [TestCase("Score", -1)]
        [TestCase("GoalProgress", -1)]
        [TestCase("GoalTarget", 0)]
        public void StatsView_RejectedFormatting_LeavesEveryValueAndFillUnchanged(string property, int value)
        {
            var original = new LevelProgress(20, TileType.Red, 25, 100);
            original.ApplyValidMove(new ResolutionResult(new[] { TileType.Red }));
            hud.View.SetLevel(2, 6);
            hud.View.Show(original);
            var invalid = new LevelProgress(7, TileType.Blue, 9, 50);
            typeof(LevelProgress).GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(invalid, value);

            Assert.Throws<ArgumentOutOfRangeException>(() => hud.Stats.Show(invalid, TestTiles.SpriteFor(TileType.Blue)));

            Assert.That(hud.Moves.text, Is.EqualTo("19"));
            Assert.That(hud.Score.text, Is.EqualTo("100"));
            Assert.That(hud.GoalCount.text, Is.EqualTo("1"));
            Assert.That(hud.GoalTotal.text, Is.EqualTo("/ 25"));
            Assert.That(hud.LevelNumber.text, Is.EqualTo("2"));
            Assert.That(hud.LevelTotal.text, Is.EqualTo("/ 6"));
            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
            Assert.That(hud.GoalFill.enabled, Is.True);
            Assert.That(hud.GoalFill.rectTransform.anchorMax, Is.EqualTo(new Vector2(1f / 25, 1)));
        }

        [TestCase(0, 6)]
        [TestCase(7, 6)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        public void SetLevel_InvalidPair_LeavesBothTextsUnchanged(int number, int count)
        {
            hud.View.SetLevel(2, 6);
            Assert.Throws<ArgumentOutOfRangeException>(() => hud.View.SetLevel(number, count));
            Assert.That(hud.LevelNumber.text, Is.EqualTo("2"));
            Assert.That(hud.LevelTotal.text, Is.EqualTo("/ 6"));
        }

        [Test]
        public void SetLevel_UsesBothSuppliedValues_AndDoesNotChangeSessionValues()
        {
            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));
            hud.View.SetLevel(22, 22);
            Assert.That(hud.LevelNumber.text, Is.EqualTo("22"));
            Assert.That(hud.LevelTotal.text, Is.EqualTo("/ 22"));
            Assert.That(hud.Moves.text, Is.EqualTo("20"));
            Assert.That(hud.GoalCount.text, Is.EqualTo("0"));
            Assert.That(hud.GoalFill.enabled, Is.False);
        }

        [Test]
        public void StatsView_NullArguments_ThrowWithoutWriting()
        {
            hud.View.Show(new LevelProgress(20, TileType.Red, 25, 100));

            Assert.Throws<ArgumentNullException>(() => hud.Stats.Show(null, TestTiles.SpriteFor(TileType.Blue)));
            Assert.Throws<ArgumentNullException>(() => hud.Stats.Show(new LevelProgress(7, TileType.Blue, 9, 100), null));
            Assert.That(hud.Moves.text, Is.EqualTo("20"));
            Assert.That(hud.GoalCount.text, Is.EqualTo("0"));
            Assert.That(hud.GoalTotal.text, Is.EqualTo("/ 25"));
            Assert.That(hud.GoalIcon.sprite, Is.SameAs(TestTiles.SpriteFor(TileType.Red)));
        }
    }
}
