# Phase 16 — Validation record

Date: 2026-11-12. Recorded at Step 17, with full-regression baseline `1161850`
and the later Level 5–6 catalog adjustment in `fec0d76`. This is the end-of-phase summary.
The step records listed under "Sources" hold the details.

**Result:** the level system and game flow are implemented; all full suites passed
at `1161850`. Validation of the subsequent Level 5–6 content adjustment is recorded
separately below. The S23 smoke test passed on the earlier build. The phase gate is
**not yet closed**: Levels 5–6 still need normal human playthroughs (see "Still open").

## Final level count

Six levels, defined in `Assets/Levels/InitialLevelCatalog.asset`. The list index is
the level's order and identity. All levels use the global 8×8 board and six-colour
palette.

| Level | Goal | Moves | Seed | Human result so far |
| ---: | --- | ---: | ---: | --- |
| 1 | Red 25 | 20 | 42 | Won on S23; easy |
| 2 | Green 22 | 22 | 17 | Won on S23; easy |
| 3 | Blue 25 | 24 (was 20) | 73 | Won at 20 moves; medium–hard; 24 not yet played |
| 4 | Yellow 24 | 26 (was 22) | 111 | Won at 22 moves; medium–hard; 26 not yet played |
| 5 | Purple 28 | 24 (was 20) | 202 | Not played; easier move budget awaiting human review |
| 6 | Orange 30 | 26 (was 22) | 314 | Not played; easier move budget awaiting human review |

Level 1 keeps the pre-Phase 16 configuration (seed 42, 20 moves, Red 25). The Level
3–4 budgets were raised after human feedback, in `1161850`.

The user subsequently requested the same easier direction for Levels 5–6. Each
gets four extra moves, preserving its goal colour, target, seed, list position and
starting board. Levels 1–4 are unchanged by this follow-up. The extra moves provide
more room for ordinary play; they do not establish a difficulty curve or prove
that the levels feel easy. There is no adaptive difficulty or gameplay-rule change.

## Progression rule

- Win on the level at zero-based index *i* → the saved progression becomes
  `max(savedProgression, i + 1)`, so winning an earlier level (for example, during
  a replay) never lowers it. A non-final Win offers **Next Level** and **Menu**.
- Loss → **Retry** (the same level, fresh from its deterministic start) and **Menu**.
- Restart and in-game Menu ask for confirmation first (see `RestartConfirmation.md`).
- Main Menu label: **Play** (progression 0), **Continue** (1–5, starts that level),
  **Play Again** (6, starts Level 1).
- There is no Level Select.

## Persistence rule

- One integer, `Match3.Progression.v1` in PlayerPrefs, range `0 .. 6`.
- Saved only when game flow accepts a `Won` outcome. It never decreases.
- A fresh install (missing key) reads 0. Stored integers below 0 are clamped to 0
  and those above 6 to 6, without writing; `ProgressionStoreTests` covers these
  numeric cases. Other storage failures are not covered.
- The active session (Board, RNG, score, moves, selection) is never saved. A
  relaunch during a level opens the Main Menu, and Play/Continue starts a fresh
  session from the saved progression, which is not necessarily the interrupted
  level (a replay after full completion restarts at Level 1).
- Replay after full completion is not persisted: Play Again always starts Level 1,
  and the stored 6 is kept.

## Android Back

| State | Back does |
| --- | --- |
| Main Menu | Moves the app to the background; no quit prompt |
| Settled gameplay | Opens the Leave confirmation |
| Move resolving, including a visible but still-locked outcome | Ignored, not queued |
| Restart or Leave confirmation | Cancels; the session and selection are kept |
| Settled Won / Lost, including final Win | Returns to the Main Menu |

Implementation: one code-created `InputAction` on `GameFlowController`, plus
`Input.backButtonLeavesApp = false` set once on Android. Details are in
`PHASE16_STEP12_ANDROID_BACK.md`.

## Final-level behavior

Winning Level 6 saves progression 6 and shows **All Levels Complete** with **Play
Again** and **Menu**, with no Next Level. Play Again starts Level 1 and keeps the
stored 6. This was covered by Editor tests; a six-level run on the S23 was not
replayed.

## S23 game-flow result (Step 15)

Samsung Galaxy S23, Android 17. This is one-device evidence.

- **Passed:**
  - cold launch → Main Menu → Play → first real finger tap;
  - swipe;
  - Restart → Cancel and Restart → Confirm;
  - Leave → Menu → Play / Continue;
  - Win → Next Level and Loss → Retry;
  - Back in every state above;
  - background/resume with a prompt open and during a move;
  - safe-area layout on menu, gameplay, confirmation and outcome.
