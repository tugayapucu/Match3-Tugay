# Phase 2 Execution Plan — Core Board Model

This file breaks Phase 2 into small learning-oriented checkpoints.

## Working Rule

For every step:

1. Read only the current step.
2. Explain the intended implementation briefly.
3. Implement only that step.
4. Run the relevant tests.
5. Check Unity compilation / Console when applicable.
6. Summarize what changed and why.
7. Commit using the specified commit message.
8. STOP and wait for my approval before continuing.

Do not implement future steps early.

---

## Step 1 — Core Assembly + Fundamental Value Types

### Goal
Create the smallest possible core layer.

### Implement
```text
Assets/Scripts/Core/
    Match3.Core.asmdef
    GridPosition.cs
    TileType.cs
```

### Requirements
`GridPosition`:
- immutable value type
- integer X and Y
- value equality
- no Unity types
- no world-position logic

`TileType`:
- enum
- `Empty = 0`
- several playable colors/types
- no sprite/material references

`Match3.Core.asmdef`:
- core gameplay assembly
- no unnecessary Unity dependencies
- disable Unity engine references if compatible with the planned code

### Tests
Create only tests needed for these types:
- GridPosition equality
- GridPosition inequality
- X/Y values
- TileType.Empty is zero

### Learning Questions
- Why is `GridPosition` a struct instead of a class?
- Why should it be immutable?
- Why does value equality matter?
- Why is `TileType` an enum right now?
- Why is `Empty = 0` useful?

### Commit
```text
Add core Match-3 value types
```

STOP after this commit.

---

## Step 2 — Board State Container

### Goal
Create the authoritative logical board state.

### Implement
```text
Assets/Scripts/Core/
    Board.cs
```

### Requirements
- positive configurable width/height
- private `TileType[,]` storage
- board owns its internal array
- do not expose raw array
- controlled reads/writes
- bounds validation
- new board starts as `Empty`
- no Unity dependencies

### Tests
- width/height
- initialization to Empty
- valid reads/writes
- invalid coordinates
- invalid dimensions
- rectangular boards
- 1x1 board behavior

### Learning Questions
- Why does `Board` own the array?
- Why not expose `TileType[,]` publicly?
- Why is `Board` a class while `GridPosition` is a struct?
- What does “authoritative state” mean?
- What should future `BoardView` be allowed to do?

### Commit
```text
Implement logical board state
```

STOP after this commit.

---

## Step 3 — Board Generator

### Goal
Generate deterministic initial boards with no automatic matches.

### Implement
```text
Assets/Scripts/Core/
    BoardGenerator.cs
```

### Requirements
- accepts width and height
- accepts a playable palette
- rejects `TileType.Empty`
- rejects duplicate palette entries
- requires at least 3 distinct playable tile types
- uses explicitly seeded `System.Random`
- fills deterministically for the same seed
- prevents initial horizontal matches of 3+
- prevents initial vertical matches of 3+
- no retry loop required for ordinary generation
- no Unity dependencies

### Generation Rule
Fill left-to-right, bottom-to-top.

When choosing a value for `(x, y)`:
- if `(x-1, y)` and `(x-2, y)` are equal, exclude that type
- if `(x, y-1)` and `(x, y-2)` are equal, exclude that type
- choose randomly from remaining playable types

### Tests
- complete board generation
- same seed => same board
- different seeds usually produce different boards
- no initial horizontal 3+ matches
- no initial vertical 3+ matches
- rectangular boards
- small boards
- Empty rejected from palette
- duplicate palette rejected
- fewer than 3 playable types rejected

Run multiple seeds for the no-match tests.

### Learning Questions
- Why does checking the previous two cells prevent runs of 3+?
- Why does this also prevent runs of 4 or 5?
- Why use `System.Random` with a seed?
- Why is reproducibility useful for debugging?
- Why does Phase 2 not guarantee at least one legal move?

### Commit
```text
Add deterministic board generation
```

STOP after this commit.

---

## Step 4 — Phase 2 Validation Pass

### Goal
Validate the completed Phase 2 without adding new architecture.

### Tasks
- run all EditMode tests
- verify Unity compilation
- inspect Unity Console
- inspect the core code for accidental Unity dependencies
- check that no unnecessary abstraction was introduced
- summarize the full Phase 2 architecture

### Do Not Add
- `Tile` class
- `IBoard`
- `IRandomProvider`
- factories
- views
- MonoBehaviours
- presentation code
- swapping
- match detection
- gravity
- refill

### Learning Review
Explain:
```text
GridPosition
TileType
Board
BoardGenerator
```

Also explain how Phase 3 will render the board without moving gameplay rules into Unity views.

### Commit
Only create a commit if validation requires a real code/test/doc change.
If no files need changing, do not create an empty commit.

STOP and report that Phase 2 is complete.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md.

We are executing Phase 2 using PHASE2_EXECUTION.md.

Read only:
- the project goals needed for context
- Step 1 of PHASE2_EXECUTION.md

Do not read or implement later steps yet.

For Step 1:
1. explain the intended implementation briefly
2. implement only Step 1
3. run relevant tests
4. verify compilation/Unity Console as appropriate
5. summarize what changed and explain the learning questions
6. commit using the specified commit message
7. STOP and wait for my approval

Do not proceed to Step 2 until I explicitly tell you to continue.
```

# Continue Prompt

```text
Approved.

Proceed to the next step in PHASE2_EXECUTION.md only.

Follow the same workflow:
- explain
- implement
- test
- validate
- summarize
- commit
- STOP

Do not start any later step.
```
