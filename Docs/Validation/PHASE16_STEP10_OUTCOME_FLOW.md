# Phase 16 Step 10 — Outcome Flow

Date: 2026-11-09. Status: reviewed and approved by Claude; committed locally at the user's request. Not pushed.

## Commit reading order

1. `f984553` — Show a distinct title after completing the final level. Follow the title from GameFlow to the HUD, then inspect TMP auto-sizing and the final-replay tests.
2. `e8e354c` — Derive HUD action availability from busy and outcome state. Read the two stored display inputs and RefreshActionAvailability, then the tests for Playing, resolving and terminal levels.
3. `c052080` — Add Menu navigation to every level outcome. Follow the new button's scene reference, listener, supplied callback and GameFlow.ReturnToMenu; then read the repeated-action and touchscreen tests.
4. This validation record — manual reproduction, test results, layout review and limits.

The approved final production/scene/test files were unchanged while staging these separate concerns. The test results below apply to that final combined state; intermediate commits were not individually run through Unity.

## Product behavior

| Outcome | Title | Primary button | Secondary button |
| --- | --- | --- | --- |
| Loss | Out of Moves | Retry | Menu |
| Non-final Win | Level Complete! | Next Level | Menu |
| Final Win | All Levels Complete | Play Again | Menu |

Retry creates a fresh session from the same definition. Next starts the next catalog index; the accepted Win already saved progression. Final Play Again starts index 0 without lowering the completion sentinel. Menu covers the retained board with the existing Main Menu, hides the HUD and gates board input off; it does not replace the session or save.

These navigation rules already existed in GameFlowController. This step exposes Menu on the outcome card and adds the final-completion title. No new flow mode, persistence rule, Core rule, polling method or package was added. In-game Menu and Restart/Leave confirmation remain Step 11.

## Code and ownership

- `LevelHudView.Initialize(Action restart, Action outcome, Action menu)` stores three replaceable callbacks. Each button listener is registered once in Awake and removed in OnDestroy.
- The existing serialized `playAgainButton` field remains the contextual primary button: Retry, Next Level or Play Again. Its name was retained to preserve its existing scene reference.
- `outcomeMenuButton` is a new required serialized Button. Missing wiring fails in the existing HUD configuration check before a session starts.
- `SetOutcomePresentation(string title, string actionLabel)` replaces `SetOutcomeActionLabel`. Flow supplies the catalog-aware title and primary label; the view does not inspect the catalog.
- The existing `SetRestartAvailable(bool)` remains Bootstrap's single pushed busy gate. The HUD remembers that gate and the last displayed Playing/terminal status, then derives interactability: Restart requires Playing; both outcome buttons require a terminal status. All require the busy gate to be available.
- Show does not release the busy gate. The outcome may become visible inside the locked callback, but both outcome buttons stay disabled until FinishSwap reports busy false. Restart remains disabled after Win/Loss.
- Existing flow mode/busy guards refuse a duplicate or premature action. No navigation occurs in the outcome callback. Bootstrap remains the sole production writer of board input's enabled flag.

## Scene changes and manual reproduction

Existing hierarchy, with the new child shown:

```text
Canvas / SafeArea / LevelHud / OutcomePanel / Card
├── OutcomeTitle
├── FinalScoreLabel
├── OutcomeScore
├── PlayAgainButton
└── MenuButton
    └── Text (TMP)
```

1. Outside Play Mode, expand the hierarchy above and select PlayAgainButton. Keep its bottom-centre point anchors and pivot (0.5, 0). Set size 268×84 and anchored position (-146, 40).
2. Duplicate it under Card as MenuButton. Keep the copied orange Image, font, button colours and label settings. Set position (146, 40), size 268×84, and label Menu. This leaves 24 units between buttons and 40 units to each card side. Both buttons use Navigation None.
3. On LevelHud's LevelHudView, drag MenuButton into Outcome Menu Button. Keep the Inspector On Click lists empty: Awake adds the listeners in code.
4. On OutcomeTitle's TMP component, enable Auto Size, minimum 32, maximum 60. Its existing box remains 600×80. The card stays 640×400, centred inside the existing safe area.
5. Save SampleScene. Check Loss, non-final Win and final Win. Before move cleanup, neither action should be interactable; afterward both are. Restart should remain unavailable on the outcome screen.

