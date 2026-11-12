# Phase 16 Execution Plan — Level System & Game Flow

Phase 16 turns the current single-level board into a small complete game flow.

The phase adds:

```text
Main Menu
→ configured levels
→ Win / Loss flow
→ Retry / Next / Menu
→ minimal progression
→ Android Back behavior
```

It must **not** move navigation or progression concerns into the Match-3 Core.

---

# Phase Roles

```text
Codex  → main developer
Claude → reviewer / challenger
```

Codex implements the approved slices.

Claude reviews architecture, diffs, edge cases and tests. Claude must not edit production files unless explicitly asked.

Do not let both agents edit the same feature simultaneously.

---

# Phase 15 Handoff

Phase 15 completed:

- swipe and two-tap share the same swap-attempt path;
- taps commit on release;
- tapping the selected tile again cancels selection;
- selection feedback is Presentation-only;
- touch interaction was reviewed with a real finger on the Galaxy S23;
- the current swipe threshold remains `0.25`;
- Restart is easy enough to reach that confirmation was deferred for a later game-flow/UI phase;
- a controller component may safely be disabled after outcome, but deactivating its whole GameObject while a move coroutine is running can prevent cleanup;
- an unexplained first automated-tap miss remains documented and must not be “fixed” speculatively.

Phase 16 must preserve all of these behaviors.

Most importantly:

```text
active move / resolution
→ do not destroy, replace, deactivate or navigate away from its gameplay objects
→ wait until the session is settled
```

Do not solve navigation by adding broad coroutine cancellation or exception middleware.

---

# Goal

By the end of Phase 16 the app should feel like a small game rather than a single isolated board demo.

Expected player flow:

```text
launch
  ↓
Main Menu
  ↓
Play / Continue
  ↓
Level N
  ↓
┌───────────────┬────────────────┐
│ Win           │ Loss           │
│               │                │
│ Next Level    │ Retry          │
│ Menu          │ Menu           │
└───────────────┴────────────────┘
```

Restart during an active Playing session remains available, but receives the approved lightweight confirmation.

The first release should contain approximately **5–10 intentionally configured levels**.

This phase optimizes for:

- clear ownership;
- deterministic/testable level configuration;
- a complete small product flow;
- safe session transitions;
- minimal persistence;
- simple mobile navigation.

It does **not** optimize for content volume or a future live-service architecture.

---

# Architecture Boundary

Keep this distinction explicit:

```text
LevelDefinition / level catalog
            ↓
GameFlowController chooses a level
            ↓
BoardBootstrap starts one gameplay session
            ↓
Board + LevelProgress run level gameplay
            ↓
BoardBootstrap reports terminal outcome
            ↓
GameFlowController decides Retry / Next / Menu
```

## Core owns

- Board state;
- matching;
- swap validation;
- resolution;
- specials;
- dead-board recovery rules;
- one level's `LevelProgress`;
- Win/Loss calculation inside that level.

## Game-flow / Presentation owns

- which level is active;
- which level is unlocked;
- Main Menu;
- Next / Retry / Menu navigation;
- persistence of progression;
- Android Back behavior;
- restart confirmation UI;
- final-level product flow.

Do not make `Board`, `LevelProgress`, `MatchFinder`, `SwapValidator`, or other Core classes know:

```text
"Level 4"
"Next Level"
"Main Menu"
"Unlocked"
"PlayerPrefs"
"Android Back"
```

---

# Preferred Minimal Architecture

The final names may change during the design review, but start from this shape.

```text
LevelDefinition
    data/config for one level

LevelCatalog
    ordered list of LevelDefinition entries

GameFlowController
    current level / menu / progression / navigation decisions

BoardBootstrap
    builds and owns one active gameplay session
    from a supplied LevelDefinition

LevelHudView
    displays current level state + outcome actions

MainMenuView
    displays Play / Continue and any approved menu actions

Progression persistence
    one stored progression integer
```

Avoid:

- global singleton managers;
- service locators;
- generic scene/navigation frameworks;
- generic dialog frameworks;
- event buses;
- account/cloud-save abstractions;
- a level-framework hierarchy;
- separate state classes for every screen unless concrete complexity demands it.

---

# Level Configuration Policy

## Per-level data

Start with only values that are genuinely level-specific:

```text
starting move count
goal TileType
goal target
board seed
```

Potentially add another field only if a real configured level requires it.

## Identity

For the initial unreleased game:

```text
catalog list index = level order / identity
```

No explicit stable `Id`, no `DisplayOrder`, no unique-ID/order validation. The displayed level number is `index + 1`.

Accepted trade-off: reordering levels after public release would need a progression migration. We are pre-release, so that is not solved now.

## Level 1 is today's level

```text
Level 1
Seed: 42
Moves: 20
Goal: Red
Target: 25
```

Moving configuration out of `BoardBootstrap` must not silently change the existing deterministic starting level or the regression tests that rely on it.

## Keep global until evidence says otherwise

Prefer to keep these as existing game-wide rules initially:

- board size: 8×8;
- current six-colour palette;
- points per cleared tile;
- existing generation / special rules.

