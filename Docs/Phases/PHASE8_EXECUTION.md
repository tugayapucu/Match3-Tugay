# Phase 8 Execution Plan — Explicit Game State

Phase 8 is a **decision-first architecture phase**.

The goal is not to force the State Pattern into the project. The goal is to inspect the Phase 7 gameplay flow and decide whether more explicit state would materially improve clarity, changeability, or testing.

A valid outcome is any one of:

```text
A. keep the current simple representation
B. introduce one small explicit enum/state value
C. introduce state classes only if the current code truly justifies them
```

Do **not** choose C merely because this phase is named "Explicit Game State".

The current behavior must remain unchanged:

```text
player selects adjacent tiles
        ↓
logical swap
        ↓
valid?
 ↙              ↘
no               yes
↓                 ↓
forward           forward
animation         animation
↓                 ↓
rollback          resolve
↓                 ↓
backward          clear / fall / refill / cascades
animation         ↓
↓                 stable
└──────────────┬──┘
               ↓
          selection clear
          input available
```

This phase does **not** add scoring, moves, goals, win/lose behavior, special tiles, boosters, hints, dead-board shuffling, or a generic state-machine framework.

---

# Critical Learning Rule for Unity Editor / MCP Actions

This rule applies to every step.

Before any Unity Editor / Unity MCP action that inspects or changes scene state, prefabs, components, serialized fields, project settings, Inspector data, or Play Mode:

1. Tell me **what** you are about to do.
2. Explain **why** it is necessary.
3. Tell me where I would do the same thing manually in Unity.
4. Give the exact manual steps.
5. Explain how I can verify the result.

After the action, report exactly what changed or what was observed.

Do **not** silently manipulate the scene, prefab, Inspector, Play Mode, or project settings.

If no Unity-side action is needed, say so.

---

# Phase Goal

The project already has temporal gameplay phases, but today they are represented mainly by:

```text
coroutine position
isInputLocked
selectedPosition
BoardResolutionRunner execution
```

Phase 8 asks whether those implicit phases should become explicit.

Potential concepts:

```text
Idle
Swapping
Resolving
Clearing
Falling
Refilling
Win
Lose
```

Do not assume all of these belong in one state model.

First determine:

```text
Which are real externally meaningful states?
Which are only implementation substeps?
Which belong to a later level system?
Which are separate state dimensions?
```

A likely current interaction flow is:

```text
Idle
 ↓ accepted adjacent attempt
Swapping
 ├── invalid → rollback → Idle
 └── valid   → Resolving → Idle
```

This is only a hypothesis until the real code is inspected.

---

# Architecture Principles

Keep the existing dependency direction:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

Core owns logical Match-3 rules:

```text
Board
MatchFinder
SwapValidator
BoardResolution
BoardGenerator
logical value types
```

Presentation owns temporal interaction behavior:

```text
input
selection
swap animation
rollback sequencing
resolution sequencing
visual animation
input gating
```

A state such as:

```text
"swap animation is running"
"board is resolving"
```

is probably a Presentation concern.

Future concepts such as:

```text
level won
level lost
moves remaining
goal complete
```

belong to the later level/gameplay loop and must not be invented here.

---

# Important Design Question — One State Model or Several Concepts?

The potential list may contain separate dimensions:

```text
Board interaction:
Idle
Swapping
Resolving

Resolution substep:
Clearing
Falling
Refilling

Level outcome:
Playing
Won
Lost
```

Before coding, explicitly ask:

- Does any current consumer need to know Clearing vs Falling vs Refilling?
- Or are those private sequencing details inside `BoardResolutionRunner`?
- Does Win/Lose exist in the current game at all?
- Would placeholder Win/Lose values be dead code?
- Is the current busy/ready distinction already enough?
- Should Phase 9 introduce level status separately?

Prefer cohesive state models over one giant "everything state".

---

# Explicit State vs State Pattern

These are different ideas.

A simple explicit value might be:

```csharp
public enum BoardInteractionState
{
    Idle,
    Swapping,
    Resolving
}
```

That is **not** the State Pattern.

