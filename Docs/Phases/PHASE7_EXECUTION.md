# Phase 7 Execution Plan — Board Resolution

Phase 7 implements the complete Match-3 resolution cycle after a valid swap.

```text
match
  ↓
clear
  ↓
gravity
  ↓
refill
  ↓
match again?
 ↙        ↘
yes        no
 ↓          ↓
repeat     stable / idle
```

The logical `Board` remains authoritative throughout the process. Player input remains locked until the board is fully stable.

This phase does **not** add scoring, goals, move counters, special tiles, boosters, dead-board shuffling, hints, or object pooling.

---

# Critical Learning Rule for Unity Editor / MCP Actions

This rule applies to **every step in this phase**.

If you are about to perform any Unity Editor or Unity MCP action that changes or inspects the scene, prefab, component setup, serialized fields, project settings, Play Mode state, or other Editor-managed data:

## Before doing the Unity action, tell me:

1. **What you are about to do in Unity.**
2. **Why it is necessary.**
3. **Where I would do the same thing manually in the Unity Editor.**
4. **The exact manual steps I could repeat later.**
5. **How I can verify the result afterward.**

Example:

```text
Unity action:
Add BoardResolutionController to BoardRoot and assign BoardView.

Why:
The scene needs an explicit reference for runtime resolution.

Manual equivalent:
1. Open SampleScene.
2. Select GameRoot/BoardRoot in Hierarchy.
3. Click Add Component.
4. Search for BoardResolutionController.
5. Drag BoardRoot's BoardView component into the Board View field.
6. Verify the reference is assigned in the Inspector.
```

Then perform the MCP/Editor action.

## After the Unity action

Tell me exactly what changed and where I can see it in the Editor.

Do **not** silently manipulate scene, prefab, Inspector, Play Mode, or project settings.

The goal is that I can reproduce every Unity-side setup myself later without MCP.

This teaching rule does not apply to ordinary C# file edits, but explain new Unity APIs/concepts before using them.

---

# Phase Goal

After Phase 6:

```text
valid swap
  ↓
match remains visible on Board
```

After Phase 7:

```text
valid swap
  ↓
resolve current match
  ↓
clear
  ↓
collapse survivors
  ↓
refill empty cells
  ↓
detect new matches
  ↓
repeat automatically
  ↓
no matches remain
  ↓
unlock input
```

Invalid swaps still use the Phase 6 forward + rollback flow and must **not** enter board resolution.

---

# Architecture Principles

Keep the existing dependency direction:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

Core owns logical resolution rules.

Presentation owns:

```text
visual removal
fall animation
refill animation
coroutine sequencing
input locking
```

Core must not know about:

```text
GameObject
Transform
MonoBehaviour
Coroutine
SpriteRenderer
Camera
TileView
BoardView
```

Presentation must not calculate gravity or decide what logically refills.

---

# Logical / Visual State Principle

Continue the existing rule:

```text
logical state changes first
visual state catches up afterward
```

Examples:

## Clear

```text
Board matched cells → Empty
        ↓
matched visuals disappear
```

## Gravity

```text
Board survivors move to final lower cells
        ↓
surviving TileViews animate to those cells
```

## Refill

```text
Board empty cells receive new TileTypes
        ↓
new TileViews are created and animate into those cells
```

Input remains locked while logical and visual state temporarily differ.

After each visual stage finishes, `Board`, `BoardView` lookup, `TileView.Position`, and world positions must agree.

---

# Important Identity Constraint

Do **not** infer gravity movement by matching `TileType`.

Two tiles may both be red while still being different visual objects:

```text
RedView_A
RedView_B
```

Gravity must preserve surviving `TileView` identity.

Therefore Core-side gravity should provide enough information for Presentation to know:

```text
which cell a tile moved from
which cell it moved to
```

without guessing from colour.

A small movement record such as:

```text
from GridPosition
to GridPosition
TileType
```

may be appropriate.

Exact naming should be decided after inspecting the current code.

---

# Refill Rule

Refill fills empty cells with playable tile types.

Important:

```text
Refill is allowed to create new matches.
```

