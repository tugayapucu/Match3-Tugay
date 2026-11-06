# Phase 15 Step 13 — Full regression validation

Date: 2026-11-06. Production code is unchanged since `07f663c`. Validated at
`e8b2b95`, which only strengthens one test.

**Result: pass.** Both full suites passed, and the Console held only the
messages tests declare.

## Suites

| Suite | Result |
|---|---|
| EditMode (full) | **603 / 603 passed** |
| PlayMode (full) | **233 passed, 0 failed, 3 skipped** (236 total) |

The three skips are the `[Explicit]` real-frame-rate tests in
`FrameRateTimingTests` (30, 60 and 120 fps). They need a display that can
deliver those rates and run only when selected by hand, as in every earlier
phase.

The Phase 15 focused tests are part of these suites:

- `BoardInputControllerGestureTests`
- `BoardInputControllerSelectionTests`
- `SelectionHighlightTests`
- the Phase 15 `Update_*` cases in `BoardInputControllerTests`
- the swipe and selection-cue additions to `BoardBootstrapTests` and
  `SceneTouchInputTests`

All passed.

## Confirmed unchanged

| Area | Covering tests (examples) |
|---|---|
| Core gameplay | EditMode suite: matching, gravity, refill, specials, level progress |
| Deterministic restart | `RestartLevel_RecreatesTheSameStartingBoardAndANewSnapshot`, `RestartLevel_ReplaysPropellerTargetsAndRefillAcrossTwoPlayerMoves` |
| Dead-board recovery | `ValidMoveResolved_DeadBoardWhilePlaying_RecoversWithoutChangingProgress`, `Seed93Swipes_RecoveryFinishesInsideTheLockedProductionCallback` |
| Special chains | `SpecialSwap_TwoSpecialsAndLongMixedChain_CostOneMoveWithoutBonus`, `Swipe_SpecialPair_UsesNormalChainRulesAndChargesOneMove` |
| Invalid rollback | `HandlePointerDown_SwapWithoutMatch_AnimatesForwardThenRollsBack`, `Update_TwoTouchTaps_WithoutAMatch_RollBack` |
| Win / Loss | `Level_GoalReached_WinsAndStopsNewMoves`, `Level_LastMoveWithoutGoal_LosesAndStopsNewMoves`, `Update_TouchAfterWon_IsIgnored`, `Update_TouchAfterLost_IsIgnored` |
| Busy / Restart | `RestartLevel_DuringAMove_ThrowsAndTheMoveFinishesNormally`, `ValidMove_HudUpdatesWhenTheMoveFinishes_AndRestartIsUnavailableUntilThen` |
| Board / View consistency | `Resolve_SeveralRealMoves_LeaveNoStaleLookupState`, plus `AssertViewsMatchBoard` after moves throughout |

## Input paths

Each path is driven through the controller's real `Update` poll with a virtual
Input System device, not through the public handler methods.

| Path | Test | "No duplicate moves" evidence |
|---|---|---|
| Mouse two-click | `Update_MouseTap_SelectsOnReleaseAndDoesNotRepeatWhileHeld` | busy → free exactly once; an idle poll after unlock starts nothing |
| Touch two-tap | `Update_TwoTouchTaps_ResolveExactlyOneValidMove` | one report; busy → free exactly once |
| Touch swipe | `Swipe_ValidCascade_ChargesOneMoveAndReportsEveryRoundExactlyOnce` | one report covering every cascade round; held after unlock, no repeat |

**Gap closed in this step.** The mouse test already drove a real two-click into
the swap, but it never counted attempts. Commit `e8b2b95` adds:

- one idle poll after unlock;
- exactly one busy → free cycle;
- no report;
- the rolled-back board.

This is a test-only change. The swap on that board makes no match, so the
correct result is one invalid attempt that rolls back.

## Console

The Console was cleared before the final PlayMode run. Afterwards it held exactly
the five messages the tests declare with `LogAssert.Expect`:

- `InvalidOperationException: The goal tile type Purple is not in the palette.`
- `InvalidOperationException: BoardView clearDuration must be finite and nonnegative, not -1.`
- `InvalidOperationException: Assign a child selection highlight with a sprite before displaying selection.`
- `InvalidOperationException: Assign the TNT sprite before displaying that tile.`
- Warning: `Board camera fitting paused: the HUD and Restart leave no room. Keeping the current camera until the layout changes.`

It also held two test-runner housekeeping lines (saving `TestResults.xml` and the
performance-testing post-build cleanup). There were no `KEYS`/`TSCR` input
errors and no other errors or warnings.

## Device

No device run in this step. The installed S23 APK was built from the same
production code (Step 10). Device evidence is in the Step 10–12 records.

## Editor state

SampleScene is active, clean and stopped. No scene, prefab, settings or package
changes.
