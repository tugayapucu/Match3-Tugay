# Phase 16 Step 14 — Busy-state and navigation adversarial review

Date: 2026-11-12. Baseline: `0c6d42c`. Status: reviewed and approved by Claude; no bugs or blocking production follow-ups.

## Commit reading order

1. `cb37115` — Challenge navigation during moves and session transitions. Read the nine cases, the shared busy-command assertions, and the callback/focus limits.
2. This separate review/validation record and the execution plan's two Step 15 device-check additions.

The approved test file was committed unchanged. The recorded passing results apply; tests were not rerun for this commit-only operation.

## Result and scope

No reproducible production defect was found in the focused review. Existing flow commands and Bootstrap's lock/gate are sufficient. Additions are `GameFlowNavigationAdversarialTests.cs`, its Unity `.meta`, and this report. The execution plan's Step 15 checklist also carries the reviewer-requested device observations. There is no production fix, new framework, exception policy, input queue, pause system, scene/settings change or phone operation.

The plan supplies the challenge list below. Claude independently read the tests, relevant code and this report, approved the step without rerunning the suites, and supplied the additional challenges recorded below.

## New coverage: nine PlayMode cases

All methods are in the existing partial `BoardBootstrapTests` fixture, prefixed `NavigationAdversarial_`.

| Method suffix | Cases | What actually runs |
| --- | ---: | --- |
| `RestartAndSwipe_FirstAcceptedActionOwnsTheFrame` | 2 | Virtual Touchscreen events versus Restart's button callback, in both same-frame orderings. Restart first disables input without changing the board; swipe first holds the move lock and refuses Restart. No delayed prompt or second session appears. |
| `EveryBusyFrameRejectsNavigation_ThroughRollbackCascadeOrChain` | 3 | Each busy frame challenges all flow commands and forcibly invokes the four HUD button callbacks despite their disabled visuals. The same Board/progress/index/save remain. Rollback is observed after the moving view's cell updates to the destination; a scripted three-round cascade earns exactly 900 points; the special-chain case confirms at least two planned effects and observes the Wisp flyer. |
| `Seed93Recovery_RejectsRequestsOnBothSidesOfTheLockedRecovery` | 1 | The ten recorded moves resolve through real input handlers, runner and Flow. The tenth callback observes the exact dead board before Bootstrap, then the recovered playable board afterward, with navigation refused on both sides while locked. Final values remain 10 moves, Red 3/25, 3,300 points and the same Board object. |
| `SingleLevel_LastMoveWinAndRepeatedActionsRespectFinalBoundary` | 1 | A one-level catalog; Play pressed six times starts one session. A real Spectrum Core swap wins with zero moves left. Back and all actions are refused in the locked outcome callback, including forced button invocations. Duplicate terminal notification is ignored; final title is correct, Next/Retry are refused, repeated Play Again starts one fresh index-0 session and retains completion 1. |
| `FocusLossOnConfirmation_PreservesIntentAndCancelSelection` | 2 | Restart and Leave each retain their intent, board/progress and visible prompt after simulated focus loss/regain. Board input remains disabled until Back cancels; selection/cue return and a second Cancel is refused. |

The cascade case also invokes focus loss while the move is locked, then focus regain after resolution. The coroutine completes, charges one move and leaves no deferred navigation.

## Challenge list disposition

“Covered” means the named Editor test/assertion scope; it does not imply device lifecycle validation.

| Planned challenge | Disposition and evidence |
| --- | --- |
| Restart as swipe starts | Covered: new two-ordering race test. |
| Menu/Back during swap | Covered: new race/busy-frame tests; existing `Escape_DuringResolution_IsDiscardedEvenIfHeldPastCleanup` checks InputAction delivery and holding Back past cleanup. |
| Menu/Back during invalid rollback | Covered: new Rollback case, including the observed reverse phase and exact board restoration/no move charge. |
| Menu/Back during cascade | Covered: new three-round Cascade case, including its locked Win callback. |
| Menu/Back during special chain | Covered: new Chain case, including observed Wisp flight. |
| Menu/Back during recovery | Covered at the locked callback boundaries by the new seed-93 test. Recovery itself is synchronous; see limits below. |
| Final-move Win with Back | Covered: new single-level real Win, Back called while Won and zero moves are visible but cleanup is locked. |
| Outcome double-click | Covered: new final Play Again repetition; existing `GameFlow_RealWin_SavesOnceWhileLocked_AndCannotNavigateBeforeCleanup` repeats Next, and `GameFlow_OutcomeMenu_ReturnsWithoutSavingOrReplacingTheSession` repeats Menu. |
| Restart confirmation + Back | Covered: existing `Escape_OnEitherConfirmation_CancelsAndRestoresSelection` and new focus/confirmation cases. |
| Controller disable/enable around confirmation | Covered: existing `Confirmation_CancelPreservesTheSessionAndRestoresTheSelectedCue` and `Confirmation_CancelSameUpdateUiTapNeedsAFreshBoardPress`; new focus cases also check disabled input and restored selection. |
| Main Menu Play spam | Covered: new single-level case checks one Board after six callback invocations. |
| One Win callback, repeated UI actions | Covered: new single-level case and existing locked Win/Next test; “reached” flags and callback counts prevent assertions silently not running. |
| Focus loss on confirmation | Callback simulation covered for both intents; native Android lifecycle remains unverified in this step. |
| Focus loss during a move | Callback simulation covered during the Cascade case; native Android suspension remains unverified in this step. |
| Final-level Win | Covered: new one-level final Win and existing `GameFlow_CompletedReplay_NextUsesCurrentIndex_AndFinalPlayAgainKeepsCompletion`. |
| Corrupt persisted index | Covered: existing `GameFlow_MenuLoadsProgress_AndPlayStartsOnlyTheChosenLevel` includes -1 and 99; rerun `ProgressionStoreTests.Load_StoredValue_IsClampedWithoutWriting` covers integer extremes and proves Load does not write. |
| Catalog with one level | Covered: new single-level case exercises the combined first/final boundary and completion sentinel. |
| First/last-level edges | Covered: single-level case, existing two-level replay test and Step 13's real Win → Next isolation test. |
| Reviewer: double-tap Cancel with a tile selected | Theoretical UX edge, not reproduced in this step; no production fix. The first tap cancels the prompt and restores selection A. A second fresh tap can reach board cell B underneath the former Cancel button and attempt A → B if adjacent. The same-update handoff guard intentionally permits a genuinely later press. Added to the Step 15 human S23 checklist. |
| Reviewer: press on tile → Restart → move/release | Code-traced safe: controller OnDisable clears the pending gesture; continuation cannot swap before or after Cancel without a fresh press. Optional extra ordering test omitted; existing gesture-cancellation tests cover the clearing rule. |
| Reviewer: Back and UI click in one frame | Code-traced safe in either ordering: Back first opens Leave and rejects Restart; Restart first opens its prompt and Back cancels it. Both follow existing guarded commands; no fix or new machinery. |

