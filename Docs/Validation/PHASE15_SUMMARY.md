# Phase 15 — Mobile interaction polish: summary

Completed 2026-11-06. Plan and gate: `Docs/Phases/PHASE15_EXECUTION.md`.

This file replaces the five Phase 15 step reports (Steps 9–13). They are still in
git history. Read one with, for example:

```text
git show b4363fc:Docs/Validation/PHASE15_STEP11_DEVICE_REGRESSION.md
```

Old names: `PHASE15_STEP9_THRESHOLD_TUNING`, `STEP10_HUMAN_TOUCH_COMFORT`,
`STEP11_DEVICE_REGRESSION`, `STEP12_ADVERSARIAL_REVIEW`, `STEP13_FULL_REGRESSION`.

## What was built

- **Swipe input.** A swipe is turned into the same "first cell + adjacent second
  cell" intent as two taps, and goes through the existing swap path. Core knows
  nothing about swipes.
- **Selection feedback.** A child corner-bracket highlight on the selected tile. It
  is off by default, leaves the tile's renderer, scale and collider unchanged, and
  clears before any move animation.
- **Tap timing.** Selection and the adjacent second-tap swap both happen on
  **release**, not press.

## Final behavior

| Rule | Value |
| --- | --- |
| Swipe threshold | `0.25` × screen-space cell spacing, measured at press time (about 30 px on the S23) |
| Direction | The larger axis wins (`>=`); an exact dx/dy tie resolves horizontally |
| Attempts | One swipe → at most one swap attempt |
| Tap on the selected tile | Cancels the selection on release, at no move cost (added after the human review) |
| Non-adjacent or outside tap, rejected swipe | Selection is kept |
| Press begun while busy | Ignored, even if held past unlock; a fresh press is needed |
| Off-board swipe | Consumed until release |
| Focus loss, pointer-device change, Restart, outcome | Gesture cancelled |
| Second finger | Ignored; only the primary touch is tracked |

## Key decisions

- **Threshold stayed at 0.25.** On the S23 the developer compared 0.20, 0.25 and 0.35
  (one development build each) and noticed no difference. 0.25 is the conservative
  middle, not a measured optimum.
- **Tap-to-cancel added** after the developer found an accidental first selection
  could not be undone. It is the smallest change, in the existing tap-release branch.
- **Restart confirmation deferred**, then implemented in Phase 16
  (`KnownIssues/RestartConfirmation.md`).
- **Selection art kept.** Nova's permanent gold corners and the white selection cue
  were "clearly different" on the S23.

## Evidence

**S23 (Galaxy S23 SM-S911B, one device):**

- **Human finger:** centre, edge and corner cells, two-tap, swipes and one-handed use
  were all "comfortable". The first tap after a cold launch worked in 5/5 rounds,
  each verified as a fresh process. The two-finger drag test selected the first
  tile, with no swap.
- **ADB-injected native touches:**
  - four swipe directions, rollback, a valid cascade, and the Nova and special
    chains;
  - edge and corner rejection;
  - busy presses held through unlock;
  - Win/Loss input ignored, and Play Again.

  Every settled board matched Core's prediction.
- **Dead-board recovery:** a temporary seed-93 build replayed the ten-move recovery
  route with the current input code. It finished at 10 moves, 3/25, 3,300 points.
  Exact callback ordering is proven by the PlayMode test
  `Seed93Swipes_RecoveryFinishesInsideTheLockedProductionCallback`.

**Adversarial review (Step 12):** no bug found. One gap closed with a test:
`Update_SecondFingerMoving_NeverDragsTheTrackedGestureIntoASwap`. Documented as
theoretical, with no change:

- a press in the exact frame of unlock is ignored (a one-frame window);
- a pointer-device change mid-gesture drops a same-frame press on the new pointer;
- `SetActive(false)` on the controller during a move stops `FinishSwap`, leaving
  input locked. Disabling the component is safe; see
  `KnownIssues/LockedAfterException.md`.

**Unexplained but harmless so far:** one ADB first tap after launch was missed.
The best hypothesis is that Input System 1.20 cannot see a press and release that
land in the same input update on a pointer that was never queried before. Human
taps span frames, which fits all the evidence. It is not proven, and no workaround
was added.

**Final regression (`e8b2b95`):**

- EditMode 603/603.
- PlayMode 233 passed, 0 failed, 3 skipped (the `[Explicit]` frame-rate tests).
- The Console held only the declared test messages.
- Mouse two-click, touch two-tap and touch swipe each reach the real `Update` path
  with exactly one attempt.

## Notes worth keeping

- **Interrupted PlayMode runs can leave a virtual Touchscreen** and `KEYS`/`TSCR`
  format errors. Restarting the Editor restored native Keyboard/Mouse
  (`KnownIssues/WatchList.md`).
- **S23 logs contain known noise:** the `AssetPackManager` startup exception (a
  Phase 13 audit investigation item), `AHardwareBuffer` errors and `DexFile`
  assertions. No gameplay failure was tied to them.
- **APKs, screenshots, routes and logs** are local, Git-ignored files under
  `Builds/Android/Phase15-Step9/` to `Phase15-Step11/`.
