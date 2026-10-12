# Phase 6 Execution Plan — Swap Validation

This phase turns adjacency-only swapping into real Match-3 move validation.

A player may still attempt any orthogonally adjacent swap, but the swap only remains if the resulting board contains a match involving at least one of the two swapped cells.

Invalid swaps must still animate forward so the player sees the attempted move, then animate back to the original state.

This phase does **not** clear matches, apply gravity, refill the board, resolve cascades, score moves, create special tiles, or add move goals.

---

# Phase Goal

Build this gameplay flow:

```text
first tile selected
        ↓
adjacent second tile selected
        ↓
logical Board swap
        ↓
validate resulting Board
        ↓
does a match involve either swapped position?
        ↓
   yes       no
    ↓         ↓
animate     animate
forward     forward
    ↓         ↓
 keep      restore Board
              ↓
          animate back
              ↓
        original state restored
```

Input remains locked for the entire operation.

For an invalid swap, unlocking must happen only after the rollback animation finishes.

---

# Architecture Goal

The existing dependency direction remains:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

Gameplay validation belongs in Core.

Presentation may orchestrate:

```text
input
logical operation
animation
rollback animation
locking
```

but Presentation must not own the rule:

```text
"a swap is valid only if the resulting Board creates
a match involving one of the swapped cells"
```

Keep that rule testable without Unity.

---

# Current Relevant Responsibilities

Before Phase 6:

```text
Board
├── stores tile values
├── validates bounds
└── swaps orthogonally adjacent cells

MatchFinder
└── returns every horizontal/vertical matched GridPosition

BoardInputController
├── polls mouse input
├── picks TileView
├── remembers first selection
├── requests Board swap
└── locks input during animation

BoardView
├── maps GridPosition → TileView
└── animates two visuals exchanging positions
```

Phase 6 should extend these responsibilities minimally.

Do not turn `BoardInputController` into the owner of gameplay rules.

---

# Swap Validity Rule

After an adjacent logical swap has been applied:

```text
valid
```

only if `MatchFinder` finds a match containing:

```text
first swapped position
OR
second swapped position
```

A match somewhere else on the Board is **not enough**.

Example:

```text
R R R . . .     existing unrelated match
. . . B G .     swapped cells here
```

If swapping `B` and `G` creates no match involving those two positions, the swap is invalid even though the Board contains `RRR` elsewhere.

This explicit rule keeps validation correct even if a future bug or intermediate board state contains unrelated matches.

---

# Logical / Visual Timing

The intended model-view timing is:

## Valid swap

```text
1. Board swaps
2. Core validates the swapped state
3. visuals animate forward
4. animation finishes
5. input unlocks
```

The Board changes first; the view catches up.

## Invalid swap

```text
1. Board swaps
2. Core validates the swapped state as invalid
3. visuals animate forward
4. Board is restored to the original logical state
5. visuals animate back
6. input unlocks
```

During rollback animation, the Board should already represent the final original state.

This preserves the existing principle:

```text
logical state changes first
visual state catches up afterward
```

Input must remain locked across both animations.

---

# Preferred Core Design

Prefer a very small, stateless Core abstraction for the validation rule.

A possible shape is:

```csharp
public static class SwapValidator
{
    public static bool IsValidResult(
        Board board,
        GridPosition first,
        GridPosition second);
}
```

The intended meaning would be:

```text
The Board already contains the result of the attempted swap.
Return true only if a detected match contains first or second.
Do not modify the Board.
```

Exact naming may differ if Codex finds a simpler, clearer API after inspecting the current code.

Important constraints:

- validation logic stays in Core
- no Unity references
- no Presentation references
- no hidden state
- no Board mutation inside a query-style validator
- do not add a complex move/result hierarchy without a concrete need
- do not change `MatchFinder`'s public API unless Phase 6 exposes a real problem that cannot be solved cleanly otherwise

A simple loop over the returned matched positions is sufficient if membership needs to be checked without relying on MatchFinder's concrete collection type.

---

# Working Rule

For every step:

1. Read only the current step.
2. Inspect the relevant existing code before editing.
3. Explain the intended design briefly.
4. Implement only that step.
5. Add or extend tests.
6. Run the relevant tests.
7. Verify Unity compilation and inspect the Console.
8. Summarize changed files and design decisions.
9. Answer the learning questions for that step.
10. Commit only when the step explicitly requests it.
11. STOP and wait for approval.

Do not implement later steps early.

---

# Step 1 — Pure Core Swap Validation

