# Phase 5 Execution Plan — Match Detection

This phase adds pure Match-3 detection logic.

The goal is to identify every board cell that belongs to a horizontal or vertical run of 3 or more matching tiles.

This phase does **not** clear tiles, apply gravity, refill the board, score matches, or decide whether a swap should be rolled back.

---

# Phase Goal

Build this logical flow:

```text
Board
  ↓
MatchFinder
  ↓
scan horizontal runs
scan vertical runs
  ↓
collect all matched GridPositions
  ↓
return matched positions
```

The algorithm must be:

```text
deterministic
presentation-independent
fully testable in EditMode
```

---

# Architecture Goal

Expected dependency direction remains:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

Match detection belongs in Core because it is a gameplay rule.

The Core layer owns:

```text
Board
GridPosition
TileType
MatchFinder
```

The Presentation layer must not be needed to determine whether a match exists.

---

# Match Rules

A match is a contiguous horizontal or vertical run of the same non-empty `TileType` with length at least 3.

Examples:

```text
R R R            horizontal match of 3

B B B B          horizontal match of 4

G
G                vertical match of 5
G
G
G
```

Intersections are allowed:

```text
  R
R R R
  R
```

The center cell belongs to both a horizontal and vertical run, but should appear only once in the final matched-position set.

Overlapping groups must not lose positions.

`TileType.Empty` must never form a match.

---

# Preferred Result Shape

The result should be useful for later board resolution.

Preferred shape:

```csharp
IReadOnlyCollection<GridPosition>
```

or another simple collection that:

- contains every matched cell
- avoids duplicate positions when horizontal and vertical matches intersect
- does not expose Presentation types
- is convenient for later clearing

A `HashSet<GridPosition>` internally is acceptable because `GridPosition` already supports value equality and hashing.

Do not introduce a complex `Match`, `MatchGroup`, graph, or pattern hierarchy unless a concrete requirement appears.

---

# Working Rule

For every step:

1. Read only the current step.
2. Inspect the relevant Core code before editing.
3. Explain the intended design briefly.
4. Implement only that step.
5. Add or extend EditMode tests.
6. Run the relevant tests.
7. Verify Unity compilation and inspect the Console.
8. Summarize changed files and design decisions.
9. Answer the learning questions for that step.
10. Commit only when the step explicitly requests it.
11. STOP and wait for approval.

Do not implement later steps early.

---

# Step 1 — MatchFinder Skeleton and Horizontal Runs

## Goal

Introduce a pure Core `MatchFinder` and detect horizontal matches of length 3 or more.

No vertical detection yet.

---

## Preferred Design

Create:

```text
Assets/Scripts/Core/MatchFinder.cs
```

A preferred API is:

```csharp
public static IReadOnlyCollection<GridPosition> FindMatches(Board board)
```

Exact naming may differ only if there is a clearly simpler design.

For this step, the implementation may only scan horizontally.

The method should:

- reject a null Board clearly
- scan each row left to right
- detect contiguous runs of the same non-empty TileType
- add every position in runs of length 3+
- detect matches at the start, middle, and end of a row
- detect lengths 3, 4, 5, and longer

Do not mutate the Board.

---

## Suggested Horizontal Scan

For each row:

```text
track current run type
track run start
advance across columns
when type changes or row ends:
    if run length >= 3
        add every position in that run
```

Be especially careful with the final run at the right edge.

A sentinel-style loop or explicit final-run handling are both acceptable.

Prefer clarity over cleverness.

---

## Required Horizontal Tests

Add focused EditMode tests for at least:

```text
XXX.....
........

.XXX....
........

.....XXX
........

XXXX....
........

XXXXX...
........

X.X.X...
........
```

Also test:

- no horizontal matches
- two separate horizontal matches in one row
- horizontal matches on multiple rows
- `Empty` does not match
- Board remains unchanged after detection
- null Board behavior

---

## Do Not Add

Do not add:

- vertical scan
- clearing
- gravity
- refill
- scoring
- swap rollback
- animation
- Presentation dependencies
- match group classes
- special tile logic

---

## Learning Questions

I should be able to answer:

