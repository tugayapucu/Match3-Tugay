# Phase 10 Execution Plan — Special Tiles / Power-Ups

Phase 10 adds special-tile gameplay without turning the current clean Match-3 pipeline into a collection of special cases.

This is a **design-first phase**.

Do not begin by adding `Rocket`, `Bomb`, or `ColorBomb` values and branching everywhere.

First define:

```text
what special tiles exist
how they are created
how they activate
what counts as one player move
how they participate in cascades
how they chain into each other
how score / goals count their effects
how special + special swaps behave
how the existing Board / MatchFinder / resolution flow must evolve
```

The implementation must remain understandable enough that a developer can trace one move from input to final stable board.

---

# Why the Original Phase 10 Scope Needs Expansion

The original roadmap listed:

```text
4 in a row  → Rocket
5 in a row  → Color Bomb
T / L       → Bomb
```

That misses an important Royal Match-style power-up:

```text
4 in a 2×2 square → Propeller
```

For this phase, study the complete four-power-up set before coding:

```text
Rocket
Propeller
TNT
Light Ball
```

Use those names in the design discussion unless the repository gives a concrete reason to use different internal names.

`Bomb` / `Color Bomb` from the old roadmap correspond conceptually to:

```text
Bomb       → TNT
Color Bomb → Light Ball
```

Do not rename existing code merely for cosmetic similarity to another game. The naming decision belongs in Step 1.

---

# External Gameplay Reference — Study, Not Blind Copy

The current Royal Match help documentation describes four board-created power-ups:

| Power-up | Creation | Basic effect |
| --- | --- | --- |
| Rocket | 4 same-colored items in a line | clears a row or column |
| Propeller | 4 same-colored items in a square | clears neighboring tiles on takeoff, then a random game element |
| TNT | 5 same-colored items in an L or T | clears within a two-tile radius |
| Light Ball | 5 same-colored items in a line | clears all items of one color |

Royal Match also allows a power-up to be activated by tapping it or swapping it.

This is a **behavioral reference**, not a requirement to clone Royal Match exactly.

The project may intentionally choose a smaller input rule or a more deterministic targeting rule when that keeps the architecture clearer. Any deliberate difference must be written down in Step 1.

---

# Combination Reference — Approved Scope and Deferred Work

Royal Match-style games also support power-up combinations.

Community-documented examples include:

```text
Rocket + Rocket
Rocket + Propeller
Rocket + TNT
Rocket + Light Ball

Propeller + Propeller
Propeller + TNT
Propeller + Light Ball

TNT + TNT
TNT + Light Ball

Light Ball + Light Ball
```

Typical effects are stronger versions of the individual powers:

```text
Rocket + Rocket     → row + column
Rocket + TNT        → several rows + columns
TNT + TNT           → larger blast
Light Ball + Light Ball → board-wide clear

Light Ball + special
→ convert many normal tiles into that special
→ activate them

Propeller + special
→ carry / apply the paired special near a remote target
```

These bonus effects are **future reference material**, not approved Phase 10 rules.

Approved Phase 10 scope is Option A:

```text
A. individual specials + chain reactions only — APPROVED for Phase 10
B. individual specials + a small combination subset — deferred
C. the full four-special combination matrix — deferred
```

A special + special swap is still valid: queue both specials for their ordinary activation, including chains, for one player move. There is no combination bonus.

Step 1 must preserve this approved scope. Explicit combination bonuses require separate future approval; do not implement them because they exist in another game.

---

# Current Project Architecture That Must Be Preserved

As Phase 10 begins, the important flow is:

```text
Board
└── authoritative logical cells

MatchFinder
└── finds normal 3+ line matches

SwapValidator
└── validates normal adjacent swaps

BoardResolution
├── ClearCells
├── ApplyGravity
└── Refill

BoardResolutionRunner
└── clear → fall → refill → repeat until stable

BoardInputController
├── selection
├── swap / rollback
├── input lock
└── completed-valid-move callback

ResolutionResult
└── cleared TileType facts for one completed player move

LevelProgress
└── move cost / score / goal / Won-Lost

BoardBootstrap
└── composition + small level lifecycle

BoardView / TileView
└── visuals only
```

Current dependency direction remains:

```text
Match3.Presentation → Match3.Core
Match3.Core         ✗ Match3.Presentation
```

Special tiles must not make Core depend on Unity.

---

# Phase 10 Scope

Phase 10 should support, after the approved design:

```text
creation of the approved special tiles
activation of each approved special tile
special-triggered clears
special chain reactions
specials moving under gravity
refill continuing to create normal tiles only
score / goal counting the normal tiles removed by special effects
one player move cost per player-triggered special action
visual distinction for specials
deterministic tests
```

Approved special + special baseline:

```text
adjacent special + special swap
→ queue both for ordinary activation
→ no combination bonus
```

Phase 10 does **not** add:

```text
blockers / obstacles
explicit special-combination bonuses, transformations, or carry effects
booster inventory
pre-level boosters
coins
end-of-level bonus conversion
live economy
save/load
object pooling
production art
mobile gesture redesign
analytics
generic event bus
generic ability framework
generic state machine
```

---

# Important Gameplay Questions That Must Be Settled Before Coding

These are not implementation details. They change the model.

## 1. What is a special tile in the Board?

Current `TileType` means:

```text
what occupies a board cell
```

Possible designs:

```text
A. extend TileType with:
   HorizontalRocket
   VerticalRocket
   Propeller
   TNT
   LightBall

B. introduce a richer Tile / CellContent value:
   normal color + optional special kind

C. store specials separately from Board
```

Evaluate all three.

### Likely smallest candidate

Extending `TileType` may be sufficient **if specials are colorless/non-matchable after creation**.

That avoids combinations such as:

```text
RedHorizontalRocket
GreenHorizontalRocket
...
```

and avoids a parallel special-grid source of truth.

But do not approve this merely because it requires fewer edits.

Ask:

- Does any special need to retain its source color after creation?
- Does Light Ball need a color? Usually no.
- Should a Rocket itself participate in normal color matching? Probably not.
- Would extending `TileType` make palette validation ambiguous?
- Can `TileRules` or equivalent clearly distinguish:
  - Empty
  - normal match item
  - special item?

If a richer cell value is genuinely necessary, accept the larger refactor **before** special behavior is added.

---

## 2. What counts as a normal match after specials exist?

Current MatchFinder assumes every non-Empty `TileType` can match.

That will no longer be true if special values enter `TileType`.

The design needs one explicit rule such as:

```text
only normal colored tiles participate in ordinary matches
```

A Rocket beside three Reds must not accidentally become a Red merely because it was originally created from Red.

Do not spread checks like:

```csharp
type != Rocket && type != TNT && ...
```

through several classes.

Prefer one small source of truth for tile classification.

Do not build an inheritance hierarchy just for classification.

---

## 3. How are match shapes represented?

Current `MatchFinder.FindMatches` returns only:

```text
set of matched positions
```

That is enough for clearing, but insufficient to answer:

```text
Was this a 4-line?
Was it horizontal or vertical?
Was it a 5-line?
Was it a T?
Was it an L?
Was it a 2×2 square?
Which special should be created?
Where should that special appear?
```

