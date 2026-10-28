# Agentic Match-3 Unity Project Plan

> **Goal:** Build a polished, small mobile Match-3 game in Unity 6 that is understandable as an engineering/learning project and can be taken through a real Google Play release process.
>
> This repository is intentionally **AI-assisted / agentic**. The purpose is not to pretend every line was written manually. The purpose is to learn how to specify, supervise, review, test, understand, ship, and maintain an AI-built Unity project.
>
> **Roadmap evolution (after Phase 12):** Phases 0-12 achieved the original polished-mobile-prototype goal and produced a validated Android build on a physical device. From Phase 13 onward, the roadmap deliberately shifts from prototype completion to release readiness. The project should remain small and understandable; becoming store-ready does not justify adding ads, IAP, backend, live-ops, or unrelated product scope.

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
- Android production-build readiness
- Google Play release readiness for the approved small-game scope

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

The goal of Phase 11 is to improve visual readability, feedback, and presentation without changing gameplay rules or deterministic logical behavior.

The project remains primarily an engineering / gameplay-programming / agentic-development portfolio piece, so art production should stay focused and modest.

## 1. Visual Identity Pass

Phase 10 intentionally used temporary text markers for specials:

- `<->`
- `P`
- `TNT`
- `LB`

Phase 11 should replace these placeholders with an original, coherent visual language.

Approved working direction:

**Prismatic Energy**

Player-facing concepts:

- HorizontalRocket / VerticalRocket → Beam Crystal
- Propeller → Wisp
- TNT → Nova Gem
- LightBall → Spectrum Core

These names are presentation/design vocabulary only for now.
Do not rename Core enums, gameplay classes, tests, or domain terminology unless a future player-facing UI actually requires a name mapping.

Keep the existing Core identifiers unchanged:

- HorizontalRocket
- VerticalRocket
- Propeller
- TNT
- LightBall

### Normal tile visual language

Normal collectible tiles should:

- share the same outer gemstone silhouette;
- remain primarily distinguished by color;
- optionally use subtle inner symbols/patterns as a secondary readability/accessibility cue;
- remain visually simpler than specials.

Normal colors remain:

- Red
- Green
- Blue
- Yellow
- Purple
- Orange

Do not make each normal color use a completely different outer silhouette unless real board-size testing proves it necessary.

### Special visual language

Specials should:

- have clearly different silhouettes from normal tiles and from each other;
- remain recognizable in grayscale;
- remain readable around 32–64 px;
- communicate their gameplay role through shape;
- use a consistent outline/material/lighting language.

Working concepts:

Beam Crystal


# 19. Phase 13 — Release Readiness & Product Gap Audit

## Goal

Determine what is actually missing between the current validated Android prototype and a small game that we are comfortable distributing through Google Play.

This is an **audit and prioritization phase first**.

Do not begin fixing issues merely because they are discovered.

The output of this phase should classify every relevant gap as:

- **MUST FIX BEFORE STORE**
- **SHOULD FIX BEFORE STORE**
- **CAN DEFER AFTER RELEASE**
- **NOT A REAL ISSUE / CLOSE**

The classification must be justified by player impact, crash/stuck risk, policy/release requirements, maintainability, and evidence from the current repository.

## Current baseline

Phase 12 already established:

- touch-only mobile gameplay;
- portrait safe-area layout;
- responsive camera framing;
- Android 60 FPS target policy;
- successful IL2CPP + ARM64 development APK;
- physical Samsung Galaxy S23 validation;
- deterministic restart behavior;
- broad EditMode / PlayMode / runtime scenario coverage.

Do not repeat those checks without a reason.

## Audit inputs

Inspect:

- current known-issue documents;
- current tests and coverage;
- Android Player settings;
- build artifacts and validation reports;
- current UI/presentation;
- current app lifecycle behavior;
- current repository documentation;
- current Google Play requirements.

At minimum review these existing issues:

- DeadBoard
- LockedAfterException
- SelectionFeedback
- ColourOnlyGems
- RefillSeedReuse
- MissingReadme
- TallScreenComposition
- OutcomeDimSafeArea
- MobileLayoutAndPicking remaining limitations
- FrameRatePolicy remaining limitations
- WatchList

