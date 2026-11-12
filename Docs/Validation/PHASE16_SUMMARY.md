# Phase 16 — Level system and game flow: summary

Completed 2026-11-12. Plan and gate: `Docs/Phases/PHASE16_EXECUTION.md`.

This file replaces the ten Phase 16 step reports (Steps 6–15) and the interim
validation record. They are still in git history. Read one with, for example:

```text
git show b4363fc:Docs/Validation/PHASE16_STEP11_CONFIRMATIONS.md
```

Old names: `PHASE16_STEP6_LEVEL_CATALOG`, `STEP7_PROGRESSION_PERSISTENCE`,
`STEP8_GAME_FLOW`, `STEP9_MAIN_MENU`, `STEP10_OUTCOME_FLOW`, `STEP11_CONFIRMATIONS`,
`STEP12_ANDROID_BACK`, `STEP13_SESSION_ISOLATION`, `STEP14_NAVIGATION_ADVERSARIAL`,
`STEP15_S23_GAME_FLOW`, `PHASE16_VALIDATION_RECORD`. They also hold:

- step-by-step manual Unity setup;
- Editor layout measurements;
- the Step 6 winning move routes for each level;
- APK hashes.

## What was built

| Piece | Responsibility | Main commits |
| --- | --- | --- |
| `LevelDefinition`, `LevelCatalog` | Six levels as data; the list index is identity and order | `43ec9b1`, `fbda6bf` |
| `BoardBootstrap.StartLevel` | Builds one fresh, deterministic session; validates before swapping | `43ec9b1`, `2923396` |
| `ProgressionStore` | One saved integer, `0..levelCount`, never lowered | `59e1b1c` |
| `GameFlowController` | Modes, navigation, saving on Win, Back; refuses everything while busy | `b3a64df`, `ac300e5`, `ca227bd` |
| `MainMenuView` | Opaque full-screen menu with a single Play / Continue / Play Again button | `b9e1d42` |
| `LevelHudView` changes | Outcome titles, outcome Menu, gameplay Menu, three availability inputs | `f984553`, `e8e354c`, `c052080`, `f60de56` |
| `ConfirmationView` | Restart / Leave prompt; the view only forwards callbacks | `5b13625`, `ac300e5` |
| `BottomControls` row | Restart and Menu share one row; camera framing unchanged | `0d0c04a`, `c95861c` |

Core did not change in Phase 16. There is one scene (`SampleScene`), with no
`SceneManager` or `DontDestroyOnLoad`.

## Final behavior

**Levels** (`Assets/Levels/InitialLevelCatalog.asset`, global 8×8 board, six colours):

| Level | Goal | Moves | Seed | Human play |
| ---: | --- | ---: | ---: | --- |
| 1 | Red 25 | 20 | 42 | Easy |
| 2 | Green 22 | 22 | 17 | Easy |
| 3 | Blue 25 | 24 (was 20) | 73 | Medium–hard at 20, so raised |
| 4 | Yellow 24 | 26 (was 22) | 111 | Medium–hard at 22, so raised |
| 5 | Purple 28 | 24 (was 20) | 202 | Raised the same way; played, good |
| 6 | Orange 30 | 26 (was 22) | 314 | Raised the same way; played, good |

Level 1 is the pre-Phase 16 level, unchanged. These are individual human
impressions, not a measured difficulty curve.

**Progression and saving:**

- `Match3.Progression.v1` in PlayerPrefs holds one integer, `0..6`.
- On an accepted Win at zero-based index *i*, it saves `max(saved, i + 1)`. Nothing
  else saves.
- The menu shows **Play** (0), **Continue** (1–5) or **Play Again** (6, which starts
  Level 1 and keeps 6).
- Missing → 0. Out-of-range stored integers are clamped on load, without writing.
- The session (Board, RNG, moves, score, selection) is never saved. A relaunch
  shows the menu, and Play/Continue builds a fresh level from the saved index.

**Outcomes:**

| Outcome | Title | Buttons |
| --- | --- | --- |
| Loss | Out of Moves | Retry, Menu |
| Non-final Win | Level Complete! | Next Level, Menu |
| Final Win | All Levels Complete | Play Again, Menu |

**Confirmations.** In-game Restart and Menu open "Restart level?" or "Leave level?"
with Cancel / confirm.

- **Cancel** keeps the session and the selected tile.
- **Confirm Restart** starts the same level once.
- **Confirm Leave** shows the menu.
- **During a move:** both requests are refused.
- **Outcome buttons** stay immediate.