## Goal

Add a pure Core rule that determines whether an already-applied adjacent swap produced a match involving either swapped cell.

Do not change Presentation yet.

---

## Before Editing

Inspect:

```text
Board
MatchFinder
GridPosition
existing Board tests
existing MatchFinder tests
```

Explain:

- where the validation rule should live
- why `BoardInputController` should not own it
- whether a new small Core type is warranted
- whether the current `MatchFinder` result is sufficient without changing its API
- the exact contract: does the validator expect the Board before or after the attempted swap?

Prefer an explicit contract over cleverness.

---

## Required Logical Cases

At minimum, test:

- valid when the first swapped position participates in a match
- valid when the second swapped position participates
- valid when both swapped positions participate
- invalid when there are no matches
- invalid when only an unrelated existing match exists
- valid when a swapped position is part of an intersection
- valid for length 4+ and 5+ runs involving a swapped position
- null / out-of-bounds handling consistent with existing Core conventions

The unrelated-match test is especially important.

---

## Invariants

The validator must:

- not mutate the Board
- not animate anything
- not know about mouse/touch
- not know about `TileView`
- not know about `BoardView`
- return the same result when called repeatedly on the same Board
- use only Core/.NET types

---

## Learning Questions

I should be able to answer:

- Why isn't `FindMatches(board).Count > 0` always sufficient?
- Why must the match involve one of the swapped cells?
- Why should the validator be a pure Core rule?
- Why should a query-style validator not mutate the Board?
- Why can an unrelated pre-existing match cause a false positive?
- Does Phase 6 require richer `MatchGroup` information? Why or why not?

---

## Commit

```text
Add swap match validation
```

STOP after this commit.

---

# Step 2 — Integrate Validation into the Swap Flow

## Goal

Use the Core validation rule in `BoardInputController`.

A valid swap remains.

An invalid adjacent swap animates forward, restores the Board, animates back, then unlocks input.

---

## Intended Runtime Flow

For an adjacent second selection:

```text
lock input
    ↓
Board.TrySwapAdjacent(first, second)
    ↓
Core swap validation
    ↓
BoardView.AnimateSwap(first, second)
    ↓
valid?
├── yes
│    ├── clear selection
│    └── unlock
│
└── no
     ├── restore Board by swapping the same cells back
     ├── BoardView.AnimateSwap(first, second) again
     ├── clear selection
     └── unlock
```

The first visual animation should occur for both valid and invalid attempts.

Do not make invalid swaps visually disappear instantly.

---

## Reuse Existing Animation

Prefer reusing:

```csharp
BoardView.AnimateSwap(first, second, duration)
```

for the rollback.

After the first animation, `BoardView` has already updated:

```text
GridPosition → TileView lookup
TileView.Position
```

Calling the same swap animation again should naturally return the visuals and lookup to their original arrangement.

Do not add a separate rollback animation system unless the existing API genuinely cannot express it.

---

## Input Lock

The lock begins before the Board mutates.

For valid swaps:

```text
locked during forward animation
unlock after forward animation
```

For invalid swaps:

```text
locked during forward animation
locked during logical rollback
locked during backward animation
unlock only after rollback animation finishes
```

Clicks during either animation must do nothing.

---

## Selection State

After the whole operation:

```text
valid swap   → selection cleared
invalid swap → selection cleared
```

During an invalid rollback, do not allow a new selection between the two animations.

---

## Failure / Exception Safety

Do not overengineer a transaction framework.

But inspect the current coroutine flow and ensure normal validation branches cannot leave:

```text
isInputLocked = true
```

forever.

If a small local restructuring makes the normal flow clearer, it is acceptable.

Do not add a global state machine.

---

## Required PlayMode Tests

At minimum:

### Valid swap

- creates a match involving a swapped cell
- Board remains swapped
- visuals end at swapped positions
- input unlocks after one animation
- selection clears

### Invalid swap

- Board initially swaps
- first animation completes
- Board returns to original tile arrangement
- second animation returns visuals
- `TileView.Position` returns to original cells
- BoardView lookup returns to original mapping
- input unlocks only after rollback completes
- selection clears

### Rapid input during invalid rollback

Attempt clicks during:

```text
forward animation
rollback animation
```

Expected:

```text
one attempted logical swap
one forward animation
one rollback
no extra mutation
```

### Same / diagonal / distant selection

Existing Phase 4 behavior remains unchanged.

These are rejected before a logical adjacent swap occurs.

### Valid no-regression case