A richer State Pattern might look like:

```text
IState
├── IdleState
├── SwappingState
└── ResolvingState
```

with behavior delegated to objects.

Do not introduce a class hierarchy unless states have enough distinct behavior and lifecycle to justify separate objects.

A class hierarchy where each state only stores its name is worse than an enum.

---

# Decision Criteria

## Keep the current/simple design when

- transitions are centralized
- the number of meaningful states is tiny
- state mainly answers whether input is accepted
- coroutine sequencing is already easy to follow
- no state-specific polymorphic behavior is needed
- tests already express the behavior clearly

## Prefer a simple enum when

- `isInputLocked` hides **why** input is locked
- tests or future code benefit from distinguishing Swapping from Resolving
- one state value can replace a boolean rather than duplicate it
- transitions remain centralized

## Consider state classes only when there is concrete evidence

Examples:

- each state owns materially different behavior
- transition logic is scattered
- state-specific conditionals are growing
- entry/exit work is substantial
- adding a state requires touching many unrelated places
- isolated state objects clearly simplify tests

Future possibilities alone are not evidence.

These are **not** sufficient reasons:

```text
"State Pattern is common in games"
"we may have Win/Lose later"
"it looks good in a portfolio"
"the diagram looks cleaner"
```

---

# Single Source of Truth Rule

If explicit state is introduced, avoid two independent mutable truths:

```text
isInputLocked
+
state
```

Bad example:

```text
state = Resolving
isInputLocked = false
```

Prefer one authoritative state.

For example, if an enum is chosen, this may be reasonable:

```csharp
public bool IsInputLocked => State != BoardInteractionState.Idle;
```

Exact naming must be decided after inspecting the real code.

---

# Working Rule

For every step:

1. Read only the current step.
2. Inspect relevant code before recommending architecture.
3. Explain the current behavior before proposing changes.
4. Prefer the smallest design that solves a demonstrated problem.
5. Follow the Unity learning rule for Editor/MCP actions.
6. Implement only after the step explicitly allows it.
7. Add tests only for real contracts.
8. Run relevant tests.
9. Verify Unity compilation and Console.
10. Summarize changes and design decisions.
11. Answer the learning questions.
12. Commit only when explicitly requested.
13. STOP and wait for approval.

Do not implement later steps early.

---

# Step 1 — Audit the Current State Flow

## Goal

Understand how state is represented **today**.

Do not modify files.

Do not add an enum.

Do not add state classes.

## Inspect

At minimum:

```text
BoardInputController
BoardResolutionRunner
BoardBootstrap
BoardView
relevant Phase 6/7 PlayMode tests
```

Search for all reads/writes of:

```text
isInputLocked
IsInputLocked
selectedPosition
SelectedPosition
Initialize
FinishSwap
ResolveUntilStable
HandlePointerDown
```

Use actual repository names.

## Produce the Actual Transition Graph

Trace:

### First selection

- What changes?
- Does the game become busy?
- Can the next click still be accepted?

### Illegal/non-adjacent second selection

- Does state change?
- Is selection preserved?
- Does animation start?

### Accepted adjacent swap

- At exactly what point is input blocked?
- When is the Board swapped?
- When does animation start?

### Invalid swap

- What is true during forward animation?
- What is true during rollback?
- When is input available again?

### Valid swap

- When does Swapping conceptually end?
- When does Resolving conceptually begin?
- When does resolving end?

### Cascades

- Does the controller need to know Clearing/Falling/Refilling?
- Or only that resolution is still running?

### Completion

- What clears selection?
- What releases input?
- Is there one exit point or several?

## Identify Current State Sources

List every mechanism that currently implies phase:

```text
isInputLocked
selectedPosition
coroutine position
BoardResolutionRunner execution
```

Do not call them problems automatically. Explain what each represents.

## Current Invariants

Verify from code:

```text
input accepted only while ready
selection persists while an accepted move is executing
invalid rollback never enters resolution
valid move enters resolution only after forward swap animation
input returns only after rollback or full stable resolution
```

## Learning Questions

I should be able to answer:

- What is program state even without the State Pattern?
- Which values/control flow currently imply gameplay phase?
- What is interaction state vs a resolution substep?
- Why is coroutine position part of temporal state?
- Why inspect real transitions before selecting a pattern?

## Commit

No commit.

No files change.

STOP and present the audit.

Do not start Step 2 until approved.

---

# Step 2 — Architecture Decision

## Goal

Compare the current/simple approach with an enum and explicit state classes.

Do not modify files.

---

## Option 0 — Keep Current Representation

Evaluate:

```text
isInputLocked
+
coroutine sequencing
```

Ask:

- Does production code need to know why input is blocked?
- Does anything need to distinguish Swapping from Resolving?
- Are transitions already centralized and readable?
- Would an enum improve behavior/tests or merely rename information?
- Is Phase 9 a more natural point for broader level state?

If no change is justified, say so.

Phase 8 may validly conclude that the current design is sufficient.

---

## Option 1 — Simple Explicit Enum

A likely shape might be:

```text
Idle
Swapping
Resolving
```

but do not force these names.

Explain:

- who owns the state
- exact transition points
- why it belongs in Presentation or elsewhere
- whether `isInputLocked` disappears or becomes derived
- what test or production consumer benefits

Do not add:

```text
Clearing
Falling
Refilling
```

unless a current consumer genuinely needs them.

Do not add:

```text
Win
Lose
```

before a level loop can actually win or lose.

---

## Option 2 — State Classes / State Pattern

A real proposal must answer:

- What interface/base type exists?
- What behavior moves into each class?
- Who owns the active state?
- Who transitions?
- What does Enter do?
- What does Exit do?
- Which conditionals disappear?
- Which tests become simpler?
- How many types/files are added?
- Why is the complexity justified today?

If state classes mostly wrap existing coroutine calls, reject this option.

Do not create a generic framework merely to demonstrate the pattern.

---

## Required Comparison

Produce a concise table:

| Question | Current / bool | Simple enum | State classes |
| --- | --- | --- | --- |
| Shows why input is blocked | ? | ? | ? |
| New types/files | ? | ? | ? |
| Transition clarity | ? | ? | ? |
| Testing benefit | ? | ? | ? |
| Boilerplate | ? | ? | ? |
| Helps current requirements | ? | ? | ? |
| Requires speculative future needs | ? | ? | ? |

Use actual repository evidence.

## Required Recommendation

Recommend exactly one:

```text
A. Keep current design
B. Introduce a minimal explicit enum
C. Introduce state classes
```

Explain:

1. the concrete problem being solved
2. why the choice is the smallest sufficient design
3. what is deferred
4. why the other options are unnecessary

### Important Gate

STOP after the recommendation.

Do not implement until I approve.

If recommending state classes, provide a concrete design and wait for an additional explicit approval before code.

## Learning Questions

I should be able to answer:

- enum state vs State Pattern
- when a boolean is enough
- when an enum is better than a boolean
- when state classes are better than an enum
- what state explosion means
- why Clearing/Falling/Refilling may remain private substeps
- why Win/Lose may be a separate future level-status concern

## Commit

No commit.

No files change.

STOP and wait for approval.

---

# Step 3 — Implement the Approved Minimal Design

## Goal

Implement **only** the design approved after Step 2.

If Step 2 says the current representation is sufficient, this step may be skipped entirely.

---

## Path A — Keep Current Design

Do not make production changes merely so Phase 8 has a commit.

If comments/tests need no correction:

```text
no code change
no commit
```

Proceed after reporting that the step was intentionally skipped.

---

## Path B — Simple Enum

Use the exact approved name/API.

A likely example:

```csharp
public enum BoardInteractionState
{
    Idle,
    Swapping,
    Resolving
}
```

This is only an example.

### Likely Ownership

If state means:

```text
input ready
swap/rollback in progress
resolution in progress
```

the likely owner is `BoardInputController`, because it already owns input, selection, swap/rollback sequencing, and the lock.

Do not move Core rules into the controller.

### Likely Transition Contract

#### Initialization

```text
Idle
```

#### First selection