No change to BoardRoot, camera fitting, safe-area calculations, prefab, catalog, packages or final project settings. OutcomeDimSafeArea and TallScreenComposition remain deferred.

## Automated validation

- Focused HUD and real-scene touch tests: **39/39** passed.
- Full EditMode: **632/632** passed.
- Final full PlayMode: **296 passed, 3 skipped, 0 failed**. The skips are the existing explicit hardware-timing tests. Baseline was 288 passing; this step adds eight cases.
- HUD tests cover independent/replaced callbacks, missing Menu reference, final title/score display and the busy/status combination for all three buttons.
- Flow tests cover Menu after Loss, non-final Win and final Win without changing the retained session or stored progression. Existing real-win callback assertions now include Menu being disabled while locked and enabled after cleanup, with Restart still disabled. Repeated primary/Menu invocations are refused after their accepted transition.
- Scene tests use queued touchscreen events through Update/EventSystem. They exercise Retry, Next, final Play Again and Menu after Win/Loss; verify action-on-release, safe-area containment/button separation, retained completion, and no board selection leaking from the UI gesture.
- The four older failing expectations were migrated deliberately: outcome actions are unavailable while Playing, and Restart is unavailable after Win/Loss. Bootstrap's two terminal-session replacement tests now call its session-start API directly; outcome navigation is exercised separately through the contextual buttons.
- One intermediate full PlayMode run was interrupted by an Editor domain reload, producing a Unity Test Framework PlayModeRunTask exception. It was not counted as a result. Its orphan job and exact temporary InitTestScene asset were cleaned up before the successful full rerun. No package/framework code was modified.
- Test preference keys are isolated and cleaned up. No DeleteAll or production save writes were added.

## Editor layout review

All three states were measured in each case below, after layout and TMP mesh updates. Card and buttons stayed inside their intended bounds, buttons did not overlap, and no active HUD text overflowed. Final title remained one line (59.2 canvas units; the other titles used 60).

| Game view / safe area | Card bounds in screen pixels (x, y, width, height) |
| --- | --- |
| 1080×1920, full | 220, 760, 640, 400 |
| 1080×1200, full | 220, 400, 640, 400 |
| 720×1280, full | 146.67, 506.67, 426.67, 266.67 |
| 1080×2340, full | 220, 970, 640, 400 |
| 1080×2340, left 72 / right 24 / top 96 / bottom 48 insets | 244, 946, 640, 400 |

The 720×1280 and asymmetric final-completion screenshots were visually inspected: the title, score and both labels are readable. The known undimmed safe-area strips are still visible in the asymmetric case. These previews supplied calculated LevelProgress values and labels directly to the HUD, without playing or saving wins; real flow and UI behavior is covered separately by the tests. Insets are simulated, not device evidence.

Local ignored screenshots: `Builds/Validation/Phase16Step10/final-1080x1920.png`, `final-1080x1200.png`, `final-720x1280.png`, `final-asymmetric.png`. No APK build or S23 installation was performed; physical readability/reachability remains the planned device review. Production progression remained present with value 2 before and after previews.

## Final state and changed files

SampleScene is active, clean and outside Play Mode; compilation is idle. Active target is StandaloneWindows64. Phone Game view is restored to index 8, 1080×1920. SafeAreaLayout is enabled; HUD remains active; the saved menu objects remain inactive. Run In Background is restored to false in runtime and project settings and saved to disk. Only native Keyboard and Mouse devices remain. Final Console error/warning query is empty. `git diff --check` passes.

Nine existing files changed: SampleScene; LevelHudView; GameFlowController; TestHud; LevelHudViewTests; GameFlowControllerTests; BoardBootstrapTests; BoardBootstrapSessionTests; SceneTouchInputTests. This validation record is the only new tracked file. Unity's empty-name whitespace churn was removed while the saved scene was unloaded; existing scene formatting was otherwise preserved. No production Core, Bootstrap, input, prefab or project settings changes remain.

Step 11 has not started. These commits were requested after review; pushing still requires the user's request.