## Working Google Play facts to verify during execution

Google Play requirements change over time. Re-check official documentation during the phase instead of relying only on this roadmap.

As of 2026-10-28:

- New mobile apps and app updates submitted to Google Play must target Android 16 / API level 36 or higher.
- New Google Play apps are published using Android App Bundles (AAB).
- New apps use Play App Signing; release/upload-key handling must be planned safely.
- Play Console requires store-listing and app-content declarations such as content rating, target audience, ads declaration, privacy/data-safety information as applicable.
- If the developer account is subject to Google's new-personal-account production-access testing requirement, production access requires a qualifying closed test. Verify the account-specific requirement in Play Console before planning dates.

Official references:

- Target API: https://support.google.com/googleplay/android-developer/answer/11926878
- Android App Bundle: https://developer.android.com/guide/app-bundle
- Play App Signing: https://support.google.com/googleplay/android-developer/answer/9842756
- App-content declarations: https://support.google.com/googleplay/android-developer/answer/9859455
- Personal-account testing requirement: https://support.google.com/googleplay/android-developer/answer/14151465
- Store preview assets: https://support.google.com/googleplay/android-developer/answer/9866151

## Likely blocker candidates

These are candidates, not pre-approved conclusions:

- a player can reach a dead board with moves remaining;
- exception recovery can leave the game locked;
- production release configuration/signing is not yet established;
- Play-ready AAB has not yet been produced;
- store/policy declarations are not yet prepared.

The audit may promote or demote other issues.

## Deliverable

Create:

Docs/Phases/PHASE13_RELEASE_READINESS_AUDIT.md

It should contain:

- current product/release baseline;
- MUST / SHOULD / DEFER / CLOSE table;
- evidence for each classification;
- proposed order of work;
- risks;
- explicit non-goals.

Do not make production changes until the audit is reviewed and approved.

### Learning gate

I should be able to explain the difference between:

- a gameplay bug;
- a release blocker;
- a polish issue;
- a policy/compliance requirement;
- a performance concern;
- a deferred product feature.

---

# 20. Phase 14 — Gameplay Robustness & Adversarial Testing

## Goal

Remove player-facing gameplay states that can make the game impossible to continue or leave it incorrectly locked.

The exact implementation scope comes from the approved Phase 13 audit.

## Expected focus

### Dead-board handling

A stable board is not necessarily a playable board.

Define and test:

- what counts as at least one legal player move;
- when legal-move detection runs;
- what happens when no legal move exists;
- whether the board reshuffles, regenerates, or uses another minimal recovery rule;
- how determinism/restart behavior should work;
- how presentation communicates recovery if necessary.

Do not add a large level-generation framework merely to solve dead boards.

### Exception / lock recovery

Review the documented LockedAfterException path.

The goal is not to hide exceptions.

The goal is to avoid leaving the player in an unrecoverable input-locked state after a recoverable presentation/runtime failure.

Any recovery policy must:

- preserve useful error visibility;
- avoid silently corrupting Board/View state;
- define when restart/rebuild is safer than continuing.

### Adversarial review

Reuse the old adversarial-review idea here.

Ask a reviewer to generate concrete board states and interaction sequences that try to break:

- matching;
- swapping;
- rollback;
- gravity;
- refill;
- cascades;
- specials;
- chain reactions;
- dead-board recovery;
- outcomes;
- restart;
- input locking.

Convert high-value findings into focused tests.

## Validation gate

At minimum:

- all existing EditMode tests;
- all existing PlayMode tests;
- new robustness tests;
- runtime scenarios for every recovery path;
- clean Console except declared negative-test output;
- deterministic restart still behaves as intended.

Do not move to polish while a confirmed MUST-FIX gameplay blocker remains open.

---

# 21. Phase 15 — Mobile Interaction & Presentation Polish

## Goal

Make the small game feel intentional enough to release without turning it into a much larger product.

Use the Phase 13 audit to decide which items are mandatory.

## Candidate work

### Swipe input

The current mobile flow uses two taps to select adjacent tiles.

