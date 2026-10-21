# Phase 11 Execution Plan — Polish / Prismatic Energy

Phase 11 improves visual readability, feedback, and presentation without changing gameplay rules or deterministic logical behavior.

The current Phase 10 gameplay system is complete and validated. Phase 11 builds on that stable foundation rather than redesigning it.

The project remains primarily an engineering / gameplay-programming / agentic-development portfolio piece. Art production should stay focused and modest: enough to make the game intentional, readable, and presentable in screenshots/video, without turning the phase into a large art-production effort.

The approved visual direction is:

**Prismatic Energy**

Player-facing concepts:

- HorizontalRocket / VerticalRocket → Beam Crystal
- Propeller → Wisp
- TNT → Nova Gem
- LightBall → Spectrum Core

These names belong to presentation/design vocabulary. Existing Core names remain unchanged.

The major boundary for this phase is:

```text
Core
→ gameplay facts / Board / effect plans

BoardResolutionRunner
→ sequencing

BoardView / TileView / focused presentation helpers
→ visual representation and animation

LevelHudView
→ HUD presentation only
```

Core remains authoritative. Presentation visualizes already-decided facts.

Do not introduce a generic animation framework unless a concrete requirement proves one is necessary.

---

# Current Presentation Baseline

As Phase 11 begins:

- `TileView` uses the tile root `SpriteRenderer`.
- Normal tiles use the same white sprite tinted by `TileView`.
- Specials use a TMP child with temporary markers:
  - `<->`
  - `P`
  - `TNT`
  - `LB`
- Vertical Rocket rotates only that TMP label.
- `BoardView.AnimateSwap` uses linear `Vector3.Lerp`.
- `BoardView.MoveTogether` drives fall/refill movement with linear interpolation.
- `SpecialActivationAnimation` provides a short gold pulse.
- `BoardResolutionRunner` receives `SpecialEffectPlan` before special removals.
- `SpecialEffect` exposes:
  - `Origin`
  - `Type`
  - `Footprint`
  - `RemoteTarget`
- `SpecialEffectPlan.Effects` preserves activation order.
- `LevelHudView` already has functional moves, score, goal, status and Restart behavior.

Phase 11 polishes these systems. It does not rebuild their gameplay logic.

---

# Visual Identity Contract

## Normal tiles

Normal collectible tiles should:

- share one gemstone family / outer silhouette;
- remain primarily distinguished by color;
- optionally use subtle inner marks as a secondary readability/accessibility cue;
- remain visually simpler than specials.

Normal colors remain:

- Red
- Green
- Blue
- Yellow
- Purple
- Orange

Do not give every normal color a completely different outer silhouette unless actual board-size testing proves that necessary.

## Special tiles

Specials should:

- use silhouettes clearly different from normal tiles and from each other;
- remain recognizable in grayscale;
- remain readable at approximately 32–64 px;
- communicate function through shape;
- share one outline/material/lighting language.

Working concepts:

### Beam Crystal

- elongated, double-ended crystal;
- horizontal and vertical presentation;
- silhouette suggests a line clear.

### Wisp

- compact luminous core;
- strong simple silhouette;
- may use a small cross/plus cue for the local effect;
- supports a clear flight animation toward `RemoteTarget`.

### Nova Gem

- central gem with square/corner framing;
- visual language should suggest an area/square effect rather than a circular explosion.

### Spectrum Core

- circular segmented core;
- few bold segments rather than tiny detail;
- should support highlighting one selected color.

Resting special colors should not be easily confused with normal Blue/Purple/other collectible colors.

Spectrum Core is the intentional multicolor exception.

Do not copy Royal Match sprites, silhouettes, UI, colors, effects, animation timing or other art assets.

The gameplay studies established Match-3 conventions, but the final visual presentation should be original.

---

# External Art Input / Provenance

Production-oriented Prismatic Energy assets are prepared outside the repository before implementation.

