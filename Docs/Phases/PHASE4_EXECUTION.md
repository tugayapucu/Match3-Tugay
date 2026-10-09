# Phase 4 Execution Plan — Input and Swapping

This phase adds player selection and adjacent tile swapping.

The goal is to let the player choose two neighboring tiles, update the authoritative `Board`, and animate the corresponding `TileView` objects to the resulting positions.

This phase does **not** decide whether a swap creates a match.

---

# Phase Goal

Build this interaction flow:

```text
Mouse click
    ↓
screen position
    ↓
BoardInputController
    ↓
GridPosition selection
    ↓
Board.TrySwapAdjacent(...)
    ↓
authoritative Board changes
    ↓
BoardView animates TileViews
```

The model remains authoritative even while the visual animation is catching up.

During a swap animation, additional input must be ignored.

---

# Architecture Goal

Expected dependency direction:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

The Core layer owns:

```text
adjacency rule
logical tile swap
board state
```

The Presentation layer owns:

```text
mouse input
screen/world conversion
tile picking
selection state
animation
input locking during animation
```

Do not introduce match validation yet.

---

# Interaction Rules

For this phase:

1. First valid tile click selects a logical cell.
2. Clicking an orthogonally adjacent tile attempts the swap.
3. Clicking the same tile again does nothing.
4. Clicking a diagonal or distant tile does nothing.
5. Invalid second selections leave the first selection intact.
6. A valid adjacent swap updates the `Board`.
7. The corresponding views animate to their new cells.
8. Input remains locked until the swap animation finishes.
9. After the animation finishes, the selection is cleared.
10. The swap is **not** reverted if it creates no match.

These rules are intentionally simple for Phase 4.

---

# Working Rule

For every step:

1. Read only the current step.
2. Inspect the relevant existing code before editing.
3. Explain the intended design briefly.
4. Implement only that step.
5. Add tests for pure Core logic introduced.
6. Run relevant validation.
7. Verify Unity compilation and inspect the Console.
8. Summarize changed files and design decisions.
9. Answer the learning questions for that step.
10. Commit only when the step explicitly requests it.
11. STOP and wait for approval.

Do not implement later steps early.

---

# Step 1 — Core Adjacency and Swap Rule

## Goal

Teach the logical `Board` how to swap two orthogonally adjacent cells.

This must remain pure C# with no Unity dependency.

---

## Preferred Design

Add the smallest API needed to `Board`.

A preferred shape is:

```csharp
bool TrySwapAdjacent(GridPosition first, GridPosition second)
```

Exact naming may differ only if there is a clearly simpler design.

The method should:

- validate both positions are inside the board
- reject the same position
- reject diagonal positions
- reject positions more than one cell apart
- accept horizontal neighbors
- accept vertical neighbors
- swap the two `TileType` values
- return whether the swap occurred

Do **not** check for matches.

Do **not** create a swap service/interface unless a concrete need appears.

---

## Orthogonal Adjacency

Two positions are orthogonally adjacent when their Manhattan distance is exactly 1:

```text
abs(dx) + abs(dy) == 1
```

Examples:

```text
(2, 3) ↔ (3, 3)   valid
(2, 3) ↔ (2, 4)   valid

(2, 3) ↔ (3, 4)   invalid diagonal
(2, 3) ↔ (4, 3)   invalid distant
(2, 3) ↔ (2, 3)   invalid same cell
```

Keep the rule in Core.

---

## Tests

Add focused EditMode tests for:

- horizontal adjacency
- vertical adjacency
- diagonal rejection
- distant rejection
- same-position rejection
- actual tile values swap
- rejected swaps leave board unchanged
- invalid/out-of-bounds positions behave consistently with the existing `Board` API

Prefer parameterized tests where useful.

---

## Do Not Add

Do not add:

- match detection
- match validation
- swap rollback
- scoring
- gravity
- refill
- Unity types
- animation
- input
- `ISwapService`
- command pattern

---

## Learning Questions

I should be able to answer:

- Why does adjacency belong in Core rather than Presentation?
- What is Manhattan distance?
- Why is diagonal adjacency rejected?
- Why should the Board enforce the rule instead of trusting the UI?
- Why is there still no match validation?

---

## Commit

```text
Add adjacent board swapping
```

STOP after this commit.

---

# Step 2 — Swap-Ready BoardView

## Goal

Teach `BoardView` how to find the visual for a logical position and animate two tile views exchanging positions.

No input yet.

---

## View Lookup

`BoardView` now has a concrete reason to map:

```text
GridPosition → TileView
```

Use the simplest suitable collection, such as:

```csharp
Dictionary<GridPosition, TileView>
```

