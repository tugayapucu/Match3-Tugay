using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    public enum ConfirmationIntent { Restart, Leave }

    // One prompt with two explicit wordings. Flow owns the pending action and all navigation.
    // This object is the full-screen dimmer; its card lives inside the existing SafeArea.
    public sealed class ConfirmationView : MonoBehaviour
    {
        [SerializeField] private GameObject card;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        private Action onConfirmRequested;
        private Action onCancelRequested;

        public void Initialize(Action onConfirmRequested, Action onCancelRequested)
        {
            if (onConfirmRequested == null) throw new ArgumentNullException(nameof(onConfirmRequested));
            if (onCancelRequested == null) throw new ArgumentNullException(nameof(onCancelRequested));
            if (card == null || titleText == null || messageText == null || confirmText == null
                || confirmButton == null || cancelButton == null)
                throw new InvalidOperationException("Assign the confirmation card, texts and both buttons before initializing.");
            this.onConfirmRequested = onConfirmRequested;
            this.onCancelRequested = onCancelRequested;
        }

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }

        public void Show(ConfirmationIntent intent)
        {
            if (intent != ConfirmationIntent.Restart && intent != ConfirmationIntent.Leave)
                throw new ArgumentOutOfRangeException(nameof(intent));
            titleText.text = intent == ConfirmationIntent.Restart ? "Restart level?" : "Leave level?";
            confirmText.text = intent == ConfirmationIntent.Restart ? "Restart" : "Leave";
            messageText.text = "Your progress in this level will be lost.";
            gameObject.SetActive(true);
            card.SetActive(true);
        }

        public void Hide()
        {
            card.SetActive(false);
            gameObject.SetActive(false);
        }

        private void HandleConfirmClicked() => onConfirmRequested?.Invoke();
        private void HandleCancelClicked() => onCancelRequested?.Invoke();
    }
}
