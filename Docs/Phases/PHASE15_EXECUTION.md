# Phase 15 Execution Plan — Mobile Interaction Polish

Phase 15 makes the existing Match-3 interaction feel natural and understandable on a phone.

The two release requirements are already approved:

```text
Directional swipe input
Selection feedback
```

This phase does not change Match-3 rules.

Architecture remains:

```text
pointer / touch gesture
        ↓
swap intent: first GridPosition + adjacent second GridPosition
        ↓
existing BoardInputController swap path
        ↓
existing Core validation / resolution
```

Core must not know what a swipe is.

Presentation-dependent visual polish beyond the interaction cue belongs to Phase 18.

Phase roles:

```text
Codex → developer
Claude → reviewer/challenger
```

Claude reviews and challenges the work; it must not edit production files or this execution plan unless explicitly asked.

---

# Phase 14 Handoff

Phase 14 established:

- starting boards are guaranteed playable;
- runtime dead boards recover deterministically;
- dead-board recovery finishes before input unlocks;
- startup configuration is validated before gameplay begins;
- no normal-play trigger for `LockedAfterException` was identified under the tested configuration;
- no general exception-recovery framework was added.

Phase 15 must preserve all of that behavior.

In particular:

```text
busy / locked
→ no new gesture may begin a move

terminal Win/Loss
→ no board gesture may begin a move

dead-board recovery
→ completes before gesture input becomes available again
```

---

# Goals

By the end of Phase 15:

1. a player can swap adjacent tiles with a natural directional swipe;
2. two-tap selection/swap works on release, including the same-tile tap-to-cancel adjustment approved during Step 10;
3. swipe and two-tap both feed the same existing swap path;
4. the currently selected tile has a clear but small visual cue;
5. selection feedback does not change gameplay rules;
6. invalid second taps remain understandable;
7. swipe threshold and feel are validated with a real finger on the Galaxy S23;
8. edge/corner cells behave correctly;
9. invalid-swap rollback still works;
10. input stays blocked during swap/resolution/recovery;
11. no new Core dependency or generic gesture framework is introduced.

---

# Explicit Non-Goals

Do not add:

- Core swipe logic;
- a generic gesture-recognition framework;
- diagonal swaps;
- multi-touch gameplay;
- drag-the-gem-following-the-finger visuals;
- swipe trails;
- haptics unless separately approved after device testing;
- selection-rule changes beyond the same-tile tap-to-cancel adjustment approved during Step 10;
- hint systems;
- hover states;
- keyboard/gamepad interaction;
- menu navigation;
- Android Back behavior;
- level flow;
- persistence;
- accessibility art changes;
- tall-screen layout polish;
- outcome-screen polish;
- animation-framework refactors;
- unrelated input abstractions.

Phase 15 is:

```text
gesture → existing swap intent
+
small selection cue
```

---

# Architecture Rules

## 1. Core does not know about swipe

A swipe is Presentation input interpretation.

Do not add gesture/pointer concepts to `Match3.Core`.

## 2. One swap execution path

Two-tap and swipe must converge before gameplay mutation.

Preferred shape:

```text
two-tap
    ↓
first + second
    ↓
TryStartSwap(first, second)

swipe
    ↓
start position + direction
    ↓
adjacent second position
    ↓
TryStartSwap(first, second)
```

Do not duplicate:

- input locking;
- `Board.TrySwapAdjacent`;
- `SwapValidator.Analyze`;
- busy callbacks;
- coroutine start;
- rollback behavior.

## 3. Selection is controller state

`BoardInputController` owns which logical position is selected.

`TileView` may display selected/unselected state, but must not decide selection rules.

## 4. Feedback is presentation-only

Selection feedback must not alter:

- TileType;
- Board state;
- collider size;
- logical GridPosition;
- Core behavior.

The current `TileView`, `SpriteRenderer`, and full-cell collider share the same root. Do not scale that root for selection: it would resize the picking footprint. Prefer a child selection-highlight visual, off by default, toggled through a narrow Presentation API. Keep the existing tile renderer, tint, alpha, and collider unchanged; do not build an animation/material framework for the cue.

Remove the visual selection cue before swap, special, or clear animation begins. Logical `selectedPosition` may retain its existing lifetime until move completion, but the cue must not compound with scale-based effects.

## 5. Swipe threshold is a device-feel decision

Do not choose the final threshold only from theory.

Use a serialized fraction such as `swipeThresholdFraction` of one tile's on-screen center-to-center distance, rather than an S23-specific pixel count. Measure the reference spacing when the press begins and keep that threshold for the gesture. The exact calculation is defined in Step 3; the final fraction requires human-finger testing.

## 6. Dominant cardinal direction

For displacement from the initial press, use:

```text
dominantDistance = max(abs(dx), abs(dy))
threshold crossed when dominantDistance >= threshold

abs(dx) >= abs(dy) → horizontal
otherwise         → vertical
```