Before release, add a natural directional swipe path while preserving the existing gameplay semantics.

Desired behavior:

- pointer/touch begins on a tile;
- movement beyond an approved threshold determines the dominant cardinal direction;
- that direction resolves to one orthogonally adjacent GridPosition;
- the existing swap validation/resolution path handles the move;
- busy/terminal input rules remain unchanged.

Architecture rule:

~~~text
touch / pointer gesture
        ↓
swap intent: first position + adjacent second position
        ↓
existing gameplay swap path
~~~

Core does not need to know what a swipe is.

Do not add a generic gesture framework unless a concrete need appears.

Keep two-tap input initially unless device testing shows the two interaction styles conflict or create ambiguity.

Validate thresholds and feel on a physical phone rather than choosing a pixel value only from theory.

### Selection feedback

Touch interaction should make the currently selected tile understandable.

Prefer a small presentation-only cue.

Do not move selection rules into TileView.

### Tall-screen composition

The centered camera framing is technically correct but can leave large empty vertical areas on tall phones.

The rejected vertical-bias experiment remains historical evidence.

Future solutions should prefer composition/art/UI changes such as:

- board backdrop;
- stronger board-area framing;
- taller or richer HUD treatment;
- carefully reviewed side margins.

Do not reintroduce device-specific hacks.

### Outcome dimmer

Consider a full-screen decorative dimmer while keeping the interactive outcome card inside SafeArea.

Background/decorative UI may cover the full screen; interactive UI must remain safe-area aware.

### Colour accessibility

Normal gems are primarily distinguished by colour.

Evaluate a small secondary cue such as:

- subtle internal symbol;
- pattern;
- material/shape detail.

Do not destroy the established Prismatic Energy visual family.

### Audio / haptics

The original roadmap included basic sound/VFX hooks but the project has focused mainly on visuals.

Audit whether a store-ready version should add a small audio/haptic feedback pass for:

- selection/swap;
- match clear;
- special activation;
- win/loss;
- buttons.

Keep it minimal. No audio-manager architecture unless a real need exists.

## Product-scope rule

Do not add:

- economy;
- coins;
- shop;
- ads;
- IAP;
- daily rewards;
- backend;
- login/accounts;
- live-ops;
- dozens of levels.

The target remains one small, coherent game experience.

## Validation

Review on at least:

- the Galaxy S23;
- a short portrait simulation;
- a tall portrait simulation;
- asymmetric safe-area simulation.

Human feel review is required for presentation choices.

---

# 22. Phase 16 — Level System & Game Flow

## Goal

Turn the current single-level gameplay loop into a small, coherent game flow with a beginning, progression, and repeatable level sessions.

This phase should add product/navigation structure without moving level-navigation concerns into Core board logic.

## Main menu

Add a minimal main menu suitable for the first release.

Initial scope may be:

- Play / Continue;
- optional Level Select if it materially improves testing/player flow;
- Settings only if there are real settings to expose.

Do not add empty menu buttons merely to imitate a larger game.

The app should no longer feel like it launches directly into an isolated technical board demo.

## Level definition

Introduce the smallest clear representation for level configuration.

Candidate data:

- level identifier / order;
- starting move count;
- goal type / color;
- goal amount;
- board seed or approved generation configuration.

Prefer data/configuration over hard-coded per-level branches.

Do not create a large level framework until requirements justify it.

## Initial release scope

Target approximately **5–10 intentionally configured levels** for the first release.

The goal is to demonstrate progression and a complete player flow, not content volume.

Do not add dozens of levels merely to make the game appear larger.

## Progression

Define:

- how the current/unlocked level is tracked;
- what happens after Win;
- what happens after Loss;
- how Retry works;
- how Next Level works;
- how returning to the main menu works;
- what happens after the final available level.

A basic expected flow is:

~~~text
Main Menu
   ↓
Play / Continue
   ↓
Level
   ↓
Win ─────→ Next Level
 ↓
optional menu return

Loss ────→ Retry
~~~

If persistence is added, keep it minimal and explain exactly what is stored.

Do not introduce accounts, cloud save, backend, or live-ops.

