# Phase 16 Step 8 — Game-flow owner

Implemented on 2026-11-07; final verification and review completed on 2026-11-08. Status: approved for this intermediate Step 8 checkpoint.

## Ownership and scope

`GameFlowController` chooses catalog entries and navigation. `BoardBootstrap` still creates one fresh session and is the only production writer of `BoardInputController.enabled`. Core and the catalog contents are unchanged.

Flow modes are `Uninitialized`, `MainMenu`, `Playing` and `Outcome`. These guard navigation, not gameplay rules; `LevelProgress` still determines Won/Lost, and Bootstrap's effective input gate still refuses terminal sessions.

Public actions return whether they were accepted: `Play()`, `Restart()`, `Retry()`, `Next()`, `PlayAgain()`, `ReturnToMenu()`. `Initialize(ProgressionStore = null)` validates scene references and the catalog before constructing the default store and loading progression. Tests supply stores with isolated keys before Unity Start. There is no singleton, event bus, polling loop or coroutine cancellation.

| Action | Allowed from | Result |
| --- | --- | --- |
| Play | MainMenu, settled | Loaded progression index; completed sentinel starts index 0 |
| Restart | Playing, settled | Fresh session for the same index |
| Retry | Lost Outcome, settled | Fresh session for the same index |
| Next | Non-final Won Outcome, settled | Current index + 1, independent of stored progression |
| Play Again | Final Won Outcome, settled | Index 0; stored completion is retained |
| Menu | Playing or Outcome, settled | Gate input off, hide HUD, retain active world objects |

Only an accepted Won callback saves `currentIndex + 1`. It changes flow to Outcome and sets the existing primary button label, without navigating. Duplicate callbacks in Outcome are ignored. Bootstrap's existing busy callback remains the sole source of move-settled button availability; the primary action stays disabled until FinishSwap unlocks. Click-time guards also reject navigation while busy.

`BoardBootstrap.initialLevel`, its autostart fallback, and its automatic HUD restart callback were removed. Bootstrap Start retains the Android 60 FPS request and applies input availability; it does not choose a level. Level 1 now comes exclusively from catalog index 0, still 42 / 20 / Red / 25.

## HUD and scene changes

- New active `GameRoot/GameFlow` with explicit catalog, Bootstrap and HUD references.
- `LevelHudView.Initialize` now takes separate Restart and outcome actions, replacing callbacks rather than accumulating them. Configuration validation is separate so Bootstrap can validate without taking over navigation.
- The existing primary outcome button is reused: Retry for Loss, Next Level for a non-final Win, Play Again for final Win. Its existing TMP text is assigned to the new `outcomeActionText` field. Styles and RectTransforms are unchanged.
- A CanvasGroup on LevelHud controls alpha, interactability and raycast blocking. The HUD hierarchy and its TopBar/Restart layout objects stay active for camera fitting. BoardRoot and GameRoot are never deactivated by navigation.
- No changes to SafeAreaLayout, camera fitting, BottomControls, tile geometry, colliders, prefab, packages or project settings.

This is an intermediate checkpoint: launch enters MainMenu state with HUD hidden and no gameplay session. The visible full-screen menu background and Play/Continue controls belong to Step 9 and are not present yet. Returning to Menu currently leaves the retained board visible behind the hidden HUD; Step 9's opaque full-screen background will cover it. Menu/level labels and the fuller outcome presentation remain their later UI steps. Restart is still immediate here; confirmation is Step 11. This is not claimed as the completed player-facing menu flow.

Review follow-up for Steps 10/11: after outcome cleanup the existing busy gate re-enables both legacy buttons, but Flow rejects Restart outside Playing. The outcome overlay normally covers that button; recheck its displayed availability when the outcome UI changes. No behavior change was requested in Step 8.

## Automated evidence