Do **not** reuse the initial `BoardGenerator` rule that avoids starting matches. New refill matches are exactly how cascades happen.

Randomness must be controllable enough for deterministic EditMode tests.

Prefer `System.Random` or another simple injected deterministic source in Core. Do not use `UnityEngine.Random` in Core.

Do not add a custom RNG abstraction unless a concrete need appears.

---

# Cascade Rule

Resolution repeats until:

```text
MatchFinder.FindMatches(board).Count == 0
```

Conceptually:

```text
while matches exist:
    clear
    animate clear
    gravity
    animate gravity
    refill
    animate refill
    find matches again
```

Do not unlock input between cascade rounds.

Do not accept another player move until the final stable board has no matches.

---

# Working Rule

For every step:

1. Read only the current step.
2. Inspect relevant existing code before editing.
3. Explain the proposed design before changing files.
4. If Unity/MCP work is needed, follow the **Critical Learning Rule for Unity Editor / MCP Actions** above.
5. Implement only that step.
6. Add or extend tests.
7. Run relevant tests.
8. Verify Unity compilation and inspect Console.
9. Summarize changed files and design decisions.
10. Answer the learning questions.
11. Commit only when the step explicitly requests it.
12. STOP and wait for approval.

Do not implement later steps early.

---

# Step 1 — Clear Matched Cells in Core

## Goal

Add a pure Core operation that clears supplied matched cells by setting them to `TileType.Empty`.

Do not implement gravity or refill yet.

## Before Editing

Inspect:

```text
Board
MatchFinder
SwapValidator
existing Core tests
```

Explain:

- where clear logic should live
- whether this deserves a new small resolution type or belongs on an existing Core type
- why MatchFinder should still only *find* matches
- why clearing must not know about TileView or animation

Prefer the smallest API that keeps responsibilities clear.

## Required Behavior

Examples:

```text
R R R B
```

becomes:

```text
. . . B
```

for supplied positions `(0,0)(1,0)(2,0)`.

Test:

- horizontal match
- vertical match
- intersection
- overlapping groups
- multiple simultaneous matches
- unaffected cells remain unchanged
- empty input is a no-op
- invalid input handling follows existing Core conventions

Prefer:

```text
find matches
then clear those positions
```

as separate responsibilities.

## Invariants

The clear operation must:

- modify only supplied positions
- set them to `Empty`
- preserve Board dimensions
- not animate
- not perform gravity
- not refill
- not call Unity APIs

## Learning Questions

I should be able to answer:

- Why does MatchFinder not clear tiles itself?
- Why is `Empty` useful as an intermediate logical state?
- Why should clear, gravity and refill be separate operations?
- What happens to non-matched cells?

## Commit

```text
Add match clearing
```

STOP after this commit.

---

# Step 2 — Gravity in Core

## Goal

Collapse all non-empty tiles downward within each column.

Do not refill yet.

## Gravity Rule

For each column, all non-empty tiles move as far downward as possible, their relative order is preserved, and all Empty cells end up above them.

Remember:

```text
y = 0 is the bottom cell
```

Example from bottom to top:

```text
before:
G
.
B
.
R

after:
G
B
R
.
.
```

## Movement Information

Presentation needs to know which surviving visual moved where.

Return enough information to describe each actual move.

A likely minimal record:

```csharp
from
to
tileType
```

Do not report stationary tiles unless it clearly simplifies the API.

Do not infer visual movement later by comparing colours.

## Preferred Algorithm

Use a write-pointer / compaction pass.

Conceptually for each column:

```text
writeY = 0

for readY = 0 .. Height-1:
    if cell is non-empty:
        move it to writeY if needed
        writeY++

fill remaining cells above writeY with Empty
```

Target complexity:

```text
O(W × H)
```

## Required EditMode Tests

At minimum:

- no empties → no moves
- all-empty column
- one gap
- several gaps
- alternating empty/non-empty
- multiple columns
- order preservation
- top tile falls several cells
- same-colour tiles preserve logical order
- all Empty cells end at the top
- movement records contain exact `from` and `to`
- Board state matches movement records
- repeated gravity on collapsed board is a no-op

