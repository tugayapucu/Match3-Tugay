# Restart confirmation

Status: Resolved in Phase 16. Implemented and reviewer-approved in Step 11
(2026-11-10), covered by the Step 13–16 Editor suites, and checked by hand on
the S23 in Step 15 (2026-11-12).

## Original problem

Restart was easy to reach on the S23, and an accidental press would immediately
discard the current session's progress. The developer reported this concern during the
human comfort review; an actual accidental-restart incident was not reported.
The original deferral is recorded in
`Docs/Validation/PHASE15_STEP10_HUMAN_TOUCH_COMFORT.md`.

## Final behavior

The fix is deliberately narrow: one confirmation view with two intents, and no
generic dialog framework.

| Action | Behavior |
| --- | --- |
| Restart while settled and playing | Opens "Restart level?" with Cancel / Restart |
| In-game Menu, or Android Back, while settled and playing | Opens "Leave level?" with Cancel / Leave |
| Either action during a move | Refused; nothing is queued |
| Cancel, or Back while the prompt is open | Closes the prompt; the same session, HUD and selected tile remain |
| Confirm Restart | Starts the same level once, from its deterministic start |
| Confirm Leave | Returns to the Main Menu; the unfinished attempt is not saved |
| Retry / Next / Play Again on an outcome | Immediate; the level is already over, so no progress is lost |

`GameFlowController` owns the prompt (`GameFlowMode.Confirmation`,
`PendingConfirmation`). Board input stays gated through `BoardBootstrap` while
the prompt is open, so the HUD and the board cannot act behind it. The dimmer
covers the full screen; the card stays inside the safe area.

## Evidence

- Implementation, tests and manual scene setup:
  `Docs/Validation/PHASE16_STEP11_CONFIRMATIONS.md`.
- Back on the prompt: `Docs/Validation/PHASE16_STEP12_ANDROID_BACK.md`.
- Adversarial ordering (busy, double Confirm, Back + tap in one frame):
  `Docs/Validation/PHASE16_STEP14_NAVIGATION_ADVERSARIAL.md`.
- S23 Restart → Cancel, Restart → Confirm, Leave, Back-to-cancel, and
  background/resume with the prompt open, all passed:
  `Docs/Validation/PHASE16_STEP15_S23_GAME_FLOW.md`.

## Residual edge (theoretical, not a defect)

With a tile selected, a fast double-tap on Cancel could let the second, genuinely
later press reach the board cell under Cancel, and swap if that cell is adjacent to
the kept selection. This follows the approved fresh-press and kept-selection rules.
The S23 attempt showed no swap but was inconclusive (touch position not recorded).
No cooldown was added. Revisit only if a player reports an unintended swap after
Cancel.