No item produced a concrete normal-play trigger for LockedAfterException. The accepted exception decision is unchanged. No speculative production fix is proposed.

The double-tap Cancel scenario follows the approved fresh-press and retained-selection rules; it is an observation to assess on the device, not grounds for adding a cooldown now. Focus simulations also do not establish that all remaining lifecycle risk is visual: actual native input/focus delivery after resume is unverified here. Step 15 now explicitly observes both continuation/animation behavior and a working fresh touch after resume.

## Test seams and limits

- The race uses virtual touch events and the production controller Update. HUD `onClick.Invoke()` tests command defenses; it is not EventSystem hit-testing or a physical double-tap measurement.
- Busy-frame Back calls use `HandleBack()` directly. The existing Escape tests cover the InputAction-to-command connection. These new cases do not claim native Back timing on each special-effect frame.
- Test boards and scripted refills are controlled integration fixtures. For Cascade/Rollback the rows are top-first (`GBY / GPO / RGG`), matching the existing PlayMode helper. The Cascade is terminal, so its deliberately finite refill script is not asked to seed dead-board recovery. The Chain changes the bottom pair to Spectrum Core/Wisp and uses a seeded refill stream. No production level asset changes.
- The recovery route uses programmatic pointer handlers/camera/raycast and the production resolution loop. The game does not yield inside recovery: UI callbacks cannot interleave with its Core call on the main thread. The test checks before/after under the lock, without a production hook. An OS event delivered after unlock is not distinguished here from a genuinely later press; no sub-frame device claim is made.
- Focus is simulated by invoking `BoardInputController.OnApplicationFocus(false/true)`. This checks our callback's state effects; it does not suspend Unity, recreate an Activity, or prove OS background/resume delivery. Physical lifecycle evidence belongs to the device review, not an invented pause implementation.
- Preferences use existing GUID test keys and cleanup. No production key, phone data or catalog asset is edited.

## Validation

| Check | Result |
| --- | --- |
| New adversarial PlayMode cases | 9/9 passed; 0 failed/skipped |
| Existing progression store + catalog EditMode tests | 29/29 passed; 0 failed/skipped |
| Full PlayMode regression | 344 passed, 3 explicit hardware-timing skips, 0 failed; 103.60 seconds |
| Full EditMode baseline | Prior 632/632; full suite not rerun because production/Core/EditMode code is unchanged |

Jobs: focused PlayMode `4ed8bee1006444d8a1d091bd0e5bb698`; focused EditMode `42b123e41565445eaec05cd492255c45`; full PlayMode `71f3bdc08b9241d39fece451f1989ba7`.

Compilation succeeded without warnings/errors. The final Console query returned only the expected no-room camera warning and test-runner result/cleanup messages, with no unexpected errors. The skipped tests are the existing explicit real 30/60/120 FPS tests.

SampleScene is active and unmodified; Play Mode and Pause are off, compilation is idle, and StandaloneWindows64 remains active. Development Build, Script Debugging, Autoconnect Profiler and configured/runtime Run In Background remain false. Only native Keyboard and Mouse remain; the virtual Touchscreen fixtures were removed. Production progression is unchanged at 2; isolated test preferences were cleaned up.

All 42 backed-up scene/settings/package files match their pre-test bytes; no restoration was needed. The change set contains the new adversarial test file, its `.meta`, this report, and the execution plan's two Step 15 checklist additions. `git diff --check` passes. No scene/prefab was saved, no build was made and no phone input was sent. After Claude's approval, only documentation was updated; the tested code is unchanged and tests were not rerun for those doc edits.

## Manual Unity reproduction

Use Assets → Refresh, wait for compilation/reload, then Window → General → Test Runner → PlayMode and filter `NavigationAdversarial_`. Run the nine cases. Clear the filter for the normal full suite; explicit hardware-timing tests stay excluded. There are no new Inspector fields, scene objects or prefab assignments. The focus tests simulate callbacks and should not be described as manually backgrounding the phone.

The user requested separate commits and a push after approving Step 14. Step 15 has not started.