Resolve sign to left/right/up/down.

No diagonal swap.

An exact tie resolves horizontally.

## 7. One swipe → at most one move attempt

A gesture starts only from a fresh `wasPressedThisFrame`. When it starts a swap attempt through the shared path, clear all active gesture state immediately, before animations run. Do not carry a consumed flag through the locked move: release may occur while Update exits early. After unlock, a held pointer cannot start another gesture without a new press, and release with no active gesture does nothing.

Only an off-board swipe, which starts no move, keeps local consumed-until-release state. It cannot retry or become a tap on release.

## 8. Busy/terminal rules stay authoritative

Swipe must obey the same existing guards:

- `isInputLocked`;
- disabled controller after Win/Loss;
- uninitialized board.

Cancel gesture state on Initialize / Restart, controller disable / terminal outcome, application focus loss, or a change of active pointer device. A held press must not re-arm a cancelled gesture.

A press that begins while locked is ignored completely, even if the board unlocks before release. A new gesture requires a new press after unlock. Keep this lifecycle state local to `BoardInputController`; no gesture framework.

---

# Step 1 — Phase Entry and Baseline

## Goal

Confirm the current input behavior and clean starting state.

Inspect:

- `git status`;
- current HEAD / relation to origin;
- `PLAN.md` Phase 15 only;
- `AGENTS.md`;
- `BoardInputController`;
- `BoardView`;
- `TileView`;
- current input/touch PlayMode tests;
- `SelectionFeedback.md`;
- Phase 13 findings for Swipe / SelectionFeedback / HumanTouchComfort.

Record:

- current test baseline;
- current two-tap behavior;
- current drag behavior;
- current selection behavior;
- current S23 touch-size evidence;
- expected Console negative-test output.

Record the current press-based behavior as the baseline:

- first press selects one tile;
- non-adjacent second tap keeps the original selection;
- adjacent second press attempts a swap;
- invalid adjacent swap animates forward then rolls back;
- selection clears after attempted swap completes;
- outside/empty/foreign views are ignored;
- input while locked is ignored;
- dragging currently does not swap;
- Win/Loss disables board input.

Phase 15 intentionally changes input timing: a classified first tap selects on release, and an adjacent second tap starts its swap on release. Dragging gains the approved swipe path. Preserve the remaining selection, validation, rollback, and locking rules.

Steps 1–3 are proposal/design only. Do not edit production code, tests, prefabs, scenes, or settings during this first review boundary.

STOP if unrelated working-tree changes exist.

---

# Step 2 — Propose One Swap-Intent Path

## Goal

Inspect the controller and propose the smallest refactor so two-tap and swipe reuse the same attempt logic. Do not implement the refactor yet.

Preferred local helper:

```csharp
private bool TryStartSwap(GridPosition first, GridPosition second)
```

or a similarly narrow name.

It should own the shared sequence:

```text
reject out-of-bounds first/second before locking → false
→ validate duration
→ lock
→ TrySwapAdjacent (false → unlock, preserve old selection/cue, return false)
→ Analyze
→ commit selectedPosition = first, remove visible selection cue, clear active gesture state
→ busy true
→ StartCoroutine(FinishSwap)
→ return true
```

`Board.TrySwapAdjacent` throws for out-of-bounds positions; it does not return false for them. The shared helper must check `board.IsInBounds` for both positions before setting `isInputLocked`, even though swipe interpretation also rejects off-board neighbours. An outward swipe must never throw after acquiring the lock.

Return true when an animated swap attempt starts, including an invalid-match swap that will animate forward and roll back. Return false for a request that starts no attempt. Rejection must preserve the previous logical selection and visible cue, leave input unlocked, and send no busy notification.

Commit the new logical selection and clear the cue/gesture inside the accepted shared path, before busy callbacks and animation. Do not make those changes in the caller after the method returns: callbacks and coroutine startup must already observe the correct state. This is start/rejection ordering, not a new exception-recovery mechanism.

After tap classification, preserve two-tap selection/swap rules except for the Step 10 approved same-tile tap-to-cancel adjustment. Model B deliberately moves selection and second-tap execution from press to release.

Important existing behavior:

```text
non-adjacent second tap
→ TrySwapAdjacent false
→ unlock
→ retain original selection
```

Preserve this.

Propose focused tests for later implementation:

- first tap selection on release;
- adjacent second tap starts the swap on release;
- non-adjacent second tap retains selection;
- valid adjacent attempt;
- invalid adjacent rollback;
- locked input;
- terminal disabled input;
- busy callback ordering, including the committed selection and cleared cue/gesture;
- out-of-bounds neighbours rejected before locking, with no exception or busy notification;
- rejected attempts preserve the old selection/cue; invalid-match animated attempts return true.

Do not edit or commit. Production edits, including this refactor, begin only after the Steps 1–3 review is approved.

---

# Step 3 — Define Swipe Semantics

## Goal

