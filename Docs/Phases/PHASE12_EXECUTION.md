# Phase 12 Execution Plan — Mobile Pass

Phase 12 prepares the existing Match-3 game for real mobile use without redesigning gameplay systems that are already complete.

The focus is:

```text
input
layout
screen adaptation
safe areas
touch picking
mobile performance
frame-rate policy
Android validation
```

This is a **mobile-readiness phase**, not a gameplay-feature phase.

Do not use Phase 12 as an excuse to add:

```text
new match rules
new specials
special-combination bonuses
new level systems
economy
save/load
level select
new art direction
large UI redesign
generic input framework
generic performance framework
```

The game already has a working desktop/pointer path, deterministic Core logic, a functional HUD, special feedback, and a stable resolution loop.

Phase 12 should adapt those systems rather than replace them.

---

# Current Architecture Baseline

The important dependency direction remains:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Unity / Presentation
```

Relevant responsibilities:

```text
Board
→ authoritative logical board

BoardInputController
→ pointer/tile selection
→ screen position → camera → Physics2D → TileView → GridPosition
→ valid/invalid swap flow
→ input lock

BoardView
→ tile presentation
→ swap/fall/refill/clear/special animations

BoardBootstrap
→ composition root
→ creates the Board
→ seeds and creates RNG streams
→ wires input, view, resolution and HUD

LevelProgress
→ moves / score / goal / outcome

