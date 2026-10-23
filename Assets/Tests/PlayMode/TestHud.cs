using System.Reflection;
using Match3.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation.Tests
{
    // A LevelHudView with real TMP texts and a real Button, wired the way the scene wires it in the Inspector.
    internal sealed class TestHud
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        public GameObject Root;
        public LevelHudView View;
        public TMP_Text Moves;
        public TMP_Text Score;
        public TMP_Text Goal;
        public TMP_Text Status;
        public Button Restart;
        public Image GoalIcon;

        public static TestHud Create()
        {
            var hud = new TestHud();
            // Inactive while wiring, so Awake (which hooks the button) sees every reference.
            hud.Root = new GameObject("TestHud");
            hud.Root.SetActive(false);
            hud.Moves = AddText(hud.Root, "Moves");
            hud.Score = AddText(hud.Root, "Score");
            hud.Goal = AddText(hud.Root, "Goal");
            hud.Status = AddText(hud.Root, "Status");
            var buttonObject = new GameObject("Restart");
            buttonObject.transform.SetParent(hud.Root.transform);
            hud.Restart = buttonObject.AddComponent<Button>();
            var iconObject = new GameObject("GoalIcon");
            iconObject.transform.SetParent(hud.Root.transform);
            hud.GoalIcon = iconObject.AddComponent<Image>();

            hud.View = hud.Root.AddComponent<LevelHudView>();
            Set(hud.View, "movesText", hud.Moves);
            Set(hud.View, "scoreText", hud.Score);
            Set(hud.View, "goalText", hud.Goal);
            Set(hud.View, "statusText", hud.Status);
            Set(hud.View, "restartButton", hud.Restart);
            Set(hud.View, "goalIcon", hud.GoalIcon);
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

        private static void Set(LevelHudView view, string field, object value)
        {
            typeof(LevelHudView).GetField(field, Fields).SetValue(view, value);
        }
    }
}