Agree on gesture state and threshold before implementation.

Preferred gesture lifecycle:

```text
fresh wasPressedThisFrame on a valid tile
→ remember start screen position
→ remember start GridPosition and active pointer device
→ measure cell-center screen spacing and store this gesture's pixel threshold
→ do not execute tap or change selection yet
→ while pressed:
      measure displacement
      if threshold crossed and gesture is not off-board-consumed:
          choose dominant cardinal direction
          derive adjacent GridPosition
          if neighbour is out of bounds:
              retain old selection; consume until release
          otherwise:
              request shared swap path with gesture start + neighbour
              if started:
                  shared path has already committed selection, hidden cue, and cleared gesture
              otherwise:
                  retain old selection; consume until release
→ release:
      if no active gesture:
          do nothing
      otherwise if gesture is not off-board-consumed:
          inspect final pointer position and displacement
          if threshold crossed:
              resolve swipe intent with the same rules as while held
          otherwise:
              process the remembered start cell as the existing tap interaction
      clear gesture state
```

Do not track a gesture if the initial press did not begin on a valid tile. A cancelled or off-board-consumed gesture must not produce a tap on release. Starting any animated swap attempt, including one that will roll back, clears gesture state in the shared path. No consumed swipe flag survives the locked move.

## Two-tap coexistence

### Model A — select on press (not adopted)

```text
press tile
→ select immediately
→ if drag later crosses threshold:
      swipe attempt
```

Pros:
- preserves current timing;
- smallest change.

Conflict:
- if A is already selected, pressing adjacent B immediately attempts A ↔ B and locks input, preventing an intended swipe from B toward C;
- the cue may also flash briefly before a swipe.

### Model B — classify tap vs swipe before committing the tap (preferred)

```text
press
→ wait
→ release below threshold = tap using the remembered start cell
→ threshold crossed = swipe
```

Pros:
- cleaner tap/swipe separation.

Costs:
- larger rewrite;
- changes existing timing/tests.

Use **Model B**. The first-tile selection appears on tap release, and a second-tap adjacent swap also starts on release. A swipe starts from its own pressed tile, regardless of any retained selection, without first executing that selection's two-tap swap. Once classified as a tap, the existing selection/swap rules apply.

This timing change is intentional. Do not invent a hybrid press/release model unless implementation evidence identifies a concrete problem with Model B; bring that problem back for review.

### Deliberate timing deviation from PLAN.md

`PLAN.md` says to keep two-tap initially unless device testing shows a conflict. Two-tap remains supported, but its commitment moves from press to release before device testing because design review found this deterministic conflict:

```text
existing selection A
→ press adjacent B to begin a swipe from B
→ press-first handling immediately starts A ↔ B
```

This is an interaction-order conflict, not a subjective touch-feel finding. Model B resolves it without removing two-tap or changing its classified-tap selection rules.

### Swipe with an existing selection

For an in-bounds swipe from B toward adjacent C, any old tap selection A must not force A ↔ B. At threshold crossing, request B ↔ C through the shared path. Once the attempt is accepted, that helper makes `selectedPosition` B, removes A's visible cue, and clears active gesture state before busy callbacks or animation. Do not briefly show B's cue just to remove it. Logical B remains selected until the attempt finishes, then clears normally.

For an off-board swipe from B, no move starts: preserve old selection A and its cue, and consume only that gesture until release. If the shared helper rejects a swipe request with false, use this same no-start behavior; do not lose A or leave a retryable gesture.

### Release-frame handling

Do not inspect movement only while `pointer.press.isPressed`. On `wasReleasedThisFrame`, a release without an active gesture does nothing. Otherwise inspect the final pointer position before clearing the gesture:

1. calculate displacement from the press origin;
2. if not off-board-consumed and the threshold is reached, classify as a swipe;
3. if not off-board-consumed and below threshold, classify as a tap;
4. then clear gesture state.

This must handle press → large displaced release with no intermediate movement poll, without producing both a swipe and a tap.

### Same-frame press and release

Define and test press + release arriving in the same frame: a valid below-threshold gesture produces exactly one tap, and an already-selected adjacent tap starts at most one swap attempt. Specify how the polling path supplies press origin and final release position when both flags occur in that frame; do not silently lose or duplicate the gesture.

The Phase 13 observation that particular ADB taps did not register does not establish production behavior or justify omitting this case. Verify it with Input System events and include very quick human taps in S23 testing.

## Threshold

Start with a fraction of one tile's on-screen center-to-center distance.

Use a serialized field such as `swipeThresholdFraction`. At the start of each permitted press, project the start cell center `(x, y)` and the virtual adjacent grid center `(x + 1, y)` through the existing input camera, measure their screen-pixel spacing, and calculate the threshold once for that gesture. This accounts for responsive camera framing and safe-area layout without hard-coded S23 pixels, edge branching, or per-frame threshold recalculation.