```text
Idle remains Idle
selection becomes non-null
```

#### Illegal second selection

```text
Idle remains Idle
first selection preserved
Board unchanged
```

#### Accepted adjacent attempt

```text
Swapping
```

before the operation continues.

#### Invalid result

Stay:

```text
Swapping
```

through forward animation, logical rollback, and backward animation.

Then:

```text
Idle
```

#### Valid result

After forward swap animation and before board resolution:

```text
Resolving
```

Remain Resolving through all cascades.

After full stability:

```text
selection cleared
Idle
```

### Input Gate

Prefer input acceptance derived from the authoritative state:

```text
input accepted only when State == Idle
```

If `IsInputLocked` remains useful for compatibility/tests, derive it from state rather than storing a second mutable boolean.

### Reinitialization While Busy

Preserve the current behavior. Do not weaken safety.

### Resolution Runner

`BoardResolutionRunner` should continue to own:

```text
clear → fall → refill → repeat
```

The controller should not need resolution substep states.

### No Future Values

Do not add unused:

```text
Win
Lose
Clearing
Falling
Refilling
Paused
Shuffling
```

---

## Path C — State Classes

Only implement the exact design explicitly approved after Step 2.

Requirements:

- no generic reusable framework
- no reflection
- no service locator
- no Singleton state manager
- no Core rules inside state objects
- no duplication of BoardResolutionRunner
- no placeholder Win/Lose classes
- no class per resolution substep without concrete need

If implementation reveals mostly pass-through wrappers, STOP and recommend returning to the simpler design.

---

# Step 3 Tests

For a simple enum, likely coverage:

- initialize is Idle
- first selection stays Idle
- invalid geometry stays Idle
- accepted adjacent attempt enters Swapping
- invalid swap stays Swapping through rollback
- invalid swap becomes Idle only after rollback
- valid swap is Swapping during forward animation
- valid swap becomes Resolving before resolution
- cascades remain Resolving
- stable completion becomes Idle
- input ignored whenever non-idle
- if `IsInputLocked` remains, it exactly agrees with state
- reinitialize-while-busy remains rejected

Use meaningful state conditions, not exact frame numbers.

Do not rewrite the entire PlayMode suite only to assert the enum.

## Learning Questions

I should be able to answer:

- Why is the chosen state the single source of truth?
- Why does first selection remain Idle?
- Why is invalid rollback part of Swapping?
- Why does Resolving start after forward swap animation?
- Why does the controller not need Clearing/Falling/Refilling?
- If `IsInputLocked` remains, why derive it?

## Commit

If a real production change is made:

```text
Make board interaction state explicit
```

If the approved terminology differs materially, propose the commit message before committing.

Do not create an empty commit.

STOP after the commit.

---

# Step 4 — Transition and Sequence Coverage

## Goal

Prove the chosen state model remains correct across sequences.

If Step 3 made no production change, add tests only if the audit found a real missing contract.

Do not add tests just to increase the count.

## Scenarios

Where applicable:

```text
first selection → ready
invalid geometry → ready
invalid swap → swapping/busy → ready
valid swap → swapping → resolving → ready
cascade → resolving across every round
invalid → valid
valid → invalid
valid → valid
several resolved moves
```

## Invariants

```text
one authoritative state value
input acceptance agrees with state
selection behavior agrees with state
Board rules unchanged
BoardView lookup rules unchanged
```

After a complete move:

```text
ready/Idle
input available
selection null
Board stable
views consistent
```

## Avoid Brittle Frame Assertions

Do not assert:

```text
state changes on frame N
tile is halfway on frame N+3
```

Prefer meaningful conditions:

```text
forward animation still active
resolution has produced Empty cells
refill has populated Board
controller still resolving
board stable
controller idle
```

## Regression

Existing Phase 4–7 behavior must still pass:

```text
invalid rollback
unrelated match does not resolve after invalid move
input ignored while busy
cascades finish before input returns
TileView identity survives gravity
```

## Learning Questions

I should be able to answer:

- Why test transition sequences rather than isolated states?
- What happens if Idle is restored too early?
- Why use behavior instead of frame numbers?
- Why is one source of truth important?
- Why keep older gameplay assertions after adding state?

## Commit

If meaningful tests are added:

```text
Strengthen game-state transition coverage
```

Do not create an empty commit.

STOP.

---

# Step 5 — Runtime Smoke Test and Architecture Review

## Goal

Verify the final state design in the real `SampleScene`.

No new feature work.

If Phase 8 kept the current design unchanged, this is a lightweight confirmation, not a reason to add debug UI.

## Unity Learning Requirement

Before every Editor/MCP action explain:

```text
what
why
manual Unity steps
how to verify
```

Revert temporary changes afterward.

Do not add permanent debug UI merely to display state.

## Runtime Scenarios

### First selection

```text
one tile selected
game still ready for second input
no busy operation started
```

### Invalid geometry

```text
first selection remains
no animation
game remains ready
```

### Invalid adjacent swap

```text
forward
rollback
input unavailable while running
ready only after rollback
```

### Valid swap

```text
forward
resolution
cascade if produced
input unavailable throughout
ready only after stable board
```

### Repeated moves

Perform several moves and verify the state never becomes stuck.

## Architecture Review

If a simple explicit state was chosen, expected responsibility may resemble:

```text
BoardInputController
├── input
├── selection
├── swap / rollback sequencing
├── interaction-state ownership
└── input gating derived from state

BoardResolutionRunner
└── clear / fall / refill / cascade sequencing

BoardView
└── visual state and lookup

Core
└── logical gameplay rules
```

Use actual final names.

## Explicitly Confirm Absent

Unless separately justified and approved:

- no State Pattern class hierarchy
- no generic state-machine framework
- no global game manager
- no Singleton
- no event bus for state transitions
- no placeholder Win/Lose behavior
- no Clearing/Falling/Refilling global states
- no score
- no goals
- no move counter
- no specials
- no dead-board shuffle
- no pooling

## Commit

Only if a real defect is found and fixed.

No empty commit.

STOP.

---

# Step 6 — Phase 8 Validation Pass

## Goal

Close Phase 8 with full regression validation and a clear architecture conclusion.

Do not start Phase 9.

## Run

- all EditMode tests
- all PlayMode tests
- Unity compilation
- Console inspection

Afterward verify:

```text
SampleScene open
not playing
no accidental unsaved changes
working tree clean
```

## Confirm Phase 7 Behavior

```text
valid swap
→ forward animation
→ resolution
→ cascades
→ stable
→ input available

invalid swap
→ forward animation
→ rollback
→ input available
```

Non-adjacent selection behavior remains unchanged.

## Confirm State Contract

If explicit state was added, verify all approved transitions.

If no state code was added, explicitly record why the current representation remained the simpler sufficient design.

## Confirm Boundaries

Core still has:

```text
no UnityEngine
no Match3.Presentation
no MonoBehaviour
```

State/orchestration changes must not pull Presentation concepts into Core.

## Confirm Scope

Still absent:

```text
score
move counter
goal tracking
win
lose
restart
special tiles
boosters
dead-board reshuffle
generic state framework
```

---

# Final Learning Review

I should be able to explain:

1. what program state means without the State Pattern
2. how Phase 7 represented temporal state
3. the real interaction transition graph
4. boolean lock vs explicit enum
5. enum vs State Pattern
6. why the chosen design is the smallest sufficient design
7. what evidence would justify state classes later
8. why Clearing/Falling/Refilling may stay private orchestration substeps
9. why Win/Lose are not added before the level loop exists
10. why unrelated state dimensions should not automatically share one enum
11. where authoritative state lives
12. how input gating derives from state
13. invalid rollback vs valid resolution transitions
14. why transition tests use behavior rather than exact frame counts
15. how Core/Presentation boundaries are preserved
16. what Phase 9 may change when moves, goals, win, and lose become real

## Commit

Only if validation requires a real fix.

Do not create an empty commit.

STOP and report:

```text
Phase 8 complete
chosen state design
why it was chosen
what was intentionally not implemented
```