Phase 10 therefore needs shape information.

Possible approach:

```text
straight runs
+
square detection
↓
MatchAnalysis / MatchGroup
↓
matched positions + special-creation candidates
```

Do not turn `MatchFinder` into a giant method containing all creation, activation, scoring, and visual logic.

The matcher should describe patterns.

Another system should decide what gameplay consequence a pattern creates.

---

## 4. What makes a Propeller match valid?

A 2×2 square is not a 3-in-a-line.

Current swap validation only understands line matches.

Approved rule:

```text
2×2 same color → Propeller
```

Creating that square must count as a valid player move.

This means Phase 10 must deliberately evolve the definition of a valid Match-3 move.

Do not bolt square detection onto Presentation.

### Initial generation and refill

The starting board must contain none of the approved resolvable patterns. The current generator already prevents line-3+ patterns; add same-color 2×2 prevention for Propeller in Step 3, when square-match semantics are introduced.

Refill must continue allowing all approved patterns. It creates normal palette tiles without preventing lines or squares, so cascades can create specials.

---

## 5. Pattern priority

One move can create overlapping patterns.

Examples:

```text
5 in a line also contains 4-in-line subsets

T / L contains two 3+ runs

a larger same-color block can contain more than one 2×2 square

two unrelated groups can resolve simultaneously
```

Step 1 must approve deterministic priority.

A likely policy to evaluate:

```text
within one connected same-color match group:
Light Ball > TNT > Rocket > Propeller > normal clear
```

This is only a candidate.

Important:

```text
disjoint match groups may each create their own special
```

Do not let hash-set iteration order determine which special wins.

### Connected groups and concrete shape cases

Step 1 must define exactly what connects a match group: which matched cells are included, whether side-by-side runs merge, and how overlapping runs/squares share a creation decision. A priority list alone does not define group membership or guarantee one creation per intended group.

Produce a small table of concrete board diagrams with expected matched cells, group membership, special kind/count, and anchor for at least:

| Shape | Decision the Step 1 table must make explicit |
| --- | --- |
| Two parallel same-color lines touching side by side | One group or two, and how many specials result |
| Plus/cross | Whether it qualifies as TNT and which anchor wins |
| 2×3 same-color block | How overlapping squares and lines compete |
| Square plus a tail / overlapping 3-line | Whether the result is Propeller, Rocket, TNT, or only a normal clear under the approved rules |
| Six in a straight line | Which special is created and whether more than one is allowed |
| Two T shapes sharing a cell | Grouping, priority, and creation count |

The approved rows become Step 3 tests directly; do not invent these outcomes during implementation.

---

## 6. Where is the created special placed?

A special replaces one of the matched cells.

The rule must be deterministic.

For the first resolution created by a player swap, consider:

```text
prefer the destination / second selected cell if it belongs to the qualifying pattern
otherwise prefer the other swapped cell
otherwise deterministic shape anchor
```

For cascades there is no player-selected cell.

Potential deterministic anchors:

```text
T / L       → intersection
straight 5  → center-most cell
straight 4  → center-most candidate
2×2 square  → deterministic corner / shape-defined anchor
```

Do not leave this to collection iteration order.

The exact policy is a Step 1 decision and must be tested.

### A normal partner can create a match during special activation

Swapping a special with a normal tile can move that normal tile into a qualifying line or square. Step 1 must explicitly decide when that pattern is analyzed relative to the special chain and gravity/refill, and whether it survives to be resolved in the same player move.

Specify whether this is the first player-created normal round and receives swap-cell anchor preference. Define exactly when that preference expires; it must not leak into later cascades. Do not silently preserve or discard a pre-chain pattern merely because of the order an implementation happens to scan the Board.

---

## 7. Does the tile transformed into a special count toward score / goal?

A match of four Reds should conceptually consume four Red match items even though one cell becomes a Rocket.

Approved contract:

```text
clear/consume all matched normal tiles
→ ResolutionResult records all four Red clears
→ create Rocket at the approved anchor
```

That keeps:

```text
collect Red
```

consistent with what the player matched.

Do not silently count only three Reds because the fourth position becomes special.

---

## 8. Does activating a special itself score?

Current Phase 9 rule is based on cleared `TileType` facts. The approved Phase 10 boundary is:

```text
ClearCells
→ truthfully reports every non-Empty TileType it physically removes, including specials

resolution aggregation
→ retains only normal collectible TileTypes for ResolutionResult

ResolutionResult
→ accepts only normal collectible TileTypes; rejects specials, Empty, and undefined values

normal colored tiles consumed by matches or special effects
→ score normally; advance the goal when their color matches

the special item that activates
→ no independent color score or goal credit
```

Do not make `ClearCells` hide physical removals to satisfy scoring. Filter the truthful clear facts at the `ResolutionResult` boundary using the shared tile classification. A normal tile consumed to create a special still contributes its original color, captured before replacement.

`LevelProgress.GoalTileType` must also be a normal collectible color. Replace its current "defined and non-Empty" check with the same central normal/special classification during Step 2; newly defined specials must not become valid goals.

No separate special-item score is added in Phase 10. A richer scoring contract would require separate future approval.

---

## 9. How is each special activated?

External reference behavior supports tap **or** swap.

The current project uses a two-click adjacent-swap interaction.

Possible project policies:

```text
A. Phase 10 supports special activation by adjacent swap only.
   Single-tap activation waits for the mobile-input phase.

B. selected special + same cell again activates it;
   selected special + adjacent cell swaps/combines it.

C. extend pointer handling now to distinguish tap vs drag/swipe.
```

Evaluate cost and clarity.

Do not accidentally turn Phase 10 into Phase 12 (mobile input).

Whatever is chosen:

```text
one player-triggered special activation = one move
chain reactions = no extra moves
```

---

## 10. When is a swap involving a special valid?

Normal swap:

```text
valid only if it creates an approved match pattern
```

Special involvement may use different rules:

```text
normal + special
special + special
```

Approved rule:

```text
any adjacent swap involving a special is a valid player action
```

For special + normal, activate the special under the approved individual rules. For special + special, queue both for ordinary activation, with no combination bonus. Either accepted action costs one move after the full resolution completes.

Step 1 still specifies the remaining activation details and deterministic queue order; it must not reopen the approved no-bonus combination scope.

### Activation position and resolution entry point

Step 1 must explicitly settle whether a swapped special activates at its old cell or its new cell. Proposed baseline: apply the logical swap first, then activate each special at its post-swap position. For example, a Rocket moved vertically would clear its new row, not its old row. A tap, if supported, uses the special's current cell.

Write the exact minimal resolution entry-point contract before Step 4 needs player-selected anchors or Step 6 integrates special input. The current `ResolveUntilStable(onResolved)` receives neither swap positions nor initial activations.

State how the approved API carries or derives:

```text
the two player-selected cells, when a swap exists
the classified action and initial special activations at the approved positions
the normal partner color for Light Ball, when present
the lifetime of first-player-round anchor preference
the existing once-per-completed-move result callback
```

Choose ordinary arguments or one small data value only if it simplifies those concrete needs. Do not add a command hierarchy, generic context framework, or an API that later steps must redesign independently.