The lookup should stay synchronized with the rendered views.

Do not add an interface around it.

---

## Animation Responsibility

Add a presentation-only operation conceptually similar to:

```text
AnimateSwap(firstPosition, secondPosition, duration)
```

The exact API may be a coroutine if that is the simplest Unity solution.

It should:

1. find both `TileView` objects
2. calculate their destination world positions
3. animate both views simultaneously
4. finish exactly at the destination positions
5. update the view-to-position mapping after the animation
6. update each `TileView.Position` so it describes the cell the visual now represents

The logical `Board` is **not** changed by `BoardView`.

---

## Model vs Visual Timing

For Phase 4, the intended sequence is:

```text
Board swaps immediately
        ↓
visuals animate toward the new Board state
        ↓
views finish synchronized with Board
```

This creates a short period where:

```text
model = new state
visuals = moving toward new state
```

That is intentional.

Input will be locked during this period in Step 3.

---

## Animation

Keep it simple.

A small serialized duration on `BoardView` is acceptable, for example:

```text
swapDuration = 0.2 seconds
```

Use ordinary interpolation.

Do not add tweening packages.

Do not add easing frameworks.

A simple coroutine with `Vector3.Lerp` is enough for this phase.

---

## TileView

If `TileView` needs a small API adjustment so its logical `Position` can be updated after movement, make the smallest change possible.

Do not make `Position` publicly settable.

Do not add gameplay logic to `TileView`.

---

## Validation

Verify:

- BoardView can locate a view by logical position
- two views animate to each other's positions
- their final logical positions are updated
- the lookup is correct after the swap
- repeated swaps do not corrupt the lookup
- Core remains unchanged by presentation animation

No player input is required yet.

---

## Learning Questions

- Why is the `GridPosition → TileView` dictionary useful now?
- Why does the Board change before the animation finishes?
- Why must input be locked while model and visuals are temporarily out of sync?
- Why does animation belong in Presentation?
- Why shouldn't BoardView mutate the Board?

---

## Commit

```text
Add animated tile swapping
```

STOP after this commit.

---

# Step 3 — Mouse Selection and Input Lock

## Goal

Allow the player to select tiles with the mouse and trigger adjacent swaps.

The input design should be easy to extend to touch later.

---

## Implement

Create a small presentation component, preferably:

```text
BoardInputController.cs
```

A different simple name is acceptable if clearly justified.

---

## Touch-Compatible Input Boundary

Do not put the swap logic directly inside a mouse-specific callback.

Separate:

```text
input source
```

from:

```text
handle a pointer at this screen position
```

Conceptually:

```csharp
Update()
{
    if mouse clicked:
        HandlePointerDown(mouseScreenPosition)
}
```

Later touch input should be able to call the same pointer-handling method with a touch screen position.

Mouse is implemented now.

Touch support itself is not required yet.

---

## Tile Picking

Use the simplest Unity approach appropriate for the existing SpriteRenderer-based board.

A reasonable design is:

```text
screen position
    ↓
world position
    ↓
2D physics/raycast/overlap
    ↓
TileView
    ↓
GridPosition
```

Do not put input polling inside every `TileView`.

Do not add a new input package unless already available and clearly required.

---

## Selection State

The controller should track at most one selected `GridPosition`.

Behavior:

```text
no selection + tile click
    → select tile

selection + same tile
    → ignore

selection + distant/diagonal tile
    → ignore; keep first selection

selection + adjacent tile
    → attempt Board.TrySwapAdjacent(...)
```

Do not add match validation.

A visual selection highlight is optional and should only be added if it remains trivial and scoped.

---

## Input Lock

When a valid swap starts:

```text
isInputLocked = true
```

Then:

1. update the Board through the Core swap API
2. animate the views through `BoardView`
3. wait for animation completion
4. clear the selection
5. unlock input

While locked, all new pointer input is ignored.

---

## Important Failure Rule

If the Board rejects a swap:

- do not animate
- do not corrupt selection state
- do not modify the Board

---

## Validation

Verify manually and/or with MCP:

- first click selects a tile
- adjacent horizontal swap works
- adjacent vertical swap works
- diagonal second click does nothing
- distant second click does nothing
- same-tile second click does nothing
- rapid clicking during animation does nothing
- Board state changes exactly once per valid swap
- after animation, visual positions match Board positions

---

## Learning Questions

- Why separate mouse polling from pointer handling?
- How does that make touch easier later?
- Why is input lock a Presentation concern?
- What bug could happen if another swap starts halfway through the first animation?
- Why do invalid selections not mutate the Board?

---

## Commit

```text
Add tile selection input
```

STOP after this commit.

---

# Step 4 — Composition Root and Scene Wiring