## Learning Questions

I should be able to answer:

- What is the write-pointer technique?
- Why must tile order be preserved?
- Why do we need movement records?
- Why can't Presentation infer movement from TileType?
- Why is one compaction pass O(W × H)?

## Commit

```text
Add board gravity
```

STOP after this commit.

---

# Step 3 — Deterministic Refill in Core

## Goal

Fill every `TileType.Empty` cell with a playable tile type and report what was created.

Do not implement the cascade loop yet.

## Inputs

The operation needs:

```text
Board
playable palette
random source
```

Reuse palette validation cleanly where practical.

Do not couple Core to `UnityEngine.Random`.

## Important Rule

Refill may create:

```text
3+
4+
5+
intersections
```

and that is intentional.

Do **not** prevent matches during refill.

## Spawn Information

Return enough information for Presentation to create correct new views.

A minimal spawn record may contain:

```text
GridPosition
TileType
```

Presentation chooses temporary world-space spawn positions above the board.

Core must not contain world coordinates.

## Required EditMode Tests

At minimum:

- full board → no refill
- all-empty board → every cell filled
- partially empty board → only empties filled
- existing tiles unchanged
- every produced tile is in supplied palette
- deterministic with fixed seed / deterministic random source
- spawn records exactly match newly filled cells
- no `Empty` remains after refill
- refill is allowed to create a match
- invalid palette behavior consistent with BoardGenerator

If sharing palette validation requires a tiny Core refactor, explain it before editing.

## Learning Questions

I should be able to answer:

- Why shouldn't refill use BoardGenerator's no-match rule?
- Why use `System.Random` rather than `UnityEngine.Random` in Core?
- Why do we need spawn records?
- Why is deterministic randomness useful in tests?
- Why does Core return logical positions rather than world positions?

## Commit

```text
Add board refill
```

STOP after this commit.

---

# Step 4 — Resolution Presentation: Clear, Fall, Refill

## Goal

Teach `BoardView` how to visually follow the three logical resolution stages.

Do not add the full cascade loop yet.

## Before Editing

Inspect:

```text
BoardView
TileView
Tile.prefab
BoardInputController
movement/spawn records from Steps 2–3
```

Explain:

- how matched views will be removed
- how surviving visual identity will be preserved during gravity
- how new TileViews will be created for refill
- when the `Dictionary<GridPosition, TileView>` changes
- what visual durations are needed
- whether any scene/prefab changes are necessary

If any Unity Editor/MCP action is needed, follow the **Critical Learning Rule** before doing it.

## Clear Visuals

Core has already set matched Board cells to `Empty`.

Presentation should:

```text
remove/hide corresponding TileViews
update lookup consistently
```

A simple immediate disappearance is acceptable in this phase unless a small clear animation is already natural.

Do not add particles/effects.

## Gravity Visuals

Use exact Core movement records.

For every move:

```text
from → to
```

animate the existing `TileView` to its destination.

After the fall animation:

```text
Dictionary lookup
TileView.Position
world position
```

must all reflect the destination.

Surviving views must not be destroyed and recreated.

## Refill Visuals

For every logical spawn:

```text
position + TileType
```

create a new TileView.

A reasonable presentation:

```text
spawn above its target column
animate downward to target cell
```

Presentation chooses world-space start positions.

Core does not.

After refill settles, Board and views must agree.

## Required Tests

Add focused Presentation tests for:

- matched views removed from lookup
- unmatched object identity survives clear
- gravity animates correct existing objects
- same-colour views are not confused
- lookup correct after gravity
- new refill views have correct type/position
- lookup correct after refill
- BoardView does not mutate Board
- zero-duration variants settle immediately if supported

Use PlayMode where frame-by-frame motion matters.

## Learning Questions

I should be able to answer:

- Why move surviving TileViews instead of recreating them?
- When should lookup change during a fall animation?
- Why does Core report logical moves while BoardView calculates world positions?
- Why are refilled cells allowed to use new TileView objects?
- Why must BoardView never mutate Board?