Consider whether `SwapValidator` should grow this responsibility or whether a small move-classification concept is now clearer.

Do not create an abstract command framework just to classify three move kinds.

---

## 11. Propeller target selection

Royal Match reference behavior includes a remote/random target.

The current project has:

```text
one color-collection goal
no blockers
no obstacle model
```

Do **not** make Core special activation depend directly on `LevelProgress`.

Possible Phase 10 policy:

```text
Propeller:
1. clears approved neighboring cells
2. selects one remote occupied target from Board
3. clears / activates that target
```

Targeting must be deterministic under tests.

If random selection is used, use an injected `System.Random`.

### Eligible remote targets

Step 1 must define the exact candidate set before a random choice is made. Specify whether the target may be another special (and therefore chain), whether it must lie outside the takeoff footprint, and how to exclude the Propeller itself.

Also settle whether already-planned removals and specials already queued/activated remain eligible. A frozen Board still contains those cells physically; reading it alone does not prevent a wasted or duplicate target. Define the no-eligible-target fallback explicitly, and use a stable candidate order before selecting a random index.

### Important RNG separation

Do not casually share the refill RNG with Propeller targeting.

Otherwise:

```text
Propeller target choice
→ consumes random values
→ changes future refill sequence
```

A separate deterministic RNG stream is likely cleaner.

Step 1 must decide the smallest implementation.

The targeting stream is part of the level session: every start/restart must create a fresh deterministic Propeller stream, just as refill receives a fresh stream. Step 1 must specify its owner and seed derivation; do not reuse the refill stream or retain a targeting stream from the previous level.

Restarting with the same configuration and replaying the same moves must reproduce both target choices and refill behavior. Test stream isolation and restart reproducibility separately; no generic random-service framework is needed.

This is a concrete reason to revisit random-stream ownership; it is not a reason to add a generic random-service framework.

---

## 12. TNT geometry

Reference wording is:

```text
two-tile radius
```

The project must translate that into a precise grid rule.

A likely simple rule:

```text
5×5 square centered on TNT
(clipped to board bounds)
```

or equivalently:

```text
Chebyshev distance <= 2
```

Do not rely on an ambiguous word like "radius" in tests.

Approve the exact geometry first.

---

## 13. Rocket orientation

Creation must produce an explicit direction.

Likely:

```text
horizontal 4-line → Horizontal Rocket
vertical 4-line   → Vertical Rocket
```

Activation:

```text
Horizontal Rocket → its full row
Vertical Rocket   → its full column
```

Do not infer orientation from animation or view rotation.

Orientation is logical gameplay data.

---

## 14. Light Ball target color

Approved targeting contract for a swap with a normal tile:

```text
target = the normal tile's color
```

For a chain-triggered activation or any activation without a normal partner (including an ordinary special + special swap, or a tap if approved):

```text
choose the most common normal color on the Board
tie-break by the lowest normal TileType numeric value
if no normal colors exist, produce no additional color clear
```

Count only normal collectible tiles, excluding Empty and specials. Removing/activating the Light Ball itself gives no color score or goal credit. Do not use nondeterministic dictionary order.

---

## 15. Chain reactions

If a Rocket/TNT/Propeller effect hits another special:

```text
the hit special should activate
```

This must not recursively mutate the board in an uncontrolled call stack.

A likely algorithm:

```text
queue specials to activate
set/hash of already activated positions

while queue not empty:
    dequeue special
    compute affected cells
    if affected cell contains unactivated special:
        enqueue it
    accumulate cells to remove

apply the logical changes deterministically
```

Benefits:

```text
no accidental infinite recursion
no double activation
chain order can be tested
```

Do not over-generalize this into a reusable graph framework.

### Chain read state and pending effects

Step 1 must make the chain's read-state contract explicit. Recommended approach: keep Board positions/content fixed while computing one activation chain, accumulate its affected positions, then apply the logical changes before gravity/refill. This does not require cloning the Board if ownership already prevents mutation.

Define how pending removal/activation sets affect target eligibility and how Light Ball counts colors against that read state. Deduplicate physical removals and normal-color facts when multiple effects hit the same cell. Frozen contents alone do not define any of those policies.

### Activation chain and creation ordering

Resolve an existing special activation chain completely before creating specials from newly found normal matches. Finish the chain's effects before continuing gravity/refill and normal-match resolution.

Newly created specials are not retroactively hit or activated by the already-resolved effect that caused that resolution. They may be activated by a later, separate effect. Step 1 must preserve this phase boundary when defining the resolution flow.

---

## 16. Special + special combinations

Approved Phase 10 baseline:

```text
two specials swapped
→ valid adjacent action
→ queue both for ordinary activation, each at most once in the chain
→ one player move, no combination bonus
```

Explicit combination bonuses are a separate game rule and are deferred beyond this Phase 10 scope. Ordinary activation of both specials is not a combination bonus.

The optional combination step remains documented as future work. If separately approved later, define the selected matrix **before that future implementation**.

For each pair specify:

```text
trigger
effect
targeting
orientation
transformations
chain behavior
score/goal facts
determinism
```

---

# Critical Unity Learning Rule

Same rule as previous phases.

Before any Unity Editor / Unity MCP action that inspects or changes:

```text
scene hierarchy
prefabs
components
serialized fields
sprites/materials
TileView setup
Play Mode
project settings
```

tell me:

1. what you are about to do
2. why
3. where I would do it manually
4. exact manual steps
5. how I can verify it

After the action, report exactly what changed or was observed.

Do not silently manipulate Unity.

If a step is Core-only and needs no Unity action, say so.

---

# Architecture Principles

## Core owns gameplay facts and rules

Examples:

```text
tile classification
match-shape analysis
special creation rule
special effect footprint
chain-reaction logic
move classification
logical board mutation
```

## Presentation owns timing and visuals

Examples:

```text
special icons / sprites
Rocket animation
Propeller flight animation
TNT visual blast
Light Ball visual feedback
sequencing logical results with animations
```

## LevelProgress remains separate

Specials may change **what gets cleared**.

They should not calculate:

```text
score
goal
Win
Lose
```

They produce cleared facts.

`LevelProgress` interprets those facts exactly as before.

Its scoring and Win/Lose rules remain unchanged. Its goal-type constructor validation does change in Step 2 to use the shared normal-color classification; "separate from specials" does not mean the entire class is left untouched.

---

# Pattern Guidance

Phase 10 is explicitly a place to **evaluate** patterns, not demonstrate them.

## Strategy

Could become useful if activation behaviors require independent interchangeable objects.

But with only:

```text
Rocket
Propeller
TNT
Light Ball
```

a cohesive switch in one Core resolver may still be clearer.

Use Strategy only if it removes growing, scattered behavior branches.

Do not create four classes whose only method is five lines long merely to claim Strategy.

## Factory

Likely unnecessary if creating a special means constructing a small enum/value.

Consider Factory only if creation itself gains substantial dependencies or setup.

Do not add:

```text
SpecialTileFactory.Instance
```

## Observer / Event Bus

Not needed merely because specials trigger each other.

Chain reactions are gameplay resolution, not loosely coupled application events.