## Goal

Connect the new input controller to the same authoritative Board and make the existing scene interactive.

Use Unity MCP for Editor work where helpful.

---

## BoardBootstrap

Extend the composition root only as much as needed.

The intended ownership remains:

```text
BoardBootstrap
    ↓ creates
Board
    ↓ passes same instance to
BoardView / BoardInputController
```

A simple initialization flow may be:

```text
Board generated
    ↓
BoardView.Render(board)
    ↓
BoardInputController.Initialize(board, boardView)
```

Exact ordering may differ if justified, but the same `Board` instance must be used.

Do not expose a global singleton.

---

## Scene Setup

Wire the scene through Unity Editor/MCP:

- add the input controller
- assign required Camera/reference fields
- give each tile a 2D collider if the chosen picking approach requires it
- assign references explicitly
- preserve existing Phase 3 scene structure where practical

Do not manually edit Unity YAML when MCP/Editor operations can perform the setup.

---

## Runtime Smoke Test

In Play Mode verify:

### Valid swaps

- horizontal adjacent swap
- vertical adjacent swap
- animation completes cleanly
- input becomes available again after animation

### Invalid selections

- diagonal
- distant
- same tile

must not swap.

### Input lock

Attempt rapid repeated clicks during animation.

Expected:

```text
one logical swap
one visual animation
no extra board mutation
```

### Authoritative state

After each valid swap:

- inspect `Board`
- inspect `TileView.Position`
- confirm visuals agree after animation

---

## Important

A swap that produces no match must still remain swapped.

That is intentional in Phase 4.

Example:

```text
swap occurs
no match
swap stays
```

Phase 5 will introduce match detection/validation.

---

## Commit

```text
Wire interactive tile swapping
```

STOP after this commit.

---

# Step 5 — Phase 4 Validation Pass

## Goal

Verify the full input/swapping architecture before match detection is introduced.

Do not add new features.

---

## Run

- all EditMode tests
- Unity compilation
- Console inspection
- Play Mode smoke tests

---

## Core Review

Expected Core responsibility:

```text
Board
├── stores logical grid
└── enforces orthogonally adjacent swap rule
```

Core must not contain:

```text
mouse
touch
Camera
Collider2D
Coroutine
animation
GameObject
Transform
```

---

## Presentation Review

Expected Presentation responsibilities:

```text
BoardInputController
├── mouse input
├── pointer-to-tile picking
├── selection
└── input lock

BoardView
├── position lookup
└── swap animation

TileView
└── one tile's visual representation

BoardBootstrap
└── composition/wiring
```

---

## Explicitly Confirm Absent

There must still be no:

- match validation
- match detector used by gameplay
- swap rollback
- gravity
- refill
- score
- goals
- move counter
- special tiles
- event bus
- state machine
- command pattern
- object pooling
- global Board singleton

---

## Final Learning Review

I should be able to explain:

- logical Board state vs animated visual state
- why adjacency is Core logic
- Manhattan distance for orthogonal neighbors
- how a mouse click becomes a `GridPosition`
- why input is locked during animation
- why the Board changes before the visual movement finishes
- how the same input architecture can later accept touch
- why a no-match swap is currently allowed to stay

---

## Commit

Create a commit only if validation requires a real code/test/scene change.

Do not create an empty commit.

STOP and report whether Phase 4 is complete.

Do not start Phase 5.

---

# Recommended Start Prompt

```text
Follow AGENTS.md and CLAUDE.md.

We are executing Phase 4 using Docs/phases/PHASE4_EXECUTION.md.

Read only:
- the project context needed for the current task
- Step 1 — Core Adjacency and Swap Rule

Do not read or implement later steps yet.

Before modifying files:
1. inspect the current Board and GridPosition code
2. briefly explain the smallest design for enforcing orthogonal adjacency and swapping
3. explain why this logic belongs in Core

Then implement Step 1 only.

Requirements:
- keep Core independent of Unity
- only orthogonally adjacent positions may swap
- do not validate matches
- add focused EditMode tests
- do not introduce unnecessary abstractions

After implementation:
1. run relevant tests
2. verify Unity compilation
3. inspect the Console
4. summarize changed files
5. answer the Step 1 learning questions
6. commit with:

Add adjacent board swapping

7. STOP and wait for approval

Do not start Step 2.
```

---

# Continue Prompt

```text
Approved.

Proceed to the next step in Docs/phases/PHASE4_EXECUTION.md only.

Follow the same workflow:
- inspect
- explain
- implement
- test
- validate Unity compilation and Console
- summarize
- answer the learning questions
- commit using the step's specified message
- STOP

Do not start any later step.
```