- **No problems observed:** stale selection, duplicate session, frozen input, or old score or moves leaking.
- **Double-tap Cancel:** inconclusive (see the residual edge in `RestartConfirmation.md`).

The tested APK predates `1161850`. There is no phone result for the adjusted Level 3–6
budgets yet.

## Automated regression

| Run | Commit | EditMode | PlayMode |
| --- | --- | --- | --- |
| Step 16 full suites | `48f0849` | 632 / 632 | 344 passed, 0 failed, 3 skipped |
| Step 16 Phase 16 focused tests | `48f0849` | 29 / 29 | 96 / 96 |
| Codex post-adjustment focused checks | `1161850` | `LevelCatalogTests` 5 / 5 | `CatalogLevel_` and Level 1 scene: 7 / 7 |
| Step 17 full rerun | `1161850` | 632 / 632 | 344 passed, 0 failed, 3 skipped |
| Level 5–6 budget adjustment, tested before committing the same asset bytes | `fec0d76` | `LevelCatalogTests` 5 / 5 | Six `CatalogLevel_` cases and Level 1 scene: 7 / 7 |

- **Skips:** the three skips are always the `[Explicit]` real-frame-rate tests in `FrameRateTimingTests` (30, 60, 120 fps).
- **Step 16 Console:** apart from tooling and Test Framework housekeeping lines, it held only the declared camera-fitting warning from
  `SceneTouchInputTests.CameraFit_NoRoom_WarnsOnceKeepsCamera_AndRecoversWhenLayoutChanges`.
- **Step 17 Console:** after the rerun it held one Unity MCP bridge line ("WebSocket is not initialised"), with no project errors or warnings.
- **Editor state:** after each run, SampleScene was active and clean, Play Mode was stopped, and the platform was StandaloneWindows64.

The full-suite rows above predate the Level 5–6 budget adjustment. Do not present
them as a new full run against the modified catalog. No tests were added or
changed for the two-value content adjustment.

Follow-up jobs: EditMode `2f6aa88ad3d745728c3fb6c69fa3ce8f`, PlayMode
`e89579283e1b4c058d248d60c68310dc`. Both passed with zero failures/skips. These
checks validate configuration, stable/playable deterministic starts and session
replacement, not subjective difficulty. The Console held only four runner/tool
housekeeping entries. SampleScene remained clean and stopped, compilation idle,
StandaloneWindows64, Run In Background false and host progression unchanged at 2.
No APK was rebuilt or installed. The user requested modular commits and a push
for the validated catalog and documentation. Existing known-issue documentation
edits were preserved and committed separately.

## Deferred issues

| Issue | Status | Record |
| --- | --- | --- |
| Input stays locked if a move throws | Accepted risk; persistence and navigation re-reviewed | `KnownIssues/LockedAfterException.md` |
| Outcome dimmer leaves strips outside the safe area | Cosmetic, deferred | `KnownIssues/OutcomeDimSafeArea.md` |
| Tall-screen composition | Deferred to UI polish | `KnownIssues/TallScreenComposition.md` |
| `AssetPackManager` cold-start `ClassNotFoundException` | Investigation item, neither assumed a crash nor harmless; triage in the first Phase 17 Play-build smoke test, closure in Phase 20 | `Phases/PHASE13_RELEASE_READINESS_AUDIT.md` (Finding 6); log context in `KnownIssues/WatchList.md` |
| Double-tap Cancel reaching the board | Theoretical; no fix | `KnownIssues/RestartConfirmation.md` |

Resolved in this phase: Restart confirmation (`KnownIssues/RestartConfirmation.md`).

## Still open before the Phase 16 gate

- Normal human playthroughs of Levels 5 and 6.
- A replay of Levels 3–4 at their new budgets to confirm the feel. This is optional, but
  the difficulty is otherwise unverified.
- Rebuild the APK before claiming any phone result for the adjusted catalog.
- End-of-phase code cleanup: remove `[FormerlySerializedAs("restartButton")]` from
  `SafeAreaLayout`.
- Step 18 learning review, then human review of the final diff (Step 19).

## Sources

`PHASE16_STEP6_LEVEL_CATALOG.md`, `PHASE16_STEP7_PROGRESSION_PERSISTENCE.md`,
`PHASE16_STEP8_GAME_FLOW.md`, `PHASE16_STEP9_MAIN_MENU.md`,
`PHASE16_STEP10_OUTCOME_FLOW.md`, `PHASE16_STEP11_CONFIRMATIONS.md`,
`PHASE16_STEP12_ANDROID_BACK.md`, `PHASE16_STEP13_SESSION_ISOLATION.md`,
`PHASE16_STEP14_NAVIGATION_ADVERSARIAL.md`, `PHASE16_STEP15_S23_GAME_FLOW.md`.
