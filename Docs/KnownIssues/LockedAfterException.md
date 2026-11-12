# Input stays locked if a move throws

Status: Reviewed in Phase 14 and kept as SHOULD, with accepted risk. No normal-play trigger for `LockedAfterException` was identified under the tested configuration. That does not prove the failure is impossible. No exception recovery was added (Phase 14 decision: Path B). Re-reviewed at the end of Phase 16 (2026-11-12): still accepted and unresolved. Relaunch still rebuilds a clean session, now from the saved progression. Navigation refuses while busy, and the progression save adds one more call inside the locked window (see "Phase 16 review" below).

## Mechanism

`BoardInputController.TryStartSwap` sets `isInputLocked` when a move attempt starts, before the Board changes. Since Phase 15, that happens on the release of the second tap, or when a swipe crosses its threshold. Before Phase 15, it happened in `HandlePointerDown`, on the second press. Only the end of `FinishSwap` clears it. An exception anywhere in between stops that sequence, and `isInputLocked` stays `true` for the rest of the session. That covers:

- the swap and its analysis;
- the swap or rollback animation;
- resolution, special effects and refill;
- the move callback in `BoardBootstrap`, which applies progress, updates the HUD and runs dead-board recovery.

## Impact

- **Board input:** stays locked.
- **Restart and Play Again:** stay disabled. Only `onBusyChanged(false)` at the end of `FinishSwap` re-enables them, and it never runs.
- **Starting a session:** since Phase 16, `BoardBootstrap.StartLevel` throws while input is locked ("Wait for the current move to finish…"). Game flow never reaches it in that state, because its commands check `IsBusy` first. (Before Phase 16, the removed `RestartLevel` method refused in the same way.)
- **A throw in `TrySwapAdjacent` or `SwapValidator.Analyze`:** this is after the lock but before `onBusyChanged(true)`, so Restart still looks enabled. Clicking it is refused.
- **Game flow (since Phase 16):** every navigation command refuses while `BoardBootstrap.IsBusy`. In-game Menu, Android Back, confirmations and outcome actions therefore cannot leave a stuck session either.
- **Recovery today:** relaunching the app. Since Phase 16 a relaunch opens the Main Menu, and Play/Continue creates a fresh session from the saved progression. That is not necessarily the interrupted level; for example, during a replay after full completion it is Level 1. The failed attempt itself is always lost.

## Phase 14 evidence

**Static trace.** Every throw site inside the locked window falls into one of five groups. None is reachable by normal player input in the tested configuration:

- internal null and argument guards;
- Inspector configuration checked lazily, inside a move;
- Board/view or Core invariants, which only a bug can break;
- state guards for situations input already prevents;
- dead-board recovery's own argument and fallback checks, valid from session start.

**Core simulation.** About 100,000 settled-board checkpoints, with 0 exceptions.

**Adversarial Play Mode run.** This used SampleScene with the real bootstrap, input controller, runner and view, driven by programmatic `HandlePointerDown` presses.

| Measure | Result |
| --- | --- |
| Moves | 4,000: 2,926 normal, 654 special, 420 invalid with rollback |
| Retained-selection sequences | 164 |
| Presses while locked | 27,901, all ignored |
| Level flow | 90 mid-level restarts, 60 wins, 96 losses, 202 seeds |
| Exceptions or errors logged | 0 |
| Stuck locks | 0 |

**Adversarial review (Step 10).** No trigger found. The three coverage gaps it raised were closed with tests.

**Startup configuration check (`6ada8cd`).** `BoardView.ValidateConfiguration` now runs when a level starts and checks:

- `clearDuration`, `specialActivationDuration` and `wispFlightDuration`;
- the sprite for every palette colour and every special.

A broken Inspector setup therefore fails at startup instead of mid-move. The runtime guards inside the move methods are unchanged.

**Not done, by decision:** fault injection. A synthetic exception would only show the known consequence; it would not show that a player can reach it.

## Why no recovery was added

The evidence does not justify any of these:

- a custom coroutine executor;
- a faulted-game state;
- broad `try`/`catch`;
- global exception handling.

