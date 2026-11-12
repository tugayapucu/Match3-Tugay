# Phase 16 Step 15 — S23 game-flow smoke test

Date: 2026-11-12. Status: reviewed and approved by Claude; double-tap observation clarified as inconclusive. No new production defect was reproduced and no production fix was needed. The user approved committing this smoke-test record after review.

## Device and build

Samsung Galaxy S23 SM-S911B, Android 17 / API 37, 1080×2340. This is one-device evidence, not validation of every Android device.

The installed APK's SHA-256 is `ae31dffd4fff85ba65a6d399bdecbeb989d3cf3bba7d7194b8d2b7850904eb3b`, identical to the final Step 12 APK at `Builds/Android/Phase16-Step12/Match3-Phase16-Step12-Development.apk`. Production scripts, scenes, prefabs, catalog, settings and packages have no diff between `ca227bd` and this step's baseline `48f0849`; Steps 13–14 added tests/documentation only. No rebuild or reinstall was needed.

The phone's current game preferences were backed up before testing. No old backup was substituted and no preferences were cleared. Screenshots, logs and session observations are saved under ignored `Builds/Validation/Phase16Step15/`.

## Method and safety

Human finger checks are distinguished from ADB observations. ADB input, when used, is one action at a time, only after inspecting a screenshot and confirming Match3 is foreground. The helper refuses input if the current screenshot differs from the inspected reference. A launch command or fixed delay is not treated as proof that the game is ready.

In this sitting all taps, swipes, Back and Home/resume checks were performed by the user. ADB cold-launched Match3, verified the installed APK/foreground, captured screenshots, and read this app's preferences/logs. The guarded single-action helper was prepared but no coordinate tap/swipe or Back command was sent through it.

## Smoke-test observations

| Check | Evidence and result |
| --- | --- |
| Cold launch → menu | ADB reported `LaunchState: COLD`; inspected screenshot showed the opaque Main Menu and Play. |
| Play → first real finger tap | User: first tap shows the brackets. Screenshot confirms one selected tile, 20 moves, Red 0/25 and score 0. |
| Restart → Cancel | User confirms the same selected tile and HUD are preserved; screenshot agrees. |
| Double-tap Cancel | No swap observed; inconclusive. User described a quick double-tap as behaving like a single Cancel; screenshot retains selection and 20 / 0/25 / 0. The selected red tile at column 2, row 6 from the top-left is adjacent to the cell under Cancel's centre, but the button spans multiple cells and the actual second-touch position/timing was not recorded. A second tap may not have reached the board, or may have hit a non-adjacent cell, which also preserves selection. This does not prove input was blocked. Step 14's theoretical fresh-second-press risk remains documented; no cooldown or fix added. |
| Restart → Confirm | User confirms original board, 20 moves / Red 0/25 / score 0, no selection; reopened Restart prompt captured. |
| Ordinary swipe | Human swiped the bottom-row column-5 green gem upward; after the cascade/resume the screenshot shows 19 moves, Red 9/25 and score 3500, matching the known first move. |
| Leave → Menu → Play | User confirms leaving the partial session returns to menu and Play creates a fresh Level 1 at 20 moves / Red 0/25 / score 0 with no old selection; fresh selection works. Screenshot confirms starting board and HUD. |
| Level 1 Won | Human-directed normal play reached Level Complete at 15 moves left, Red 25/25 and score 8900. User: easy but reasonably easy; result text and buttons readable, not bad. No recorded move route was supplied for this run. |
| Win → Next Level | User confirms Next starts a different Level 2 at 22 moves / Green 0/22 / score 0, with no old selection/outcome. |
| Menu → Continue | On Level 2, user confirms Menu → Leave shows Continue; Continue starts fresh Level 2. Screenshot confirms the expected starting board and 22 / Green 0/22 / 0. Reading the phone's preferences after the Level 1 win found saved `Match3.Progression.v1 = 1`. |
| Loss → Retry | User deliberately avoided Blue on Level 3 and reached Out of Moves; screenshot shows 0 moves / Blue 16/25 / score 10500 and correct Retry/Menu buttons. Retry immediately restored 20 / Blue 0/25 / 0, original starting board and no outcome/old selection. User confirms a fresh finger selection works. This is a flow check, not normal-play difficulty evidence. |
| Six-level final completion | Not replayed on the phone in this sitting; the plan makes this conditional on practicality. Editor final-boundary coverage remains earlier evidence, not a physical-device result. |
| Back during settled gameplay and on confirmation | User confirms fresh selection works after resume, Back opens Leave, and a second Back cancels and restores that selection. |
| Back during resolution | On the second upward swipe, user confirms Back was ignored during animation; after settlement a fresh Back opened Leave. Captured prompt shows 18 moves, Red 17/25 and score 5500, so only the intended second move was charged. |
| Back in Main Menu | User confirms Back backgrounds to Home, reopening shows the menu with Play, and Play works afterward. |
| Back on outcome | User confirms Android Back on the Level 2 Won panel returns to Main Menu with Continue and keeps the app open. Captured menu agrees. Loss-specific Back has earlier Step 12 evidence, not a new check here. |
| Background/resume on confirmation | User went Home for about five seconds with Restart open, reopened Match3 and confirmed the prompt and HUD were preserved. Cancel and a fresh finger selection worked afterward; the resulting gameplay screenshot was captured. The prompt's intermediate native lifecycle was human-observed, not frame-instrumented. |
| Background/resume during a move | User went Home while the first swipe's animation ran. On reopening, the animation continued where it left off; the user reports everything working and good. Settled screenshot is 19 / 9/25 / 3500. No obvious visual glitch was reported; this is a human observation, not frame-time instrumentation. The subsequent fresh-selection and real swipe checks also passed. |
| Menu/gameplay/confirmation/outcome safe-area appearance | Inspected full-screen menu, gameplay, both confirmations, Won and Lost screenshots. HUD, board, bottom controls, cards and buttons are visible/separated; no clipping or text overflow observed. User found the Won card readable and Lost buttons correct. The outcome dimmer still leaves the known bright top strip, while the confirmation dimmer covers the whole screen. |
| Six independent human level playthroughs | Levels 1–2 have smoke-session human wins recorded below. After the review, the user also reported completing Levels 3–4 at the original budgets, both between medium and hard. Levels 5–6 remain unplayed before the phase gate. Machine-assisted winning routes do not close these requirements. |