Use explicit Core data flow.

## State Pattern

Not justified by special-tile type.

Do not create:

```text
RocketState
BombState
...
```

---

# Step 1 — Architecture Audit and Exact Rule Specification

## Goal

Decide the remaining logical contract before changing code, preserving the approved generation, rendering timing, clear/result, goal classification, chain-edge, and Option A scope clarifications above.

No file changes.

No Unity changes.

This step is intentionally detailed because mistakes here would cause broad refactors later.

## Inspect

At minimum inspect the current versions of:

```text
TileType
Board
BoardGenerator
PaletteValidation
MatchFinder
SwapValidator
BoardResolution
BoardResolutionRunner
BoardInputController
BoardView
TileView
ResolutionResult
LevelProgress

relevant Core tests
relevant PlayMode resolution/input tests
```

## Required Decisions

Produce a concise table covering:

```text
1. board representation
2. normal-vs-special classification
3. match-analysis result shape
4. 2×2 Propeller detection
5. T/L detection
6. pattern priority
7. special creation anchor
8. whether cascade matches create specials
9. score/goal semantics
10. activation input policy
11. special-swap validity
12. Propeller target policy
13. RNG ownership
14. TNT footprint
15. Light Ball target policy
16. chain-reaction algorithm
17. confirm approved Option A (ordinary two-special activation, no combination bonuses)
18. Strategy decision
19. Factory decision
20. initial generation prevents lines and squares; refill allows both
21. truthful physical clear facts vs normal-only ResolutionResult and goal validation
22. minimal special rendering by Step 4; distinctive visuals in Step 8
23. existing activation chain completes before new special creation
24. activation position after a swap, with an old-cell/new-cell example
25. special + normal partner match timing and lifetime of swap-anchor preference
26. exact resolution entry point carrying/deriving swap context and initial activations
27. connected-group definition and concrete shape/outcome table
28. Propeller RNG owner, seed derivation, and reset on every restart
29. Propeller eligibility, pending-effect exclusions, and no-target fallback
30. chain read state, target evaluation, and deduplicated clear facts
```

These are design outputs for Step 1, not authorization to implement them while reviewing this plan. Preserve the previously approved contracts; settle the remaining questions before later steps choose APIs or behavior.

## Required Special Rule Table

At minimum propose the project contract for:

| Special | Created by | Logical data needed | Activation footprint | Player activation |
| --- | --- | --- | --- | --- |
| Rocket | ? | orientation | ? | ? |
| Propeller | ? | ? | ? | ? |
| TNT | ? | ? | ? | ? |
| Light Ball | ? | target color? | ? | ? |

Do not implement.

## Approved Combination Scope

Confirm the existing approval rather than choosing a new scope:

```text
A. four individual specials + chain reactions, no explicit combination bonuses
special + special swap → ordinary activation of both, one player move
```

Selected bonus combinations and the full matrix are deferred future work, not undecided Phase 10 tasks. Step 7 is skipped without implementation or commit.

## Required Move Flow Diagrams

Show:

### Normal valid move

```text
swap
→ shape analysis
→ decide special creations and first-round anchors
→ consume original normal colors, then place specials
→ fall / refill
→ cascades
→ stable
→ LevelProgress
```

### Single special activation

```text
player action
→ special effect
→ chain reactions
→ fall / refill
→ normal matches / special creation
→ repeat
→ stable
→ LevelProgress
```

For a special + normal swap, show both the special's activation position and the normal partner's possible match. State the approved analysis/anchor timing rather than assuming it is an ordinary cascade.

### Special + special ordinary activation

```text
swap specials
→ queue both for ordinary activation
→ finish existing special chain, no combination bonus
→ normal resolution
→ create specials from newly found normal matches, without retroactive hits
→ stable
→ LevelProgress
```

## Learning Questions

I should be able to answer:

- Why does the current `FindMatches` result no longer contain enough information?
- Why does Propeller change what "valid match" means?
- Why is special creation different from special activation?
- Why must creation anchors be deterministic?
- Why can sharing the refill RNG with Propeller targeting be undesirable?
- Why is a queue useful for special chain reactions?
- Why might extending `TileType` be cleaner than a new inheritance hierarchy?
- What evidence would actually justify Strategy?
- Why is Factory probably unnecessary today?
- Why should special effects still produce facts for `LevelProgress` instead of changing score directly?

## Commit

No commit.

STOP and wait for approval.

---

# Step 2 — Special-Capable Board Representation

## Goal

Make the logical Board capable of storing the approved special-tile representation **without adding special creation or activation behavior yet**.

This step should be a representation migration only.

The old Match-3 behavior must remain identical.

## Possible Work

Depending on Step 1's approved design:

```text
extend TileType
or
introduce Tile / CellContent value
```

Also add the approved central classification helpers, for example:

```text
IsEmpty
IsNormalMatchItem
IsSpecial
IsRocket
```

Exact API must come from Step 1.

## Test Board Notation

Choose distinct test-fixture symbols for every special kind, including Rocket orientations. `P` already means Purple in `TestBoards` and the PlayMode helpers; preserve that meaning rather than reusing it for Propeller.

Update the relevant parsers/formatters consistently in their existing test assemblies. This is fixture notation, not a reason to add a shared test framework or runtime string-based tile model.

## Requirements

- Board can store every approved special kind.
- Gravity can move specials exactly like occupied cells.
- Refill creates only normal palette tiles.
- BoardGenerator creates only normal palette tiles.
- PaletteValidation rejects specials in the normal spawn palette.
- MatchFinder does not treat specials as normal matching colors.
- LevelProgress goal type remains a normal collectible color, validated through the shared classification rather than Enum.IsDefined/non-Empty alone.
- ClearCells continues reporting every TileType it physically removes, including special values; do not filter its truthful removal report for scoring.
- ResolutionResult accepts only normal collectible TileTypes and rejects specials, Empty, and undefined values. Any caller assembling it filters physical removal facts using the shared classification.
- Existing board behavior is unchanged when no specials are present.
- Core stays Unity-free.

## No Special Behavior Yet

Do not implement:

```text
4-line creation
square creation
T/L creation
special activation
chain reactions
special input
combinations
```

## Tests

At minimum:

- normal palette still validates
- any special in spawn palette is rejected
- Board get/set supports specials
- gravity moves specials correctly
- refill never creates specials
- ordinary match detection ignores special cells
- every special is rejected as a LevelProgress goal and as a ResolutionResult entry
- ClearCells reports normal and special values truthfully when physically removed
- ordinary Phase 2–9 Core behavior remains unchanged

If representation changes existing public types substantially, update tests mechanically but do not change gameplay semantics.

## Learning Questions

- Why separate "normal match item" from "occupied cell"?
- Why must specials be excluded from the refill palette?
- Why is one classification helper better than scattered special-case checks?
- What future requirement would make the chosen representation insufficient?

## Commit

Suggested:

```text
Support special tiles in board data
```

STOP.

---

# Step 3 — Shape-Aware Match Analysis and Special Creation Rules

## Goal

Detect the shapes needed to decide special creation.

No activation yet.

## Required Detection

According to the approved Step 1 rules, support:

