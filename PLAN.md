# Agentic Match-3 Unity Project Plan

> **Goal:** Build a polished mobile Match-3 prototype in Unity 6 while learning Unity architecture, Match-3 gameplay systems, Git workflows, Codex, Claude Code, and Unity MCP.
>
> This repository is intentionally **AI-assisted / agentic**. The purpose is not to pretend every line was written manually. The purpose is to learn how to specify, supervise, review, test, and understand an AI-built Unity project.

---

## 1. Project Goals

By the end of this project, the game should include:

- Configurable Match-3 board (start with 8×8)
- Multiple tile colors/types
- Adjacent tile swapping
- Invalid swap rollback
- Horizontal and vertical match detection
- Clearing matched tiles
- Gravity
- Refilling
- Cascading matches
- Input locking while the board resolves
- Move counter
- Score
- Simple level goals
- Win / lose states
- Special tiles / boosters
- Basic animations
- Basic sound / VFX hooks
- Mobile-friendly input
- Automated tests for core gameplay logic

The project should also demonstrate:

- Separation of gameplay logic from presentation
- Sensible OOP
- Appropriate use of design patterns
- Testable code
- Clean Git history
- AI-assisted development workflow
- Unity MCP integration
- Ability to explain every important architectural decision

---

# 2. Learning Rules

This project is useful only if I understand what the agents build.

For every meaningful feature, I must be able to answer:

1. **Why does this class exist?**
2. **What responsibility does it own?**
3. **Why is that responsibility not inside another class?**
4. **What data flows into and out of it?**
5. **What would change if the requirement changed?**
6. **What is the algorithmic complexity where relevant?**
7. **What Unity-specific behavior does it depend on?**

If I cannot answer these questions, the feature is **not considered complete**, even if the game runs.

---

# 3. AI Roles and Token-Efficient Context Strategy

Default roles:

## Codex — Primary Implementer

Use Codex primarily for:

- Implementing scoped features
- Editing multiple files
- Running tests
- Inspecting Git diffs
- Performing mechanical refactors
- Using Unity MCP tools
- Fixing compilation errors
- Implementing approved architecture

## Claude Code — Reviewer / Challenger

Use Claude Code primarily for:

- Architecture reviews
- Code reviews
- Finding edge cases
- Explaining unfamiliar code
- Challenging design decisions
- Reviewing performance risks
- Interview-style questioning

The roles may occasionally be swapped.

### Important Rule

**Never let Codex and Claude edit the same feature simultaneously.**

Default flow:

```text
clean Git state
      ↓
Codex implements
      ↓
I inspect the result
      ↓
Claude reviews
      ↓
I decide what feedback is valid
      ↓
Codex fixes approved issues
      ↓
tests
      ↓
commit
```

## Context / Token Rule

Do not duplicate the same instructions across several large files.

Use this hierarchy:

```text
AGENTS.md
↓
short shared engineering + workflow rules

CLAUDE.md
↓
tiny Claude-specific role override

PLAN.md
↓
long roadmap and feature requirements
(read on demand)
```

`PLAN.md` is a **reference document**, not something every agent should read in full before every task.

For a small task such as:

```text
Fix this NullReferenceException.
```

the agent should inspect the relevant code and follow `AGENTS.md`; it does not need the full roadmap.

For roadmap-specific work, point the agent to the relevant phase:

```text
Implement Phase 5 — Match Detection from PLAN.md.
```

This keeps context smaller and reduces unnecessary token usage.

---

# 4. Repository Instructions

Create these files at the repository root:

```text
AGENTS.md
CLAUDE.md
PLAN.md
README.md
```

This file is `PLAN.md`.

## Source of Truth

### `AGENTS.md`

Short shared source of truth for day-to-day agent behavior.

It contains:

- engineering principles
- scope discipline
- Unity rules
- testing expectations
- Git/change workflow

Keep it concise.

### `CLAUDE.md`

Claude-specific adapter only.

It should:

- tell Claude to follow `AGENTS.md`
- define Claude's default role as reviewer / mentor
- tell Claude not to edit files unless explicitly asked
- tell Claude to read only relevant parts of `PLAN.md`

Do **not** duplicate all shared rules here.

### `PLAN.md`

Long-form roadmap and feature specification.

Agents should read only the relevant phase or section when needed.

---

# 5. Git Workflow

Before each feature:

```bash
git status
```

The repository should be clean.

Create a feature branch when appropriate:

