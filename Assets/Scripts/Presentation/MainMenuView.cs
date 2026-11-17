using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Presentation
{
    // This object's image covers the full screen; controls live separately inside the existing SafeArea.
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject controls;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text playLabel;
        [SerializeField] private Button privacyButton;
        private Action onPlayRequested;
        private Action onPrivacyRequested;

        public void Initialize(Action onPlayRequested, Action onPrivacyRequested)
        {
            if (onPlayRequested == null) throw new ArgumentNullException(nameof(onPlayRequested));
            if (onPrivacyRequested == null) throw new ArgumentNullException(nameof(onPrivacyRequested));
            if (controls == null || playButton == null || playLabel == null || privacyButton == null)
                throw new InvalidOperationException("Assign Main Menu controls, Play button and label, and Privacy Policy button before initializing.");
            this.onPlayRequested = onPlayRequested;
            this.onPrivacyRequested = onPrivacyRequested;
        }

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(HandlePlayClicked);
            if (privacyButton != null) privacyButton.onClick.AddListener(HandlePrivacyClicked);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(HandlePlayClicked);
            if (privacyButton != null) privacyButton.onClick.RemoveListener(HandlePrivacyClicked);
        }

        public void Show(string actionLabel)
        {
            playLabel.text = actionLabel;
            gameObject.SetActive(true);
            controls.SetActive(true);
        }

        public void Hide()
        {
            controls.SetActive(false);
            gameObject.SetActive(false);
        }

        private void HandlePlayClicked() => onPlayRequested?.Invoke();
        private void HandlePrivacyClicked() => onPrivacyRequested?.Invoke();
    }
}
