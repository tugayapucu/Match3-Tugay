# Phase 3 Execution Plan — Board Presentation

This phase turns the logical `Board` from Phase 2 into a visible Unity board.

The goal is **presentation only**.

Phase 3 must not introduce swapping, match detection, gravity, refill, scoring, or other gameplay rules.

---

# Phase Goal

Build this dependency flow:

```text
BoardGenerator
      ↓
    Board
      ↓
  BoardView
      ↓
  TileView
      ↓
SpriteRenderer / Transform
```

The dependency direction must remain one-way:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

`Board` remains the authoritative logical state.

`BoardView` and `TileView` only represent that state visually.

---

# Working Rule

For every step:

1. Read only the current step.
2. Explain the intended implementation briefly.
3. Implement only that step.
4. Run relevant validation.
5. Check Unity compilation and Console.
6. Summarize what changed and why.
7. Commit using the specified commit message.
8. STOP and wait for my approval.

Do not implement future steps early.

Do not introduce architecture merely because it may be useful later.

---

# Step 1 — Presentation Assembly + TileView

## Goal

Create the smallest Unity-facing presentation layer.

## Implement

```text
Assets/Scripts/Presentation/
    Match3.Presentation.asmdef
    TileView.cs
```

## `Match3.Presentation.asmdef`

Requirements:

- reference `Match3.Core`
- allow Unity engine references
- use `Match3.Presentation` as the root namespace if appropriate
- do not add unrelated package dependencies

The dependency direction must be:

```text
Match3.Presentation
        ↓
   Match3.Core
```

Never the reverse.

---

## `TileView`

`TileView` represents **one logical tile visually**.

It may know about:

```text
MonoBehaviour
SpriteRenderer
Transform
GridPosition
TileType
```

It must not know about:

```text
BoardGenerator
match detection
gravity
refill
score
moves
win / lose rules
```

### Suggested responsibilities

- store the logical `GridPosition` represented by this view
- store or expose the currently represented `TileType`
- update its visual appearance from a `TileType`
- allow its world position to be assigned by presentation code

Keep the implementation small.

For this learning prototype, using simple colors for tile types is acceptable.

Example conceptual mapping:

```text
Red      → red visual
Green    → green visual
Blue     → blue visual
Yellow   → yellow visual
Purple   → purple visual
Orange   → orange visual
Empty    → hidden / disabled visual
```

Do not put this visual mapping into `Match3.Core`.

### Important

Do not create a `Tile` gameplay class.

`TileView` is a Unity presentation object and is not the logical tile model.

---

## Validation

Verify:

- project compiles
- Unity Console has no new errors
- `Match3.Core` still has `noEngineReferences: true`
- presentation can reference core
- core cannot reference presentation
- `TileView` contains no gameplay rules

No PlayMode behavior is required yet.

---

## Learning Questions

I should be able to answer:

- Why can `TileView` use UnityEngine while `Board` cannot?
- Why is `TileView` a `MonoBehaviour`?
- Why is `TileView` not the authoritative tile state?
- Why does visual color/sprite mapping belong in presentation?
- What is the dependency direction between Core and Presentation?

---

## Commit

```text
Add tile presentation layer
```

STOP after this commit.

---

# Step 2 — BoardView

## Goal

Render an existing logical `Board` as a grid of `TileView` objects.

## Implement

```text
Assets/Scripts/Presentation/
    BoardView.cs
```

## Responsibility

`BoardView` receives a `Board` and visually represents it.

It does **not**:

- generate a board
- decide gameplay rules
- detect matches
- perform swaps
- mutate the board as part of rendering

Conceptually:

```text
Board
  ↓ read
BoardView
  ↓ creates/updates
TileView
```

---

## Suggested serialized presentation data

Keep only what is required, such as:

```text
TileView prefab
Transform tile root
float tile spacing
```

Names may differ if the implementation has a cleaner minimal design.

---

## Rendering

A render method may conceptually look like:

```text
Render(Board board)
```

It should:

1. read `board.Width` and `board.Height`
2. iterate logical positions
3. call `board.GetTile(position)`
4. create a `TileView`
5. initialize its logical position and tile type
6. place it at the corresponding Unity world position

Basic mapping is sufficient:

```text
worldX = x * spacing
worldY = y * spacing
```

Do not add animation yet.

---

## View Lookup

It is acceptable for `BoardView` to keep a collection such as:

```text
GridPosition → TileView
```

if there is a concrete reason:

- finding the visual corresponding to a logical position
- future presentation updates

Do not add a larger abstraction around it.

---

## Re-rendering

If `Render()` can be called more than once, previous presentation objects must not accumulate accidentally.

Use the simplest safe approach.

Do not introduce object pooling in this phase.

---

## Validation

Using Unity MCP where helpful:

- create or use a temporary GameObject with `BoardView`
- verify compilation
- inspect Console
- verify no gameplay code moved into presentation

A complete visible board is not required until Step 4.

---

## Learning Questions

- Why does `BoardView` receive a `Board` instead of creating one?
- Why should `BoardView` read logical state rather than own it?
- Why should world coordinates not be stored in `Board`?
- Why is a `GridPosition → TileView` lookup useful?
- Why are we not using object pooling yet?

---

## Commit

```text
Implement logical board rendering
```

STOP after this commit.

---

# Step 3 — Board Bootstrap / Composition Root

## Goal

Connect board generation to board presentation without coupling the two classes directly.

## Implement

Use a small Unity `MonoBehaviour` responsible for startup wiring.

Suggested name:

```text
BoardBootstrap.cs
```

A different simple name is acceptable if clearly justified.

---

## Responsibility

The bootstrap object is the **composition root** for this prototype.

It may:

1. receive simple configuration from the Inspector
2. call `BoardGenerator.Generate(...)`
3. retain the generated `Board`
4. pass that board to `BoardView.Render(...)`

Conceptually:

```text
Inspector configuration
        ↓
 BoardBootstrap
    ↙        ↘
Generator   BoardView
    ↓           ↑
   Board ───────┘
```

`BoardGenerator` should not know about `BoardView`.

`BoardView` should not know about `BoardGenerator`.

---

## Suggested Inspector Configuration

Keep it minimal:

```text
width
height
seed
TileType[] palette
BoardView reference
```

Starting defaults may be:

```text
width  = 8
height = 8
seed   = 42
palette:
- Red
- Green
- Blue
- Yellow
- Purple
- Orange
```

Do not introduce a ScriptableObject configuration system yet.

---

## Current Board

It is useful for the bootstrap layer to retain the generated board so future gameplay systems can access the same authoritative instance.

Do not create a global Singleton.

---

## Validation

Verify:

- board generation still uses Phase 2 code unchanged
- BoardView receives the same generated Board instance
- no global static gameplay state was introduced
- no gameplay logic was added to Bootstrap

---

## Learning Questions

- What is a composition root?
- Why should `BoardView` not call `BoardGenerator` directly?
- Why should `BoardGenerator` not instantiate presentation objects?
- Why keep the generated `Board` instance?
- Why are we avoiding a Singleton?

---

## Commit

```text
Connect board model to presentation
```

STOP after this commit.

---

# Step 4 — Unity Scene + Prefab Setup with MCP

## Goal

Create the actual visible 8×8 board in Unity.

This step should use Unity MCP for Editor operations where practical.

---

## Scene Setup

Use the existing scene unless there is a strong reason to create another.

Create a minimal hierarchy such as:

```text
SampleScene
├── Main Camera
├── Global Light 2D
└── GameRoot
    ├── BoardRoot
    └── BoardBootstrap
```

Exact hierarchy may differ slightly if the implementation provides a cleaner equivalent.

---

## Tile Prefab

Create a simple tile prefab.

It should include what `TileView` actually requires, for example:

```text
Tile
├── SpriteRenderer
└── TileView
```

Use simple visuals.

Do not spend time on final art.

The goal is to prove the presentation architecture.

---

## Wire References

Using MCP / Unity Editor:

- create required GameObjects
- add components
- create the tile prefab
- assign serialized fields
- connect `BoardBootstrap` to `BoardView`
- connect `BoardView` to the tile prefab/root
- configure width/height/seed/palette

Prefer Editor/MCP operations over hand-editing Unity YAML.

---

## PlayMode Smoke Test

Enter Play Mode.

Expected result:

```text
8 × 8 visible grid
64 tile views
different logical tile colors
no initial horizontal 3+ match
no initial vertical 3+ match
```

No interaction is required.

---

## Determinism Test

With the same:

```text
width
height
palette order
seed
```

the logical board should remain the same between runs.

Visual presentation should represent that board consistently.

---

## Camera

Adjust the camera only enough to make the board visible.

Do not implement a sophisticated dynamic camera system.

---

## Validation

Using Unity MCP:

- inspect scene hierarchy
- enter Play Mode
- check Unity Console
- verify expected number of tile objects
- verify no compile errors
- verify serialized references are assigned

If practical, inspect several logical positions and their `TileView` types.

---

## Learning Questions

- What parts of this step are scene configuration rather than game logic?
- Why is the prefab a presentation concern?
- Why does MCP help here more than it helped in the pure core phase?
- If tile spacing changes, which layer should change?
- If board contents change, which object remains authoritative?

---

## Commit

```text
Set up visible Match-3 board scene
```

STOP after this commit.

---

# Step 5 — Phase 3 Validation Pass

## Goal

Validate the entire presentation boundary before adding player input.

Do not add new gameplay features.

---

## Architecture Review

Expected dependency direction:

```text
Match3.Core
├── GridPosition
├── TileType
├── Board
└── BoardGenerator


Match3.Presentation
├── TileView
├── BoardView
└── BoardBootstrap

Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

---

## Verify Responsibilities

### Core

Owns:

```text
logical coordinates
logical tile identities
board state
initial board generation
```

Does not own:

```text
GameObjects
Transforms
Sprites
world positions
scene hierarchy
```

### Presentation

Owns:

```text
GameObjects
SpriteRenderer
world positions
tile visuals
scene wiring
```

Does not own:

```text
match rules
swap validation
gravity
refill
score
goals
```

---

## Validation Tasks

- run all existing EditMode tests
- verify all Phase 2 tests still pass
- verify Unity compilation
- inspect Console
- enter Play Mode
- verify visible board
- inspect dependency direction
- inspect for accidental gameplay logic inside views
- confirm no unnecessary patterns were introduced

---

## Do Not Add

Do not add:

- input handling
- tile selection
- swapping
- match detection
- gravity
- refill
- score
- moves
- goals
- State Pattern
- event bus
- object pooling
- ScriptableObject architecture
- dependency injection framework

Those belong to later phases only if needed.

---

## Final Learning Review

Explain in my own words:

- logical state vs presentation state
- `Board` vs `BoardView`
- `TileType` vs `TileView`
- why presentation references core
- why core must not reference presentation
- what the bootstrap/composition root does
- what Unity MCP contributed in this phase

---

## Commit

Only create a commit if validation requires a real code/test/configuration change.

Do not create an empty commit.

STOP and report whether Phase 3 is complete.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md.

We are executing Phase 3 using docs/phases/PHASE3_EXECUTION.md.

Read only:
- the project goals needed for context
- Step 1 of PHASE3_EXECUTION.md

Do not read or implement later steps yet.

For Step 1:
1. explain the intended implementation briefly
2. implement only Step 1
3. run relevant validation
4. verify Unity compilation and check the Console
5. summarize what changed
6. answer the Step 1 learning questions
7. commit using the specified commit message
8. STOP and wait for my approval

Do not proceed to Step 2 until I explicitly tell you to continue.
```

---

# Continue Prompt

```text
Approved.

Proceed to the next step in docs/phases/PHASE3_EXECUTION.md only.

Follow the same workflow:
- explain
- implement
- validate
- summarize
- answer the learning questions
- commit
- STOP

Do not start any later step.
```