## Human content observations

These are individual playthrough observations, not a measured success rate or an established difficulty curve.

| Level | Result | Moves left / goal / score | Human assessment |
| --- | --- | --- | --- |
| 1, Red | Won | 15 / 25/25 / 8900 | Easy but reasonably easy; outcome readable. |
| 2, Green | Won | 10 / 22/22 / 17300 | Easy and understandably easy. User described completion "in 10 moves"; the captured HUD has 10 remaining from the 22-move budget (12 moves used). |
| 3, Blue | Won, reported after the smoke review; original 20-move budget | Winning counters not captured | Between medium and hard. The earlier deliberate flow-only Loss (0 / 16/25 / 10500) remains separate evidence. |
| 4, Yellow | Completion reported after the smoke review; original 22-move budget | Counters not captured | Same difficulty assessment as Level 3: between medium and hard. |
| 5, Purple | Pending | — | — |
| 6, Orange | Pending | — | — |

## Limits and final state

This step does not change production code, scene/prefab wiring, settings, the centered camera policy, or accepted exception policy. Tall-screen composition and the outcome dimmer's safe-area strips remain deferred unless a functional blocker is observed.

The cold-launch player log contains the already documented `AssetPackManager` ClassNotFoundException. It is not a new finding and is not assumed harmless. No package or exception-policy change is attempted here.

At the final log check after Retry, that same cold-start exception remains the only matching exception in the captured current-player log. No new managed exception or fatal exception was found in that sample. This is not a claim of an error-free startup or universal lifecycle correctness.

No automated suites were rerun for this device-only validation; no production/test source changed. The earlier baseline remains 632 EditMode passes and 344 PlayMode passes with three explicit timing skips. Full regression belongs to Step 16.

Read-only Editor checks: SampleScene active and not dirty, Play Mode off, compilation idle, StandaloneWindows64; Development Build, Script Debugging, Autoconnect Profiler and Run In Background false. Editor Console returned no error/warning entries. Host progression remains 2. On-device progression remains 2 after Loss and Retry, preserving the two human-earned wins; no preferences were restored or artificially advanced. The original pre-smoke backup is retained.

No frozen input, visible duplicate navigation/session, stale outcome/selection leakage or old score/move leakage was observed in these checks. This is user/screenshot evidence, not a frame-by-frame Android Board/View audit or object-identity proof; those stronger invariants have earlier Editor tests. No frame-rate, allocation, or SyncTransforms-cost measurement is claimed here.

At the smoke-test checkpoint the phone was left in fresh Level 3 after Retry, with saved progression 2; no more automated phone input was scheduled. Later user-reported play is recorded in the human-content table above; those later preference values were not read back. Final six-level completion was not physically replayed. Tall-screen whitespace, outcome dimmer strips, and the known startup exception remain as documented.

At the approved Step 15 checkpoint this Markdown was the only repository addition; no Assets, scene, prefab, catalog, settings, packages, or test-source changes were part of that smoke test. The double-tap clarification needed no phone retest or suite rerun. Step 16 subsequently passed and was reviewed separately. Local screenshots, preference backups, logs and the single-action helper remain ignored under Builds.