**Android Back:**

| State | Back |
| --- | --- |
| Main Menu | Backgrounds the app (`moveTaskToBack`) |
| Settled play | Opens Leave |
| Move resolving, even with the outcome visible | Ignored, not queued |
| Either prompt | Cancels |
| Settled outcome | Main Menu |

## Key decisions (the "why")

- **Session vs flow.** Bootstrap builds a level; Flow decides which level. The
  outcome callback runs inside the move lock, so it records and displays only.
  Navigation waits for the lock to release.
- **One input writer.** Only `BoardBootstrap` sets `inputController.enabled`, as
  `inputAvailable && Status == Playing`. A finished level can't be re-enabled.
- **Overlays, not deactivation.** Menus and prompts hide only themselves.
  Deactivating the board would kill the move coroutine and leave input locked.
- **Refusals return `false`; programmer errors throw.**
- **No Level Select, no pause, no dialog framework.** Each was rejected as not
  needed yet.
- **Back flag set once.** The first prototype toggled `backButtonLeavesApp` on menu
  entry. On the S23, Back on a Win screen then backgrounded the app. The final
  version sets it to `false` once and backgrounds explicitly from the menu only.
- **Confirmation layering.** The full-screen dimmer is the last Canvas child, and
  the card sits in SafeArea with its own Canvas (Override Sorting 1 + Graphic
  Raycaster). One setup slip, a missing Override Sorting, let the dimmer swallow
  the button touches; the tests caught it.

## Evidence

**Automated tests.** EditMode went from 603 to **632**, and PlayMode from 233 to
**347** (344 passed, 3 skipped as `[Explicit]` frame-rate tests).

| Test classes / test-name prefixes | What they prove |
| --- | --- |
| `LevelCatalogTests`, `CatalogLevel_` | Catalog shape; each level starts stable and playable, with Level 1 matching the real scene settings |
| `ProgressionStoreTests` | Missing key, clamping, never-lower, final sentinel; each test uses its own preference key |
| `GameFlowControllerTests`, `GameFlow_`, `Outcome_`, `FlowGate_` | Modes, saves only on Win, no navigation while locked, same-update UI-tap guard |
| `MainMenuViewTests`, `ConfirmationViewTests`, `Confirmation_` | Views, intents, Cancel keeps selection |
| `GameFlowBackTests` (`Back_`, `Escape_`) | Back in every state, held Back, Back during resolution |
| `SessionIsolation_` | Next, Menu→Continue and Restart replace Board, progress, views and both RNGs; Restart replays exactly |
| `NavigationAdversarial_` | Every busy frame (rollback, cascade, special chain, seed-93 recovery) refuses all navigation; same-frame races; repeated clicks; focus loss |

**Final full run** at `c95861c`:

- EditMode 632/632.
- PlayMode 344 passed, 0 failed, 3 skipped.
- Console: only the declared camera-fitting warning plus runner/tool lines.
- SampleScene clean, Play Mode stopped.

**S23 (Galaxy S23, Android 17, one device; human fingers, with screenshots via ADB):**

- **Passed:**
  - cold launch → menu → first tap;
  - swipe;
  - Restart → Cancel and Restart → Confirm;
  - Leave → Continue;
  - Win → Next and Loss → Retry;
  - Back in every state;
  - Home/resume with a prompt open and during a move;
  - safe-area layout on every screen.
- **Not observed:** stale selection, duplicate session, frozen input, or leaked
  score.
- **APK version:** the tested APK predates the move-budget changes; Levels 5–6 were
  played afterwards.
- **Not replayed on the phone:** a six-level final completion (covered by Editor
  tests).

**Gate (Step 19):** all items are met, including the developer's own playthroughs,
learning review and final diff review.

## Deferred and known limits

| Item | Status | Where |
| --- | --- | --- |
| Input locked if a move throws | Accepted risk; re-reviewed for saving and navigation | `KnownIssues/LockedAfterException.md` |
| Outcome dimmer leaves strips outside the safe area | Cosmetic; the confirmation dimmer shows the fix | `KnownIssues/OutcomeDimSafeArea.md` |
| Tall-screen composition | UI polish | `KnownIssues/TallScreenComposition.md` |
| `AssetPackManager` startup exception | Investigation item; triage in Phase 17, close in Phase 20 | Phase 13 audit, Finding 6 |
| Double-tap Cancel reaching the board | Theoretical; S23 result inconclusive | `KnownIssues/RestartConfirmation.md` |

Resolved: Restart confirmation. Phase 17 has not started.