A valid matching swap must not animate back.

---

## Learning Questions

I should be able to answer:

- Why validate after the logical swap?
- Why does an invalid move still animate forward?
- Why restore the Board before the rollback animation?
- Why can the same `AnimateSwap` method be reused in reverse?
- Why must input stay locked through both animations?
- What is the difference between a non-adjacent selection and an adjacent-but-invalid Match-3 move?

---

## Commit

```text
Rollback invalid swaps
```

STOP after this commit.

---

# Step 3 — Logical / Visual Consistency Tests

## Goal

Stress the boundary between the authoritative Board and the animated views.

Prefer adding tests over changing production code unless a real defect is found.

---

## Required Scenarios

Test at least:

### Valid matching swap

After completion:

```text
Board == swapped state
TileView.Position == swapped state
BoardView lookup == swapped state
input unlocked
selection cleared
```

### Invalid adjacent swap

After completion:

```text
Board == original state
TileView.Position == original state
BoardView lookup == original state
input unlocked
selection cleared
```

### Repeated invalid attempts

Perform an invalid swap, let it roll back, then repeat it.

The second attempt must behave exactly like the first.

### Invalid then valid

```text
invalid attempt → rollback
valid attempt   → keep
```

The rollback must not leave stale view lookup state.

### Valid then invalid

```text
valid attempt   → keep
invalid attempt → rollback relative to the new Board state
```

Do not assume the original generated board forever.

### Match elsewhere

Construct a Board with an unrelated existing match.

An adjacent swap elsewhere that creates no match involving its swapped positions must roll back.

### Intersecting result

A swap that creates an intersection involving a swapped cell is valid.

---

## Animation Timing Assertions

Where practical, verify intermediate states without making tests fragile.

During forward invalid animation:

```text
Board = attempted swapped state
input locked
```

During rollback animation:

```text
Board = restored original state
input locked
```

After completion:

```text
Board and views agree
input unlocked
```

Do not assert exact frame counts.

Prefer state transitions over timing implementation details.

---

## Learning Questions

I should be able to answer:

- What does "Board is authoritative" mean during forward animation?
- What does it mean during rollback animation?
- Why are end-state lookup checks important?
- Why test invalid→valid and valid→invalid sequences?
- What bug would a stale `Dictionary<GridPosition, TileView>` cause?

---

## Commit

```text
Strengthen swap rollback coverage
```

Only create this commit if Step 3 adds or changes tests/code.

STOP after this commit.

---

# Step 4 — Runtime Smoke Test and Architecture Review

## Goal

Verify the real scene behaves like a Match-3 swap interaction without introducing resolution systems.

No new feature work.

---

## SampleScene Smoke Tests

In Play Mode verify:

### Valid move

Perform a swap that creates a match.

Expected:

```text
swap forward
match remains on Board
visuals remain swapped
input restores
```

Do not clear the match yet.

### Invalid adjacent move

Perform a swap that creates no match.

Expected:

```text
swap forward
swap backward
original Board restored
original visuals restored
input restores
```

### Rapid clicking

During both directions of invalid rollback:

```text
extra clicks ignored
```

### Existing invalid-selection behavior

Verify:

```text
same tile
diagonal tile
distant tile
```

still do not start an adjacent swap animation.

---

## Architecture Review

Confirm responsibilities remain:

```text
Board
├── authoritative tile storage
└── primitive adjacent logical swap

MatchFinder
└── detect matched cells

SwapValidator
(or chosen Core equivalent)
└── decide whether the swapped Board state is a valid Match-3 result

BoardInputController
├── input
├── selection
├── operation orchestration
└── input locking

BoardView
└── visual lookup + animation

TileView
└── one tile visual

BoardBootstrap
└── composition root / wiring
```

If implementation used a different small Core type name, review the actual structure rather than forcing `SwapValidator`.

---

## Explicitly Confirm Absent

There must still be no:

- match clearing
- gravity
- refill
- cascades
- score
- combo system
- goals
- move counter
- special tiles
- automatic match-resolution animation
- event bus
- state machine
- command pattern
- object pooling
- global Board singleton

A valid swap may leave a visible match on the board. That is intentional.

---

## Learning Questions

I should be able to explain:

- the full click-to-validation-to-animation flow
- where adjacency is checked
- where match validity is checked
- who owns rollback orchestration
- when Board changes during an invalid attempt
- when BoardView lookup changes
- why input remains locked
- why matches are not cleared yet

---

## Commit

Create a commit only if the smoke test or review requires a real fix.