```text
normal 3+ horizontal / vertical runs
4 straight
5+ straight
2×2 square
T
L
multiple simultaneous disjoint groups
intersections
```

Do not infer power-up creation from presentation.

## Recommended Responsibility Split

A likely clean split is:

```text
Match analysis
→ describes patterns / connected matched cells

Special creation rule
→ interprets pattern
→ chooses special kind + anchor
```

Exact class names are not prescribed.

Do not make one class own:

```text
scan
special creation
activation
score
view effects
```

## Backward Compatibility

If useful, preserve:

```text
MatchFinder.FindMatches(...)
```

as a simple API for callers/tests that only need matched positions.

It may internally use richer analysis.

Do not duplicate two independent match algorithms.

## Pattern Priority

Implement the exact approved Step 1 priority.

Tests must prove that larger patterns do not accidentally create weaker specials.

Example:

```text
5 straight
must not become Rocket merely because it contains 4-straight subsets
```

## Anchor Rules

Implement deterministic anchors.

Test both:

```text
player-created first-round pattern
cascade-created pattern
```

Do not depend on HashSet iteration order.

## Valid-Move Semantics

A 2×2 Propeller-forming swap must be recognized as a valid player move if that is the approved rule.

Update pure validation logic here or in the approved move-classification type.

Do not integrate special activation yet.

## Initial Generation and Refill

Update BoardGenerator when the new square-match rule is introduced: the initial board must have no approved resolvable patterns. Preserve line-3+ prevention and add same-color 2×2 prevention.

Keep refill unconstrained by match prevention so it can create line and square patterns that resolve into cascades/specials. Determinism remains required, although generated layouts may change to satisfy the new starting-board rule.

Keep the minimum palette size at three normal colors. During left-to-right, bottom-to-top generation, a blocked square's color equals both the immediate left and below tiles. Any horizontal or vertical exclusion that also exists is therefore that same color; square prevention never raises the maximum of two distinct excluded colors. There is always a candidate with three colors; do not raise the minimum "to be safe".

Inspect seed-dependent tests when generated layouts change, including `Resolve_SeveralRealMoves_LeaveNoStaleLookupState` and fixtures using seeds such as 42 or 7. Update seeds, explicit fixtures, or exact expected layouts as needed while preserving each test's behavioral strength. Do not lower its required number of moves, remove identity/lookup checks, or weaken stability assertions merely to make a new layout pass.

## Tests

At minimum:

- ordinary 3 produces no special
- horizontal 4 produces the approved Rocket orientation
- vertical 4 produces the approved Rocket orientation
- straight 5 produces Light Ball
- T produces TNT
- L produces TNT
- 2×2 produces Propeller
- 5-line beats 4-line
- TNT pattern beats lower-priority sub-patterns
- deterministic anchor
- two disjoint qualifying groups produce two creations if approved
- intersection positions counted once
- no false square from different colors
- no false T/L through a special cell
- Propeller-forming swap passes move validation
- every concrete shape/outcome row approved in Step 1, including adjacent/overlapping groups
- generated boards contain no resolvable lines or same-color 2×2 squares, including small-board and multiple-seed cases
- refill can still produce line and square patterns rather than excluding them
- three-color palettes remain sufficient with line and square prevention

## Learning Questions

- Why are matched positions alone insufficient now?
- What is a connected match group?
- Why separate pattern detection from creation policy?
- Why does deterministic anchoring matter for tests and replays?
- How does pattern priority prevent double creation?

## Commit

Suggested:

```text
Detect special tile creation patterns
```

STOP.

---

# Step 4 — Create Specials During Board Resolution

## Goal

Turn qualifying matches into real special tiles while preserving the existing:

```text
clear
fall
refill
cascade
stable
```

contract.

No player-triggered activation yet.

## Logical Sequence

A likely round:

```text
analyze matches
→ decide special creations
→ consume all matched normal tiles
→ filter physical clear facts to normal collectible colors for ResolutionResult
→ place special(s) at creation anchor(s)
→ remove/update views
→ gravity
→ refill normal cells
→ analyze next cascade
```

Exact implementation must match Step 1/3 decisions.

Use the resolution entry-point contract approved in Step 1 to receive the first player swap's context and anchor preference. Preserve its explicit lifetime; do not invent a separate temporary API here that Step 6 must replace for queued activations.

## Critical Score / Goal Rule

If four Reds create a Rocket:

```text
ResolutionResult reports four Red match items consumed
```

The special appears **after** those match-item facts are captured.

## Cascade Creation

If approved:

```text
refill/fall-created 4, square, T/L, or 5-line
→ also creates special
```

Cascades must not need player input.

## Visual Contract

Presentation must end each round with one correct view per occupied Board cell.

Add minimal special rendering support by this step at the latest. TileView must be able to represent every approved special without throwing for an unknown type. Simple temporary appearance is sufficient here; Step 8 owns distinctive icons/orientation visuals and activation feedback.

For a special creation anchor, either:

```text
reuse the old TileView and retheme it
or
replace it with a new special TileView
```

Both are acceptable if explicitly chosen and tested.

Do not accidentally weaken survivor identity guarantees for unrelated tiles.

## Tests

At minimum:

- 4-match leaves one Rocket at anchor
- square leaves one Propeller
- T/L leaves one TNT
- 5-line leaves one Light Ball
- all consumed normal colors are in ResolutionResult
- ResolutionResult contains no special values, and creating a special preserves credit for the original normal color
- created special remains on board through gravity rules
- refill does not overwrite the special
- cascade can create special if approved
- unrelated TileView identities remain stable
- final Board/View/lookup agree
- TileView can represent newly created specials without throwing before Step 8

## Commit

Suggested:

```text
Create special tiles during resolution
```

STOP.

---

# Step 5a — Rocket, TNT, and the Chain Queue

## Goal

Implement Rocket and TNT logical effects plus the shared activation-chain queue using the Step 1 contract. Keep this review separate from Propeller targeting and Light Ball color selection, which belong to Step 5b.

Do not integrate player input yet.

Tests should be able to trigger an activation directly through Core/resolution APIs.

## Rocket

Implement approved:

```text
Horizontal Rocket → row
Vertical Rocket   → column
```

Affected normal tiles become clear facts.

Specials hit by the Rocket become chain activations.

## TNT

Implement exact approved footprint.

Clip cleanly at board edges/corners.

Do not special-case every edge manually; compute positions from coordinates.

## Chain Queue

Use the approved queue/set approach or the exact Step 1 equivalent. Honor the approved read-state/pending-effect contract and activation origin. Keep this step's activation tests scoped to Rocket/TNT; do not add no-op handlers for the Step 5b specials to pretend they already work.

Contract:

```text
a special hit by an effect
→ activates once

same special hit by several effects
→ still activates once

chains eventually terminate
existing activation chain finishes before specials from newly found normal matches are created
newly created specials are not retroactively hit by the completed effect
```

## Clear Facts

ClearCells truthfully reports what it physically removes, including specials. Filter these facts using the shared normal/special classification before constructing the eventual `ResolutionResult`.

All **normal collectible tiles** consumed by matches or special effects must be included. Special items themselves must not be included, and ResolutionResult rejects them. A normal tile consumed to create a special counts as its original color; removing/activating the special gives no color score or goal credit.

