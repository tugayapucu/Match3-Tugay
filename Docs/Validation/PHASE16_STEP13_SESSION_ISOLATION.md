# Phase 16 Step 13 — Cross-level session isolation

Validation date: 2026-11-11. Status: reviewed and approved by Claude; no bugs or blocking follow-ups. Committed locally at the user's request on 2026-11-12; not pushed. Baseline: `b5e5abe`.

## Commit reading order

1. `e9fb222` — Verify cross-level session isolation and deterministic restart. Read the four cases, then the shared fresh-session assertions and RNG checks.
2. This separate validation report, covering the approved resume policy, test results, input limits and final Editor state.

The approved test file was committed unchanged. The reported test results apply; tests were not rerun for the commit-only operation.

## Result and scope

The existing session construction is sufficient; no production change is needed. This step adds `Assets/Tests/PlayMode/GameFlowSessionIsolationTests.cs` and its Unity-generated `.meta`, plus this report. Core, flow, Bootstrap, input controller, scene, prefab, catalog and settings remain unchanged. The phone is not used.

The tests extend the existing `BoardBootstrapTests` fixture. Its board is 5×5 with a Red/Green/Blue palette and an in-memory two-level catalog: seed 42 / 4 moves / Red 1, and seed 73 / 7 moves / Blue 1000. These are integration fixtures, not changes to the six production levels or evidence of their human difficulty. The existing catalog tests cover the production definitions separately.

## Four focused cases

| Case | Evidence |
| --- | --- |
| Real Win → Next | A controlled Spectrum Core swap resolves through the real controller, runner, Bootstrap outcome callback and Flow. The outcome primary button starts index 1. Board, progress, starting snapshot, resolution runner and both RNG instances are replaced. |
| Playing → Menu → Continue | After a real move, selection and a held virtual touch exist. Leave is confirmed through the existing button callbacks. Continue builds the current saved level fresh; the old touch's release cannot select or swap on it, while a fresh touch still works. |
| Completed replay → Menu → Play Again | The same held-touch scenario starts after replaying and winning index 0, then entering index 1. The stored completion sentinel stays 2. Returning to menu offers Play Again, which starts index 0, as approved. |
| Restart current level | A controlled Wisp move uses the real special-resolution path. Confirmed Restart recreates index 1 with its original tiles and fresh independent RNGs. Repeating the same move reproduces the settled board, moves, score, goal and subsequent refill/targeting draws. |

Common start assertions cover configured tiles, fresh moves/score/goal, Playing status, no current matches, a legal move, no selection/highlights, hidden outcome/menu/confirmation, enabled gameplay actions, disabled outcome actions, and exactly 25 correctly registered and positioned tile views. Old Unity view instances must be destroyed after the frame boundary. New gameplay must not mutate the replaced Board.

The RNG check verifies that the runner shares Bootstrap's refill object and keeps targeting separate. After Next, eight draws from each live stream match independently seeded `Random` instances, including the targeting XOR seed. Those reads occur only at test end. The Restart test samples stream continuations after its real Wisp moves; sampling the abandoned stream cannot affect the new session. Reflection inspects existing private fields; no production test API is added.

## Approved resume policy

Continue stores an index, not an in-progress session. Leaving a partially played level and continuing it creates a new deterministic starting board with full configured moves and zero score/goal progress. After all levels are complete, replay position is intentionally not saved: Main Menu Play Again returns to Level 1 and preserves the completion sentinel.

## Test and input limits

The wins and Wisp moves use controlled special-tile fixtures followed by programmatic `HandlePointerDown`/`HandlePointerUp` calls through camera/raycast picking and the real resolution coroutine. They do not claim physical input or human special creation. Button `onClick.Invoke()` exercises existing callbacks, not EventSystem picking. The held-touch cases queue virtual Touchscreen events into the Input System and poll the production Update method; no gesture fields are edited.

The earlier same-input-update handoff, busy navigation and native UI tests remain in the full regression suite. This step does not repeat Android Back or the later physical-device review.

## Validation

| Run | Result |
| --- | --- |
| Focused `SessionIsolation_` cases | 4/4 passed, 0 failures/skips |
| Full PlayMode | 335 passed, 3 explicit hardware-timing skips, 0 failed; 94.46 seconds |
| EditMode | Not rerun; prior 632/632 baseline retained because no production or EditMode code changed |

Focused job: `b835f1b5e3a047a79c1aee64bf019af6`. Full PlayMode job: `f292ae5a69d44cc29c3f1fb06e5e28e6`.

Compilation completed without errors or warnings. The final Console query returned the expected no-room camera warning and routine test-runner result/cleanup messages, with no unexpected errors. The three skipped tests are the pre-existing explicit real 30/60/120 FPS tests.

SampleScene is active and unmodified, Play Mode and Pause are off, compilation is idle, and the platform is StandaloneWindows64. Development Build, Script Debugging, Autoconnect Profiler, configured/runtime Run In Background remain false. Only native Keyboard and Mouse remain; no test Touchscreen is registered. The host's production progression remains 2. Every test uses the existing GUID-key fixture cleanup; no production preferences or phone data are reset.

All 42 scene/settings/package files compared byte-for-byte equal to the pre-test snapshot; no restoration was needed. Git has only the three new intended paths, and `git diff --check` passes. No scene/prefab was saved and no build/platform change or phone command was made.

## Manual Unity steps

1. Put the test file under Assets/Tests/PlayMode and use Assets → Refresh. Unity generates the `.meta`; there are no Inspector fields to assign.
2. Wait for compilation and reload to finish. Open Window → General → Test Runner → PlayMode.
3. Filter for `SessionIsolation_` and run the four cases. Clear the filter and run the normal PlayMode suite for regression coverage; the three explicit hardware-timing tests are excluded unless deliberately selected.
4. Verify the Console, exit Play Mode, and confirm SampleScene is active and unmodified. No scene save or platform switch is required.

Step 13 is reviewed and committed locally. Step 14 has not started; no push was requested for these commits.
