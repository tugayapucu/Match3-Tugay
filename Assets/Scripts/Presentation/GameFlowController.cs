using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    public enum GameFlowMode { Uninitialized, MainMenu, Playing, Outcome }

    // Chooses sessions and navigation; Bootstrap and Core retain all in-level gameplay responsibilities.
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private BoardBootstrap bootstrap;
        [SerializeField] private LevelHudView hud;

        private ProgressionStore store;
        private LevelStatus outcome;

        public GameFlowMode Mode { get; private set; }
        public int CurrentLevelIndex { get; private set; } = -1;
        public int Progression { get; private set; }
        public string MainMenuActionLabel => Progression == 0 ? "Play"
            : Progression == catalog.Count ? "Play Again" : "Continue";

        private void Start()
        {
            if (Mode == GameFlowMode.Uninitialized) Initialize();
        }

        // Tests supply a store with an isolated key before Start; production uses the versioned default.
        public void Initialize(ProgressionStore progressionStore = null)
        {
            if (Mode != GameFlowMode.Uninitialized)
                throw new InvalidOperationException("Game flow is already initialized.");
            if (catalog == null || bootstrap == null || hud == null)
                throw new InvalidOperationException("Assign the catalog, Bootstrap and HUD before starting game flow.");
            catalog.Validate();
            store = progressionStore ?? new ProgressionStore(catalog.Count);
            int loaded = store.Load();
            hud.Initialize(() => Restart(), HandleOutcomeAction);
            bootstrap.SetInputAvailable(false);
            Progression = loaded;
            Mode = GameFlowMode.MainMenu;
            hud.SetVisible(false);
        }

        public bool Play()
        {
            if (Mode != GameFlowMode.MainMenu || bootstrap.IsBusy) return false;
            StartSession(Progression == catalog.Count ? 0 : Progression);
            return true;
        }

        public bool Restart()
        {
            if (Mode != GameFlowMode.Playing || bootstrap.IsBusy) return false;
            StartSession(CurrentLevelIndex);
            return true;
        }

        public bool Retry()
        {
            if (Mode != GameFlowMode.Outcome || outcome != LevelStatus.Lost || bootstrap.IsBusy) return false;
            StartSession(CurrentLevelIndex);
            return true;
        }

        public bool Next()
        {
            if (Mode != GameFlowMode.Outcome || outcome != LevelStatus.Won || bootstrap.IsBusy
                || CurrentLevelIndex + 1 >= catalog.Count) return false;
            StartSession(CurrentLevelIndex + 1);
            return true;
        }

        public bool PlayAgain()
        {
            if (Mode != GameFlowMode.Outcome || outcome != LevelStatus.Won || bootstrap.IsBusy
                || CurrentLevelIndex != catalog.Count - 1) return false;
            StartSession(0);
            return true;
        }

        public bool ReturnToMenu()
        {
            if ((Mode != GameFlowMode.Playing && Mode != GameFlowMode.Outcome) || bootstrap.IsBusy) return false;
            bootstrap.SetInputAvailable(false);
            Mode = GameFlowMode.MainMenu;
            hud.SetVisible(false);
            return true;
        }

        private void StartSession(int index)
        {
            // Build first; bad configuration must not change the current flow state.
            bootstrap.StartLevel(catalog[index], HandleLevelEnded);
            CurrentLevelIndex = index;
            Mode = GameFlowMode.Playing;
            hud.SetVisible(true);
            bootstrap.SetInputAvailable(true);
        }

        private void HandleLevelEnded(LevelStatus status)
        {
            // A synchronous terminal callback is accepted once; it never navigates inside the move lock.
            if (Mode != GameFlowMode.Playing) return;
            if (status != LevelStatus.Won && status != LevelStatus.Lost)
                throw new ArgumentOutOfRangeException(nameof(status));
            if (status == LevelStatus.Won) Progression = store.Save(CurrentLevelIndex + 1);
            outcome = status;
            Mode = GameFlowMode.Outcome;
            bootstrap.SetInputAvailable(false);
            hud.SetOutcomeActionLabel(status == LevelStatus.Lost ? "Retry"
                : CurrentLevelIndex + 1 < catalog.Count ? "Next Level" : "Play Again");
            // Bootstrap's existing busy callback enables the outcome action only after FinishSwap unlocks.
        }

        private void HandleOutcomeAction()
        {
            if (Mode != GameFlowMode.Outcome) return;
            if (outcome == LevelStatus.Lost) Retry();
            else if (CurrentLevelIndex + 1 < catalog.Count) Next();
            else PlayAgain();
        }
    }
}