Do not copy every BoardBootstrap Inspector field into `LevelDefinition` merely because it exists.

## Seed vs fixed board

Preferred first-release approach:

```text
approved deterministic seed per level
```

Do **not** add a full 64-cell fixed-layout format unless a concrete level-design need appears.

Why:

- current generation is already deterministic;
- starting playability is already validated;
- runtime dead-board recovery already exists;
- a seed gives curated reproducibility with far less data.

A fixed start can be reconsidered later if hand-authored openings become a product requirement.

---

# Persistence Policy

Decided first-release policy:

```text
persist only progression, as one integer
```

Without a player-facing Level Select, "current level" and "highest unlocked" are the same value, so only one integer is stored:

```text
0 .. levelCount - 1
→ current playable level

levelCount
→ all currently available levels completed
```

| Stored value | Result |
| --- | --- |
| missing (fresh install) | 0 |
| negative | clamp/reset to 0 |
| above `levelCount` | clamp to `levelCount` |

A Win/save never lowers the stored progression value. After final completion the Main Menu shows `Play Again`, which starts Level 1. Replaying after full completion must not erase the stored completion sentinel.

## Replay after full completion

```text
storedProgress == levelCount
Main Menu → Play Again
Play Again → Level 1
```

Replay position is **not persisted**. If the player returns to the Main Menu during a replay, `Play Again` starts Level 1 again. This is intentional for the first release; do not add separate replay-progress persistence.

Do **not** persist:

- an in-progress Board;
- current RNG state;
- partial score;
- current move count;
- selection state;
- animation state.

Closing the app mid-level may restart that level from its deterministic start when reopened.

This keeps persistence understandable and avoids serializing active gameplay sessions.

If `PlayerPrefs` is used, keep the API isolated enough that invalid/missing stored values can be tested and clamped safely, but do not build a storage framework.

---

# Session Identity Rule

A gameplay session means:

```text
one LevelDefinition
+ fresh Board
+ fresh LevelProgress
+ fresh RNG streams
+ fresh Board/View/Input wiring
```

Retry / Restart creates a **new session for the same LevelDefinition**.

Next Level creates a **new session for the next LevelDefinition**.

Returning to Menu ends the current settled session from the player's perspective.

Never carry:

- Board;
- score;
- goal progress;
- selected tile;
- gesture state;
- RNG objects;
- outcome state

from one level session into another.

---

# Safe Navigation Rule

This is a Phase 16 invariant.

```text
if BoardInputController.IsInputLocked
→ do not Restart
→ do not go to Menu
→ do not load Next
→ do not deactivate gameplay hierarchy
```

The existing move coroutine owns Board/View cleanup until it finishes.

Navigation must be:

- ignored while busy; or
- requested and deferred until settled,

depending on the approved UX.

Prefer **ignore/disable while busy** unless deferred navigation has a concrete UX benefit.

Do not call `SetActive(false)` on a GameObject containing the running controller/coroutine while a move is active.

Disabling the `BoardInputController` component for a terminal outcome remains distinct from deactivating its GameObject.

---

# Outcome Timing Rule

Win/Loss is detected **inside** the locked move window: `BoardBootstrap.HandleValidMoveResolved` runs from the move coroutine before `FinishSwap` unlocks. `StartLevel` throws while input is locked.

Required order:

```text
move resolves while input is still locked
→ BoardBootstrap / game flow records Won or Lost
→ outcome UI may update
→ no session navigation occurs yet
→ FinishSwap completes and unlocks
→ Next / Retry / Menu button may then start navigation
```

Do not start another level, return to Menu or rebuild a session directly from the outcome callback.

Outcome buttons remain unavailable until the existing busy gate (`onBusyChanged(false)` → `LevelHudView.SetRestartAvailable`) reports the move fully finished. Reuse that gate; do not add a second one.

## Progress saving

Persist progression when game flow receives an accepted `Won` outcome, not when the player presses `Next`.

Reason:

```text
Win
→ close app from outcome screen
→ win must still be preserved
```

That save runs inside the locked window, so keep it trivial: write the one progression integer. Do not rebuild sessions, navigate or do other work there.

---

# Scene Decision

Phase 16 uses **one production scene**.

Main Menu, gameplay and game-flow UI live in the same scene. Navigation is presentation/game-flow state inside that scene.

Do not introduce:

- `SceneManager` navigation;
- `DontDestroyOnLoad`;
- static cross-scene progression state;
- multiple-scene lifecycle machinery.

Why: unloading a scene stops its coroutines just like `SetActive(false)`, and carrying the chosen level across scenes pushes toward static or persistent singleton objects.

Any hiding/deactivation of gameplay hierarchy must obey the Safe Navigation Rule.

---

# Board Input Gating Rule

Canvas/EventSystem UI does **not** block board input. `BoardInputController` polls `Pointer.current` and performs its own Physics2D raycast, so a tap on a menu or confirmation panel can still reach the tiles behind it.

Therefore Main Menu, confirmation and any other blocking flow UI must gate board interaction explicitly.

`BoardInputController.enabled` has one production owner:

```text
GameFlowController decides whether gameplay interaction should be allowed
        ↓
BoardBootstrap exposes one narrow input-gating operation
        ↓
BoardBootstrap is the only class that writes inputController.enabled
```

The gate must never re-enable a terminal Won/Lost session.

Do not add a generic input manager.

## Flow gate vs move lock

These are two separate concepts:

```text
BoardInputController.IsInputLocked
→ lifecycle of the currently resolving player move

game-flow input gate
→ whether menus / confirmation / flow state allow board interaction
```

The flow gate does not replace the per-move lock.

The narrow `BoardBootstrap` gating API may technically disable the controller while a move is locked, because disabling the component does not stop the running coroutine. However:

- game-flow navigation must still never occur while `IsInputLocked`;
- UI should not normally open confirmation/navigation while busy;
- re-enabling through the flow gate must never revive a terminal Won/Lost session;
- terminal session state has priority over a request to enable input.

```text
flow requests input available
AND current LevelProgress.Status == Playing
→ controller may be enabled
```

Use the existing `LevelProgress.Status`; do not add a second gameplay-state system to implement this.

## Selection across the gate

Preserve and test the Phase 15 behavior:

```text
selected tile
→ confirmation opens / controller disabled
→ selection remains logical
→ Cancel / controller enabled
→ selection cue is derived and appears again
```

Disabling the component hides the cue while keeping `selectedPosition` and leaving the running GameObject/coroutine ownership intact.

---

# Explicit Non-Goals

Do not add:

- accounts;
- login;
- backend;
- cloud save;
- analytics SDK;
- ads;
- IAP;
- live ops;
- remote config;
- daily rewards;
- lives/energy;
- level map;
- stars;
- boosters;
- inventory;
- currencies;
- achievements;
- pause system unless the approved Back UX truly requires it;
- dozens of levels;
- procedural difficulty framework;
- generic scene-navigation framework;
- generic modal/dialog system;
- generic persistence/repository framework;
- async loading architecture without a real need;
- Addressables merely for level definitions;
- new Match-3 rules.

---

# Step 1 — Phase Entry Gate

## Goal

Confirm the clean Phase 16 baseline.

Inspect:

- `git status`;
- current HEAD and relation to origin;
- `PLAN.md` Phase 16 only;
- `AGENTS.md`;
- `BoardBootstrap`;
- `LevelProgress`;
- `LevelHudView`;
- current outcome tests;
- current Restart tests;
- `RestartConfirmation.md`;
- `LockedAfterException.md`;
- current safe-area / mobile layout components;
- current Scene hierarchy relevant to Menu / Gameplay roots.

Record:

- current test baseline;
- current SampleScene structure;
- current session startup path;
- current outcome path;
- current restart path;
- where the board/input objects live in the hierarchy;
- every current writer of `inputController.enabled`;
- whether any proposed menu-root hiding would deactivate the running coroutine host.

Do not modify production files.

STOP if unrelated working-tree changes exist.

---

# Step 2 — Game-Flow UX Decision

## Goal

Agree on product behavior before architecture/code changes.

Produce one explicit state-flow diagram.

At minimum decide:

## Main Menu

Preferred starting scope:

```text
title
Play / Continue
```

Do not add Settings unless a real setting exists.

Evaluate Level Select separately.

### Level Select decision

Ask:

- does replaying unlocked levels materially improve the first release?
- does it materially improve manual QA?
- can automated test seams cover QA without a player-facing level picker?

Preferred default:

```text
no Level Select initially
```

unless there is a concrete reason to add it.

Do not create a disabled/placeholder Level Select button.

## Win

Define:

```text
Win
→ Next Level
→ Menu
```

If the current level is the final available level, the outcome is decided:

```text
All Levels Complete

Play Again
Menu
```

Do not show `Next Level`. `Play Again` starts Level 1 and must not lower the stored completion sentinel from `levelCount`.

## Loss

Define:

```text
Loss
→ Retry
→ Menu
```

Retry starts the same configured level fresh.

## Restart while Playing

Implement the already-approved direction:

```text
Restart pressed
→ small confirmation
    Cancel
    Restart
```

Rules:

- Cancel preserves the active session exactly.
- Confirm invokes the existing same-level restart path once.
- no generic dialog framework;
- confirmation cannot open while the board is resolving;
- terminal Play Again / Retry can remain immediate unless the UX review finds a reason otherwise.

## Menu while Playing

Decided: gameplay exposes a real Menu action.

```text
settled Playing level
→ Menu
→ Leave Level confirmation

Cancel
→ preserve the current session exactly

Leave
→ return to Main Menu
```

A Menu request during an active resolution stays unavailable/ignored under the Safe Navigation Rule.

Restart and Leave may reuse the same small confirmation presentation if that keeps the implementation simple. The confirmation has explicit modes/intent, so Restart can never execute Leave behavior or vice versa.

Do not build a generic dialog/modal framework.

## Final-level behavior

Must be explicit before implementation.

Decided:

- final-level Win shows `All Levels Complete` with `Play Again` and `Menu` (see Win above);
- after final completion the Main Menu shows `Play Again`, which starts Level 1, and the completion sentinel is kept;
- replay position is not persisted (Persistence Policy).

Report together with Steps 1, 3 and 4 (see First Developer Assignment).

---

# Step 3 — Level Data Design

## Goal

Define the smallest level configuration representation.

Preferred concept:

```csharp
LevelDefinition
```

with fields equivalent to:

```text
StartingMoveCount
GoalTileType
GoalTarget
Seed
```

Identity and order come from the catalog list index (Level Configuration Policy).

Exact serialization choice is a design decision.

Compare:

### Option A — ScriptableObject per level

Pros:
- Unity Inspector authoring;
- easy asset references;
- readable small catalog.

Cons:
- one asset per level.

### Option B — serializable entries in one catalog asset/component

Pros:
- all 5–10 levels in one place;
- fewer assets.

Cons:
- one large Inspector list.

### Option C — hard-coded list in C#

Reject unless there is a strong reason. It makes tuning content require code edits.

Preferred direction for 5–10 levels:

```text
data asset(s), not per-level branches in code
```

Also define:

```text
LevelCatalog
→ ordered entries (list index = level order / identity)
→ at least one level
```

Do not build inheritance or level-type subclasses.

## Validation rules

`LevelDefinition` stays simple configuration. Do not duplicate rules already enforced by:

- `LevelProgress` (positive moves, normal collectible goal colour, positive target);
- `BoardGenerator`;
- `BoardBootstrap` cross-setting validation (goal colour in the palette, view configuration).

Existing session construction remains the authoritative gameplay validation path. `StartLevel` already builds the new session before touching the current one, so an invalid definition fails before the running level changes.

The catalog validates only the structural requirements it uniquely owns:

- the catalog contains at least one entry;
- entries required by its representation are present (for example, no missing/null entry).

Every `int` seed is valid; there is no seed check.

Report together with Steps 1, 2 and 4.

---

# Step 4 — Progression Ownership and Persistence Design

## Goal

Decide where progression state lives.

Preferred responsibilities:

```text
GameFlowController
→ current level index
→ interprets Win/Loss
→ chooses next session/menu

Progression storage
→ load/save only minimal unlocked/current progress
```

Do not put progression in `LevelProgress`.

## Minimal stored value

Decided: one progression integer, `0 .. levelCount` (Persistence Policy). There is no separate current-level and highest-unlocked value.

## Persistence behavior

Confirm against the code, for:

- fresh install → 0;
- negative value → 0;
- value larger than `levelCount` → `levelCount`;
- app relaunch after winning → the win is kept;
- app relaunch during an unfinished level → that level starts fresh from its deterministic start;
- final level completed → `levelCount`; Main Menu shows `Play Again` and starts Level 1.

```text
invalid stored progress
→ clamp/reset safely
→ never crash startup
```

Do not silently unlock arbitrary future content.

## Save timing

Persist when game flow receives an accepted `Won` outcome, not when the player presses `Next` (Outcome Timing Rule).

## `LockedAfterException` persistence review

`LockedAfterException.md` says to revisit when saving is added. Review it here and record the result.

Expected reasoning, to be checked rather than assumed:

- progression persistence stores only an accepted completed-level result;
- an interrupted gameplay session is still disposable;
- relaunch reconstructs a clean deterministic session;
- persistence must not make a partially failed Board authoritative.

Do not claim the known issue is solved unless evidence supports that.

Report together with Steps 1–3.

---

# Step 5 — Refactor BoardBootstrap to Accept Level Configuration

## Goal

Make BoardBootstrap responsible for **one session**, not for choosing which level exists.

Current hard-coded/Inspector per-level values should move behind a supplied definition only where they are actually per-level:

- seed;
- starting moves;
- goal tile;
- goal target.

Level 1 must reproduce today's seed 42 / 20 moves / Red / 25 exactly. This move also changes how `SampleScene` serializes these values; report that scene change explicitly.

Preserve game-wide settings if still global:

- dimensions;
- palette;
- points per tile;
- animation durations;
- view/input references.

Preferred conceptual API:

```text
StartLevel(LevelDefinition definition)
RestartCurrentLevel()
```

or similarly narrow names.

Do not expose Board internals merely for game flow.

## Outcome reporting

BoardBootstrap needs a narrow way to report:

```text
Won
Lost
```

plus the identity of the current definition if needed.

Do not let it choose Next Level.

The outcome is reported while input is still locked (Outcome Timing Rule). The receiver may record it and update UI, but must not start navigation from the callback.

## Input gate

Add the one narrow input-gating operation from the Board Input Gating Rule. After this step, `BoardBootstrap` is the only production writer of `inputController.enabled`, and the gate cannot re-enable a Won/Lost session.

Possible direction:

```text
GameFlowController supplies:
- current LevelDefinition
- outcome callback
```

Avoid a global event bus.

## Restart

Restart must recreate the current definition's session:

```text
same definition
→ same seed
→ same deterministic starting board
→ fresh progress
→ fresh RNG streams
```

## Tests

At minimum:

- Level A starts with A's seed/moves/goal;
- Level B starts with B's values;
- restart repeats the current definition;
- changing level does not reuse prior Board instance;
- selection/gesture state is cleared;
- outcome callback fires once, while input is still locked;
- terminal input behavior remains correct;
- the input gate cannot re-enable a Won/Lost session;
- the existing seed-42 Level 1 board and its regression tests are unchanged;
- dead-board recovery still works within the supplied definition.

Show diff/tests and STOP before commit.

---

# Step 6 — Implement Level Catalog and Initial Configured Levels

## Goal

Create approximately 5–10 intentional level definitions.

Do not optimize for quantity.

Each level should deliberately choose:

- seed;
- move count;
- goal colour;
- goal amount.

## Content criteria

A level must:

- start stable;
- start playable;
- have its intended config;
- be deterministic from the seed;
- be realistically winnable under normal play;
- not be trivially identical to every other level.

Do not claim difficulty ordering from configuration alone.

Use play/testing evidence.

## Initial count

Choose the exact count before implementation.

A reasonable target is:

```text
6–8 levels
```

unless testing shows that 5 or 10 is more appropriate.

## Testing

For every definition:

- configuration validation;
- deterministic starting Board;
- no immediate match;
- at least one legal move;
- goal colour present;
- session starts without Console errors.

Add a compact parameterized test rather than duplicating one test per field per level.

---

# Step 7 — Minimal Progression Persistence

Implemented before the game-flow owner and Main Menu, so the menu's `Play` / `Continue` / `Play Again` label is driven by loaded progression from the start. Do not hard-code a temporary label.

## Goal

Load and save the one progression integer from the Persistence Policy.

Preferred implementation may use `PlayerPrefs` if sufficient.

Requirements:

- one clearly named key/version if needed;
- deterministic default: 0;
- invalid values clamped as the Persistence Policy table says;
- a save never lowers the stored value;
- Retry/Loss/Restart do not advance it;
- final-level completion (`levelCount`) remains representable;
- tests do not depend on the developer's real PlayerPrefs state.

Do not store active Board state. Do not build a storage framework.

This step builds and tests load/clamp/save in isolation. Saving on an accepted `Won` is wired in the game-flow step.

## Test isolation

Tests must clean up their keys.

Do not call `PlayerPrefs.DeleteAll()` in production or tests.

---

# Step 8 — Introduce GameFlowController

## Goal

Add the smallest Presentation/game-flow owner.

Responsibilities may include:

- menu vs gameplay mode;
- current level index;
- loading current definition;
- handling outcome;
- Retry;
- Next;
- Menu;
- final-level handling;
- persistence coordination: save the progression integer when it receives an accepted `Won` (Outcome Timing Rule);
- deciding when board interaction is allowed, through `BoardBootstrap`'s input gate (Board Input Gating Rule).

It must **not**:

- find matches;
- mutate Board rules;
- calculate score;
- resolve cascades;
- choose dead-board recovery.

## State representation

Prefer a small enum/value if useful, e.g.:

```text
MainMenu
Playing
Outcome
Confirmation
```

Do not create one class per state.

Only add explicit state if it prevents illegal navigation.

## No singleton

Wire references explicitly in the scene/composition root.

## Tests

Prove transitions, not implementation details:

```text
Menu → Play
Win → Next
Loss → Retry
Playing → Menu
Final Win → final flow
```

Also test:

- rejected transitions while gameplay is busy;
- no navigation from inside the outcome callback;
- progression saved on `Won`, before any `Next` press;
- Loss/Retry/Restart never change stored progression.

---

# Step 9 — Main Menu UI

## Goal

The app no longer launches straight into the board demo.

Implement the approved minimal Main Menu.

At minimum:

```text
Game title
Play / Continue button
```

Optional only if approved:

- Level Select;
- a real Settings button;
- other release-required links.

Do not add empty buttons.

## Play / Continue / Play Again label

The label comes from the progression loaded in Step 7:

| Stored progression | Label | Starts |
| --- | --- | --- |
| 0 | `Play` | Level 1 |
| 1 .. `levelCount - 1` | `Continue` | that level |
| `levelCount` | `Play Again` | Level 1, keeping the completion sentinel |

Do not overcomplicate localization in this phase.

## Board input behind the menu

The menu canvas does not block board taps (Board Input Gating Rule). While the menu is shown, board interaction is gated through `BoardBootstrap`.

## Safe area

Reuse the existing safe-area approach.

Test tall/short aspect layouts without redesigning the whole UI.

## Transition rule

The menu transition must not deactivate active gameplay objects during a running move.

If gameplay is already settled and being left, a controlled hide/deactivate is acceptable.

Document exactly which GameObjects are enabled/disabled and when.

---

# Step 10 — Outcome Flow: Retry / Next / Menu

## Goal

Replace the current single-level "Play Again" product behavior with level-aware actions.

## Loss

Show:

```text
Retry
Menu
```

Retry:
- same LevelDefinition;
- fresh session;
- no progression advance.

## Win

For non-final level:

```text
Next Level
Menu
```