Unlocking after a caught exception would let play continue on a board that may be inconsistent with its views.

A plain `try`/`finally` around the move is unlikely to be enough. The move yields nested coroutines, which Unity runs itself, so an exception inside one may not reach a `try` in the outer routine. This is unproven reasoning, not established Unity behaviour, and it was not tested.

## Another way to stay locked: deactivating mid-move (Phase 15 Step 12)

Calling `SetActive(false)` on the controller's GameObject during a move stops every coroutine on it, `FinishSwap` included. Input then stays locked, exactly as after an exception.

Disabling only the component is safe: a disabled MonoBehaviour's running coroutines continue, and `FinishSwap` still unlocks. The bootstrap uses `enabled = false` when the level ends. No production code deactivates the GameObject today.

Keep it that way. If later game flow (Phase 16 menus) hides the board, it should wait until the controller is not busy, or disable the component rather than deactivate the GameObject.

## Phase 16 review (game flow and saving)

**Component disable vs GameObject deactivation.** Phase 16 kept the safe side of that rule:

- `BoardBootstrap` is the only writer of `inputController.enabled`. Menus, prompts and outcomes gate input through `SetInputAvailable`, which disables the component, never the GameObject.
- The Main Menu and the confirmation are overlays that activate and deactivate only their own objects. The board, the input controller and their coroutines stay active behind them.
- Every `GameFlowController` command (Play, Restart, Retry, Next, Play Again, Menu, Confirm, Cancel, Back) returns `false` while `bootstrap.IsBusy`. `BoardBootstrap.StartLevel` also throws if called mid-move. No flow path replaces a session or hides the board while `FinishSwap` still owns the lock.
- The outcome callback runs synchronously inside the lock. `HandleLevelEnded` records the outcome and shows the panel, but never navigates there; Next, Retry and Menu are accepted only after `FinishSwap` unlocks.

Evidence: busy-refusal and same-frame ordering tests in `GameFlowNavigationAdversarialTests`, `GameFlowBackTests` and `GameFlowSessionIsolationTests` (Steps 12–14), and the S23 "Back during resolution" check (Step 15).

Constraint for later phases: any new navigation (pause, level select, scene change) must keep this rule. Refuse while busy, and gate input through Bootstrap.

**Persistence review (Step 7, confirmed by the integrated flow).**

- Only one integer is stored: the progression index. It is written only when game flow accepts a `Won` outcome (`HandleLevelEnded` → `ProgressionStore.Save`), and it never decreases.
- Board, RNG, score, moves and selection are never saved. A partially failed Board can therefore never become the authoritative state.
- A relaunch after a stuck lock opens the Main Menu. Play/Continue then builds a fresh deterministic session from the saved progression, which is not necessarily the level that was interrupted. The "relaunch fully recovers" statement now reads: relaunch returns to a clean session at the saved progression; the failed attempt is lost, as for any interrupted level.

**New call in the locked window.** `ProgressionStore.Save` (PlayerPrefs) runs inside the win callback, so a storage exception there would also leave the session locked. It belongs to the existing accepted risk. What a failed save leaves on disk is unverified: depending on where it failed, the new index may or may not survive a relaunch. No storage failure was observed in Editor or S23 testing; this is not evidence that one cannot happen.

**Not claimed.** Phase 16 does not solve or recover from the general exception risk. No normal-play trigger was identified, and saving adds another call inside the locked window.

## Accepted risk and when to revisit

**Accepted risk.** A future bug or an unforeseen state inside the move window freezes the session until the app is relaunched.

**Revisit when:**
- saving grows beyond the progression index (for example, saving an active session), since a relaunch would then no longer rebuild a clean session. The Phase 16 progression save was reviewed above and does not trigger this; or
- a real move-time exception is seen in testing, Play pre-launch reports or crash reports. Consider fault injection then.

**Escalation rule.** If a reachable trigger is found, this becomes MUST.

**Rules for any future recovery design:**
- keep the error visible;
- never resume normal play on an uncertain board;
- make a clean Restart available through the normal session-creation path.