## Commit

```text
Add board resolution visuals
```

STOP after this commit.

---

# Step 5 — Integrate Automatic Resolution and Cascades

## Goal

After a valid Phase 6 swap finishes its forward animation, automatically resolve until stable.

Invalid swaps must still rollback exactly as before.

## Before Editing

Inspect current Phase 6 orchestration.

Explicitly compare:

```text
A. keep full resolution coroutine in BoardInputController

B. introduce a dedicated Presentation-side resolution orchestrator
```

Prefer the simpler architecture, but do not let `BoardInputController` become a God class.

A dedicated `BoardResolutionController` / `BoardResolutionRunner` is justified if its one clear responsibility is:

```text
sequence clear → gravity → refill → repeat
```

Do not add a generic state machine.

If a new component or scene reference is required, follow the **Critical Learning Rule** before modifying Unity state.

## Intended Valid-Swap Flow

```text
input locked
    ↓
Board swap
    ↓
SwapValidator = valid
    ↓
forward swap animation
    ↓
find matches
    ↓
while matches exist:
    clear Board
    ↓
    clear visuals
    ↓
    gravity Board
    ↓
    animate falls
    ↓
    refill Board
    ↓
    animate refills
    ↓
    find matches again
    ↓
stable
    ↓
selection clear
input unlock
```

## Invalid Swap Flow

Must remain:

```text
Board swap
validate false
forward animation
Board rollback
backward animation
unlock
```

Do not resolve unrelated matches during an invalid move.

## Cascade Behavior

A cascade is:

```text
refill creates match
    ↓
automatic second clear
    ↓
gravity
    ↓
refill
```

and repeats until no matches remain.

No player click occurs between cascade rounds.

## Stable Board Contract

When input unlocks after a valid move:

```text
MatchFinder.FindMatches(board).Count == 0
```

and:

```text
Board
BoardView lookup
TileView.Position
world positions
```

all agree.

The board may still have no possible legal moves. Dead-board detection/shuffle is not Phase 7.

## Required PlayMode Tests

At minimum:

### One-round resolution

Valid swap creates a match; refill creates no new match.

Verify:

```text
swap → clear → gravity → refill → stable → unlock
```

### One cascade

Deterministic refill creates another match.

Verify two complete resolution rounds before unlock.

### Multiple cascades

Use deterministic refill behavior to force at least two cascade rounds.

Do not rely on random luck.

### Input during resolution

Attempt clicks during:

```text
clear
fall
refill
between cascade rounds
```

Expected: ignored.

### Invalid regression

Invalid swap still performs only rollback. No resolution starts.

### Final consistency

After stable:

```text
no matches
no Empty cells
input unlocked
selection cleared
all views agree with Board
```

## Infinite / Long Cascade Consideration

Do not add an arbitrary production cascade limit unless a real need appears.

Tests should use deterministic refill data that is known to reach stability.

## Learning Questions

I should be able to answer:

- What is a cascade?
- Why call MatchFinder again after refill?
- Why keep input locked across the whole cascade?
- Why does resolution begin only after a valid swap's forward animation?
- Why doesn't an invalid swap run resolution?
- What makes a board stable in Phase 7?
- Why is stable not the same as "has a legal move"?

## Commit

```text
Resolve matches and cascades
```

STOP after this commit.

---

# Step 6 — Resolution Sequence and Consistency Coverage

## Goal

Stress-test long sequences and identity synchronization.

Prefer tests over production changes unless a defect appears.

## Required Scenarios

Test:

- valid move → resolution → next valid move
- valid move → resolution → invalid move
- invalid move → valid move → resolution
- multiple same-colour survivors during gravity
- multiple columns collapsing simultaneously
- cascade with an intersection
- refill after different gap depths
- several deterministic resolution cycles without stale lookup state

## Settled-State Assertion

Create/extend a helper that verifies every board cell:

```text
Board tile == TileView.Type
BoardView lookup returns that TileView
TileView.Position == cell
TileView world position == expected cell world position
```

and globally:

```text
no Empty cells
no active matches
input unlocked
selection cleared
```