Next:
- navigates only; progression already advanced once, when `Won` arrived;
- next LevelDefinition starts fresh.

For final level:

```text
All Levels Complete

Play Again
Menu
```

- `Play Again` starts Level 1 and never lowers the stored completion sentinel from `levelCount`;
- `Next Level` is not shown.

## Timing

The outcome panel may appear while the final move is still locked. Its buttons stay unavailable until the existing busy gate reports the move finished (Outcome Timing Rule). Progression was already saved when `Won` arrived; pressing `Next` only navigates.

## Idempotency

Repeated button presses / duplicate callbacks must not:

- unlock multiple levels;
- start two sessions;
- fire navigation twice.

Disable or guard transition actions once accepted.

---

# Step 11 — Restart and Leave Level Confirmation

## Goal

Resolve the deferred Phase 15 usability issue, and add the gameplay Menu action's Leave Level confirmation (Step 2).

Scope is intentionally narrow.

Restart flow:

```text
Playing + settled
→ Restart
→ Restart confirmation

Cancel
→ close panel
→ same Board / progress / selection state preserved

Restart
→ invoke same-level restart exactly once
```

Leave flow:

```text
Playing + settled
→ Menu
→ Leave Level confirmation

Cancel
→ close panel
→ current session preserved exactly

Leave
→ return to Main Menu
```

## Explicit modes

Restart and Leave may share one small confirmation presentation if that stays simple. It must carry an explicit mode/intent (Restart or Leave), so Confirm in Restart mode can never leave and Confirm in Leave mode can never restart. Test both modes.

## Input behavior

While confirmation is open:

- board input unavailable;
- no move can start;
- Restart and Menu buttons cannot open another copy;
- Android Back behavior follows the approved policy.

Do not use `SetActive(false)` on the running controller GameObject mid-move.

A Canvas panel alone does not block board taps (Board Input Gating Rule). Because the prompt can only open while settled, gate board interaction through `BoardBootstrap`'s input-gating operation; do not write `inputController.enabled` from the confirmation. Cancel re-enables through the same gate, which shows the preserved selection cue again. Test the cleanup/re-enable semantics explicitly, including the Phase 15 selection behavior:

```text
selected tile
→ confirmation opens / controller disabled
→ selection remains logical
→ Cancel / controller enabled
→ selection cue is derived and appears again
```

## Terminal outcome

Retry / Play Again after Loss does not need this confirmation unless explicitly approved.

Do not create a generic dialog framework.

---

# Step 12 — Android Back UX Decision and Implementation

## Goal

Define Back per game-flow state before coding it.

At minimum decide:

## Main Menu

Preferred mobile behavior:

```text
Back
→ system-standard app exit/back behavior
```

Do not build a custom "Are you sure you want to quit?" dialog unless there is a product reason.

## Playing — settled

Choose explicitly:

### Option A
```text
Back → leave-level confirmation → Menu
```

### Option B
```text
Back → Menu immediately
```

Preferred: Option A if active level progress would be discarded.

## Playing — resolving / input locked

Preferred:

```text
Back ignored
```

Do not navigate away mid-resolution.

Do not queue a deferred Back request unless human UX testing shows it is needed.

## Restart confirmation open

Preferred:

```text
Back → Cancel confirmation
```

## Win/Loss outcome

Preferred:

```text
Back → Main Menu
```

because the board is terminal and settled.

## Implementation

Keep Back handling in game-flow / Presentation.

Do not add Android-specific concepts to Core.

Use the existing Input System where practical.

Avoid a pause system unless the approved UX requires one.

## Tests

Cover every state above, especially:

```text
Back during resolution
→ no navigation
→ move finishes normally
→ controller unlocks
```

## Device verification

Editor tests and Input System semantics are not sufficient on their own. How Android delivers Back to the Input System, and what happens when the game leaves Back unhandled, must be checked on the device.

Verify the final Back implementation on the Galaxy S23:

- Main Menu Back;
- settled gameplay Back;
- Back while resolution is active;
- Back on confirmation;
- Back on Win/Loss.

---

# Step 13 — Cross-Level Session Isolation

## Goal

Prove one level cannot leak state into another.

Test sequences such as:

```text
Level 1
→ make moves / create selection
→ Win
→ Next Level
```

Then verify Level 2 has:

- its own Board instance;
- its configured start;
- fresh score;
- fresh move count;
- fresh goal progress;
- no selection;
- no active gesture;
- no stale highlight;
- no old outcome UI;
- fresh RNG streams;
- correct Restart behavior.

Also test:

```text
Level N → Menu → Continue
```

according to the approved resume policy.

---

# Step 14 — Busy-State / Navigation Adversarial Review

## Goal

Challenge the new product flow where it is most dangerous: session transitions.

Codex implements; Claude reviews/challenges.

Claude should propose sequences including:

