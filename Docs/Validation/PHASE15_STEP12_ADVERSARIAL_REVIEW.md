# Phase 15 Step 12 — Claude adversarial review

Date: 2026-11-06. Reviewed: production code at `9b2ea22` and the Step 11 device
report (`d337ca0`). Claude reviewed read-only, then added the one focused test below
with the developer's permission.

**Result: no concrete bug found.** One coverage gap was reproducible in tests and is
now closed. The remaining items are theoretical and documented below.

## Checklist

| Case | Outcome |
|---|---|
| Threshold boundary (0.249 rejected, 0.25 accepted) | Covered: close |
| Equal dx/dy (resolves horizontally) | Covered: close |
| Rapid press/release; press and release in one frame (mouse and touch) | Covered: close |
| Displaced release without a movement poll | Covered: close |
| Press outside, then drag inside | Covered: close |
| Press a tile, then drag outside; edges and corners | Covered in tests and on device: close |
| Release below threshold → tap on press-start cell | Covered: close |
| Held swipe through unlock; release during lock | Covered in tests and on device: close |
| Off-board-rejected swipe held until release | Covered in tests and on device: close |
| New touch right after unlock | Covered; one-frame window documented below |
| Mixed tap/swipe sequences | Covered: close |
| Invalid rollback; special swipe | Covered in tests and on device: close |
| Restart / reinitialize cleanup | Covered: close |
| Focus loss; pointer-device change | Covered; dropped same-frame press documented below |
| Dead-board recovery re-render | Covered in tests and on device (seed 93): close |
| Terminal outcome; disabled controller | Covered: close |
| Mouse; virtual touchscreen | Covered by Editor tests: close |
| Second finger touches during a primary touch | Covered: close |
| Second finger **moves** during a primary touch | Gap closed by the new test below |

## Closed gap: a moving second finger

`Update_AdditionalFingers_AreIgnoredUntilAllFingersLift` sent extra fingers only
as `Began`/`Ended`. The new
`Update_SecondFingerMoving_NeverDragsTheTrackedGestureIntoASwap` drags a second
finger across three cells:

- `false`: while the primary finger is still held;
- `true`: after the primary finger has lifted. That primary touch is "orphaned":
  still pressed, its position frozen.

In both cases the test expects no lock, no swap, no busy notification, and exactly
one tap on the primary's press-start cell once every finger has lifted. The test
can fail: a leaked position would read as a full-cell drag from the origin and
start a swap.

Why the code passes (Input System 1.20 `Touchscreen.cs`): only the finger that
began the primary touch writes `primaryTouch.position`. If it lifts while other
fingers remain, the primary touch stays pressed with its last position. It ends
only when the last finger lifts. No production change was needed.

Results: focused 5/5. Full PlayMode **233 passed, 3 explicit timing skips,
0 failed**. The Console showed only the declared negative-test exceptions and the
camera no-room warning.

## Unverified hypothesis: the Step 10 automated first-tap miss

In 1.20, the **first** query of `ButtonControl.wasPressedThisFrame` on a control
compares the current value with the previous frame's value. A press that spans
frames is detected. A press **and** release inside the same input update is not.
After that first query the control tracks both edges, including same-update taps.
`OnEnable` primes only the pointer that is `Pointer.current` at that moment.

If the touch control was not primed when the first tap arrived, an ADB
`input tap` would be missed, because its down and up land in one update. A human
tap spans several frames, so it would work. That matches the evidence: one missed
automated first tap, five human first taps that worked on cold launch, and later
automated taps that worked.

This explains the pattern; it is not a reproduced cause. Why the control might be
unprimed on that launch is not established. For players it matters only if a
complete tap fits inside one long frame. No production change is proposed. To test
the hypothesis later: on cold launch, send about five first taps by ADB and see
whether only automated first taps miss.

## Theoretical items (documented, no change)

- **A press in the exact frame of unlock is ignored.** `Update` runs before the
  coroutine that unlocks, so that press counts as begun while busy. This matches
  the busy rule, and the window is one frame.
- **A pointer-device change mid-gesture drops a same-frame press on the new
  pointer.** `Update` clears the gesture and returns before reading the new
  pointer's press. This needs mouse and touch at the same time, so it is not a
  factor on the S23.
- **`SetActive(false)` on the controller's GameObject during a move stops
  `FinishSwap`, leaving input locked.** Disabling only the component is safe,
  and the bootstrap uses `enabled = false`. No production code deactivates the
  GameObject. Step 14 should mention this next to the existing `OnDisable`
  comment's guarantee.

## Device report review

The Step 11 report keeps ADB and human evidence separate, calls sampled frames
sampled, credits exact recovery ordering to the PlayMode test, and reports log
errors without claiming causes. No unsupported claim was found. The device serial
number was removed from the Step 9 and Step 11 records.

Step 11 had no two-finger check, so the developer ran one by hand on the S23:
hold one finger on a tile, drag a second finger across the board, lift both.
Expected: at most a selection of the first tile, and no swap. Developer's result:
"the first tile is selected nothing swapped". This is one human check on one
device, matching the new automated test; it is not a multi-touch guarantee.
