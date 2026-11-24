using System.Reflection;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation.Tests
{
    // A LevelHudView with real TMP texts and real Buttons, wired the way the scene wires it in the Inspector.
    internal sealed class TestHud
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        public GameObject Root;
        public LevelHudView View;
        public HudStatsView Stats;
        public TMP_Text Moves;
        public TMP_Text Score;
        public TMP_Text Goal;
        public Button Restart;
        public Button GameplayMenu;
        public Image GoalIcon;
        public GameObject OutcomePanel;
        public TMP_Text OutcomeTitle;
        public TMP_Text OutcomeScore;
        public Button PlayAgain;
        public TMP_Text OutcomeAction;
        public Button OutcomeMenu;
        public GameObject OutcomeDimmer;

        public static TestHud Create()
        {
            var hud = new TestHud();
            // Inactive while wiring, so Awake (which hooks the button) sees every reference.
            hud.Root = new GameObject("TestHud");
            hud.Root.SetActive(false);
            // Like the scene's TopBar: the stats view sits on its own object above the value texts.
            var statsObject = new GameObject("Stats");
            statsObject.transform.SetParent(hud.Root.transform);
            hud.Moves = AddText(statsObject, "Moves");
            hud.Score = AddText(statsObject, "Score");
            hud.Goal = AddText(statsObject, "Goal");
            hud.Restart = AddButton(hud.Root, "Restart");
            hud.GameplayMenu = AddButton(hud.Root, "GameplayMenu");
            var iconObject = new GameObject("GoalIcon");
            iconObject.transform.SetParent(statsObject.transform);
            hud.GoalIcon = iconObject.AddComponent<Image>();
            hud.Stats = statsObject.AddComponent<HudStatsView>();
            SetStats(hud.Stats, "movesText", hud.Moves);
            SetStats(hud.Stats, "scoreText", hud.Score);
            SetStats(hud.Stats, "goalText", hud.Goal);
            SetStats(hud.Stats, "goalIcon", hud.GoalIcon);
            hud.OutcomePanel = new GameObject("OutcomePanel");
            hud.OutcomePanel.transform.SetParent(hud.Root.transform);
            hud.OutcomeTitle = AddText(hud.OutcomePanel, "OutcomeTitle");
            hud.OutcomeScore = AddText(hud.OutcomePanel, "OutcomeScore");
            hud.PlayAgain = AddButton(hud.OutcomePanel, "PlayAgain");
            hud.OutcomeAction = AddText(hud.PlayAgain.gameObject, "OutcomeAction");
            hud.OutcomeMenu = AddButton(hud.OutcomePanel, "Menu");
            // In the scene it sits under the root Canvas, outside the HUD. Here it is parented to Root only so
            // tests that destroy Root also clean it up; the view toggles its own active state either way.
            hud.OutcomeDimmer = new GameObject("OutcomeDimmer");
            hud.OutcomeDimmer.transform.SetParent(hud.Root.transform);
            hud.OutcomeDimmer.SetActive(false);

            hud.View = hud.Root.AddComponent<LevelHudView>();
            Set(hud.View, "stats", hud.Stats);
            Set(hud.View, "restartButton", hud.Restart);
            Set(hud.View, "gameplayMenuButton", hud.GameplayMenu);
            Set(hud.View, "outcomePanel", hud.OutcomePanel);
            Set(hud.View, "outcomeTitleText", hud.OutcomeTitle);
            Set(hud.View, "outcomeScoreText", hud.OutcomeScore);
            Set(hud.View, "playAgainButton", hud.PlayAgain);
            Set(hud.View, "outcomeActionText", hud.OutcomeAction);
            Set(hud.View, "outcomeMenuButton", hud.OutcomeMenu);
            Set(hud.View, "outcomeDimmer", hud.OutcomeDimmer);
            // The same distinct generated sprite per color that the tile tests use.
            Set(hud.View, "redGoalSprite", TestTiles.SpriteFor(TileType.Red));
            Set(hud.View, "greenGoalSprite", TestTiles.SpriteFor(TileType.Green));
            Set(hud.View, "blueGoalSprite", TestTiles.SpriteFor(TileType.Blue));
            Set(hud.View, "yellowGoalSprite", TestTiles.SpriteFor(TileType.Yellow));
            Set(hud.View, "purpleGoalSprite", TestTiles.SpriteFor(TileType.Purple));
            Set(hud.View, "orangeGoalSprite", TestTiles.SpriteFor(TileType.Orange));
            hud.Root.SetActive(true);
            return hud;
        }

        private static TMP_Text AddText(GameObject parent, string name)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent.transform);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private static Button AddButton(GameObject parent, string name)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent.transform);
            return buttonObject.AddComponent<Button>();
        }

        private static void Set(LevelHudView view, string field, object value)
        {
            typeof(LevelHudView).GetField(field, Fields).SetValue(view, value);
        }

        private static void SetStats(HudStatsView view, string field, object value)
        {
            typeof(HudStatsView).GetField(field, Fields).SetValue(view, value);
        }
    }
}
