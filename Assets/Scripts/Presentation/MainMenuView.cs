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
        private Action onPlayRequested;

        public void Initialize(Action onPlayRequested)
        {
            if (onPlayRequested == null) throw new ArgumentNullException(nameof(onPlayRequested));
            if (controls == null || playButton == null || playLabel == null)
                throw new InvalidOperationException("Assign Main Menu controls, button and label before initializing.");
            this.onPlayRequested = onPlayRequested;
        }

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(HandlePlayClicked);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(HandlePlayClicked);
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
    }
}