LevelHudView
→ HUD presentation only
```

Phase 12 must preserve those boundaries.

---

# Phase 12 Requirements

From the approved roadmap:

- touch input;
- portrait-oriented layout if appropriate;
- UI anchors;
- different screen aspect ratios;
- no keyboard requirement;
- avoid obviously wasteful allocations;
- avoid unnecessary physics;
- avoid unnecessary per-frame polling.

Potential build target:

```text
Android
```

Proposed Phase 12 Android development application identifier:

```text
com.tugaykapucu.match3
```

Use this Android-specific identifier during Phase 12. Do not invent additional release/store naming in this phase.

Building to a physical device is useful but not required for every implementation step.

---

# Phase 12 Design Principles

## 1. Mobile adaptation, not parallel architecture

Prefer evolving the current input/layout systems rather than introducing separate:

```text
DesktopInputController
MobileInputController
MobileBoardView
MobileHud
```

unless a real requirement proves the existing components cannot remain shared.

The final path should still be understandable as one game with adaptive presentation/input.

## 2. Touch should reuse the existing pointer semantics

The current gameplay interaction is already conceptually pointer-based:

```text
screen position
→ camera ray
→ tile collider
→ TileView
→ GridPosition
→ selection/swap
```

Touch input should feed into the same gameplay path where practical.

Do not create separate swap rules for touch.

## 3. Screen adaptation is Presentation responsibility

Board dimensions and gameplay rules remain logical.

Presentation decides:

```text
where the board appears
how large it is
where the HUD sits
how safe-area insets affect UI
```

Do not put aspect-ratio or safe-area logic into Core.

## 4. Measure before optimizing

Phase 12 includes performance review, but do not perform speculative refactors.

For each potential issue:

```text
observe / inspect
→ identify concrete cost
→ make smallest useful correction
```

Do not add pooling, caching layers or custom frameworks without measured need.

## 5. Mobile usability beats editor-only convenience

The target interaction must not require:

```text
keyboard
mouse hover
right click
Editor-only shortcuts
```

The player should be able to complete the game using touch only.

---

# Step 1 — Input Audit and Touch Support

## Goal

Understand exactly how the current `BoardInputController` receives input, then add the smallest explicit touch connection needed for mobile play.

Important current fact:

```text
BoardInputController.Update()
→ currently reads Mouse.current only
```

Therefore Phase 12 must not assume touch is already connected.

This step is:

```text
audit first
→ define touch semantics
→ implement the smallest shared mouse/touch path
```

Do not change gameplay swap rules.

## Inspect

At minimum:

- `BoardInputController`;
- Input System package usage;
- current mouse/pointer polling;
- selection state;
- screen-to-world conversion;
- `Physics2D` picking;
- `TileView` / collider assumptions;
- input locking;
- terminal-state disable behavior;
- PlayMode tests covering input.

## Questions to answer

1. How is `Mouse.current` currently polled?
2. What is the smallest way to read touchscreen input without creating separate gameplay rules?
3. Should mouse and touch share one internal `HandlePointerDown(screenPosition)` path?
4. Is any keyboard input required for normal gameplay?
5. How should touch pointer IDs interact with Unity UI?
6. How will additional simultaneous fingers be ignored?
7. How will duplicate mouse/touch processing be prevented on platforms that may synthesize mouse input?
8. What happens when input arrives while a move is resolving?
9. Does the current collider-based picking remain reliable after portrait resizing?

## Preferred outcome

Keep one gameplay pointer path:

```text
pointer press
→ screen position
→ HandlePointerDown(...)
→ tile picking / selection / swap
```

During the audit, explicitly evaluate Unity Input System `Pointer.current` as the preferred unification candidate. Mouse, Touchscreen and Pen are pointer devices; if the project's required primary-touch behavior is correct through `Pointer.current`, prefer that single read path over parallel mouse/touch branches.

Approved touch semantics for Phase 12:

```text
tap first tile
→ tap adjacent second tile
→ attempt swap
```

Do not add swipe-to-swap in this phase.

Additional simultaneous fingers are ignored.

Input received while the board is busy is discarded.

Prevent duplicate processing of one physical interaction.

### UI blocking policy

Do not prescribe `EventSystem.IsPointerOverGameObject()` before testing it with the current Input System UI module.

Prefer first to verify the existing protections:

- interactive UI is laid out outside the board's pickable region;
- terminal-state board input is disabled;
- tests confirm Restart / Play Again interaction does not select a tile underneath.

Only add an explicit UI-over-pointer guard if a real overlap/leak is reproduced and the chosen API is verified for the project's Input System timing.

Do not add a second touch-specific gameplay controller.

## Tests

Add focused tests only for mobile-specific input behavior that is not already covered.

Required cases where practical:

- simulated touchscreen press selects a tile through the real touch connection;
- second tap on an adjacent tile follows the same swap path as mouse;
- additional simultaneous fingers are ignored;
- one physical touch cannot also trigger duplicate mouse processing;
- touch while locked is ignored;
- terminal states ignore board input;
- touching Restart / Play Again does not select the board underneath;
- the chosen unified pointer read does not process one touch twice.

Tests should inject actual simulated touchscreen events through Unity's Input System where possible.

Calling `HandlePointerDown` directly is useful for lower-level picking tests, but by itself does **not** prove that touchscreen input is wired into the controller.

## Validation

No scene/prefab changes unless the audit proves they are necessary.

If no production change is required, record the result and do not create an empty commit.

STOP for approval.

---

# Step 2 — Portrait Layout Design

## Goal

Define the mobile portrait composition before changing scene transforms.

Use a real mobile target shape rather than resizing objects ad hoc.

Recommended initial reference:

```text
1080 × 1920 portrait
```

This is a design reference, not a hard-coded gameplay resolution.

## Orientation decision

Phase 12 mobile orientation is:

```text
Portrait only
```

The project currently allows AutoRotation in all four orientations.

Step 2 is design-only: confirm the portrait setting without changing project settings or scene layout. After approval, apply the portrait lock in Step 3 before responsive board fitting so later steps do not need to support accidental landscape rotation.

## Canvas Scaler decision

Audit the current Canvas Scaler explicitly.

Current baseline:

```text
Reference Resolution: 1920 × 1080
Match: 0.5
```

Do not change it merely because the game is moving to portrait.

Measure whether the current scaler gives the intended HUD size and spacing on the approved portrait reference and target aspect ratios, then make an explicit decision to keep or change it with a documented reason.

The shared content region must use the actual resulting HUD screen-space bounds rather than assuming reference-resolution pixels equal device pixels.

## Desired hierarchy

The screen should read approximately as:

```text
┌──────────────────────┐
│      safe area       │
│                      │
│  MOVES  GOAL  SCORE  │
│                      │
│                      │
│       8 × 8 BOARD    │
│                      │
│                      │
│       Restart        │
│                      │
│      safe area       │
└──────────────────────┘
```

The exact placement should follow the existing Step 8 HUD style.

Do not redesign the Prismatic Energy art or HUD identity.

## Shared mobile content-region contract

Before implementing either board fitting or safe-area handling, define one shared layout contract:

```text
screen
→ safe-area bounds
→ reserve HUD / Restart / margins
→ remaining board content region
```

Both safe-area support (Step 3) and responsive board fitting (Step 4) must use this same definition rather than develop separate calculations.

The contract should define:

- top HUD reservation;
- bottom / side margins;
- usable board region;
- safe-area influence;
- where outcome UI may overlay.

Step 2 defines this contract before Step 3 implements the runtime safe-area adapter.

## Board sizing

The board should:

- remain square;
- preserve logical 8×8 spacing;
- scale uniformly;
- fit inside the available safe content area;
- leave reasonable separation from HUD and screen edges.

Do not scale individual tiles independently to solve screen fit.

Prefer scaling / positioning the board presentation root as a unit.

## Camera / world considerations

Existing animations capture world-space start / destination positions.

Changing or scaling `BoardRoot` while a move is animating could invalidate those captured positions.

Preferred Phase 12 policy:

```text
keep board world geometry fixed during gameplay
→ adapt framing primarily through Camera orthographic size / Camera position
```

This keeps tile world positions, collider geometry and animation destinations stable.

If a persistent `BoardRoot` placement change is needed for the portrait baseline, apply it while the board is settled and treat that transform as the new fixed layout.

Do not continuously move or scale `BoardRoot` during active resolution.

If runtime resize/orientation handling must change BoardRoot geometry, defer that change until the board is settled.

Do not change Core board coordinates.

## Before implementation

Show the proposed portrait layout and explain:

- board bounds;
- HUD bounds;
- approximate margins;
- how the board will remain centered;
- how the same scene will handle landscape/editor use if needed.

STOP for approval before changing scene layout.

---

# Step 3 — Safe Area Support

## Goal

Keep critical HUD controls away from:

- display cutouts;
- rounded corners;
- status bars;
- gesture/home indicators.

## Scope

Apply the portrait orientation setting approved in Step 2 before implementing safe-area support. Apply any Canvas Scaler or baseline HUD layout changes approved in Step 2 as part of this UI setup; do not introduce a new layout design here.

Safe-area handling belongs to Presentation/UI only.

Do not modify gameplay coordinates.

## Preferred design

Use one explicit safe-area container / adapter rather than sprinkling inset calculations across multiple UI components.

It must implement the safe-area side of the shared content-region contract defined in Step 2.

After this step, the HUD / Restart should live inside the safe-area-aware UI layout and expose their actual on-screen edges for board fitting.

Avoid a global UI framework.

The system should:

- read Unity's safe-area rectangle;
- convert it into Canvas / RectTransform coordinates correctly;
- apply it only where needed;
- avoid recalculating every frame when the safe area has not changed.

## Validate

Use Device Simulator or equivalent editor tooling if available.

Check at least:

- no notch;
- top notch/cutout;
- bottom gesture area;
- asymmetric safe-area case.

Verify:

- Moves / Goal / Score remain visible;
- Restart remains reachable;
- outcome panel remains centered and readable;
- Play Again remains reachable.

Do not overfit to one specific phone model.

STOP for approval.

---

# Step 4 — Responsive Board Fitting

## Goal

Make the board presentation adapt to available screen space instead of relying on one fixed editor framing.

## Scope

Introduce the smallest Presentation-side mechanism needed to fit the board into a defined content region.

Possible approaches include:

- camera-size calculation;
- board-root scaling;
- board-root positioning;
- a small board-layout helper.

Choose based on the current scene architecture.

## Preserve

- Board logical Width / Height;
- tile spacing relationships;
- tile view identity;
- collider-to-sprite alignment;
- animation paths;
- deterministic gameplay.

## Requirements

The fitting logic must:

- use screen/aspect information only in Presentation;
- use the shared content-region contract from Step 2;
- read the safe-area-aware HUD's actual screen-space edges where practical (for example, the bottom of the TopBar and top of Restart) instead of duplicating hard-coded HUD dimensions;
- tolerate common phone aspect ratios;
- keep the board fully visible;
- avoid overlapping the HUD;
- preserve fixed board world geometry during active animations.

Inexpensive change detection is acceptable.

Avoid recomputing layout every frame when screen size / safe-area inputs are unchanged.

Do not treat "no Update()" as an absolute rule: a tiny cached-value comparison is acceptable if Unity provides no cleaner event.

## Test / review sizes

At minimum review portrait layouts approximating:

```text
1080 × 1920   (9:16 portrait)
1080 × 2160   (9:18)
1080 × 2340   (9:19.5)
1440 × 2960   (tall Android)
```

Also check one shorter/wider case to ensure the layout does not assume every device is extremely tall.

## Tests

Keep the region → camera framing calculation as pure/testable as practical.

Add a small focused test for representative aspect/content-region inputs so board fitting is not validated only by screenshots.

STOP for approval.

---

# Step 5 — Touch Picking Review

## Goal

Verify that the current Physics2D collider-based picking is appropriate for mobile and fix only real issues.

## Inspect

- collider size;
- visual tile size;
- spacing;
- touch target size;
- camera mapping;
- ray/pointer conversion;
- overlap ambiguity;
- whether decorative visual effects can intercept picking.

## Important distinction

The current tile collider is a gameplay input surface.

The visual sprite may change size or shape, but picking should remain stable.

Do not derive touch target geometry from opaque sprite pixels.

## Mobile usability

A touch should not require pixel-perfect contact with the visible gem.

If the current 1×1 collider is already appropriate, keep it.

If real mobile-scale review proves it too small or ambiguous, propose the smallest collider/picking change.

Do not remove Physics2D simply because the roadmap says "avoid unnecessary physics."

The question is:

```text
Is Physics2D currently an unnecessary cost?
```

not:

```text
Can we remove Physics2D because mobile optimization sounds good?
```

Measure / inspect before replacing it.

## Possible outcomes

A. Keep collider picking unchanged.

B. Simplify the picking path if a clearly cheaper direct grid mapping is both simpler and more reliable.

Do not choose B without a concrete reason.

STOP for approval.

---

# Step 6 — Input Polling and Per-Frame Work Audit

## Goal

Review the current runtime for obviously unnecessary mobile per-frame work.

## Inspect

At minimum:

- every `Update()` method;
- input polling;
- camera/screen-size checks;
- HUD refresh behavior;
- animation loops;
- repeated `GetComponent`;
- repeated allocations;
- temporary collections;
- physics queries;
- debug-only runtime work.

## Classification

For each per-frame behavior, classify it as:

```text
required
cheap and acceptable
avoidable
unknown / needs measurement
```

Do not refactor acceptable code merely to make the audit produce changes.

## Likely focus

`BoardInputController.Update()` may be justified if it is the one pointer polling loop.

The question is not:

```text
Can Update be eliminated?
```

The question is:

```text
Is this Update doing unnecessary work when there is no input?
```

If event-based input would materially simplify or reduce work without harming clarity, propose it first.

Do not introduce a complex input-event architecture for theoretical savings.

## Allocation review

Look for avoidable recurring allocations in gameplay loops and animations.

Do not optimize one-time startup/editor allocations.

No object pooling unless profiling shows repeated Instantiate/Destroy is a meaningful problem for the target device.

STOP for approval.

---

# Step 7 — Frame-Rate Policy

## Goal

Define an explicit mobile frame-rate policy.

Phase 11 deliberately deferred this.

Start from the **Android** platform/quality behavior, not the Editor's currently selected quality level.

If `FrameRatePolicy.md` still describes the Editor's "Very Low" quality level as globally active, correct that wording during this step.

## Questions

1. What frame rate should the game target on Android?
2. What does the current Unity version do on Android when no explicit target is set?
3. How does mobile behavior differ from Editor/desktop VSync behavior?
4. Is 60 fps the correct baseline for this project?
5. Should the Editor remain uncapped outside validation?
6. Does the chosen policy affect animation timing?

Confirm current Unity 6 documentation during this step before relying on platform-specific assumptions.

## Preferred starting point

For this portfolio Match-3 project:

```text
Android target: 60 fps
```

is a reasonable baseline unless device testing shows otherwise.

Do not add adaptive 30/60/120 logic in this phase without a concrete need.

## Important

Animations use `Time.deltaTime`, so they should remain duration-based rather than frame-count-based.

Verify timing at **real runtime frame-rate targets**.

Do not use `Time.captureFramerate` for this validation because it overrides/fakes `deltaTime` and would hide the timing behavior being measured.

For Editor validation, use real runtime settings such as:

```text
disable VSync as needed for the test
set Application.targetFrameRate
measure elapsed real time
```

Test representative targets:

```text
30 fps
60 fps
120 fps where the Editor/display permits it
```

Do not require mathematically identical wall-clock completion. Frame-driven coroutines may overshoot a target duration by roughly one rendered frame.

Use tolerances appropriate to the tested rate:

```text
30 fps  → about ±1 frame (~33 ms)
60 fps  → about ±1 frame (~17 ms)
120 fps → about ±1 frame (~8 ms)
```

The requirement is effectively frame-rate-independent duration, not bit-for-bit equality.

## Implementation

Keep policy wiring small and explicit, preferably in the composition/startup layer rather than scattered across views.

Do not put target-frame-rate logic in Core.

## Validate

Compare:

- swap duration;
- clear duration;
- Beam/Nova feedback;
- Wisp flight;
- fall/refill;

at different frame rates.

The same animation should take approximately the same wall-clock time.

STOP for approval.

---

# Step 8 — Mobile UI / Aspect-Ratio Review

## Goal

Validate the complete screen rather than individual layout components.

This is primarily a visual review step.

## Check

Across the target aspect ratios:

- board fully visible;
- board remains centered and readable;
- no HUD overlap;
- safe-area padding correct;
- goal icon readable;
- Restart reachable;
- outcome panel centered;
- Play Again reachable;
- no text clipping;
- no accidental horizontal scrolling / off-screen elements;
- no special-effect visuals clipped unexpectedly;
- touch targets remain usable.

## Readability

Review actual tile sizes.

Compare against the Phase 11 readability work:

```text
~32 px
~48 px
~64 px
```

If the portrait layout forces the board below the previously validated readable size, report it before compensating.

Do not silently shrink the board until it fits if readability is lost.

## Orientation

Portrait is the intended mobile orientation unless testing proves it inappropriate.

Do not implement landscape-specific UI unless explicitly approved.

STOP for approval.

---

# Step 9 — Android Build Readiness

## Goal

Verify the Unity project can produce an Android development build without introducing release-store scope.

Android Build Support is already installed.

## Audit

Before building, inspect:

- active build target;
- Android package/application identifier;
- minimum API / target API settings;
- portrait orientation setting;
- Android quality level;
- scripting backend;
- architecture targets;
- development-build settings;
- input-system compatibility;
- required permissions.

Phase 12 Android development identifier:

```text
com.tugaykapucu.match3
```

Current tooling already available should be verified rather than reinstalled:

```text
Unity 6000.5.10f1
Android SDK
Android NDK
OpenJDK
IL2CPP
ARM64
```

Do not change release signing or store metadata in this phase.

Use development/debug settings where appropriate.

## Build

Phase 12 Android readiness requires **one successful Android development APK build**.

Configuration inspection alone is not enough to claim build verification complete.

If tooling/environment constraints prevent the APK from being produced, report:

```text
Android configuration reviewed
Build verification incomplete
```

and leave this requirement unresolved.

A physical-device install remains optional.

### Build-target restoration policy

Switching the Editor build target to Android may trigger asset reimports and leaves the Editor on that platform.

After Android build/device validation, explicitly decide and record whether to:

```text
A. leave Android as the active target for the remainder of Phase 12
or
B. switch back to the previous development target
```

Whichever policy is chosen, restore the expected final platform before Step 11 and verify SampleScene/Git cleanliness afterward.

## Device validation if available

Check:

- app launches;
- portrait orientation;
- touch selection/swaps;
- no keyboard requirement;
- board layout;
- safe area;
- frame rate;
- Wisp flight;
- special effects;
- HUD;
- Win/Lose;
- Restart / Play Again.

Record device/model and resolution if tested.

Do not add analytics, store integration or production signing in this phase.

STOP for approval.

---

# Step 10 — Phase 12 Runtime Review

## Goal

Run the complete game as a mobile-style experience after all Phase 12 work is integrated.

Use real touch simulation / device input where available.

## Validate

### Input

- first touch selects;
- second adjacent touch swaps;
- invalid touch/swap behaves correctly;
- input while busy ignored;
- terminal board input ignored;
- no keyboard needed.

### Layout

- portrait;
- multiple aspect ratios;
- safe areas;
- board / HUD separation;
- outcome panel.

### Gameplay

- normal matches;
- Beam;
- Nova;
- Wisp;
- Spectrum Core;
- chains;
- cascades;
- Win;
- Loss;
- Restart / Play Again.

### Performance

- target frame-rate policy applied as intended;
- no obvious stutter from special effects / clears / refill;
- no new recurring allocation or polling regression found in the audit;
- no unnecessary physics changes introduced merely for optimization.

### Determinism / architecture

- Board/View consistency;
- restart determinism;
- separate refill / targeting RNG streams unchanged;
- `RefillSeedReuse` known-issue classification remains understood;
- Core remains Unity-independent.

Restore temporary test/device-simulation settings afterward.

If no defects are found, make no empty commit.

STOP for approval.

---

# Step 11 — Phase 12 Validation Pass

Run:

```text
all EditMode tests
all PlayMode tests
Unity compilation
Console inspection (no unexpected errors/warnings; known negative-test output may be reported explicitly)
final portrait SampleScene smoke test
Git clean-state check
```

Report the required Android development APK build result. If build verification remains incomplete, Phase 12 is not complete.

## Confirm architecture

### Core

- still Unity-independent;
- no screen/input/layout/platform rules.

### Presentation

- owns touch/pointer adaptation;
- owns screen layout and safe-area handling;
- owns mobile visual behavior.

### BoardInputController

- remains the single gameplay input path unless a concrete approved change required otherwise.

### BoardResolutionRunner

- sequencing only.

### BoardBootstrap

- composition/startup/platform policy wiring where appropriate.

### LevelProgress

- unchanged gameplay responsibility.

---

# Known-Issue Review

Review the existing issues after the mobile pass.

At minimum revisit:

```text
DeadBoard
LockedAfterException
MobileLayoutAndPicking
SelectionFeedback
ColourOnlyGems
FrameRatePolicy
RefillSeedReuse
MissingReadme
WatchList
```

Expected possibilities:

- `MobileLayoutAndPicking` may be resolved or narrowed.
- `FrameRatePolicy` may be resolved.
- `ColourOnlyGems` likely remains deferred unless explicitly addressed.
- `SelectionFeedback` may become more important for touch but should not be silently implemented unless approved.
- `MissingReadme` remains release/portfolio work.
- `RefillSeedReuse` remains a low-priority determinism/design note unless Phase 12 evidence changes that classification.
- `DeadBoard` and `LockedAfterException` remain gameplay/recovery concerns.

Do not automatically fix unrelated issues during final validation.

---

# Explicitly Avoid in Phase 12

Unless a concrete measured need appears, do not introduce:

```text
generic input abstraction framework
gesture-recognition framework
new state machine
global event bus
service locator
singleton manager
object pool
custom physics replacement
responsive-layout framework
third-party tweening package
third-party UI package
adaptive 30/60/120 FPS system
landscape-specific UI
store integration
analytics
production signing
```

---

# Cross-Step Rules

Every implementation step must:

1. inspect the exact current code / scene first;
2. explain the proposed change before editing;
3. preserve Core / Presentation boundaries;
4. prefer the smallest solution that satisfies the mobile requirement;
5. add or update focused tests where behavior is testable;
6. run relevant regression tests;
7. check compilation and Console;
8. inspect the diff;
9. keep commits small, focused and independently reviewable;
10. commit only the approved scope;
11. STOP for approval before the next step.

---

# Unity Learning Rule

Before every Unity Editor / MCP action, explain:

1. **what** will be changed;
2. **why** it is needed;
3. the exact **manual Unity Editor steps**;
4. **how to verify** it.

After the action, report exactly what changed or was observed.

Do not silently modify:

```text
scene hierarchy
RectTransforms
anchors
Canvas settings
safe-area setup
camera
colliders
project settings
build settings
orientation
frame-rate settings
```

The human developer must be able to reproduce every Unity-side change manually.

---

# Phase 12 Learning Review

At the end of Phase 12 I should be able to explain:

1. how Unity's Input System represents mouse and touch pointer input;
2. why touch should reuse existing gameplay input semantics;
3. screen coordinates vs world coordinates vs board coordinates;
4. why collider/picking state is separate from sprite shape;
5. how portrait layouts differ from fixed editor layouts;
6. `RectTransform` anchors and pivots;
7. Canvas Scaler reference resolution and match behavior;
8. what a mobile safe area is and why it matters;
9. why safe-area logic belongs in Presentation;
10. how board fitting can adapt without changing Core coordinates;
11. why layout recalculation should not run every frame unnecessarily;
12. when `Update()` polling is acceptable;
13. how to distinguish real allocations/performance issues from theoretical ones;
14. why Physics2D should only be removed if it is actually unnecessary;
15. how `Application.targetFrameRate` and VSync interact conceptually;
16. why delta-time-based animations remain consistent across frame rates;
17. Android development-build basics;
18. what belongs in a mobile pass vs a release/store phase;
19. which mobile concerns remain intentionally deferred;
20. how Phase 12 preserved the project's Core / Presentation architecture.

---

# Definition of Done

Phase 12 is complete when:

```text
touch-only gameplay works
portrait layout is readable
common phone aspect ratios are handled
safe areas are respected
HUD remains usable
touch picking is reliable
no keyboard is required
frame-rate policy is explicit
obvious per-frame waste has been reviewed
at least one Android development APK build succeeds
all tests pass
No unexpected Console errors or warnings; expected negative-test output is identified
SampleScene is clean
Git is clean
Core remains Unity-independent
```

Then STOP.

Do not begin the next phase automatically.
