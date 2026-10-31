# Input stays locked if a move throws

Status: Reviewed in Phase 14 and kept as SHOULD, with accepted risk. No normal-play trigger for `LockedAfterException` was identified under the tested configuration. That does not prove the failure is impossible. No exception recovery was added (Phase 14 decision: Path B).

## Mechanism

`BoardInputController.HandlePointerDown` sets `isInputLocked` on the second press of a move, before the Board changes. Only the end of `FinishSwap` clears it. An exception anywhere in between stops that sequence, and `isInputLocked` stays `true` for the rest of the session. That covers:

- the swap and its analysis;
- the swap or rollback animation;
- resolution, special effects and refill;
- the move callback in `BoardBootstrap`, which applies progress, updates the HUD and runs dead-board recovery.

## Impact

- **Board input:** stays locked.
- **Restart and Play Again:** stay disabled. Only `onBusyChanged(false)` at the end of `FinishSwap` re-enables them, and it never runs.
- **`RestartLevel`:** refuses to run while input is locked ("Wait for the current move to finish…").
- **A throw in `TrySwapAdjacent` or `SwapValidator.Analyze`:** this is after the lock but before `onBusyChanged(true)`, so Restart still looks enabled. Clicking it is refused.
- **Recovery today:** relaunching the app. Nothing is saved yet, so a relaunch recovers completely.

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

## Accepted risk and when to revisit

**Accepted risk.** A future bug or an unforeseen state inside the move window freezes the session until the app is relaunched.

**Revisit when:**
- Phase 16 adds saving, since a relaunch would then no longer fully recover; or
- a real move-time exception is seen in testing, Play pre-launch reports or crash reports. Consider fault injection then.

**Escalation rule.** If a reachable trigger is found, this becomes MUST.

**Rules for any future recovery design:**
- keep the error visible;
- never resume normal play on an uncertain board;
- make a clean Restart available through the normal session-creation path.