Expected runtime images:

## Normal

- Red
- Green
- Blue
- Yellow
- Purple
- Orange

## Specials

- Beam Crystal horizontal
- Beam Crystal vertical
- Wisp
- Nova Gem
- Spectrum Core

The source set is custom AI-assisted artwork created specifically for this project from an original visual direction. It is not copied Royal Match artwork.

Recommended repository destinations after Step 1 is approved:

```text
Docs/Art/Concepts/
    Phase11-Prismatic-Energy-Concept.png

Assets/Art/Tiles/PrismaticEnergy/Normal/
    Red.png
    Green.png
    Blue.png
    Yellow.png
    Purple.png
    Orange.png

Assets/Art/Tiles/PrismaticEnergy/Specials/
    BeamCrystal_H.png
    BeamCrystal_V.png
    Wisp.png
    NovaGem.png
    SpectrumCore.png
```

Keep large concept/reference sheets under `Docs/Art` instead of `Assets` so Unity does not import reference artwork unnecessarily.

When art is imported, add a small provenance note under `Docs/Art`.

Production PNGs are expected to use transparent 512×512 canvases.

Initial Unity import contract:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 512
Alpha: enabled
Mip Maps: off
Filter Mode: Bilinear initially
Compression: None during visual validation
```

No new graphics package should be added.

These are starting settings, not untouchable requirements. Change them only if actual board-size testing gives a concrete reason.

---

# Step 1 — Asset Intake and Import Validation

## Goal

Bring the approved art into Unity without changing gameplay or `TileView` behavior yet.

## Scope

- Add production PNGs to the approved folders.
- Add concept/reference artwork to `Docs/Art`.
- Add provenance documentation.
- Apply explicit import settings.
- Confirm transparency, dimensions, sprite bounds and consistent scale.

Do not wire the sprites into `TileView` yet.

## Validate

- All runtime PNGs import as sprites.
- Transparency is correct.
- No unexpected background pixels.
- No import warnings.
- No new package.
- Compilation remains clean.
- Existing tests remain unchanged/passing.

## Unity learning requirement

Before every Unity Editor / MCP action, explain:

1. what will be changed;
2. why;
3. exact manual Unity Editor steps;
4. how to verify it.

After the action, report exactly what changed or was observed.

## Commit

Suggested:

```text
Import Prismatic Energy tile art
```

STOP for approval.

---

# Step 2 — Tile Visual Integration

## Goal

Replace the temporary Phase 10 tile presentation with production Prismatic Energy art.

`TileView` should evolve rather than being replaced with a new gameplay component.

## Normal tiles

Map the six normal `TileType` values to their production gemstone sprites:

```text
Red
Green
Blue
Yellow
Purple
Orange
```

## Specials

Presentation mapping:

```text
HorizontalRocket → Beam Crystal horizontal
VerticalRocket   → Beam Crystal vertical
Propeller        → Wisp
TNT              → Nova Gem
LightBall        → Spectrum Core
```

Keep existing Core identifiers unchanged.

## Implementation guidance

Prefer the simplest implementation.

Investigate whether the existing root `SpriteRenderer` can display the final sprite directly. If so, prefer that over unnecessary child renderers.

Both Beam Crystal orientations already exist as production sprites, so do not rotate the whole tile GameObject merely to represent orientation.

The root transform and click collider must remain unchanged.

Remove the temporary TMP special label only after sprite rendering works and is tested.

Do not create a `ScriptableObject` visual database unless serialized duplication creates a concrete need.

A small explicit Presentation mapping is acceptable.

## Tint transition

Current presentation:

```text
white sprite + TileView color tint
```

New presentation:

```text
pre-colored production sprite + neutral Color.white resting tint
```

Therefore:

- normal tile tests verify the mapped `Sprite`, not `renderer.color == the TileType color`;
- special sprites also rest at `Color.white`;
- the existing activation pulse may still temporarily animate `renderer.color` and must restore it afterward.

## Tests

Cover:

- every `TileType` maps to the expected sprite;
- specials no longer depend on TMP labels;
- `Empty` remains hidden;
- invalid `TileType` fails before corrupting view state;
- falling/swapping preserves `TileView` identity;
- both Beam Crystal orientations render correctly;
- prefab has no missing references/scripts.

Run relevant EditMode and PlayMode regressions.

Perform a real SampleScene board check only after the Unity learning explanation.

## Commit

Suggested:

```text
Show Prismatic Energy tile art
```

STOP for approval.

---

# Step 3 — Board Readability Review

## Goal

Validate the art as gameplay UI, not merely as individual icons.

This is primarily a runtime-validation step.

## Check

- all six normal colors together;
- all four special families;
- both Beam Crystal orientations;
- small / medium / normal board presentation sizes;
- normal tiles remain easy to scan by color;
- special silhouettes remain distinct;
- Wisp remains readable at actual board size;
- Spectrum Core remains readable at actual board size;
- Nova Gem communicates an area/square effect;
- Spectrum Core is not mistaken for a normal collectible;
- Blue/Purple are not confused with resting specials;
- an 8×8 board remains visually readable.

### Minification check

The initial import contract keeps `Mip Maps: off`. Do not enable mip maps preemptively.

- inspect sprite edges while swapping and falling at actual board size;
- specifically watch for shimmer/aliasing caused by minifying 512 px sources;
- enable mip maps only if this runtime check gives a concrete reason, and report it.

Use temporary runtime layouts if necessary.

Restore the original scene/configuration afterward.

Tiny scale/import adjustments are allowed here.

If the artwork itself proves unreadable, STOP and report before inventing replacement assets.

No commit if nothing changes.

STOP for approval.

---

# Step 4 — Swap Easing

## Goal

Make swaps feel less mechanical without changing logical behavior.

Current implementation:

```text
BoardView.AnimateSwap
→ linear Vector3.Lerp
```

Replace linear progress with a small presentation-only easing function.

## Prefer

- a small mathematical easing function;
- no `AnimationCurve` asset unless needed;
- no tweening package;
- no generic animation framework.

## Preserve

- Board is already logically swapped before visuals catch up.
- View lookup changes only after movement completes.
- Invalid rollback uses the same animation path.
- Zero duration snaps immediately.
- Final positions remain exact.
- Input stays locked throughout.

## Tests

Verify:

- exact endpoints;
- view identity;
- zero-duration behavior;
- logical Board state unaffected;
- input remains locked during movement.

## Commit

Suggested:

```text
Ease tile swap movement
```

STOP for approval.

---

# Step 5 — Fall and Refill Easing

## Goal

Polish gravity and refill movement while preserving the existing movement model.

Current implementation:

```text
BoardView.MoveTogether
→ linear progress
```

It is shared by fall and refill.

Use a small presentation-only easing curve.

A fall/refill curve may differ slightly from swap if runtime testing shows a clear benefit.

## Preserve

- Core `TileMove` facts;
- Core `TileSpawn` facts;
- surviving `TileView` identity;
- lookup changes only after landing;
- deterministic logical Board behavior;
- exact final positions;
- zero-duration behavior.

Do not add per-tile gameplay timing.

Do not add bounce/overshoot unless real runtime review proves it improves readability.

## Commit

Suggested:

```text
Ease falling and refill movement
```

STOP for approval.

---

# Step 6 — Normal Clear Feedback

## Goal

Give ordinary matched clears a short readable visual response.

Current behavior:

```text
logical clear
→ view removed immediately
```

Add a small visual animation before consumed views are destroyed.

Possible techniques:

- scale down;
- fade;
- subtle flash.

Keep the effect short.

## Sequencing rules

Matching, scoring and removal decisions remain in Core.

Presentation receives already-decided positions.

Visual alpha/scale is never authoritative.

Consumed views still disappear.

Special creation anchors must remain correct.

Unrelated views must remain unchanged.

Required order, preserving the current Core mutation order:

```text
ClearCells(Board)
→ PlaceSpecials(Board)
→ AnimateClearViews
→ RemoveViews
→ PlaceSpecialViews
→ gravity / refill
```

The clear animation delays visual destruction only. Do not move or redesign the existing logical special-placement rule.

The consumed views may temporarily represent the previous visual state while the Board has already advanced; input remains locked during that controlled visual catch-up.

Prefer a focused helper if clear animation becomes a distinct responsibility.

Do not inflate `BoardView` with unrelated implementation detail.

## Tests

Verify:

- matched views animate and then disappear;
- Board facts are unaffected by animation;
- special creation still occurs at the correct anchor;
- cascade ordering remains unchanged;
- input remains locked;
- final Board/View consistency is preserved.

## Commit

Suggested:

```text
Animate normal tile clears
```

STOP for approval.

---

# Step 7a — Beam Crystal and Nova Gem Effect Feedback

## Goal

Visualize special-effect footprints without changing which cells are affected.

Use `SpecialEffectPlan` / `SpecialEffect` facts supplied by Core.

## Beam Crystal

Visualize the actual row/column effect.

Horizontal and vertical feedback must originate from the special's actual post-swap position.

Do not independently recompute gameplay geometry.

Use `SpecialEffect.Footprint`.

## Nova Gem

Visualize the actual bounds-clipped area supplied by Core.

The effect should communicate the square 5×5-style footprint.

Edge and corner activations should visibly clip according to Core's footprint.

Do not duplicate TNT footprint formulas in Presentation.

## Existing pulse

The Phase 10 gold pulse may be:

- retained;
- incorporated;
- replaced;

depending on whether the new feedback makes it redundant.

Do not stack visual effects merely because both already exist.

## Architecture

No generic effect hierarchy unless concrete duplication justifies one.

## Commit

Suggested:

```text
Show Beam Crystal and Nova Gem effects
```

STOP for approval.

---

# Step 7b — Wisp and Spectrum Core Effect Feedback

## Goal

Visualize the two targeted / data-dependent specials.

## Wisp

Show:

```text
Origin
→ local activation
→ flight
→ RemoteTarget
```

`RemoteTarget` is already chosen by Core.

Presentation must never:

- reroll;
- choose;
- rank;
- substitute;
- modify;

the target.

If `RemoteTarget` is null, do not invent one.

The flight may use a temporary Presentation-only GameObject.

That temporary object is not a Board tile and never modifies logical state.

## Spectrum Core

Communicate the color being cleared and the relationship between Spectrum Core and the affected normal cells.

Presentation must not reimplement Light Ball target selection.

Audit whether `SpecialEffectPlan` exposes enough immutable facts for the effect.

If Presentation would otherwise need to run gameplay target-selection logic again, prefer the smallest Core data-contract extension that exposes the already-decided result as passive data.

Such a contract extension may expose existing gameplay facts but must not change gameplay decisions.

## Chain order

Respect `SpecialEffectPlan.Effects` ordering wherever visual activation sequence depends on it.

Visual effects must never alter chain scheduling.

## Commit

Suggested:

```text
Show Wisp and Spectrum Core effects
```

STOP for approval.

---

# Step 8 — HUD and Outcome Presentation Polish

## Goal

Polish existing HUD presentation without rebuilding level logic.

Current `LevelHudView` already owns presentation for:

- Moves;
- Score;
- Goal;
- Status;
- Restart.

`LevelProgress` remains authoritative.

## Improve

- hierarchy;
- spacing;
- alignment;
- goal readability;
- Restart appearance;
- simple Win panel;
- simple Lose panel.

Win/Lose may use dedicated panel GameObjects if that is cleaner than the current status text.

Outcome still comes only from `LevelProgress`.

Restart must remain unavailable while resolution is busy.

## Do not add

- scoring changes;
- currency/economy;
- level selection;
- save/load;
- mobile safe-area work;
- UI framework.

Mobile layout belongs to the later mobile phase.

## Commit

Suggested:

```text
Polish gameplay HUD
```

STOP for approval.

---

# Step 9 — Runtime Sequence Review

No feature work unless a real defect is found.

Use SampleScene and the real pointer/collider input path.

## Validate

- normal-art readability;
- each special's creation;
- each special's feedback;
- chains;
- cascades;
- swap easing;
- invalid rollback;
- falls;
- refill;
- clear feedback;
- exactly one move per accepted action;
- score;
- goal;
- Win/Lose;
- Restart busy gate;
- Board/View consistency;
- restart replay determinism;
- no Presentation effect chooses gameplay targets.

Restore all temporary scene/configuration changes afterward.

No empty commit.

STOP for approval.

---

# Step 10 — Phase 11 Validation Pass

Run:

```text
all EditMode tests
all PlayMode tests
Unity compilation
Console inspection
final SampleScene smoke test
Git clean-state check
```

## Confirm architecture

### Core

- remains Unity-independent;
- contains no art/polish rules.

### Presentation

- displays facts supplied by Core;
- contains no matching, targeting or scoring decisions.

### BoardResolutionRunner

- still owns sequencing only.

### LevelProgress

- retains the same gameplay responsibility.

## Explicitly confirm absent

Unless a real need appears:

- tweening package;
- generic animation framework;
- Strategy hierarchy for visual effects;
- special gameplay `MonoBehaviour`s;
- Factory singleton;
- event bus;
- pooling;
- mobile gesture rewrite;
- explicit special-combination bonuses.

## Known issues review

Review but do not automatically fix:

- DeadBoard;
- LockedAfterException;
- MobileLayoutAndPicking;
- SelectionFeedback;
- MissingReadme;
- WatchList.

Update `WatchList` only if Phase 11 materially changes `BoardView` / helper responsibility.

## Final report

Report:

- final architecture;
- final visual identity;
- test totals;
- runtime result;
- intentionally deferred work;
- remaining mobile/release work.

No empty commit.

STOP.

Do not start Phase 12.

---

# Cross-Step Rules

Every implementation step must:

1. inspect the exact current files first;
2. explain the intended design before editing;
3. preserve Core / Presentation boundaries;
4. add or update focused tests where behavior is testable;
5. run relevant regression tests;
6. check compilation and Console;
7. inspect the diff;
8. commit only the approved step;
9. STOP for approval before the next step.

For every Unity Editor / MCP action, apply the learning rule before the action.

Before the action explain:

1. what will be done;
2. why;
3. exact manual Unity Editor steps;
4. how to verify.

After the action report exactly what changed or was observed.

Do not silently manipulate:

- prefabs;
- scenes;
- sprite import settings;
- Inspector references;
- Play Mode state.

The human developer must be able to reproduce every Unity-side change manually.

---

# Learning Review

At the end of Phase 11 I should be able to explain:

1. why visual identity belongs in Presentation;
2. why Core names did not need to change;
3. Unity sprite import settings and Pixels Per Unit;
4. why sprite orientation should not rotate gameplay/collider state;
5. logical state vs animation state;
6. easing vs linear interpolation;
7. why `TileView` identity matters during fall movement;
8. how clear feedback fits the resolution sequence;
9. how special footprints are visualized without duplicating gameplay rules;
10. how Wisp uses `RemoteTarget` without choosing it;
11. how Spectrum Core visualizes Core decisions without reimplementing targeting;
12. why focused animation helpers fit better than a generic animation framework here;
13. why no tweening package was added;
14. how HUD presentation remains separate from `LevelProgress`;
15. which polish concerns remain deferred to mobile/release phases.