```bash
git switch -c feature/core-board
```

After the agent works:

```bash
git diff
```

Review every meaningful change before committing.

Then:

```bash
git add .
git commit -m "Implement core board model"
```

Suggested feature sequence:

```text
feature/project-bootstrap
feature/core-board
feature/board-view
feature/tile-input
feature/swapping
feature/match-detection
feature/board-resolution
feature/invalid-swap
feature/game-state
feature/special-tiles
feature/goals
feature/mobile-polish
feature/performance-pass
```

Do not accumulate several large AI-generated features into one unreviewed commit.

---

# 6. Phase 0 — Project Bootstrap

## Goal

Create a clean Unity 6 2D project and Git repository.

### Tasks

- Create Unity 6 project.
- Initialize Git.
- Add a Unity `.gitignore`.
- Create:
  - `Assets/Scripts`
  - `Assets/Tests`
  - `Assets/Prefabs`
  - `Assets/Scenes`
  - `Assets/Art`
  - `Assets/Audio`
- Create `AGENTS.md`
- Create the small `CLAUDE.md`
- Add this `PLAN.md`
- Make baseline commit.

### First Agent Prompt

```text
Read AGENTS.md.

For this task, also read the project goals and early architecture sections
of PLAN.md.

Do not modify any files yet.

Inspect the repository and propose a minimal folder and architecture plan
for the Match-3 project.

The project should remain small enough for a portfolio project.

Explain:
- major classes
- responsibilities
- data flow
- game states
- testing strategy
- implementation order

Avoid enterprise-style over-engineering.
```

### Learning Gate

Before continuing, I should understand the proposed architecture.

---

# 7. Phase 1 — Unity MCP Setup

## Goal

Allow Codex / Claude Code to interact with the Unity Editor through structured tools instead of only editing `.cs` files.

Conceptually:

```text
Codex / Claude Code
        ↓
      MCP
        ↓
Unity MCP Server / Bridge
        ↓
    Unity Editor
```

Possible capabilities include:

- Inspect active scene
- Inspect hierarchy
- Create GameObjects
- Add components
- Modify component values
- Create assets / prefabs
- Enter Play Mode
- Read Unity Console
- Run tests
- Trigger builds
- Inspect project state

### Smoke Test 1

Ask the agent:

```text
Use the Unity MCP tools.

Inspect the currently open scene and tell me:
- scene name
- root GameObjects

Do not modify anything.
```

### Smoke Test 2

```text
Using Unity MCP, create an empty GameObject named MCP_Test.

Do not modify anything else.
```

Confirm visually inside Unity.

Then delete the test object.

### Success Criteria

The agent can:

- Read the Unity scene
- Modify the hierarchy
- See resulting Unity state

Do not start gameplay implementation until this connection is reliable.

---

# 8. Phase 2 — Core Board Model

## Goal

Implement gameplay state independently from visuals.

### Requirements

- Configurable width / height
- Start with 8×8
- Multiple tile types
- Logical grid representation
- Grid coordinates
- Deterministic/testable design where possible
- Initial board generation
- Initial board should not automatically contain matches
- No unnecessary MonoBehaviour dependencies

### Suggested Concepts

```text
Board
GridPosition
Tile
TileType
BoardGenerator
```

Do not force these exact names if a better design emerges.

### Codex Prompt

```text
Read AGENTS.md and PLAN.md.

Implement the core logical Match-3 board model.

Requirements:
- configurable width and height
- multiple tile types
- grid coordinates
- logical board state independent from GameObjects
- generate an initial board
- initial board should contain no automatic 3+ matches
- no unnecessary MonoBehaviour dependency

Add EditMode tests.

Do not create presentation/UI code yet.

After implementation:
1. run relevant tests
2. summarize the architecture
3. explain any trade-offs
4. stop
```

### Claude Review Prompt

```text
Review the core board implementation.

Do not edit any files.

Evaluate:
- responsibilities
- coupling
- correctness
- testability
- edge cases
- unnecessary abstractions
- whether Unity dependencies leaked into the core model

Rank findings by severity.
```

### Learning Gate

I must understand:

- How the board is stored
- Why the model is not the same thing as the visual board
- How coordinates work
- How initial matches are prevented

---

# 9. Phase 3 — Board Presentation

## Goal

Render the logical board inside Unity.

### Requirements

Create a presentation layer such as:

```text
BoardView
TileView
```

Responsibilities should remain clear:

```text
Board Model
    ↓
Board View
    ↓
Tile Views / Sprites
```

Gameplay rules should not migrate into `TileView`.

### Codex Prompt

```text
Create a Unity presentation layer for the existing logical board.

Requirements:
- visualize the current board
- create/update tile views
- keep gameplay rules in the logical model
- keep TileView focused on presentation
- use Unity MCP when appropriate for scene/prefab setup

Do not implement swapping or matching yet.

Run Unity validation/tests and check the Console before stopping.
```

### Learning Gate

Be able to explain:

> Why are `Tile` and `TileView` separate?

---

# 10. Phase 4 — Input and Swapping

## Goal

Allow the player to choose two neighboring tiles and swap them.

### Requirements

- Mouse input during development
- Touch-compatible architecture
- Only orthogonally adjacent tiles
- Ignore illegal distant selections
- Animate swaps
- Prevent additional input while the swap animation runs

Do **not** validate matches yet.

### Prompt

```text
Implement tile selection and adjacent swapping.

Requirements:
- only orthogonally adjacent tiles can swap
- board model remains authoritative
- visuals animate to the resulting board state
- input is locked during swap animation
- design should be usable with touch input later

Do not implement match validation yet.

Add tests for any pure gameplay logic introduced.
```

### Learning Gate

Understand:

- Model state vs visual state
- Why allowing input during animation can create bugs
- How adjacency is checked

---

# 11. Phase 5 — Match Detection

## Goal

Detect matches correctly and independently.

### Requirements

Detect:

- Horizontal matches of 3+
- Vertical matches of 3+
- Match length 4+
- Match length 5+
- Intersections
- Overlapping horizontal / vertical groups

### Codex Prompt

```text
Implement Match-3 detection.

Requirements:
- horizontal matches of 3+
- vertical matches of 3+
- matches may overlap/intersect
- return matched grid positions in a form useful to board resolution
- keep the algorithm testable and independent from presentation

Add comprehensive EditMode tests.

Do not implement clearing or gravity yet.
```

### Claude Teaching Prompt

```text
Explain MatchFinder to me as if I must defend it in a technical interview.

Do not modify files.

Explain:
1. algorithm
2. time complexity
3. memory complexity
4. edge cases
5. alternative implementations
6. likely failure modes
```

### Required Tests

At minimum:

```text
XXX.....
........
........

X.......
X.......
X.......

XXXX....
........

XXX.....
..X.....
..X.....

no matches

multiple simultaneous matches

edge/corner matches
```

### Learning Gate

I should be capable of implementing a basic MatchFinder myself after studying the implementation.

---

# 12. Phase 6 — Swap Validation

## Goal

A swap only remains if it creates a match.

Flow:

```text
swap
 ↓
match?
 ↙   ↘
yes   no
 ↓     ↓
keep  animate back
```

### Requirements

- Validate after swap
- Invalid swap returns tiles to original positions
- Board model remains consistent
- Input remains blocked until operation completes

### Prompt

```text
Add swap validation.

A swap is valid only if the resulting board produces a match involving
the swapped tiles.

For an invalid swap:
- visually swap
- determine it is invalid
- animate the tiles back
- restore logical state
- unlock input only after rollback finishes

Add tests for logical validation behavior.
```

---

# 13. Phase 7 — Board Resolution

## Goal

Implement the complete Match-3 resolution cycle.

Flow:

```text
Match
  ↓
Clear
  ↓
Gravity
  ↓
Refill
  ↓
Match again?
 ↙       ↘
yes       no
 ↓         ↓
repeat    Idle
```

### Requirements

- Clear matches
- Collapse tiles downward
- Fill empty spaces
- Detect cascades
- Repeat until stable
- Do not accept input during resolution
- Keep model and presentation synchronized

### Prompt

```text
Implement board resolution.

Required sequence:

match
→ clear
→ gravity
→ refill
→ detect new matches
→ repeat until stable

Requirements:
- logical board state remains authoritative
- animations complete in the correct order
- player input is disabled until the board is stable
- cascades resolve automatically
- core operations have EditMode tests

Before editing, explain the proposed state flow.
Then implement it.
```

---

# 14. Phase 8 — Explicit Game State

At this point, evaluate whether a State Pattern is actually helpful.

Potential states:

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

Do **not** create a class hierarchy merely to say the project uses the State Pattern.

Ask:

```text
Review the current gameplay flow.

Would an explicit State Pattern make the code easier to understand,
change, or test?

Do not implement anything yet.

Compare:
1. current/simple enum approach
2. explicit state classes

Recommend the simpler design that still handles the requirements.
```

Only implement a richer state architecture if justified.

---

# 15. Phase 9 — Scoring, Moves, and Goals

## Goal

Turn the prototype into a small playable level.

### Requirements

- Starting move count
- Consume one move for each valid player swap
- Score
- Goal tracking
- Win state
- Lose state
- Restart

Example level:

```text
Moves: 20
Goal: collect 25 red tiles
```

### Architecture Questions

Consider how systems communicate:

```text
Board resolves match
       ↓
Gameplay event/result
       ↓
Score / Goal systems react
       ↓
UI updates
```

This is a good place to consider Observer/events.

### Prompt

```text
Add a small level loop.

Requirements:
- configurable starting move count
- score
- collection goal
- win state
- lose state
- restart

Keep UI updates decoupled from core board logic.

Use events only where they materially reduce coupling.
Do not create a global event system unless necessary.
```

---

# 16. Phase 10 — Special Tiles

## Goal

Add deeper Match-3 behavior without destroying the architecture.

Initial rules:

```text
4 in a row     → Rocket
5 in a row     → Color Bomb
T / L match    → Bomb
```

Potential behavior:

```text
Rocket     → clears row or column
Bomb       → clears local area
Color Bomb → clears all tiles of selected type
```

### Design-First Prompt

```text
We now want to add special tiles:

- Rocket
- Bomb
- Color Bomb

Do not edit any files yet.

Inspect the current architecture and propose the smallest clean extension
that supports these behaviors.

Discuss:
- whether TileType should change
- where activation behavior should live
- whether Strategy is useful
- whether Factory is useful
- how MatchFinder should avoid becoming a giant special-case class
- how special tile interactions could be extended later

Avoid patterns that are not necessary.
```

Only after approving the design:

```text
Implement the approved special-tile design.

Add tests for:
- creation conditions
- activation behavior
- chain reactions where applicable
```

---

# 17. Phase 11 — Polish

Add improvements incrementally.

Possible tasks:

### Swap animation

```text
Improve tile swapping with simple easing.

Do not change gameplay rules.
```

### Falling animation

```text
Animate gravity-driven tile movement while preserving deterministic
logical board behavior.
```

### Clear feedback

```text
Add a small scale/fade effect when matched tiles are removed.
```

### Special tile feedback

```text
Add simple visual feedback for Rocket, Bomb and Color Bomb activation.
```

### UI polish

Add:

- Moves
- Goal progress
- Score
- Win panel
- Lose panel
- Restart

Do not spend large amounts of time creating art.

The project is primarily an engineering / agentic-development portfolio piece.

---

# 18. Phase 12 — Mobile Pass

## Requirements

- Touch input
- Portrait-oriented layout if appropriate
- UI anchors
- Different screen aspect ratios
- No keyboard requirement
- Avoid obviously wasteful allocations
- Avoid unnecessary physics
- Avoid unnecessary per-frame polling

Potential build target:

```text
Android
```

Building to device is useful but not required for every development step.

---

# 19. Phase 13 — Testing Strategy

Prioritize tests for deterministic gameplay logic.

## High-value EditMode tests

### Board

- Dimensions
- Tile access
- Invalid positions
- Generation rules

### MatchFinder

- Horizontal match
- Vertical match
- 4+
- 5+
- Intersection
- Multiple matches
- No match

### Swap

- Adjacent valid
- Diagonal invalid
- Distant invalid
- Match-producing swap
- Non-match swap

### Gravity

Example:

```text
A
.
B
.
C
```

becomes:

```text
.
.
A
B
C
```

### Refill

- All empty cells filled
- Board remains valid
- Correct dimensions

### Resolution

- Cascades terminate
- Stable board eventually returns
- Correct cleared positions

### Special tiles

- Creation
- Activation
- Chain reactions

---

# 20. Phase 14 — AI Adversarial Review

Ask Claude:

```text
Try to break this Match-3 implementation.

Do not modify files.

Generate at least 15 concrete board states or interaction sequences
that could expose bugs in:

- matching
- swapping
- rollback
- gravity
- refill
- cascades
- special tiles
- state transitions
- input locking

For each case, explain the expected behavior.
```

Convert the best findings into tests.

---

# 21. Phase 15 — Performance Review

