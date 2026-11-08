using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation.Tests
{
    internal sealed class TestMainMenu
    {
        public GameObject Background;
        public GameObject Controls;
        public MainMenuView View;
        public Button Play;
        public TMP_Text Label;

        public static TestMainMenu Create(Transform owner)
        {
            var menu = new TestMainMenu();
            menu.Background = new GameObject("TestMainMenuBackground", typeof(RectTransform), typeof(Image));
            menu.Background.SetActive(false);
            menu.Background.transform.SetParent(owner, false);
            menu.Controls = new GameObject("TestMenuControls", typeof(RectTransform));
            menu.Controls.transform.SetParent(owner, false);
            var button = new GameObject("Play", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(menu.Controls.transform, false);
            menu.Play = button.GetComponent<Button>();
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(button.transform, false);
            menu.Label = text.GetComponent<TextMeshProUGUI>();
            menu.View = menu.Background.AddComponent<MainMenuView>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(MainMenuView).GetField("controls", flags).SetValue(menu.View, menu.Controls);
            typeof(MainMenuView).GetField("playButton", flags).SetValue(menu.View, menu.Play);
            typeof(MainMenuView).GetField("playLabel", flags).SetValue(menu.View, menu.Label);
            menu.Background.SetActive(true);
            return menu;
        }
    }
}