## Architecture boundary

The Board and match-resolution Core should remain responsible for gameplay rules inside a level.

They should not become responsible for:

- which level number the player is on;
- scene/menu navigation;
- unlock/progression UI;
- store-facing product flow.

Keep the distinction clear:

~~~text
Level configuration
        ↓
start gameplay session
        ↓
Core Board / LevelProgress run the level
        ↓
level outcome
        ↓
game-flow / progression layer decides Retry / Next / Menu
~~~

## Validation

At minimum verify:

- starting each configured level uses the intended moves/goal/configuration;
- Win advances correctly;
- Loss retries correctly;
- Restart still restarts the current level;
- Next Level does not leak previous Board/View/score state;
- returning to menu and re-entering gameplay is clean;
- first and final level edge cases are defined;
- mobile safe-area/layout behavior remains valid;
- deterministic behavior remains understood where seeds are used.

### Learning gate

I should be able to explain:

- the difference between a gameplay session and game-flow/navigation state;
- why LevelDefinition is configuration rather than Board responsibility;
- where progression state lives;
- what would change if the game later had 100 levels;
- what is deliberately not being built yet.

---

# 23. Phase 17 — Performance & Stability Review

## Goal

Measure the release candidate on real hardware before optimizing.

Reuse the old performance-review phase here, but ground it in profiling evidence.

## Profile first

Inspect on Android where practical:

- CPU frame time;
- GPU frame time;
- managed allocations during idle and moves;
- garbage collection;
- memory usage;
- Physics2D.SyncTransforms cost;
- raycast/picking cost;
- tile instantiate/destroy behavior;
- Wisp temporary flyer;
- special chains;
- long cascades;
- UI/layout callbacks;
- thermal behavior over a longer play session.

## Classification

For every finding:

1. real release issue;
2. worthwhile improvement;
3. theoretical / not worth changing.

Do not add pooling because pooling is a common mobile pattern.

Add it only if profiling shows repeated allocation/Instantiate/Destroy is a meaningful problem.

## Stability checks

Include:

- repeated restart cycles;
- long play session;
- background / foreground lifecycle;
- interruption/resume where practical;
- memory growth check;
- crash/logcat review;
- frame pacing around expensive effects.

## Exit criteria

The project should have no known measured performance problem that makes the supported gameplay unpleasant on the chosen baseline device.

Do not claim broad device compatibility from one phone.

---

# 24. Phase 18 — Android Production Build, Versioning & Signing

## Goal

Move from a development APK to a real Google Play upload artifact.

This phase is release engineering, not gameplay work.

## Required decisions

### Target API

Re-check the current Google Play target-API rule.

Working requirement as of 2026-10-28:

- Android mobile new apps / updates: target API 36 or higher.

### Package identity

Keep the approved application identifier unless there is a strong reason to change it before first Play upload:

com.tugaykapucu.match3

Treat package identity as effectively permanent once published.

### Versioning

Define:

- user-visible version name;
- monotonically increasing Android version code.

Document the first-release convention.

### Build format

Produce a signed **Android App Bundle (.aab)** for Google Play.

The earlier APK remains useful for local device testing but is not the final Play publishing artifact.

### Signing

Set up release/upload signing deliberately.

Rules:

- never commit keystore files, private keys, passwords, or secrets;
- document where the upload key is backed up;
- understand the difference between upload key and Play App Signing key;
- verify that a future update can be signed/uploaded correctly.

### Build configuration

Review:

- Development Build off;
- script debugging off;
- IL2CPP;
- ARM64;
- stripping settings;
- managed stripping level;
- target/min API;
- orientation;
- permissions;
- graphics APIs;
- compression/build options;
- app icon / launcher assets;
- version name/code.

Do not change settings just because a "release" preset sounds more optimized.

## Validation

Produce at least:

- one successful signed release AAB;
- one installable release-equivalent build for device smoke testing, where practical;
- build report / size review;
- clean device launch;
- Win/Loss/restart smoke;
- no unexpected logcat errors.

Store the release artifact outside source control unless there is a deliberate artifact policy.

---

# 25. Phase 19 — Google Play Store & Policy Readiness