Using this horizontal spacing for both swipe axes assumes square cells on screen, as in the current board with one `tileSpacing` and uniform root scale.

Propose the smallest read-only Presentation seam needed, for example:

```csharp
BoardView.GetCellWorldPosition(GridPosition position)
```

It should reuse BoardView's existing arithmetic cell-to-world calculation and return a position value, without exposing mutable view collections or other internals. Document that this conversion accepts virtual grid coordinates outside the Board: it reads no tile and does not make those coordinates valid gameplay cells. That also permits spacing measurement without a real neighbour on a 1×1 board; it does not promise that such a board can start a playable level. Gameplay bounds checks remain in the shared swap path. Do not add Core gesture logic.

Define the screen-space math as:

```text
threshold = thresholdFraction × tileCenterToCenterPixels
dx = currentX - startX
dy = currentY - startY
dominantDistance = max(abs(dx), abs(dy))

dominantDistance >= threshold → swipe

abs(dx) >= abs(dy) → horizontal, sign(dx) gives left/right
otherwise         → vertical, sign(dy) gives down/up
```

An exact threshold boundary counts as a swipe; an exact directional tie resolves horizontally. Use the same math while held and on release.

Candidate exploration range:

```text
0.20–0.35 tile
```

This is only a tuning range, not a requirement.

## Off-board swipe

If the chosen direction points outside the board:

```text
no move attempt
```

Keep any existing selection, matching current ignored/non-adjacent behavior. The gesture is still consumed, so changing direction while held or releasing cannot turn it into a later move or tap.

## Gesture lifetime around locking

Use these local rules:

- start a gesture only from a fresh `wasPressedThisFrame` while input is available;
- when the shared path starts an animated swap attempt, clear all active gesture state immediately;
- do not keep a consumed swipe flag during the lock;
- a release during animation may be missed safely because there is no active gesture left;
- a still-held pointer after unlock cannot re-arm a gesture without a new press;
- release with no active gesture does nothing;
- an off-board swipe starts no move, so only that gesture stays consumed until release and cannot retry or become a tap.

## Cancellation and busy presses

Cancel gesture state on:

- Initialize / Restart;
- controller disable / terminal Win or Loss;
- application focus loss;
- active pointer device changing during the gesture.

Cancellation discards the pending tap/swipe. Do not synthesize a new gesture from an already-held finger/button when focus, controller availability, or the pointer device changes.

If a press begins while input is locked, ignore it completely. Unlocking while it remains held must not arm a gesture; require a new press after unlock.

These are local lifecycle rules in `BoardInputController`, not a generic gesture system.

STOP for review before implementation.

---

# Step 4 — Implement Gesture Tracking

## Goal

Add the minimum pointer state needed inside `BoardInputController`.

Only after approval of Steps 1–3, implement the proposed shared swap-intent refactor as a small slice, then add Model B gesture tracking. Keep both paths converging before gameplay mutation.

`Update()` may need:

- `wasPressedThisFrame`;
- current pressed state;
- `wasReleasedThisFrame`;
- pointer position.

Prefer explicit narrow methods if needed:

```csharp
HandlePointerDown(Vector2 screenPosition)
HandlePointerMove(Vector2 screenPosition)
HandlePointerUp(Vector2 screenPosition)
```

Do not add a generic gesture manager.

## Model B test seam and migration

`HandlePointerDown` now begins a gesture; it no longer represents a complete click. Add the matching release entry point, such as `HandlePointerUp(Vector2 screenPosition)`, and a narrow movement handler as needed. Production polling and direct-handler tests must use the same gesture logic.

Existing `Click(...)` helpers become conceptually:

```text
HandlePointerDown(position)
HandlePointerUp(position)
```

Update the affected tests during implementation:

- `BoardInputControllerTests`: move selection assertions from press to release; preserve assertions that press alone does not select or swap. Replace/rework `Update_TouchPress_SelectsOnce_AndDraggingDoesNotSwap` because intentional dragging now produces a swipe.
- `BoardBootstrapTests`: update the seed-93 route and other tap helpers to send down + up; keep the existing route, HUD, recovery, restart, and determinism expectations.
- `SceneTouchInputTests`: send complete Input System press/release gestures and assert selection/swap timing after release. Keep scene tests exercising real device polling rather than replacing them with direct-handler calls.

These timing/drag changes are intentional Model B behavior, not regressions. Retain separate polling tests for mouse and virtual touchscreen, including same-frame press/release, so changing helpers does not hide a production input defect.

## Picking rule

Only the initial press raycasts/picks a tile.

The second logical cell comes from:

```text
start GridPosition + cardinal direction
```

not from the finger endpoint tile.

This keeps quick swipes reliable.

## Tests

At minimum:

- below threshold → no swipe, then release processes one tap;
- press → displaced release crosses threshold with no intermediate movement poll;
- same-frame press + release produces exactly one tap or intended attempt, with no dropped/duplicate action;
- just below / exactly at threshold;
- right/left/up/down;
- horizontal-dominant diagonal-like movement;
- vertical-dominant movement;
- exact dx/dy tie resolves horizontally;
- outward swipes from every edge and corner → no move, no exception, no lock, and no busy notification; old selection/cue retained;
- shared-helper bounds rejection and false return leave selection/cue unchanged; accepted invalid-match attempts still return true;
- starting a swap attempt clears all active gesture state; a release during lock cannot leave stale state;
- held pointer after unlock cannot trigger again, and release with no active gesture does nothing;
- off-board-consumed gesture cannot retry or become a tap before release;
- start outside board → nothing;
- start on Empty → nothing;
- press begun while locked stays ignored after unlock until release + new press;
- ignored when disabled;
- Initialize / Restart cancels pending gesture;
- disable / terminal outcome cancels pending gesture;
- focus loss cancels pending gesture;
- pointer-device change cancels pending gesture;
- cancelled held input does not re-arm without a fresh press;
- press-time threshold measurement follows changed camera framing while remaining fixed within one gesture.

---

# Step 5 — Route Swipe Into Existing Swap Logic

## Goal

Prove swipe uses the same gameplay path.

Required:

```text
gesture → adjacent second cell
→ same TryStartSwap(first, second)
→ same SwapValidator
→ same animations/resolution
```

Scenarios:

### Valid swipe
- one move begins;
- locks;
- clears all active gesture state immediately, without retaining a consumed flag during the move;
- resolves;
- consumes exactly one move;
- unlocks.

### Invalid swipe
- forward animation;
- invalid analysis;
- rollback;
- no move consumed.

Starting the invalid-match attempt also clears gesture state; its animation/rollback uses the same path as two-tap.

### Special swipe
Uses existing special semantics with no gesture-specific special code.

### Recovery interaction
If a swipe leads to a dead board later, recovery still completes before unlock.

---

# Step 6 — Preserve Two-Tap

Required tests:

- first press does not select; release below threshold selects;
- adjacent second press does not attempt a move; its release below threshold starts the swap;
- a second classified tap on the selected tile clears selection and cue on release, without a move or busy notification (approved during Step 10);
- non-adjacent tap retains original selection;
- outside tap keeps selection;
- valid/invalid attempted swap clears selection after completion;
- mouse click still works;
- touch tap still works;
- swipe does not accidentally generate an extra tap move;
- accepted swipe B toward C commits B inside the shared path before busy callbacks/animation; a rejected request keeps A;
- off-board swipe from B retains old selection A and its cue until another classified interaction changes it.

Device ambiguity checks:

```text
tap tile → swipe same tile
tap tile → swipe different tile
tap A → press adjacent B → swipe B toward C (no premature A ↔ B attempt)
swipe → tap
```

Cover these sequences in focused tests before the device review. Model B resolves the known adjacent-press ambiguity; report any additional observed ambiguity before changing the agreed semantics.

---

# Step 7 — Selection Feedback Design

## Goal

Make `selectedPosition` visible without changing selection semantics.

Ownership:

```text
BoardInputController
→ decides selected GridPosition

BoardView / TileView
→ displays selected/unselected
```

Use a child selection-highlight visual after swipe behavior is stable. This is the preferred direction; the small outline/glow asset and its exact appearance remain a design decision gate before Step 8 implementation.

```text
TileView root
├── existing SpriteRenderer + full-cell collider
└── child selection highlight visual (off by default)
```

The child is toggled through a narrow Presentation API such as `TileView.SetSelected(bool)`. It owns no selection rules and never changes TileType, collider size, or picking footprint.

Do not treat root scaling, tint, and alpha as equally open options: root scaling resizes the collider, tint conflicts with the untinted-sprite rule, and alpha can read as disabled. Existing clear/special feedback already uses the root renderer/scale. Keep that behavior intact and use the separate child for selection.

Remove the visual selection cue before swap, special, or clear animation begins, so the existing scale animations snapshot the normal visual state. Logical `selectedPosition` may retain its existing lifetime until move completion.

Do not add an animation/material framework or shader system merely to support selection feedback.

Preferred API:

```csharp
TileView.SetSelected(bool selected)
```

BoardView may provide a narrow position-based facade if needed to clear the old child's cue reliably; it must not decide selection rules. Do not introduce an API framework.

Selection transitions to cover:

1. first tile selected on tap release;
2. invalid/non-adjacent second tap retains selection;
3. swap starts (visual cue already removed before animation);
4. swap finishes;
5. Restart;
6. Win/Loss;
7. dead-board re-render;
8. swipe consumes selection.

STOP for cue-design review before implementation.

---

# Step 8 — Selection Feedback Implementation

After the cue-design review, select or create the small outline/glow asset and add the child highlight visual. Do not add the asset or change the prefab before this step is approved.