Do not let special resolution update score or goal directly.

## Tests

At minimum:

### Rocket
- horizontal
- vertical
- edge-safe
- hits another special → chain

### TNT
- center
- edge
- corner
- exact footprint
- chain

### Chains
- two-special chain
- three-special chain
- same special reached twice activates once
- overlapping effects remove and report each original normal tile once
- result contains exactly the normal clears
- removed special items are reported truthfully by ClearCells but excluded from ResolutionResult
- chain completes before new special creation; new specials survive the completed effect without retroactive activation
- final logical state deterministic
- activation uses the approved origin, with a case distinguishing old and post-swap cells

## Learning Questions

- Why resolve effects as positions/facts instead of manipulating views?
- Why is a queue safer than uncontrolled recursive activation?
- Why must the same special activate at most once?
- Why must a frozen read state still track pending removals?

## Commit

Suggested:

```text
Resolve rocket and TNT activation chains
```

STOP after Step 5a and wait for explicit approval before reading or implementing Step 5b. Do not integrate player input or start Step 6.

---

# Step 5b — Propeller and Light Ball

## Goal

After Step 5a approval, extend the tested chain queue with Propeller and Light Ball. Do not integrate player input yet; tests trigger effects through the approved Core/resolution entry points.

## Propeller

Implement:

```text
local takeoff effect
+
one remote target
```

Use the exact Step 1 footprint, eligible-target set, pending-effect exclusions, stable candidate ordering, and no-eligible-target fallback. If a remote special is eligible, it enters the same once-per-chain queue.

Use the separately owned deterministic targeting stream; never consume refill RNG values. Test the fresh-stream restart contract here. Production level-start/restart wiring is completed in Step 6 alongside special-move integration.

Do not read `LevelProgress` directly to choose a target. If objective-aware targeting is deferred, document the approved board-only policy.

## Light Ball

Implement the approved target-color behavior:

```text
normal swap partner → clear every normal board item of the partner's TileType
chain-triggered / no normal partner → most common normal board color
tie → lowest normal TileType numeric value
no normal colors → no additional color clear
```

Use the Step 1 chain read-state contract when counting colors. Special cells are excluded from normal-color counts and color clears. Their own activation/removal follows the chain rules and contributes no color score or goal credit.

## Shared Chain and Clear-Fact Contract

Preserve Step 5a's queue, deduplication, truthful physical removal facts, normal-only ResolutionResult, and no-retroactive-hit boundary. Extend it to mixed chains involving all four special kinds; do not introduce a second activation engine.

## Tests

### Propeller
- exact local neighbors and edge/corner behavior
- eligible remote cells follow the approved rules, including self/takeoff-area exclusions
- planned removals and already queued/activated specials follow the approved eligibility policy
- no eligible target uses the explicit fallback
- deterministic remote target and stable candidate ordering
- same seed/state → same target
- remote special target chains if approved
- RNG does not perturb refill RNG
- recreating the targeting stream reproduces choices for a restarted session

### Light Ball
- correct target color only
- normal swap partner determines the color
- chain-triggered / no-partner activation selects the most common normal color
- tied counts use the fixed TileType-order tie-break
- all matching normal items
- no matching items
- no normal colors produces no additional color clear
- specials not mistaken for target color

### Mixed Chains
- Rocket/TNT hits Propeller or Light Ball and the chain completes
- the same special reached through several paths still activates once
- overlapping effects produce exact, deduplicated normal-color facts
- no newly created special is retroactively activated
- deterministic final logical state with separate refill/targeting streams

## Learning Questions

- Why should Propeller randomness use a separate deterministic stream?
- Why must restart recreate that stream too?
- Why do eligibility rules matter even when the Board is frozen?
- Why does Light Ball need both partner-color and no-partner contracts?

## Commit

Suggested:

```text
Resolve propeller and light ball effects
```

STOP after Step 5b and wait for explicit approval before Step 6.

---

# Step 6 — Player Input and Special-Move Integration

## Goal

Connect the already-tested special behavior to player actions.

Implement the approved ordinary two-special activation path here. Explicit special-combination bonuses remain deferred; do not implement them in this step or later in Phase 10.

Both Steps 5a and 5b must be complete and approved before this integration step.

## Resolution Context and Session Wiring

Use the Step 1 entry point to carry/derive the player swap, approved activation positions, initial queued specials, and any Light Ball partner color. A special + normal action must follow the approved normal-partner match timing and anchor-preference lifetime; later cascades must not inherit the original player anchor preference accidentally.

Wire the Propeller targeting stream into the level session. BoardBootstrap must recreate it on every start/restart from the approved deterministic seed derivation, alongside the fresh refill stream. Preserve the existing refill seed policy; do not retain an old targeting stream or share refill RNG state.

## Player Move Classification

A player action may now be:

```text
normal swap that creates a match
invalid normal swap
single-special activation
special + normal swap
special + special swap → queue both for ordinary activation, no combination bonus
```

Use the smallest approved classification.

Do not turn `BoardInputController` into the owner of special effect rules.

It should ask Core/resolution what the action means, then sequence visuals/input lock.

## Move Cost

Contract:

```text
one accepted player special action = one move

all resulting:
special chains
gravity
refill
normal cascades
special creations
= no additional move cost
```

`LevelProgress.ApplyValidMove` must still run exactly once after the complete stable resolution.

## Input Lock

Remain locked through:

```text
swap / activation
special chain
gravity
refill
normal cascade
further special creation
until stable
```

Restart remains unavailable throughout via the existing busy callback.

## Activation Gesture

Implement the exact Step 1 approved Phase 10 policy.

Do not invent a second input model during implementation.

If tap activation was deliberately deferred, write that clearly and test only the approved swap path.

## Invalid Normal Swap

Must remain exactly Phase 9 behavior:

```text
forward
rollback
no ResolutionResult delivered to LevelProgress
no move cost
```

## Tests

At minimum:

- normal invalid still free
- normal valid still costs one
- special activation costs one
- long special chain costs one
- adjacent special + special swap is valid, queues both for ordinary activation, and costs one move without a combination bonus
- Light Ball with no normal partner uses the approved most-common-color fallback
- swapped specials activate at the approved cell, with old/post-swap positions producing distinguishable footprints
- a special + normal swap that also forms a normal pattern follows the approved timing/anchor policy and still costs only one move
- first-player-round anchor preference expires before later cascades
- restart recreates the Propeller targeting stream; replaying the same moves reproduces targets and refill results
- special-created normal cascade costs no extra move
- score/goal include all normal clears
- Won can be reached through a special effect
- Lose occurs only after whole special resolution if goal not reached
- terminal cleanup still leaves:
  - selection null
  - `IsInputLocked == false`
  - controller disabled
  - board stable
- restart disabled while special chain is active

## Commit

Suggested:

```text
Integrate special tile player moves
```

STOP.

---

# Step 7 — Special + Special Combination Bonuses (Deferred / Future Work)

## Goal

Approved Option A excludes explicit combination bonuses from Phase 10. Skip this step with no implementation and no commit. The ordinary two-special activation path is implemented in Step 6.