## Goal

Prepare everything Play Console needs without adding monetization or data collection merely for the sake of publishing.

## App setup

Decide and document:

- final app/game name;
- default language;
- app/game category;
- free/paid status;
- support/contact details.

## Store listing

Prepare compliant:

- 512×512 Play Store app icon;
- 1024×500 feature graphic;
- at least the required screenshots;
- short description;
- full description;
- optional preview video only if worthwhile.

Store assets must represent the actual game.

Do not imply rankings, awards, player counts, or features that do not exist.

## App content / policy declarations

Complete the applicable Play Console declarations, including:

- ads declaration;
- app access;
- target audience and content;
- content rating;
- Data safety;
- privacy policy where required / appropriate;
- permissions declarations if any sensitive/high-risk permission exists.

The correct answers must come from the actual app behavior and dependencies.

Do not guess that "the game collects no data" without checking Unity packages, permissions, SDKs, and built manifest behavior.

## Privacy principle

Prefer a release with no unnecessary analytics/ads/tracking SDKs.

If the game does not need data collection, keeping the data surface small makes both engineering and policy work simpler.

## Policy verification

Google Play policy is time-sensitive.

Re-check official requirements immediately before Play Console submission.

---

# 26. Phase 20 — Play Testing Track & Production Access

## Goal

Test the exact Play-delivered build and satisfy any account-specific production-access requirement.

## Internal testing first

Upload the signed AAB to an internal testing track or use the appropriate Play internal distribution flow.

Verify:

- Play-generated install works;
- correct version is delivered;
- portrait orientation;
- touch;
- safe area;
- specials;
- Win/Loss;
- Restart/Play Again;
- frame pacing;
- no unexpected permissions/prompts;
- update path with a higher version code.

## Closed testing

If the account is subject to Google's new-personal-account production-access rule:

- run the required closed test;
- keep the required number of testers opted in for the required continuous period;
- collect meaningful feedback;
- answer the production-access questions accurately.

As of 2026-10-28, the official requirement states at least 12 testers continuously opted in for the preceding 14 days when applying for production access. Verify the account-specific Play Console requirement before starting the clock.

If the account is not subject to that requirement, still use a testing track before production.

## Play-side automated feedback

Review what Play Console provides, such as:

- pre-launch report;
- device compatibility;
- crashes / ANRs;
- policy warnings;
- bundle/device delivery information.

Classify issues before fixing them.

---

# 27. Phase 21 — Release Candidate & Production Submission

## Goal

Create one release candidate and decide deliberately whether it is acceptable to ship.

## Release-candidate gate

Require:

- approved known-issue list;
- no open MUST-FIX release blockers;
- complete automated regression suites;
- final physical-device smoke;
- Play-delivered test build validated;
- release AAB built with final version code/name;
- store listing complete;
- app-content declarations complete;
- privacy/data-safety answers reviewed;
- crash/ANR/pre-launch findings reviewed.

## Final cross-agent review

Reuse the old Cross-Agent Review here.

One reviewer should inspect the release candidate as a senior Unity/mobile-game engineer.

The other reviewer should challenge each finding instead of blindly accepting it.

Focus on:

- correctness;
- release risk;
- maintainability;
- Unity usage;
- mobile behavior;
- test coverage;
- policy/release configuration.

Human developer makes the final call.

## Production submission

Do not let an agent automatically publish to production.

The human developer should review:

- exact AAB;
- version;
- countries/regions;
- release notes;
- staged/full rollout choice;
- known issues;
- Play Console warnings.

Only then submit.

---

# 28. Phase 22 — Portfolio README & Engineering Story

## Goal

Make the repository understandable to a reviewer who was not present during development.

The README is no longer optional.

Suggested structure:

~~~text
# Agentic Match-3

## Overview
## Gameplay
## Features
## Screenshots / GIF
## Architecture
## Project Structure
## Testing
## Mobile / Android Validation
## Release Engineering
## AI Development Workflow
## Unity MCP
## Engineering Decisions
## Known Trade-offs
## What I Learned
## How to Run
~~~

Be transparent:

> This project was intentionally developed as an experiment in agentic game development. Codex and Claude Code were used for implementation, review, testing, and Unity Editor automation under human-written requirements and supervision.

Document:

- Core vs Presentation separation;
- deterministic gameplay;
- special-effect planning;
- mobile adaptation;
- testing totals;
- physical-device validation;
- release process;
- meaningful rejected designs, not just successful choices.

Do not turn the README into a transcript of the entire project.

---

# 29. Phase 23 — Final Learning Review & Interview Mode

## Goal

Close the learning loop.

The repository is not complete merely because it shipped.

I should be able to explain and modify the important systems without outsourcing every decision.

## Interview mode

Use a senior-engineer interview format.

Ask one question at a time and challenge weak answers.

Focus on:

- OOP;
- Core vs Presentation;
- Unity lifecycle;
- algorithms;
- MatchFinder;
- special creation/activation;
- deterministic RNG;
- state/sequence management;
- input locking;
- touch/picking;
- safe areas/camera fitting;
- allocations/performance;
- Android build/signing;
- release trade-offs;
- testing strategy.

Frequently ask:

"What would happen if the requirement changed?"

## Practical learning gate

I should be able to make at least one meaningful scoped change myself, explain the diff, and validate it without relying entirely on an agent.

---

# 30. Design Patterns to Learn Through the Project

Do not force patterns into the code.

## Observer

Useful when a gameplay fact needs multiple independent consumers.

Question:

> Does an event materially reduce coupling here, or does a direct call remain clearer?

## State

Useful when state-specific behavior becomes complex.

Question:

> Is an enum / explicit status enough, or are separate state objects genuinely easier to reason about?

## Strategy

Useful when behaviors are truly interchangeable and independently extensible.

Do not create a Strategy hierarchy merely because specials have different outcomes.

## Factory

Useful when creation itself becomes complex.

Do not hide simple prefab/view construction behind a factory without need.

## Object Pool

Useful only when measured allocation / Instantiate/Destroy cost justifies it.

## Singleton

Treat cautiously.

Convenient global access trades explicit dependencies for hidden global state.

Do not default to GameManager.Instance / AudioManager.Instance / UIManager.Instance style architecture.

---

# 31. Scope Boundaries

The release goal does **not** mean "turn this into a live-service Match-3."

Do not add these unless a later explicit product decision changes the project:

- ECS / DOTS;
- multiplayer;
- networking;
- backend;
- accounts/login;
- ads;
- IAP;
- economy/shop;
- daily rewards;
- live-ops;
- analytics SDKs without a concrete need;
- push notifications;
- complex shaders;
- custom render pipeline;
- procedural art pipeline;
- heavy dependency-injection framework;
- huge generic architecture framework;
- dozens of levels merely to make the game look larger; the approved first-release target is a small 5–10 level set.

Prefer one small, coherent, finished experience over a wide unfinished feature set.

---

# 32. Release Definition of Done

The project is release-ready when all approved MUST-FIX items are closed and the following are true:

## Gameplay

- [ ] Board generates reliably
- [ ] Initial board has no unwanted matches
- [ ] At least one legal move is guaranteed or dead-board recovery is defined
- [ ] Adjacent swaps work
- [ ] Invalid swaps rollback
- [ ] Match detection works
- [ ] Clear / gravity / refill / cascades work
- [ ] Input locking works
- [ ] Moves / score / goals work
- [ ] Win / lose / restart work
- [ ] Special tiles and chains work
- [ ] No known player-stuck state remains accepted accidentally

## Mobile / UX

- [ ] Touch-only gameplay works
- [ ] Directional swipe swapping works through the approved existing swap path
- [ ] Portrait safe-area layout works
- [ ] Supported aspect ratios remain playable
- [ ] Important touch targets are usable
- [ ] Approved presentation/accessibility blockers are closed
- [ ] Outcome/restart flow is understandable
- [ ] Human device-feel review completed

## Game flow

- [ ] Main menu / game entry flow is complete
- [ ] Initial release level set is configured and reviewable
- [ ] Win → Next Level flow works
- [ ] Loss → Retry flow works
- [ ] Restart restarts the current level
- [ ] Progression state does not leak gameplay state between levels
- [ ] Final available level behavior is defined