Use artwork made for this project or an asset with a compatible licence. Record its origin and any required licence/attribution information in `Docs/Art/README.md` when the asset is introduced; do not copy artwork from another game.

- leave the highlight off by default;
- toggle it through the narrow Presentation API;
- keep root scale, root renderer tint/alpha, collider size, and TileType unchanged by selection;
- remove the cue before swap / special / clear animation begins;
- verify swap/fall/refill and clear/special feedback keep their existing behavior;
- do not create a shader/material/animation framework.

Tests:

- first selection on tap release shows cue;
- highlight child is off by default and toggling it leaves the tile's type, root scale, tint, and alpha unchanged;
- retained selection keeps cue;
- selecting/deselecting never changes collider size or its world-space picking footprint;
- cue is gone before the first swap animation frame and before special/clear animation snapshots;
- clear removes cue;
- move completion clears cue;
- Restart clears cue;
- re-render leaves no stale cue;
- outcome leaves no misleading active cue;
- repeated select/clear restores exact normal visual state.

---

# Step 9 — Threshold Tuning on Galaxy S23

Human finger testing is mandatory.

ADB presses do not satisfy this step.

Test at least three candidate fractions by changing the serialized `swipeThresholdFraction` and producing/installing the corresponding Android development build for each. Do not add an in-game debug UI.

Try:

- deliberate tap;
- very quick human tap;
- tap with small finger jitter;
- short swipe;
- fast swipe;
- slow swipe;
- diagonal-ish swipe;
- edge/corner cells;
- one-handed use.

Evaluate:

```text
too low → accidental swipes
too high → swipes feel unresponsive
```

Record:

- representation;
- candidates;
- corresponding build identifiers and human-finger result for each candidate;
- chosen value;
- why;
- any relation to on-screen tile size.

Do not claim universal handset perfection.

### Step 9 result — 2026-11-05

The developer compared 0.20, 0.25 and 0.35 with a real finger on the S23, one development build each. They noticed no difference. At about 120 px per cell, those fractions are about 24, 30 and 42 px. All three sit between tap jitter and a deliberate swipe. 0.25 is kept as the existing default. This is a conservative choice, not a measured optimum, and it was made on one device. Details are in [the Step 9 threshold record](../Validation/PHASE15_STEP9_THRESHOLD_TUNING.md).

---

# Step 10 — Human Touch Comfort Review

Close the Phase 13 HumanTouchComfort validation item on the S23.

Use real fingers for:

- center cells;
- edges;
- corners;
- repeated two-tap;
- very quick human tap;
- repeated swipe;
- invalid swap;
- fast next gesture after unlock;
- Restart reachability;
- one-handed portrait use.

If discomfort is observed, identify the concrete failure before changing layout.

Do not redesign the whole board merely because generic Android guidance says 48 dp.

### Approved adjustment from human review — 2026-11-06

The developer found that an accidental first selection could not be cancelled. A classified tap on the already-selected tile now cancels selection on release, hides the cue, and spends no move. Press/hold alone does not cancel. Non-adjacent/outside taps and rejected swipes still retain selection; a swipe from the selected tile still follows the normal swipe path. The controller owns this rule, with no Core, prefab or layout change.

Restart confirmation was also requested but explicitly deferred to a later step; it is recorded in `Docs/KnownIssues/RestartConfirmation.md` and is not implemented in this adjustment. The developer's S23 finger review confirmed same-tile cancellation works, Nova's gold corners and white selection cue are clearly different, and the first finger tap after relaunch works. Step 10 is complete with these qualitative, single-device limits; the initial automated tap miss remains documented without a claimed root-cause fix. Evidence and build details are in `Docs/Validation/PHASE15_STEP10_HUMAN_TOUCH_COMFORT.md`. The later Step 11 results are recorded below.

---

# Step 11 — Device Regression Matrix

On the S23 verify:

### Swipe
- four directions;
- edge/corner rejection;
- valid move;
- invalid rollback;
- special swap;
- one gesture → one attempt.

### Two-tap
- select on release;
- adjacent move starts on release;
- tapping the selected tile again cancels on release without spending a move;
- retained selection.

### First finger tap after cold launch

Repeat about five cold launches on the S23. Fully close and reopen the game, wait for the board to be ready, then use a real finger for the first tile tap without a warm-up gesture. Record each result separately; where ADB is available, a changed app process ID can support that a fresh process was launched. ADB taps do not substitute for these finger checks. This follows the single automated no-cue observation from Step 10; that observation's cause is unresolved, while the developer's one finger relaunch check worked. Completed on 2026-11-06: five distinct cold launches with changed process IDs, no ADB touches before the human tap, and five reports of "First tap worked". This small sample does not establish a root cause or a universal guarantee.

### Selection cue
- visible;
- clears correctly;
- not confused with other effects;
- absent before move animations begin;
- collider/picking footprint unchanged.

### Locking
Extra gestures during:
- swap;
- rollback;
- cascade;
- special;
- dead-board recovery

