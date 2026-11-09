using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation.Tests
{
    internal sealed class TestConfirmation
    {
        public GameObject Background;
        public GameObject Card;
        public ConfirmationView View;
        public TMP_Text Title;
        public TMP_Text Message;
        public TMP_Text ConfirmLabel;
        public Button Confirm;
        public Button Cancel;

        public static TestConfirmation Create(Transform owner)
        {
            var prompt = new TestConfirmation();
            prompt.Background = new GameObject("TestConfirmationDim", typeof(RectTransform), typeof(Image));
            prompt.Background.SetActive(false);
            prompt.Background.transform.SetParent(owner, false);
            prompt.Card = new GameObject("TestConfirmationCard", typeof(RectTransform));
            prompt.Card.transform.SetParent(owner, false);
            prompt.Title = AddText(prompt.Card, "Title");
            prompt.Message = AddText(prompt.Card, "Message");
            var confirm = new GameObject("Confirm", typeof(RectTransform), typeof(Button));
            confirm.transform.SetParent(prompt.Card.transform, false);
            prompt.Confirm = confirm.GetComponent<Button>();
            prompt.ConfirmLabel = AddText(confirm, "Label");
            var cancel = new GameObject("Cancel", typeof(RectTransform), typeof(Button));
            cancel.transform.SetParent(prompt.Card.transform, false);
            prompt.Cancel = cancel.GetComponent<Button>();
            prompt.View = prompt.Background.AddComponent<ConfirmationView>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ConfirmationView).GetField("card", flags).SetValue(prompt.View, prompt.Card);
            typeof(ConfirmationView).GetField("titleText", flags).SetValue(prompt.View, prompt.Title);
            typeof(ConfirmationView).GetField("messageText", flags).SetValue(prompt.View, prompt.Message);
            typeof(ConfirmationView).GetField("confirmText", flags).SetValue(prompt.View, prompt.ConfirmLabel);
            typeof(ConfirmationView).GetField("confirmButton", flags).SetValue(prompt.View, prompt.Confirm);
            typeof(ConfirmationView).GetField("cancelButton", flags).SetValue(prompt.View, prompt.Cancel);
            prompt.Background.SetActive(true);
            return prompt;
        }

        private static TMP_Text AddText(GameObject parent, string name)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(parent.transform, false);
            return text.GetComponent<TextMeshProUGUI>();
        }
    }
}