First measure / inspect. Do not blindly optimize.

Prompt:

```text
Review this project as a mobile Unity engineer.

Do not modify anything.

Look for:
- avoidable garbage allocations
- LINQ in hot gameplay paths
- repeated GetComponent calls
- unnecessary Update methods
- excessive Instantiate/Destroy
- unnecessary physics
- expensive operations repeated every frame
- excessive coupling that could create performance bugs

Rank findings:
1. real issue now
2. likely future issue
3. theoretical / not worth optimizing
```

Possible topics to learn:

- Unity Profiler
- GC allocations
- Object pooling
- Update cost
- Instantiate / Destroy
- Sprite rendering
- Draw calls
- CPU vs GPU bottlenecks

Only add pooling if it solves an actual issue or meaningfully demonstrates mobile-game engineering.

---

# 22. Phase 16 — Cross-Agent Review

Use one model to critique the other.

### Claude Review

```text
Review the entire repository as a senior Unity/mobile-game engineer.

Do not modify files.

Identify the five highest-value engineering improvements.

Focus on:
- correctness
- maintainability
- extensibility
- Unity usage
- mobile considerations
- tests

Avoid speculative over-engineering.
```

### Codex Response

Give Codex the review:

```text
Read this external review.

Do not modify files yet.

For each finding:
- agree / disagree
- explain why
- estimate engineering value
- propose the smallest fix if needed

Do not blindly accept reviewer feedback.
```

I make the final decision.

---

# 23. Phase 17 — Interview Mode

Once the repository is mature:

```text
Act as a senior engineer interviewing me about this repository.

Ask one question at a time.

Challenge weak answers.

Focus on:
- OOP
- design patterns
- Unity
- algorithms
- state management
- coupling
- testability
- mobile performance
- Match-3 edge cases

Frequently ask:
"What would happen if the requirement changed?"
```

Useful questions include:

- Why separate `Board` and `BoardView`?
- Why isn't every Tile a gameplay-heavy MonoBehaviour?
- Why did you choose this MatchFinder algorithm?
- How do you prevent user input during a cascade?
- What happens if an animation is interrupted?
- How would you add a new booster?
- Would you use Singleton here?
- Where does Observer help?
- Where would Observer be over-engineering?
- What happens on a 12×12 board?
- How would you make the board deterministic for testing?
- What allocations happen during one move?

---

# 24. Design Patterns to Learn Through the Project

Do not force them into the project.

## Observer

Useful when:

```text
gameplay event
   ↓
score / goals / UI react
```

Question:

> Does an event reduce coupling here?

---

## State

Useful for:

```text
Idle
Swapping
Resolving
Win
Lose
```

Question:

> Is state behavior complex enough to justify explicit state objects, or is an enum sufficient?

---

## Strategy

Potentially useful for:

```text
Rocket behavior
Bomb behavior
Color Bomb behavior
```

Question:

> Are behaviors genuinely interchangeable and independently extensible?

---

## Factory

Potentially useful when creation becomes complicated:

```text
CreateTile(...)
CreateSpecialTile(...)
```

Question:

> Is creation logic complex enough to deserve its own abstraction?

---

## Object Pool

Potentially useful for:

- Tile views
- Particles
- Repeated effects

Question:

> Is Instantiate/Destroy actually causing a measurable or likely problem?

---

## Singleton

Treat cautiously.

Do not default to:

```text
GameManager.Instance
AudioManager.Instance
UIManager.Instance
BoardManager.Instance
EverythingManager.Instance
```

Be able to explain the trade-off:

> Convenient global access vs hidden dependencies and global state.

---

# 25. Scope Boundaries

Do **not** add these unless the core project is already excellent:

- ECS / DOTS
- Multiplayer
- Networking
- Backend
- Ads
- IAP
- LiveOps
- Complex shaders
- Custom render pipeline
- Procedural art pipeline
- Heavy dependency-injection framework
- Huge generic architecture framework

This project should remain understandable.

---

# 26. Definition of Done

The project is complete when:

- [ ] Board generates reliably
- [ ] Initial board has no unwanted matches
- [ ] Adjacent swaps work
- [ ] Invalid swaps rollback
- [ ] Match detection works
- [ ] Matches clear
- [ ] Gravity works
- [ ] Refill works
- [ ] Cascades resolve
- [ ] Input locking works
- [ ] Moves work
- [ ] Score works
- [ ] Goals work
- [ ] Win works
- [ ] Lose works
- [ ] Restart works
- [ ] Special tiles work
- [ ] Basic animations work
- [ ] Mobile input works
- [ ] Important core logic has tests
- [ ] Unity Console is clean
- [ ] Project can be built/run
- [ ] README explains architecture
- [ ] README discloses AI-assisted workflow
- [ ] I can explain every major class
- [ ] I can explain every design pattern used
- [ ] I can explain MatchFinder without AI
- [ ] I can modify the project without relying entirely on AI

---

# 27. README Content for the Final Repository

Suggested structure:

```text
# Agentic Match-3

## Overview

## Gameplay

## Features

## Architecture

## Project Structure

## Testing

## AI Development Workflow

## Unity MCP

## Engineering Decisions

## What I Learned

## Screenshots / GIF

## How to Run
```

Be transparent:

> This project was intentionally developed as an experiment in agentic game development. Codex and Claude Code were used for implementation, review, testing, and Unity Editor automation under human-written requirements and supervision.

That is a feature of this repository, not something to hide.

---

# 28. Parallel Dream Games Preparation

This repository is **not** the final proof that I personally can write a Match-3 game without AI.

Treat it as:

```text
Agentic Match-3
AI implementation: HIGH
Human specification/review: HIGH
Purpose:
- learn Unity
- learn Match-3 systems
- learn Codex
- learn Claude Code
- learn MCP
```

Later, create a separate project:

```text
Dream-style Match-3
AI implementation: LOW / MEDIUM
Human implementation: HIGH
Purpose:
- demonstrate personal Unity engineering
- interview preparation
- portfolio
```

Knowledge should flow from the first project into the second.

Code should not simply be copied without understanding.

---

# 29. Context and Token Budget Guidelines

AI context is a limited project resource.

## Keep Always-On Instructions Small

Aim for:

```text
AGENTS.md   → short, high-signal shared rules
CLAUDE.md   → very short Claude role adapter
PLAN.md     → detailed but on-demand reference
```

Do not repeat PLAN content inside AGENTS or CLAUDE.

## Load Only What the Task Needs

Examples:

```text
Fix this compile error.
```

Needs current code and `AGENTS.md`, not the full roadmap.

```text
Implement special tiles.
```

Should reference the Special Tiles phase in `PLAN.md`.

```text
Review the entire architecture before final polish.
```

May justify broader repository / PLAN reading.

## Prefer Repository State Over Conversation History

For a fresh agent session, prefer:

```text
Inspect the repository and current git status.
Then read Phase X of PLAN.md.
```

instead of manually pasting long summaries of previous sessions.

## Avoid Duplicate Agent Work

Do not ask Codex and Claude to independently implement the same feature unless comparison is the explicit goal.

Default:

```text
Codex = implementation
Claude = review / teaching
```

---

# 30. Recommended Working Loop

For every feature:

```text
1. Write requirement myself
       ↓
2. Point the agent to the relevant PLAN.md phase only if needed
       ↓
3. Ask for design first if architecture is non-trivial
       ↓
4. Understand / approve design
       ↓
5. Codex implements
       ↓
6. Run tests + Unity validation
       ↓
7. Read git diff
       ↓
8. Claude reviews
       ↓
9. I choose valid feedback
       ↓
10. Codex fixes approved issues
       ↓
11. Re-run tests
       ↓
12. I explain the feature in my own words
       ↓
13. Commit
```

This loop is more important than the exact architecture.

---

# 31. First Three Actions

Start here.

## Action 1

Create the Unity 6 project and Git repository.

## Action 2

Add:

```text
PLAN.md
AGENTS.md
CLAUDE.md
```

Use:

- `AGENTS.md` as the short shared rule set
- `CLAUDE.md` as a tiny Claude-specific adapter
- `PLAN.md` as an on-demand roadmap/reference

Make a baseline commit.

## Action 3

Before writing gameplay code, set up Unity MCP and pass these two smoke tests:

```text
Inspect current scene without changing it.
```

then:

```text
Create an empty GameObject named MCP_Test.
```

Once that works, begin **Phase 2 — Core Board Model**.

---

## Final Principle

The goal is not:

> "Can AI build a Match-3 game?"

It probably can build much of one.

The useful question is:

> **"Can I use AI agents to build a real Unity system while becoming capable of understanding, reviewing, debugging, extending, and eventually rebuilding the important parts myself?"**

If the answer is yes, this project succeeds.