must do nothing.

A press begun while busy must remain ignored if held after unlock. A swipe that starts a move has already cleared all gesture state; holding through unlock or releasing afterward must not trigger another move or tap. An off-board swipe remains locally consumed until release.

### Outcome
On Win/Loss:
- gestures do nothing;
- no stale cue suggests interaction remains available.

---

### Step 11 result — 2026-11-06

The S23 functional matrix passed: native Android swipes/taps, release timing, cancellation/retention, held-input rejection, special chains, Win/Loss and Play Again. A temporary seed-93 scene built with the current input code also passed recovery, continued play and Restart. Exact synchronous recovery ordering remains supported by the existing PlayMode callback test; device input was held across that boundary rather than injected inside it. The normal seed-42 APK and clean Windows64 Editor state were restored. No production files changed. Results, evidence paths, Android log diagnostics and validation limits are in [the Step 11 device report](../Validation/PHASE15_STEP11_DEVICE_REGRESSION.md). Stop for review; Step 12 has not started.

---

# Step 12 — Claude Adversarial Review

Codex remains main developer.

Claude is read-only reviewer/challenger and must not edit production files or this execution plan unless explicitly asked.

Ask it to challenge:

- threshold boundary;
- equal dx/dy;
- rapid press/release;
- press + release in the same frame;
- displaced release without an intermediate movement poll;
- press outside then drag inside;
- press tile then drag outside;
- release below threshold;
- held touch after a swipe starts a move and clears gesture state;
- release during lock, then unlock without stale gesture state;
- off-board-consumed swipe held until release;
- new touch immediately after unlock;
- mixed tap/swipe sequences;
- invalid rollback;
- special swipe;
- Restart/reinitialize cleanup;
- focus loss and pointer-device change cancellation;
- dead-board recovery re-render;
- terminal outcome;
- disabled controller;
- mouse behavior;
- virtual touchscreen behavior;
- primary touch held while a second finger touches/moves: no unintended swipe or tracked-position jump into a swap.

The second-finger case validates robustness of the existing primary-pointer model; it does not add multi-touch gameplay.

For each:

```text
covered → close
reproducible gap → focused test/fix
theoretical only → document
```

No generic gesture/multi-touch framework.

### Step 12 result — 2026-11-06

No concrete bug was found. The one reproducible gap was a second finger moving during a primary touch. It is now covered by a focused PlayMode test, with no production change; full PlayMode passed 233 with 3 explicit timing skips. Three theoretical edges and an unverified hypothesis for Step 10's automated first-tap miss are documented. The developer's two-finger check on the S23 matched: the first tile was selected and nothing swapped. Details are in [the Step 12 review record](../Validation/PHASE15_STEP12_ADVERSARIAL_REVIEW.md). Step 13 has not started.

---

# Step 13 — Full Regression Validation

Run:

- full EditMode;
- full PlayMode;
- all Phase 15 focused tests.

Confirm unchanged:

- Core gameplay;
- deterministic restart;
- dead-board recovery;
- special chains;
- invalid rollback;
- Win/Loss;
- busy/restart behavior;
- Board/View consistency.

Input-specific:

```text
mouse two-click
touch two-tap
touch swipe
```

must all reach the intended controller path with no duplicate moves.

Console must contain only declared expected messages.

### Step 13 result — 2026-11-06

Passed. EditMode 603/603; PlayMode 233 passed, 0 failed, 3 explicit frame-rate skips. The Console held only the five messages tests declare with `LogAssert.Expect`, plus test-runner housekeeping. Mouse two-click, touch two-tap and touch swipe each reach the controller's real `Update` path with exactly one attempt. The mouse test now asserts this too (`e8b2b95`, test-only). Production code is unchanged since `07f663c`. Details are in [the Step 13 regression record](../Validation/PHASE15_STEP13_FULL_REGRESSION.md). Step 14 has not started.

---

# Step 14 — Documentation

After validation:

## `SelectionFeedback.md`
If resolved:
- mark resolved;
- describe cue;
- explain the same-tile cancellation approved during Step 10 and confirm the other selection rules remain unchanged;
- mention S23 validation.

Record the human-finger S23 result in the appropriate Phase documentation.

Do not claim all-device comfort.

### Step 14 result — 2026-11-06

- `SelectionFeedback.md` is marked resolved. It describes the cue, the release-time tap commit, the approved same-tile cancellation and the unchanged rules, with S23 human and device evidence.
- `MobileLayoutAndPicking.md` records the S23 human comfort review; other devices remain unverified.
- `LockedAfterException.md` names `TryStartSwap` as the lock site and adds the Step 12 warning about deactivating the controller mid-move.
- The Step 12 first-tap hypothesis was corrected: `Update` also queries the pointer each frame.
- The human-finger S23 results are recorded in the Step 9 and Step 10 results above.

No code changed. Step 15 has not started.

---

# Step 15 — Learning Review