Do not create an empty commit.

STOP after the review.

---

# Step 5 — Phase 6 Validation Pass

## Goal

Close Phase 6 with full regression validation.

Do not start match resolution.

---

## Run

- all EditMode tests
- all PlayMode tests
- Unity compilation
- Console inspection

---

## Confirm Logical Rules

### Non-adjacent

```text
same
diagonal
distant
```

do not mutate the Board.

### Adjacent + creates relevant match

```text
valid
Board stays swapped
```

### Adjacent + no relevant match

```text
invalid
Board restored
```

### Unrelated existing match

```text
does not validate an unrelated swap
```

---

## Confirm Presentation Rules

Valid swap:

```text
one forward animation
final visual state = swapped
```

Invalid swap:

```text
one forward animation
one rollback animation
final visual state = original
```

For both:

```text
input locked until operation complete
selection cleared afterward
Board and views agree afterward
```

---

## Confirm Boundaries

Core must still have no Unity dependency.

Presentation may depend on Core validation APIs, but Core must not depend on Presentation.

---

## Explicitly Confirm Absent

There must still be no:

- clearing
- gravity
- refill
- cascade loop
- scoring
- goals
- move counter
- special tiles
- match-resolution Presentation
- global game manager
- event bus
- state machine
- command framework
- pooling

---

## Final Learning Review

I should be able to explain:

1. why adjacency and match validity are separate rules
2. why validation happens after the logical swap
3. why a match must involve a swapped cell
4. why unrelated matches cannot validate the move
5. valid-swap model/view timing
6. invalid-swap model/view timing
7. why rollback restores Board before visuals animate back
8. why the input lock spans the entire transaction
9. how MatchFinder is reused without gaining Presentation knowledge
10. why match clearing belongs to a later phase

---

## Commit

Create a commit only if validation requires a real change.

Do not create an empty commit.

STOP and report whether Phase 6 is complete.

Do not start Phase 7.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md and Docs/Phases/PHASE6_EXECUTION.md.

We are executing Phase 6 — Swap Validation.

Read only:
- the project context needed for this task
- Step 1 — Pure Core Swap Validation

Do not read or implement later steps yet.

Before modifying files:
1. inspect Board, MatchFinder, GridPosition, and their existing EditMode tests
2. explain where the rule "a swap is valid only if the resulting match involves a swapped cell" should live
3. propose the smallest Core API for that rule
4. state clearly whether the API expects the Board before or after the attempted swap
5. explain why an unrelated existing match must not validate the swap
6. confirm whether MatchFinder's current public API is sufficient without modification

Then implement Step 1 only.

Requirements:
- keep validation in Match3.Core
- no Unity or Presentation dependency
- validate the already-swapped Board state, unless you justify a simpler contract before coding
- valid only when a match contains first or second swapped GridPosition
- unrelated matches must not validate the move
- do not mutate Board inside a query-style validator
- add comprehensive focused EditMode tests
- do not modify Presentation yet
- do not implement rollback animation yet
- do not implement clearing, gravity, refill, scoring, cascades, or special tiles
- do not add unnecessary abstractions

After implementation:
1. run relevant EditMode tests
2. run the existing EditMode suite if practical
3. verify Unity compilation
4. inspect Console
5. summarize changed files and design decisions
6. answer the Step 1 learning questions
7. commit with:

Add swap match validation

8. STOP and wait for approval.

Do not start Step 2.
```

---

# Continue Prompt

```text
Approved.

Proceed to the next step in Docs/Phases/PHASE6_EXECUTION.md only.

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

Use this after Phase 6 is implemented and validated.

```text
Do not modify files.

Teach me the current Phase 6 swap-validation implementation as if I must defend it in a technical interview.

Use the actual repository implementation.

Explain:

1. the full valid-swap flow
2. the full invalid-swap + rollback flow
3. why adjacency and match validity are separate rules
4. why validation happens on the swapped Board state
5. why the match must involve one of the swapped positions
6. why an unrelated existing match must not validate a move
7. how MatchFinder is reused
8. which class owns the validation rule
9. which class owns animation/rollback orchestration
10. Board state during forward animation
11. Board state during rollback animation
12. BoardView lookup changes across both animations
13. why input remains locked through the whole operation
14. important edge cases and failure modes
15. time/memory complexity of validation
16. alternative designs and their tradeoffs
17. why clearing/gravity/refill are deliberately absent

Keep the explanation grounded in the actual code.
Do not modify files.
```