- Restart pressed as a swipe starts;
- Menu/Back during swap;
- Menu/Back during invalid rollback;
- Menu/Back during cascade;
- Menu/Back during special chain;
- Menu/Back during dead-board recovery;
- Win on the final move while Back is pressed;
- outcome button double-click;
- Restart confirmation + Back;
- disable/enable controller around confirmation;
- Main Menu → Play spam;
- Win callback delivered once but UI button pressed repeatedly;
- app focus loss while confirmation is open;
- app focus loss during a move;
- final-level Win;
- corrupt persisted index;
- catalog with one level;
- first level / last level edges.

For each:

```text
covered?
→ close

reproducible bug?
→ focused fix/test

theoretical only?
→ document
```

Do not add a general navigation framework in response to imagined races.

---

# Step 15 — S23 Game-Flow Smoke Test

## Goal

Verify the new product flow with real touch.

Do not repeat the full Phase 15 input campaign.

On the S23 test:

- cold launch → Main Menu → Play / Continue → first real finger tap on the Board works;
- ordinary swipe;
- Restart → Cancel;
- with a tile selected, open Restart and double-tap Cancel; record whether the second, fresh tap reaches the board and attempts a swap with the retained selection;
- Restart → Confirm;
- Lose → Retry;
- Win → Next Level;
- Menu return;
- Continue after menu;
- Back on Main Menu;
- Back during settled gameplay;
- Back during resolution;
- Back on outcome;
- background/resume with a confirmation open and during a move; verify the prompt/session remains consistent, the board settles, and a fresh touch works after resume; note any animation jump;
- final-level behavior if practical;
- safe-area/layout on menu, gameplay, confirmation and outcome.

Explicitly verify:

```text
no stale selection highlight
no duplicate session
no frozen input
no old score/moves leaking
```

Record one-device evidence only.

Do not claim all Android devices are validated.

---

# Step 16 — Full Regression Validation

Run:

- full EditMode suite;
- full PlayMode suite;
- all new Phase 16 focused tests.

Required regression areas:

- Board generation;
- legal-move detection;
- dead-board recovery;
- swap/tap/swipe;
- selection feedback;
- specials/chains;
- invalid rollback;
- deterministic Restart;
- Win/Loss;
- busy-state gating;
- safe-area behavior.

Console must contain only declared expected negative-test/framework messages.

Verify SampleScene / production scene is stopped and clean after testing.

---

# Step 17 — Documentation / Known-Issue Closure

Update only after final validation.

At minimum:

## `RestartConfirmation.md`

If implemented and validated:
- mark resolved;
- record the final narrow behavior.

## `LockedAfterException.md`

Add any Phase 16 navigation constraint discovered, especially around:

```text
component disable
vs
GameObject deactivation
```

Record the Step 4 persistence review: whether progression saving changes the "relaunch fully recovers" statement.

Do not claim the general exception risk is solved unless it actually is.

## Phase validation record

Record:

- final level count;
- progression rule;
- persistence rule;
- Back behavior;
- final-level behavior;
- S23 game-flow result;
- deferred issues.

Do not rewrite old Phase 13/15 audits as though they were current documents.

---

# Step 18 — Learning Review

This phase is architecture-heavy.

The developer should be able to explain:

## Gameplay session vs game flow

```text
Gameplay session
→ Board + LevelProgress + RNG + views for one level

Game flow
→ which level/session the player should be in
```

## Why LevelDefinition is configuration

It describes inputs to a session.

It does not own Board mutations or navigation.

## Why progression is not LevelProgress

`LevelProgress` answers:

```text
Did this level win or lose?
```

Game flow answers:

```text
What happens after that?
```

## Why Next Level creates a fresh session

Session-local state must not leak across content boundaries.

## Why Back is Presentation/game-flow behavior

It is a product/navigation input, not a Match-3 rule.

## Why active sessions are not persisted

Saving Board + RNG + coroutine/presentation state would add major complexity not justified by the first release.

## What changes at 100 levels

The gameplay architecture should mostly stay the same.

Likely changes:

- level authoring/catalog tooling;
- content validation;
- maybe level-selection UI;
- perhaps asset loading strategy.

Do not redesign today's runtime for 100 hypothetical levels.

Keep notebook notes short and tied to implemented classes/methods.

---

# Step 19 — Completion Gate

Phase 16 is complete only when:

- [ ] Main Menu exists and app no longer launches directly into an isolated board;
- [ ] Main Menu has only real actions;
- [ ] exact initial level count is chosen;
- [ ] approximately 5–10 intentional LevelDefinitions exist;
- [ ] LevelDefinition is configuration, not Board logic;
- [ ] catalog list index is the level order/identity, with no duplicate gameplay validation;
- [ ] Level 1 reproduces seed 42 / 20 moves / Red / 25;
- [ ] one production scene; no `SceneManager` navigation or `DontDestroyOnLoad`;
- [ ] BoardBootstrap starts a supplied/current definition;
- [ ] Restart repeats the current level definition deterministically;
- [ ] current level/progression ownership is outside Core board rules;
- [ ] Win progression is defined;
- [ ] Loss retry is defined;
- [ ] Next Level is defined;
- [ ] Menu return is defined;
- [ ] final-level behavior is defined;
- [ ] no navigation starts from the outcome callback; outcome buttons wait for the busy gate;
- [ ] progression is one stored integer `0 .. levelCount`, saved on accepted `Won`;
- [ ] final completion shows `Play Again`, starts Level 1 and keeps the completion sentinel;
- [ ] active Board/session state is not persisted;
- [ ] fresh-install behavior works;
- [ ] invalid persisted progress is handled safely;
- [ ] `LockedAfterException` persistence review is recorded;
- [ ] `BoardBootstrap` is the only production writer of `inputController.enabled`; blocking UI gates board input and never re-enables a terminal session;
- [ ] one level cannot leak Board/View/progress/RNG/selection state into another;
- [ ] Restart confirmation is implemented or explicitly re-deferred with evidence;
- [ ] gameplay Menu opens a Leave Level confirmation when settled and is unavailable while resolving;
- [ ] Cancel preserves the active session, including logical selection and its re-derived cue;
- [ ] Confirm restarts exactly once; Leave returns to Main Menu; neither mode can run the other's action;
- [ ] final-level Win shows `All Levels Complete` with `Play Again` and `Menu`, and no `Next Level`;
- [ ] the flow gate stays separate from `IsInputLocked` and enables input only while `LevelProgress.Status == Playing`;
- [ ] Android Back behavior is defined for Main Menu;
- [ ] Android Back behavior is defined for settled gameplay;
- [ ] Back during resolution is safe;
- [ ] Back behavior is defined for confirmation;
- [ ] Back behavior is defined for Win/Loss;
- [ ] Back behavior is verified on the S23 for every state above;
- [ ] no navigation deactivates a running coroutine host mid-move;
- [ ] no generic pause/navigation/dialog framework was added without need;
- [ ] menu/gameplay/confirmation/outcome respect mobile safe areas;
- [ ] S23 game-flow smoke test completed;
- [ ] cold launch → Main Menu → Play / Continue → first real finger tap on the Board rechecked;
- [ ] full EditMode passes;
- [ ] full PlayMode passes;
- [ ] Console has no unexplained errors/warnings;
- [ ] docs/known issues reflect final behavior;
- [ ] developer can explain gameplay-session vs game-flow ownership;
- [ ] human reviewed the final diff.

Do not start Phase 17 automatically.

---

# Git / Review Discipline

For each implementation slice:

```text
Codex implements
→ focused tests
→ regression
→ show diff
→ human review
→ Claude review/challenge where useful
→ approved fixes
→ commit
```

Do not commit before the requested review gate.

Do not push unless explicitly requested.

No co-author / attribution trailers.

Prefer meaningful commits, for example:

```text
Add configurable level definitions
Start gameplay sessions from level data
Add main menu and game flow
Persist unlocked level progression
Add level-aware outcome actions
Confirm restart before abandoning a level
Handle Android Back across game-flow states
Configure initial release levels
Complete Phase 16 level system and game flow
```

Do not create empty commits merely to match this list.

---

# Unity Learning Protocol

Before every Unity Editor / MCP action that changes or meaningfully exercises Editor state, explain:

1. what will be done;
2. why;
3. the exact equivalent manual Unity Editor steps;
4. how to verify.

Afterward report:

- what changed;
- what was observed;
- tests / Console result;
- temporary state restored.

This applies especially to:

- new ScriptableObject/catalog assets;
- Main Menu hierarchy;
- gameplay/menu root changes;
- outcome UI changes;
- restart-confirmation UI;
- Android build/device testing.

Read-only source inspection does not need repetitive ceremony.

---

# First Developer Assignment

Codex should begin with **Steps 1–4 only**, as one combined assignment.

This first pass is **design / inspection only**. Steps 2–4 have no intermediate stops: report all four steps together, then stop once.

Do not modify:

- production code;
- tests;
- scenes;
- prefabs;
- ScriptableObject assets;
- settings;
- documentation.

Report:

1. clean baseline / current HEAD;
2. current SampleScene hierarchy relevant to game flow;
3. current BoardBootstrap session ownership;
4. proposed exact Main Menu / Win / Loss / final-level UX;
5. Level Select recommendation;
6. Restart-confirmation behavior;
7. Menu-leave behavior;
8. exact Android Back behavior for every state;
9. proposed `LevelDefinition` representation (list-index identity, Level 1 = seed 42 / 20 / Red / 25);
10. proposed `LevelCatalog` representation and its structural-only validation;
11. exact fields that are per-level vs still global;
12. seed-vs-fixed-layout recommendation;
13. exact initial level count recommendation;
14. progression owner;
15. confirmation of the one-integer persistence policy and the exact key;
16. fresh-install / corrupt-value / final-completion behavior checked against the code;
17. the `LockedAfterException` persistence review;
18. proposed BoardBootstrap API changes, including the one input-gating operation;
19. proposed outcome callback/data flow under the Outcome Timing Rule;
20. GameFlowController responsibility boundary;
21. one-scene hierarchy plan: which roots exist and which GameObjects are shown/hidden when;
22. hierarchy/coroutine hazards, especially any `SetActive(false)` proposal;
23. focused tests that should be added;
24. any architecture concern.

Then STOP for human review and Claude challenge before implementation begins.