Notebook notes should stay short.

Suggested concepts:

```text
BoardInputController
- interprets tap/swipe
- owns selectedPosition
- converts gesture to adjacent swap intent

BoardView / TileView
- only display selected state

Swipe
- Presentation only
- dominant cardinal direction
- same existing swap path
```

No test counts/tuning logs in notebook.

---

# Step 16 — Completion Gate

Phase 15 completes only when:

- [ ] swipe semantics defined;
- [ ] threshold representation documented;
- [ ] screen-space cell spacing measured at press time through a read-only Presentation seam;
- [ ] dominant-axis threshold uses >=, with horizontal direction on exact dx/dy ties;
- [ ] dominant direction deterministic;
- [ ] one swipe triggers at most one attempt;
- [ ] swipe routes into existing swap path;
- [ ] shared helper rejects out-of-bounds positions before locking and reports whether an animated attempt started;
- [ ] rejected attempts preserve selection/cue; accepted attempts commit selection and clear cue/gesture before callbacks/animation;
- [ ] Core has no swipe dependency;
- [ ] valid swipe works;
- [ ] invalid swipe uses existing rollback;
- [ ] special swipe uses existing rules;
- [ ] two-tap still works;
- [ ] first selection and adjacent second-tap swap commit on release;
- [ ] release-frame displacement can produce a swipe without an intermediate movement poll;
- [ ] same-frame press/release behavior defined and tested without relying on ADB observations;
- [ ] mouse still works;
- [ ] selection feedback exists;
- [ ] child highlight is off by default and leaves the root renderer/scale unchanged;
- [ ] highlight asset is original or compatibly licensed, with provenance recorded;
- [ ] same-tile tap cancels selection on release without a move; other selection rules remain unchanged;
- [ ] selection cue leaves collider/picking footprint unchanged;
- [ ] visual cue removed before swap/special/clear animation;
- [ ] cue clears after move/restart/outcome/re-render;
- [ ] busy/terminal states block swipe;
- [ ] move start clears gesture state; no consumed flag persists through lock; no-active-gesture release does nothing;
- [ ] busy-started presses and held pointers after unlock require a fresh press; only off-board gestures remain consumed until release;
- [ ] Initialize/Restart, disable/outcome, focus loss and pointer-device change cancel gestures;
- [ ] dead-board recovery remains compatible;
- [ ] S23 threshold tuned with a real finger using at least three serialized candidate fractions / development builds;
- [ ] S23 human touch comfort reviewed;
- [ ] edges/corners checked;
- [ ] second-finger robustness checked without adding multi-touch gameplay;
- [ ] Claude adversarial review complete;
- [ ] full EditMode passes;
- [ ] full PlayMode passes;
- [ ] Console has no unexplained errors/warnings;
- [ ] documentation updated;
- [ ] human reviewed final diff.

Do not start Phase 16 automatically.

---

# Git / Review Discipline

For each slice:

```text
implement
→ focused tests
→ regression
→ show diff
→ human approval
→ commit
```

Do not push unless explicitly requested.

No co-author / attribution trailers.

Possible commits:

```text
Route swipe gestures through existing swap input
Show selected tile feedback
Tune mobile swipe interaction
Complete Phase 15 mobile interaction polish
```

---

# Unity Learning Protocol

Before every Unity Editor / MCP action that changes or meaningfully exercises Editor state, explain:

1. what will be done;
2. why;
3. exact manual Unity Editor steps;
4. how to verify.

Afterward report:

- what changed;
- what was observed;
- test/Console result;
- temporary state restored.

Read-only code inspection does not need repetitive ceremony.

---

# First Developer Assignment

Codex should begin with **Steps 1–3, proposal/design only**:

```text
baseline
→ identify single existing swap-attempt path
→ propose minimal refactor
→ define swipe semantics
→ propose threshold representation
```

Do not implement the shared-method refactor, swipe, or selection feedback yet. Do not edit production code, tests, prefabs, scenes, or settings at this boundary. Production edits begin only after this review is approved.

First report should contain:

- clean baseline / current HEAD;
- current tap and drag behavior;
- proposed shared swap-intent method;
- Model B lifecycle, explicit release timing change, and the reason Model A was not adopted;
- exact gesture-state fields;
- dominant-direction/tie rule;
- threshold representation and initial candidate range;
- press-time cell-spacing measurement and the proposed read-only BoardView seam;
- off-board behavior;
- release-frame displacement handling and gesture cancellation/busy-press rules;
- immediate gesture cleanup when a move starts, with consumed-until-release state only for off-board swipes;
- same-frame press/release behavior and the Model B test-helper migration;
- how two-tap and swipe coexist;
- preferred child-highlight direction, with asset/prefab implementation deferred to the selection-feedback step;
- focused tests to add;
- architecture concerns.

Then STOP for Claude's review before production implementation. Claude reviews this revised plan before Codex begins Step 4.