Do not start Phase 9.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md and Docs/Phases/PHASE8_EXECUTION.md.

We are executing Phase 8 — Explicit Game State.

IMPORTANT:
This is a decision-first phase.
Do not implement a State Pattern, enum, or other state architecture yet.

IMPORTANT UNITY LEARNING RULE:
If you perform any Unity Editor / Unity MCP action, tell me BEFORE doing it:
1. what you are about to do,
2. why,
3. the exact manual Unity Editor steps I could use,
4. how I can verify the result.

Afterward tell me exactly what changed.
Do not silently manipulate scenes, prefabs, Inspector fields, Play Mode, or project settings.

Read only:
- the project context needed for this task
- Step 1 — Audit the Current State Flow

Do not read or implement later steps.

Inspect at minimum:
- BoardInputController
- BoardResolutionRunner
- BoardBootstrap
- BoardView
- relevant Phase 6/7 PlayMode tests

Before modifying anything:
1. identify every field/control-flow mechanism that currently represents gameplay phase
2. trace first selection, invalid geometry, accepted adjacent swap, invalid rollback, valid resolution, cascades, and completion
3. show the actual transition graph
4. identify exactly when input becomes unavailable and available again
5. explain whether Clearing/Falling/Refilling are externally meaningful states or runner substeps
6. distinguish interaction state from future Win/Lose level status

Do not modify files.
Do not add tests.
Do not commit.

Answer the Step 1 learning questions.

Then STOP and wait for approval.
```

---

# Step 2 Decision Prompt

```text
Approved.

Proceed to Step 2 — Architecture Decision in Docs/Phases/PHASE8_EXECUTION.md only.

Do not modify files.

Using the actual code, compare:

A. keep the current state representation
B. introduce a minimal explicit enum
C. introduce explicit state classes / State Pattern

For each evaluate:
- clarity
- source-of-truth risk
- input gating
- transition complexity
- testing benefit
- number of new types
- boilerplate
- usefulness for current requirements
- whether it relies on speculative future features

Also review whether:
- Clearing/Falling/Refilling should remain private BoardResolutionRunner substeps
- Win/Lose should wait for Phase 9
- interaction phase and future level outcome are separate state concepts

Recommend the smallest sufficient design.

Do not implement.
Do not commit.

STOP and wait for approval.
```

---

# Continue Prompt After Architecture Approval

```text
Approved.

Proceed to the next applicable step in Docs/Phases/PHASE8_EXECUTION.md only.

Implement exactly the approved state design.
Do not make it richer for symmetry or future-proofing.

Keep obeying the Unity learning rule:
before every Unity Editor/MCP action, explain what you will do, why, the exact manual Unity steps, and how to verify it; afterward report what changed.

Workflow:
- inspect
- explain
- implement only approved scope
- test
- validate Unity compilation and Console
- summarize
- answer learning questions
- commit only when the step specifies a message and a real change exists
- STOP

Do not start later steps.
```

---

# Claude Teaching Prompt

```text
Do not modify files.

Teach me the final Phase 8 game-state design as if I must defend it in a technical interview.

Use the actual repository implementation and the architecture decision that was made.

Explain:

1. what "state" means in this project
2. how state was represented before Phase 8
3. the actual interaction transition graph
4. why the project did or did not introduce an enum
5. why the project did or did not use the State Pattern
6. boolean lock vs enum vs state classes
7. the criteria that justified the final choice
8. where authoritative state lives
9. how input gating works
10. first-selection behavior
11. invalid-geometry behavior
12. invalid swap / rollback transitions
13. valid swap / resolution transitions
14. cascade behavior
15. why Clearing/Falling/Refilling are or are not explicit states
16. why Win/Lose were deferred to Phase 9 if they do not yet exist
17. why interaction state and level outcome may be separate concepts
18. how the state design is tested
19. likely failure modes
20. what future requirements would justify state classes

Also explain any Unity Editor / scene / prefab setup Phase 8 actually required and how I could reproduce it manually.

If Phase 8 required no Unity-side setup, say so explicitly.

Keep the explanation grounded in actual code.
Do not modify files.
```