Apply the full settled assertion only after stability.

## Intermediate-State Assertions

Where practical and without exact frame counts:

### After logical clear

```text
Board has Empty matched cells
input locked
```

### During gravity animation

```text
Board already collapsed
visuals still moving
input locked
```

### During refill animation

```text
Board already contains new TileTypes
new visuals are catching up
input locked
```

## Learning Questions

I should be able to answer:

- Why are sequence tests more valuable now?
- What stale-state bug could appear after several cascades?
- Why is object identity especially important during gravity?
- Why should settled-state checks include world positions?
- Why avoid exact-frame assertions?

## Commit

```text
Strengthen board resolution coverage
```

Only create this commit if Step 6 adds or changes tests/code.

STOP after this commit.

---

# Step 7 — Runtime Smoke Test and Unity Setup Review

## Goal

Verify the real `SampleScene` performs complete board resolution.

No new feature work.

## Unity Learning Requirement

Before **every** scene/prefab/Inspector action used for this smoke test, explain:

```text
what you are doing
why
manual Unity steps
how I can verify it
```

This includes temporary changes such as:

```text
changing animation duration
changing a serialized seed
pausing
entering Play Mode
changing a scene reference
```

Revert temporary changes afterward and report final Editor state.

## Smoke Tests

### Valid swap

Observe:

```text
swap
match clear
gravity
refill
stable
```

Input stays locked until stable.

### Cascade

If seed-42 does not conveniently cascade, use a controlled temporary setup only after explaining the manual Unity changes.

Verify at least one automatic cascade.

Restore normal scene afterward.

### Invalid swap

Verify Phase 6 rollback still behaves correctly.

### Rapid clicking

Attempt input throughout resolution. No extra selection/mutation occurs.

### Final state

Verify:

```text
no active matches
no Empty cells
Board and all views agree
input unlocked
scene has no accidental unsaved changes
Console clean
```

## Architecture Review

Expected shape:

```text
Board
├── tile storage
└── adjacent primitive swap

MatchFinder
└── matched positions

SwapValidator
└── valid-move rule

Core resolution operations
├── clear
├── gravity
└── refill

Presentation resolution orchestration
├── clear visuals
├── fall animation
├── refill animation
└── cascade sequencing

BoardInputController
├── input
├── selection
├── swap orchestration
└── lock ownership / handoff

BoardView
└── visual object + lookup management

BoardBootstrap
└── composition root / wiring
```

Use actual final class names rather than forcing this exact naming.

## Explicitly Confirm Absent

There must still be no:

- score
- combo scoring
- goals
- move counter
- special tiles
- boosters
- hints
- dead-board shuffle
- legal-move search unless independently required
- event bus
- global game manager
- generic state machine
- command framework
- object pooling

## Commit

Create a commit only if the smoke test / review requires a real fix.

Do not create an empty commit.

STOP after the review.

---

# Step 8 — Phase 7 Validation Pass

## Goal

Close Phase 7 with full regression validation.

Do not start scoring, goals, specials, or later-phase work.

## Run

- all EditMode tests
- all PlayMode tests
- Unity compilation
- Console inspection

## Confirm Core Rules

### Clear

```text
matched cells → Empty
only matched cells change
```

### Gravity

```text
non-empty tiles compact downward
relative order preserved
movement records correct
```

### Refill

```text
all Empty cells filled
existing tiles unchanged
spawn records correct
new matches allowed
```

### Cascade

```text
repeat until no matches remain
```

## Confirm Presentation Rules

At final stability:

```text
no Empty cells
no matches
Board == visual state
lookup correct
TileView.Position correct
world positions correct
input unlocked
selection cleared
```

During resolution:

```text
input remains locked
```

## Confirm Regression

Phase 6 invalid swap still works:

```text
forward
rollback
unlock
```

Non-adjacent selections remain rejected without animation.

## Confirm Boundaries

Core still has:

```text
no UnityEngine
no Match3.Presentation
no MonoBehaviour
```

Presentation may consume Core resolution results.

Core must never consume Presentation types.

## Final Learning Review

