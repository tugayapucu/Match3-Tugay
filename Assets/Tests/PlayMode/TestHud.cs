using System.Reflection;
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

            hud.View = hud.Root.AddComponent<LevelHudView>();
            Set(hud.View, "movesText", hud.Moves);
            Set(hud.View, "scoreText", hud.Score);
            Set(hud.View, "goalText", hud.Goal);
            Set(hud.View, "statusText", hud.Status);
            Set(hud.View, "restartButton", hud.Restart);
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