## Quality

- [ ] Important deterministic logic has tests
- [ ] Adversarial gameplay review completed
- [ ] Performance/stability review completed
- [ ] No known release-blocking crash/ANR issue
- [ ] Unity Console / Android logs have no unexplained release errors
- [ ] Known issues are explicitly accepted, fixed, or deferred

## Android release

- [ ] Current Google Play target API requirement satisfied
- [ ] Final package identifier approved
- [ ] Version name/code policy defined
- [ ] Signed release AAB builds successfully
- [ ] Upload/signing keys are safely managed outside Git
- [ ] Play-delivered test installation succeeds
- [ ] Physical-device release smoke passes

## Google Play

- [ ] Store listing complete
- [ ] Required icon / feature graphic / screenshots complete
- [ ] App-content declarations complete
- [ ] Data safety / privacy answers match actual app behavior
- [ ] Content rating / target audience completed
- [ ] Account-specific testing requirement satisfied
- [ ] Pre-launch / Play Console findings reviewed
- [ ] Production-access requirements satisfied
- [ ] Human approves the exact production submission

## Repository / learning

- [ ] README explains architecture
- [ ] README discloses AI-assisted workflow
- [ ] README includes testing/mobile/release evidence
- [ ] I can explain every major class
- [ ] I can explain important design choices and rejected alternatives
- [ ] I can explain MatchFinder without AI
- [ ] I can modify and validate the project without relying entirely on AI

---

# 33. README Content for the Final Repository

Use Phase 22 as the source of truth.

Keep the README concise enough that a hiring reviewer can understand the project quickly.

The repository should clearly communicate:

- what the game is;
- what is technically interesting;
- how it is architected;
- how it was tested;
- what was validated on Android;
- how AI agents were supervised;
- what trade-offs remain.

---

# 34. Parallel Dream Games Preparation

This repository still serves a different purpose from a future mostly-human-implemented interview project.

Treat this repository as:

~~~text
Agentic Match-3
AI implementation: HIGH
Human specification/review: HIGH
Purpose:
- learn Unity and Match-3 systems
- learn agent supervision
- learn testing/debugging
- learn mobile engineering
- learn release engineering
- ship a real small product
~~~

Later, a separate Dream-style project may be used to demonstrate higher personal implementation ownership.

Knowledge should flow from this project into the next one.

Code should not simply be copied without understanding.

---

# 35. Context and Token Budget Guidelines

AI context is a limited project resource.

Keep always-on instructions small:

- AGENTS.md → short shared engineering/workflow rules
- CLAUDE.md → short Claude-specific adapter
- PLAN.md → detailed on-demand roadmap

Load only what a task needs.

Prefer repository state over conversation-history summaries for fresh agent sessions.

Do not ask Codex and Claude to independently implement the same feature unless comparison is the explicit goal.

---

# 36. Recommended Working Loop

For every meaningful feature or release task:

1. write/approve the requirement;
2. inspect current repository state;
3. read only the relevant PLAN.md phase;
4. ask for design first when architecture/release behavior is non-trivial;
5. understand and approve the design;
6. one agent implements;
7. run focused tests/validation;
8. inspect the diff;
9. second agent reviews/challenges;
10. human chooses valid feedback;
11. implement only approved fixes;
12. rerun validation;
13. human explains the change in their own words;
14. commit/push only after review.

This loop is more important than the exact architecture.

---

# 37. Current Next Action

Phases 0-12 are complete.

The next phase is:

**Phase 13 — Release Readiness & Product Gap Audit**

Do not start by fixing DeadBoard, UI polish, signing, or Play Console configuration.

First create the Phase 13 audit, classify the remaining work, review it, and then approve the release sequence.

---

## Final Principle

The original question was:

> Can I use AI agents to build a real Unity system while becoming capable of understanding, reviewing, debugging, and extending the important parts myself?

After Phase 12, the project adds a second question:

> Can I take that system through the boring but essential work required to ship and maintain a real small mobile game responsibly?

The project succeeds if both answers become yes.