Keep the contracts below as future reference only. Implementing a subset or full bonus matrix requires separate approval beyond this Phase 10 scope; do not add it opportunistically in earlier steps.

## Combination Contract

For every approved pair define:

```text
pair
effect
targeting
orientation behavior
transform behavior
chain semantics
cleared facts
deterministic rules
```

## If Full Matrix Is Approved

Cover all unordered pairs:

```text
Rocket + Rocket
Rocket + Propeller
Rocket + TNT
Rocket + Light Ball

Propeller + Propeller
Propeller + TNT
Propeller + Light Ball

TNT + TNT
TNT + Light Ball

Light Ball + Light Ball
```

Do not create separate pair classes unless the logic genuinely becomes clearer.

A table-driven/switch-based combination resolver may be simpler.

## Transformation Combinations

If Light Ball + special transforms normal tiles into specials, define:

```text
which color is selected
which board cells transform
which special orientation is used
whether transformed original colors count toward goal
activation order
randomness / tie-break behavior
```

No hidden random iteration.

## Propeller Carry Combinations

If implemented, define:

```text
how target is chosen
where the carried effect centers
whether local takeoff effect still occurs
what happens if target is another special
```

## Tests

One focused test per approved combination minimum, plus:

- edge/corner footprint cases where relevant
- deterministic repeated run
- no special activated twice
- one player move consumed
- correct final stable board
- correct score/goal facts
- input locked throughout

## Learning Questions

- Why are combinations separate from normal chain reactions?
- Why is a combination rule not simply "activate both independently"?
- What makes a lookup/switch enough today?
- What would justify a Strategy-based combination system later?

## Commit

No implementation or commit in Phase 10. For separately approved future combination work, a possible commit message is:

```text
Add special tile combinations
```

STOP.

---

# Step 8 — Special Tile Presentation

## Goal

Make every special visually distinguishable without an art-production detour.

Gameplay logic and minimal special rendering already work before this step. Build on Step 4's non-throwing special representation; this step adds distinctive icons/orientation visuals and activation feedback.

## Required Visual Distinction

The player must easily tell:

```text
Horizontal Rocket
Vertical Rocket
Propeller
TNT
Light Ball
```

apart from normal tiles and from each other.

Use the smallest maintainable solution compatible with the current project.

Possible temporary engineering visuals:

```text
simple icon/symbol overlay
orientation marker
different sprite/shape
small label
```

Do not spend the phase building polished art.

## TileView Responsibility

`TileView` may display:

```text
normal color
special kind
orientation
```

It must not decide:

```text
what a Rocket clears
what a TNT radius is
what a Light Ball targets
```

## Animation Scope

Basic feedback is enough.

Do not build elaborate flight / particle systems unless already trivial.

Potential simple feedback:

```text
Rocket line flash
TNT scale pulse
Propeller short movement/indicator
Light Ball flash
```

Full polish belongs to Phase 11.

## Unity Learning Rule

Before changing prefab/components/sprites/TMP/scene, explain manual steps first.

## Tests / Validation

- each special renders a distinguishable state
- gravity keeps view kind synchronized
- created special shows correct visual
- activated special disappears/changes correctly
- Board/View/lookup consistency remains intact
- no missing references
- Console clean

## Commit

Suggested:

```text
Show special tiles in presentation
```

STOP.

---

# Step 9 — Runtime Sequence Review

## Goal

Exercise the complete Phase 10 behavior in `SampleScene`.

No new feature work.

## Runtime Scenarios

At minimum verify practical examples of:

```text
4 horizontal → Horizontal Rocket
4 vertical   → Vertical Rocket
2×2          → Propeller
T/L          → TNT
5 straight   → Light Ball
```

Verify actual player activation for every supported special.

Verify:

```text
move cost exactly once
score / goal updates after stable
restart disabled while busy
restart re-enabled after cleanup
special chain
normal cascade after special
special creation during cascade if approved
Won through special clear
terminal input blocked
```

Smoke-test an ordinary special + special swap: both activate through the chain queue, the action costs one move, and no combination bonus occurs. Explicit combination bonuses remain deferred.

Also verify a swapped Rocket/TNT whose old and new footprints differ, a special + normal swap whose normal partner forms a pattern, and a restart followed by the same moves reproducing Propeller targets and refill behavior. Use the approved Step 1 timing/anchor/eligibility contracts when checking these cases.

Restore any temporary level config afterward.

## Architecture Review

Confirm:

```text
Match analysis owns pattern facts
special creation policy owns creation decision
special-effect logic owns logical footprints/chains
BoardResolutionRunner owns sequencing
BoardInputController owns input sequencing
LevelProgress owns score/goal/outcome
TileView owns visuals
```

Use actual final class names.

## Explicitly Confirm Absent

Unless Step 1 justified them:

```text
no generic ability framework
no global event bus
no SpecialTile MonoBehaviour containing gameplay rules
no Factory singleton
no State Pattern per special
no reflection-driven effect registry
no object pooling
no obstacle system
no mobile gesture rewrite
```

## Coverage Review

Map tests to:

```text
creation rules
priority
anchors
individual activation
edge footprints
chain reactions
move cost
score / goal
Won / Lost
restart busy gate
visual consistency
ordinary two-special activation; explicit combination bonuses remain deferred
```

Only add a test if a real contract is missing.

## Commit

Only for a real fix or missing test.

No empty commit.

STOP before Step 10.

---

# Step 10 — Phase 10 Validation Pass

## Goal

Close Phase 10 with a full regression pass.

Do not start Phase 11.

## Run

```text
all EditMode tests
all PlayMode tests
Unity compilation
Console inspection
```

Verify:

```text
SampleScene open
not playing
no unsaved changes
working tree clean
branch in expected Git state
```

## Confirm Existing Behavior

All Phase 2–9 behavior remains correct:

```text
normal generation
no initial approved resolvable patterns, including same-color 2×2 squares
normal adjacent swap
invalid rollback
ordinary matching
clear / gravity / refill
cascades
input locking
ResolutionResult
truthful ClearCells removal facts; normal-only ResolutionResult rejects specials
normal-only GoalTileType validation through the shared classification
moves
score
goal
Win / Lose
restart
HUD
```

## Confirm Special Rules

For every approved special:

```text
creation correct
anchor deterministic
activation correct
chains terminate
score/goal facts correct
one move cost
visual correct
```

Confirm the approved Option A boundary:

```text
special + special swaps queue both for ordinary activation
one move, no explicit combination bonuses
existing chains finish before new special creation; no retroactive hits
Light Ball partner/fallback/tie/no-normal-color rules are tested
activation origin and normal-partner match/anchor timing match Step 1
Propeller target eligibility and no-target fallback are tested
both random streams restart reproducibly without sharing state
```

## Confirm Boundaries

Core still has:

```text
no UnityEngine
no MonoBehaviour
no Presentation dependency
```

Presentation contains no special gameplay rules.

`LevelProgress` contains no board-special logic.

## Final Learning Review

I should be able to explain:

1. why Phase 10 needed richer match information
2. how special tiles are represented
3. how normal tiles are distinguished from specials
4. Propeller's effect on valid-match rules
5. creation priority
6. creation anchor policy
7. why matched colors are counted before a special is created
8. special creation vs activation
9. Rocket orientation
10. TNT footprint
11. Propeller targeting and RNG
12. Light Ball target selection
13. chain-reaction queue
14. why a special activates only once per chain
15. how special clears reach `ResolutionResult`
16. why one special action costs one move regardless of chains
17. how special activation fits the existing input lock
18. why Strategy was or was not used
19. why Factory was or was not used
20. how special combinations differ from chains
21. how Board/View consistency is preserved
22. what remains intentionally deferred

## Commit

Only if validation requires a real fix.

No empty commit.

STOP and report:

```text
Phase 10 complete
approved special rules
final architecture
test totals
runtime result
Option A confirmed; explicit combination bonuses deferred
deferred work
```

Do not start Phase 11.

---

# Known-Issue Interaction

Before finalizing Phase 10, revisit but do not automatically fix:

```text
DeadBoard
LockedAfterException
RefillSeedReuse
SelectionFeedback
MobileLayoutAndPicking
WatchList / BoardView size
MissingReadme
```

Important Phase 10-specific notes:

## LockedAfterException

Special callbacks/effect sequencing create more places where an exception could leave input locked.

Do not redesign exception recovery during a special feature unless a real failure appears, but keep the known issue visible.

## RefillSeedReuse

Propeller introduces a real second use of randomness.

Keep the existing refill-seed reuse policy unchanged. Propeller has its own deterministic targeting stream, recreated on every start/restart under the approved Step 1 ownership/seed contract; do not consume refill RNG state for targeting.

## BoardView WatchList

If special presentation makes `BoardView` substantially larger, that is real evidence for extracting an animation/presentation helper.

Do not extract preemptively.

---

# Recommended Codex Start Prompt

```text
Follow AGENTS.md and Docs/Phases/PHASE10_EXECUTION.md.

We are starting Phase 10 — Special Tiles / Power-Ups.

IMPORTANT:
This is a design-first phase.
Do not edit files yet.
Do not implement specials yet.

The old roadmap is incomplete for a Royal Match-style system because it omits Propeller.
We are studying four power-ups:

- Rocket
- Propeller
- TNT
- Light Ball

Read only:
- the project context needed for this task
- Step 1 — Architecture Audit and Exact Rule Specification

Inspect at minimum:
- TileType
- Board
- BoardGenerator
- PaletteValidation
- MatchFinder
- SwapValidator
- BoardResolution
- BoardResolutionRunner
- BoardInputController
- BoardView
- TileView
- ResolutionResult
- LevelProgress
- relevant tests

No Unity scene/prefab changes.
No code changes.
No commit.

For Step 1, specify the remaining details and confirm the already-approved contracts:
1. board representation
2. normal-vs-special classification
3. richer match-analysis shape
4. Propeller 2x2 detection
5. T/L detection
6. pattern priority
7. creation anchors
8. cascade special creation
9. score/goal semantics
10. activation input policy
11. special-swap validity
12. Propeller target policy
13. RNG ownership
14. TNT footprint
15. Light Ball targeting
16. chain-reaction algorithm
17. confirm approved Option A; no explicit combination bonuses
18. Strategy decision
19. Factory decision
20. activation origin, with the post-swap position as the proposed baseline
21. normal-partner match timing and when first-player-round anchor preference expires
22. exact resolution entry point for swap context and initial activations
23. connected groups and a concrete shape/outcome table
24. Propeller RNG owner, seed derivation, and restart reset
25. remote-target eligibility, pending-effect exclusions, and no-target fallback
26. chain read state and deduplicated physical/color-clear facts

Compare alternatives and recommend the smallest clean design.

Pay special attention to:
- current FindMatches only returns positions
- a 2x2 Propeller changes what counts as a valid match
- special creation and special activation are different operations
- a special hit by another special may chain
- Propeller randomness should not silently perturb refill randomness
- specials must not accidentally participate as normal colors
- LevelProgress must remain independent from special gameplay rules
- initial generation prevents all approved patterns, including 2x2 squares; refill still allows them
- ClearCells reports physical removals truthfully; ResolutionResult accepts only normal collectible colors
- LevelProgress goal validation uses that same normal-color classification
- minimal special rendering is ready by Step 4; distinctive visuals remain in Step 8
- Light Ball uses the normal partner color, otherwise the most common normal color with a fixed TileType-order tie-break; no normal colors means no extra color clear
- existing chains complete before new special creation; no retroactive hits on newly created specials
- a frozen Board still needs explicit pending-removal and target-eligibility rules
- three normal palette colors remain sufficient; preserve strong seeded regression tests when layouts change
- test notation must preserve P for Purple and give specials distinct symbols
- LevelProgress scoring/outcome rules remain unchanged, but its goal-type validation must change

Step 5 is split into 5a (Rocket, TNT, chain queue) and 5b (Propeller, Light Ball). Each has a separate approval stop; do not implement both under one approval.

Preserve the approved combination scope:
A. four individual specials + chain reactions, no explicit combination bonuses.
A special + special swap queues both for ordinary activation and costs one move.
Step 7 remains deferred future work, with no implementation or commit in Phase 10.

Do not implement until I approve.

Answer the Step 1 learning questions.

STOP.
```

---

# Continue Prompt

```text
Approved.

Proceed to the next step in Docs/Phases/PHASE10_EXECUTION.md only.

The activation work has two separate steps: 5a, then 5b. Approval for 5a does not authorize 5b; approval for 5b does not authorize Step 6.

Before editing:
- inspect the exact files involved
- restate the approved contract relevant to this step
- identify what must remain unchanged
- explain any migration before changing public data types

For Unity Editor / MCP actions, follow the learning rule first.

Then:
- implement only this step
- add focused tests
- run relevant regression tests
- verify compilation and Console
- summarize changed files and behavior
- answer the step's learning questions
- commit only with the step's approved message when a real change exists
- STOP

Do not start the following step.
```

---

# Claude Teaching Prompt

Use after Phase 10 is complete.

```text
Do not modify files.

Teach me the final Phase 10 special-tile system as if I must defend it in a senior Unity / gameplay-engineering interview.

Use the actual repository implementation.

Explain:

1. exact special tiles implemented
2. creation pattern for each
3. why Propeller required new match semantics
4. logical representation of normal vs special cells
5. how MatchFinder / match analysis evolved
6. pattern priority
7. creation-anchor rules
8. creation during cascades
9. special creation vs activation
10. individual Rocket behavior
11. Propeller behavior and target selection
12. Propeller RNG ownership
13. TNT footprint
14. Light Ball targeting
15. chain-reaction algorithm
16. why chain activation terminates
17. player move classification
18. move-cost behavior
19. score / goal fact flow
20. ordinary two-special activation and why explicit combination bonuses remain deferred
21. why Strategy was or was not used
22. why Factory was or was not used
23. how Presentation stays separate
24. important tests
25. edge cases
26. performance characteristics
27. what future requirements would force a redesign
28. what was deliberately deferred

Also explain every Unity Editor / prefab / scene change Phase 10 required and how I could reproduce it manually.

Keep everything grounded in the actual code.
Do not modify files.
```