I should be able to explain:

1. the full resolution lifecycle
2. why clear / gravity / refill are separate operations
3. why `Empty` is an intentional intermediate state
4. the gravity write-pointer algorithm
5. why gravity movement records are required
6. why refill may intentionally create matches
7. how deterministic randomness is tested
8. how a cascade starts
9. why MatchFinder runs after every refill
10. why input remains locked until stable
11. logical vs visual timing during clear
12. logical vs visual timing during gravity
13. logical vs visual timing during refill
14. how TileView identity survives gravity
15. why newly refilled tiles use new TileView objects
16. how board/view consistency is verified
17. why dead-board detection and scoring remain separate problems

## Commit

Create a commit only if validation requires a real fix.

Do not create an empty commit.

STOP and report whether Phase 7 is complete.

Do not start Phase 8.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md and Docs/Phases/PHASE7_EXECUTION.md.

We are executing Phase 7 — Board Resolution.

IMPORTANT LEARNING RULE:
If you perform any Unity Editor / Unity MCP action, tell me BEFORE doing it:
1. what you are about to do,
2. why,
3. the exact manual Unity Editor steps I could use to do the same thing myself,
4. how I can verify the change afterward.

After the MCP/Editor action, tell me exactly what changed.
Do not silently manipulate scenes, prefabs, Inspector fields, Play Mode settings, or project settings.

Read only:
- the project context needed for this task
- Step 1 — Clear Matched Cells in Core

Do not read or implement later steps yet.

Before modifying files:
1. inspect Board, MatchFinder, SwapValidator, and existing Core tests
2. explain where clear logic should live
3. explain why MatchFinder should still only detect
4. propose the smallest API for clearing supplied matched positions
5. explain why `TileType.Empty` is the correct intermediate logical representation

Then implement Step 1 only.

Requirements:
- Core only
- set supplied matched cells to Empty
- leave all other cells unchanged
- no gravity
- no refill
- no animation
- no Unity/Presentation dependency
- comprehensive EditMode tests
- no unnecessary abstractions

After implementation:
1. run relevant EditMode tests
2. run the existing EditMode suite if practical
3. verify Unity compilation
4. inspect Console
5. summarize changed files and design decisions
6. answer the Step 1 learning questions
7. commit with:

Add match clearing

8. STOP and wait for approval.

Do not start Step 2.
```

---

# Continue Prompt

```text
Approved.

Proceed to the next step in Docs/Phases/PHASE7_EXECUTION.md only.

Keep obeying the Unity learning rule:
before every Unity Editor/MCP action, explain what you will do, why, the exact manual Unity steps, and how to verify it; afterward report what changed.

Follow the same workflow:
- inspect
- explain
- implement
- test
- validate Unity compilation and Console
- summarize
- answer the learning questions
- commit using the step's specified message when a real change exists
- STOP

Do not start later steps.
```

---

# Claude Teaching Prompt

Use this after Phase 7 is implemented and validated.

```text
Do not modify files.

Teach me the current Phase 7 board-resolution implementation as if I must defend it in a technical interview.

Use the actual repository implementation.

Explain:

1. the full match → clear → gravity → refill → cascade lifecycle
2. which class owns each logical operation
3. which class owns each visual operation
4. why MatchFinder remains read-only
5. why Empty is used between clear and refill
6. the gravity algorithm step by step
7. the time complexity of gravity
8. how movement records preserve TileView identity
9. how same-colour tiles are handled safely
10. refill randomness and how tests make it deterministic
11. why refill is allowed to create matches
12. how cascades are detected and repeated
13. Board state during clear animation
14. Board state during fall animation
15. Board state during refill animation
16. BoardView lookup changes during each stage
17. why input remains locked through the entire resolution
18. important edge cases and likely failure modes
19. alternative designs and their tradeoffs
20. why scoring, special tiles, pooling and dead-board shuffling remain separate

Also identify every Unity Editor / scene / prefab setup decision Phase 7 required and explain how I could reproduce each one manually in Unity.

Keep the explanation grounded in the actual code.
Do not modify files.
```