- Why does MatchFinder belong in Core?
- What is a contiguous run?
- Why should `Empty` be ignored?
- Why is the right-edge run a common bug?
- Why should MatchFinder read Board without modifying it?

---

## Commit

```text
Add horizontal match detection
```

STOP after this commit.

---

# Step 2 — Vertical Runs and Deduplicated Intersections

## Goal

Extend MatchFinder to detect vertical runs and combine them with horizontal results.

Intersections must be deduplicated.

---

## Vertical Scan

Use the same core idea as horizontal scanning, but scan each column bottom to top.

Conceptually:

```text
for each x:
    scan y = 0 ... height - 1
```

Detect:

- vertical matches of 3+
- matches starting at the bottom edge
- matches ending at the top edge
- lengths 4+
- lengths 5+

---

## Intersections

Example:

```text
..R.....
RRRR....
..R.....
```

If a cell belongs to both a horizontal and vertical match, return it only once.

A simple `HashSet<GridPosition>` is a good fit.

Do not remove information from either run.

---

## Required Tests

Add tests for:

```text
X.......
X.......
X.......

X.......
X.......
X.......
X.......

X.......
X.......
X.......
X.......
X.......

XXX.....
..X.....
..X.....
```

Also test:

- horizontal + vertical matches at the same time
- one-cell intersection
- multiple simultaneous non-overlapping matches
- multiple intersections
- corner matches
- edge matches
- no duplicates in returned positions

---

## Learning Questions

I should be able to answer:

- Why can horizontal and vertical detection use the same run concept?
- Why is a `HashSet<GridPosition>` useful?
- What happens when a position belongs to two matches?
- Why is deduplication important for later clearing?

---

## Commit

```text
Add vertical and intersecting match detection
```

STOP after this commit.

---

# Step 3 — Comprehensive MatchFinder Edge Cases

## Goal

Stress-test the pure algorithm before any board resolution is built on top of it.

Prefer adding tests over changing the API unless tests reveal a real problem.

---

## Required Coverage

At minimum, verify:

```text
no matches
horizontal 3 / 4 / 5+
vertical 3 / 4 / 5+
T or cross intersection
overlapping horizontal/vertical groups
multiple simultaneous matches
edge matches
corner matches
Empty cells never match
```

Also verify:

- every returned position is in bounds
- no duplicate positions are returned
- unmatched positions are not returned
- calling MatchFinder twice on the same Board returns equivalent results
- MatchFinder never changes the Board

---

## Complexity Review

For board width `W` and height `H`:

```text
horizontal scan: O(W × H)
vertical scan:   O(W × H)
total:           O(W × H)

worst-case matched-position storage: O(W × H)
```

Do not optimize beyond this unless a concrete problem appears.

---

## Learning Questions

I should be able to answer:

- Why is the algorithm O(W × H)?
- What is the worst-case memory usage?
- Which edge case is easiest to miss?
- Why should repeated calls return the same result?
- What test catches accidental inclusion of `Empty`?

---

## Commit

```text
Strengthen MatchFinder coverage
```

Only create this commit if Step 3 adds or changes tests/code.

STOP after this commit.

---

# Step 4 — MatchFinder API Review for Board Resolution

## Goal

Review whether the result shape is sufficient for the next phase without implementing clearing or gravity.

Do not add future systems.

---

## Review Questions

Inspect the final MatchFinder API and answer:

- Can later code iterate every matched cell?
- Can intersections be processed without duplicate clearing?
- Can we query whether any matches exist?
- Does the result expose only Core types?
- Is the API simple enough to test directly?
- Does MatchFinder mutate anything?
- Does the caller need richer match metadata now?

Expected answer to the last question is probably **no**.

Do not add match-group metadata speculatively.

---

## Optional Tiny API Improvement

Only if clearly justified, a helper like:

```csharp
bool HasMatches(Board board)
```

may be considered.

Do not add it merely for convenience if:

```csharp
FindMatches(board).Count > 0
```

is already sufficient and clear.

---

## Explicitly Confirm Absent

There must still be no:

- tile clearing
- gravity
- refill
- cascades
- score
- combo system
- swap validation
- invalid-swap rollback
- animations
- special tiles
- event bus
- state machine
- pooling
- Presentation dependency

---

## Learning Questions

I should be able to answer:

- Why are matched positions enough for the next resolution step?
- When would richer match-group information become justified?
- Why avoid speculative APIs?
- Why does keeping MatchFinder stateless help testing?

---

## Commit

Create a commit only if a real code/test change is required.

Do not create an empty commit.

STOP after review.

---

# Step 5 — Phase 5 Validation Pass

## Goal

Verify MatchFinder is correct, deterministic, independent, and ready for later board resolution.

Do not add new gameplay features.

---

## Run

- all EditMode tests
- all existing PlayMode tests
- Unity compilation
- Console inspection

Phase 4 behavior must remain unchanged.

---

## Core Review

Expected Core responsibilities:

```text
Board
├── tile storage
├── bounds validation
└── adjacent logical swapping

BoardGenerator
└── deterministic no-initial-match generation

MatchFinder
└── detect all horizontal/vertical runs of 3+
```

---

## MatchFinder Review

Confirm:

- horizontal 3+
- vertical 3+
- length 4+
- length 5+
- edge matches
- corner matches
- multiple simultaneous matches
- intersections
- overlapping horizontal/vertical groups
- no duplicate returned positions
- Empty ignored
- Board unchanged
- no Unity dependency

---

## Explicitly Confirm Absent

There must still be no:

- clearing
- gravity
- refill
- cascades
- score
- goals
- move counter
- special tiles
- swap rollback
- match-driven Presentation behavior
- event bus
- state machine
- command pattern
- object pooling

---

## Final Learning Review

I should be able to explain:

- the run-scanning algorithm
- horizontal scanning
- vertical scanning
- how intersections are deduplicated
- why HashSet fits the result
- time complexity
- memory complexity
- important edge cases
- alternative implementations
- likely failure modes
- why MatchFinder stays independent from Presentation

---

## Commit

Create a commit only if validation requires a real change.

Do not create an empty commit.

STOP and report whether Phase 5 is complete.

Do not start Phase 6.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md and Docs/Phases/PHASE5_EXECUTION.md.

We are executing Phase 5 — Match Detection.

Read only:
- the project context needed for the task
- Step 1 — MatchFinder Skeleton and Horizontal Runs

Do not read or implement later steps yet.

Before modifying files:
1. inspect Board, GridPosition, TileType, and existing Core tests
2. explain the simplest MatchFinder API
3. explain the horizontal run-scanning algorithm
4. explain why this belongs in Core
5. identify the common right-edge/end-of-run failure case

Then implement Step 1 only.

Requirements:
- create a pure Core MatchFinder
- detect horizontal contiguous runs of 3+
- ignore TileType.Empty
- return matched GridPositions in a form useful for later board resolution
- do not mutate Board
- add focused EditMode tests
- do not implement vertical detection yet
- do not implement clearing, gravity, refill, scoring, or swap validation
- do not add unnecessary abstractions

After implementation:
1. run relevant EditMode tests
2. verify Unity compilation
3. inspect Console
4. summarize changed files
5. answer the Step 1 learning questions
6. commit with:

Add horizontal match detection

7. STOP and wait for approval.

Do not start Step 2.
```

---

# Continue Prompt

```text
Approved.

Proceed to the next step in Docs/Phases/PHASE5_EXECUTION.md only.

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

Do not start any later step.
```

---

# Claude Teaching Prompt

Use this after MatchFinder is implemented and validated.

```text
Do not modify files.

Teach me the current MatchFinder implementation as if I must defend it in a technical interview.

Use the actual repository implementation.

Explain:

1. the algorithm step by step
2. horizontal run scanning
3. vertical run scanning
4. how intersections and overlaps are handled
5. why the chosen result collection is appropriate
6. time complexity
7. memory complexity
8. important edge cases
9. common implementation bugs
10. alternative implementations and tradeoffs
11. why this design is independent from Presentation
12. how I could implement a basic MatchFinder myself from scratch

Keep the explanation grounded in the actual code. Do not modify files.
```