- Initial 19 focused GameFlow cases passed; a twentieth case was then added to distinguish no-save behavior from merely saving the same zero value.
- Flow checks cover missing/negative/normal/completed/oversized progression, selected starting index, duplicate actions, empty catalog validation, deterministic Retry and Restart, Menu, final replay, and advancing a replay using current index rather than persisted completion.
- A controlled LightBall/Red fixture runs through real pointer handlers, raycast picking, resolution and Bootstrap. Inside the actual locked outcome callback it observes saved progress, unchanged session identity, disabled buttons, and rejected navigation; after cleanup it observes one accepted Next. Callback assertions have a completion flag checked outside the coroutine.
- A fresh-key Loss/Retry/Restart/Menu test proves those operations do not even create a preference key. Another preserves already-earned progression.
- Ten same-input-update cases use queued mouse/touch events for Play, Restart, Next, Retry and final Play Again. The UI gesture cannot select the new board; a fresh subsequent press can. Existing gate/selection/held-input tests remain in the regression suite.
- Real-scene tests inject their unique progression store in `SceneManager.sceneLoaded`, before Start; they never save to the production key. They verify MainMenu startup with no session, then call Play to run the existing scene/picking tests. Win now exercises the real outcome button to start index 1; Loss exercises Retry at index 0. Each test deletes only its own preference key.
- Session-only fixtures now explicitly supply a definition and their own HUD routes. The four invalid-configuration tests assert synchronous StartLevel exceptions and messages instead of expecting exceptions from the removed Unity Start fallback.
- Full EditMode: 632/632 passed.
- The first full PlayMode run found one test-migration failure: the pre-Start preservation test used a helper that now explicitly requests a second session. It was changed to activate Bootstrap and yield without requesting another session. No production change was needed for this failure.
- Final full PlayMode: 281 passed, 3 explicit hardware-timing skips, 0 failed. This includes all 20 new GameFlow cases and the added HUD wiring case (260 previous + 20 + 1).

## Manual Unity steps and verification

1. Under GameRoot, create an empty GameFlow object and add GameFlowController.
2. Assign InitialLevelCatalog from Assets/Levels, the scene BoardBootstrap, and Canvas/SafeArea/LevelHud.
3. Add CanvasGroup to LevelHud; retain alpha 1 and normal interaction in the saved scene. Runtime flow hides it when appropriate.
4. Assign the existing outcome button's Text (TMP) component to LevelHud's Outcome Action Text field. The button listeners are registered by code, not duplicated in Inspector OnClick entries.
5. Save SampleScene. BoardBootstrap no longer has Initial Level in the Inspector; catalog index 0 owns that definition.
6. Run the GameFlow and real-scene PlayMode tests, or run the whole suite. At this checkpoint pressing Play in the Editor enters a menu state without visible menu controls; Step 9 supplies them. The tests invoke the flow actions to exercise navigation now.

## Persistence / exception boundary

The store still persists one progression integer only. No partial Board or RNG state becomes authoritative on relaunch. Storage exceptions are not suppressed; the accepted LockedAfterException risk is not solved. Physical Android relaunch, human level winnability, write performance, confirmation UI and finished menu usability are not claimed as validated by this step.

## Final state

The normal production Start path was also exercised in Editor Play Mode without injecting a store or starting a level: MainMenu, loaded progression 0, label Play, no session, zero tile views, disabled board input, HUD alpha 0/non-interactable/non-blocking, and active layout/BoardRoot objects. This smoke only read the production preference; it did not save or play a move.

Final Console contains routine runner messages and the declared camera no-room test warning, with no unexpected project errors. The startup guard tests now assert exceptions directly, so the earlier Purple/configuration negative-test logs are no longer expected in the full run. MCP briefly stopped answering during compilation; the user brought the Editor back and compilation/tests completed normally. That tooling interruption was not a gameplay defect.

Final Editor-state verification completed on 2026-11-08 after the user restored the MCP connection following the final reload. SampleScene is active, clean and out of Play Mode; compilation is idle and the latest Console is empty. Editor target is StandaloneWindows64, Run In Background is false, and only native Keyboard and Mouse devices remain. The saved HUD CanvasGroup alpha is 1; catalog count is six and index 0 remains seed 42 / 20 moves / Red / 25. BoardRoot remains at (-3.5, -3.5, 0) with unit scale.

Step 8 changes comprise nine existing files and five new files including metadata and this record. The scene diff is limited to the removed inline initialLevel, new GameFlow object/references, HUD CanvasGroup and outcome-text reference. Core, prefabs, catalog content, project settings and packages are unchanged; diff whitespace checks pass. Step 9 has not started at this checkpoint.
