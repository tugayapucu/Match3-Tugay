# Phase 16 Step 11 — Restart and Leave confirmation

Date: 2026-11-10. Status: reviewed and approved by Claude; committed locally at the user's request. Not pushed.

## Commit reading order

1. `5b13625` — Add a focused Restart and Leave confirmation view. Read the explicit intent, two wordings, callback registration and view tests. It does not navigate by itself.
2. `0d0c04a` — Frame the board above a shared bottom controls row. Follow SafeAreaLayout's renamed reference into the camera fitter. The row keeps the old control's top edge; the framing math stays the same.
3. `f60de56` — Add gameplay Menu and a HUD flow permission. Read the new button's callback and how gameplay permission combines with busy and terminal status. Confirmation behavior is connected in the following commit.
4. `ac300e5` — Confirm Restart and Leave while preserving selection on Cancel. Follow OpenConfirmation, Cancel and Confirm, then inspect scene layering and the integration/touch tests.
5. This review/validation record and the known-issue update.

The approved final Assets files were byte-for-byte unchanged during staging. The reported tests apply to the final combined implementation; intermediate commits were not individually run through Unity.

## Behavior and ownership

| Starting state / request | Result |
| --- | --- |
| Playing, settled → Restart | Restart level? — Cancel / Restart |
| Playing, settled → Menu | Leave level? — Cancel / Leave |
| Either prompt → Cancel | Same Board, progress, views and logical selection; input and selection cue restored |
| Restart prompt → Confirm | Fresh deterministic session for the current catalog index |
| Leave prompt → Confirm | Main Menu covers the retained session; input stays disabled |
| Busy gameplay → either request | Refused; nothing queued |
| Settled outcome → Retry / Next / Play Again / Menu | Existing immediate outcome navigation; no confirmation |

GameFlowMode adds Confirmation. GameFlow owns a nullable PendingConfirmation with the explicit Restart or Leave intent. Confirm dispatches only that intent, then the transition clears it; repeated Confirm/Cancel cannot navigate twice. Opening another prompt or issuing other navigation while Confirmation is active is refused.

`Restart()` now means request confirmation. `ReturnToMenu()` requests Leave confirmation while Playing and remains immediate on a settled outcome. `Confirm()` and `CancelConfirmation()` return whether their request was accepted. Existing callers/tests that intentionally exercised immediate Playing navigation were migrated to request + Confirm.

The new `ConfirmationView` is one small MonoBehaviour, with no Update. It owns only two sets of words and forwards Confirm/Cancel callbacks, registered once in Awake and removed in OnDestroy. `Initialize(Action confirm, Action cancel)` checks its card, texts and buttons at flow startup. Flow chooses the pending action; the view does not start sessions, write saves or gate board input.

## Input and action availability

Opening a prompt calls Bootstrap.SetInputAvailable(false). The controller's existing OnDisable clears the gesture and hides its cue, while its logical selectedPosition remains. Cancel re-enables through the same Bootstrap gate; OnEnable re-derives the cue and the existing same-input-update handoff guard requires a fresh board press. No new pointer listener, stamp, polling loop or controller code was needed.

The HUD adds a gameplayMenuButton forwarding the existing Menu callback. `SetGameplayActionsAvailable(bool)` is a separate flow permission used while a prompt is open. RefreshActionAvailability combines that with the existing busy/status inputs for Restart and gameplay Menu. Restoring the flow permission cannot override busy or terminal state. Outcome buttons keep the original busy gate. Bootstrap remains the only production writer of BoardInputController.enabled; neither Bootstrap nor input-controller source changed in this step.

Opening, cancelling or confirming does not save progression. Restart recreates the same configured level with new Board/progress/snapshot/RNG objects. Leave retains the old session behind the menu. Core, scoring, match rules, targeting, refill and animation timing are unchanged. Android Back is not implemented here; it remains Step 12.

## Bottom controls and camera boundary

Menu and Restart now share a 640×128 BottomControls row, bottom-centred at (0,48). Each button stays 300×128, at local x −170 (Menu) or +170 (Restart), y 0. The gap is 40. Existing button art/font/colours are reused; Navigation is None.

SafeAreaLayout now exposes BottomControls instead of RestartButton. The serialized field is renamed with `[FormerlySerializedAs("restartButton")]`, and the scene reference is explicitly assigned to the new row. This preserves serialization while giving the camera an accurately named boundary. BoardCameraFitter reads the row's actual top edge. The pure framing method's parameter/error wording was renamed accordingly; its calculations did not change.

The row has the same top edge as the old Restart: safe-area bottom + 48 + 128. Therefore the available board rectangle and camera framing stay unchanged. BoardRoot position/scale, cell spacing, geometry and colliders are untouched. No additional inset calculation or per-frame work was added.

## Unity hierarchy, layering and manual steps

```text
Canvas (sorting order 0)
├── MainMenuBackground
├── SafeArea
│   ├── LevelHud
│   │   ├── TopBar
│   │   ├── BottomControls
│   │   │   ├── RestartButton
│   │   │   └── MenuButton
│   │   └── OutcomePanel
│   ├── MenuControls
│   └── ConfirmationCard (Canvas override sorting 1 + GraphicRaycaster)
│       ├── Title
│       ├── Message
│       ├── CancelButton
│       └── ConfirmButton
└── ConfirmationDimmer (full-screen Image + ConfirmationView)
```

1. Under LevelHud create BottomControls with a RectTransform: anchors/pivot bottom-centre, position (0,48), size 640×128. Reparent RestartButton, set bottom-centre anchors/pivot, position (170,0), size 300×128. Duplicate it as MenuButton at (−170,0) and set its TMP label to Menu.
2. Assign the new button to LevelHudView.GameplayMenuButton and the row to SafeAreaLayout.BottomControls. Leave Inspector On Click lists empty; code registers the listeners.
3. Duplicate the existing outcome Card directly under SafeArea as ConfirmationCard. Keep its centred 640×400 rect and opaque navy Image. Keep the title at the top; replace the score content with one message: Your progress in this level will be lost. Remove the extra score text.
4. Use a 600×80 title with auto-size 32–60. The message stretches horizontally with 40-unit side insets, top pivot, y −132 and height 96; auto-size 28–36. It reuses the HUD font. Cancel uses muted blue, Confirm keeps the existing orange. Each is 268×84 at x ±146, y 40 with bottom-centre anchors/pivot: 24 between them, 40 to each card side.
5. Under Canvas create ConfirmationDimmer as a full-stretch Image with zero offsets, black alpha 0.55 and Raycast Target on. Put it last in Canvas sibling order, so it dims the existing HUD and the full screen, including inset strips.
6. Add Canvas to ConfirmationCard, set Screen Space Overlay / Override Sorting on / Sorting Order 1, then add Graphic Raycaster. This draws the safe-area card above the full-screen dimmer and allows its buttons to receive events. Save and reload the scene to verify the override persists; tests exercise actual button touches too.
7. Add ConfirmationView to the dimmer. Assign the card, title, message, confirm label and both buttons. Assign that view to GameFlow. Save both the dimmer and card inactive. Initialize stores the callbacks; showing the prompt first activates its objects and installs Awake listeners once.
8. In Play Mode, choose a tile, open either prompt, and Cancel: the same tile must still be logically selected and its cue must return. Confirm Restart should reset that level; Confirm Leave should show the menu. No gameplay object is deactivated to accomplish this.

The additional Canvas/GraphicRaycaster solve this specific drawing/input order. They are not a dialog framework. No new sprite/font asset or package was added. The existing outcome dimmer is untouched: OutcomeDimSafeArea remains deferred, even though this new confirmation dimmer correctly covers the whole screen.

## Validation

- Focused view/HUD/layout/scene-touch suite: **58/58** passed.
- Full EditMode: **632/632** passed.
- Full PlayMode: **320 passed, 3 explicit hardware-timing skips, 0 failed** (24 additional passing cases from the 296 baseline).
- Added cases: 10 ConfirmationView cases, nine flow/confirmation cases, two HUD cases, and three net additional scene cases (the old immediate Restart scene test became four intent × decision cases).
- View tests cover both wordings, show/hide scope, independent replacement callbacks and startup validation. Flow tests cover both intents, exact Cancel preservation including selection cue, fresh same-level Restart, retained-session Leave, repeated decisions, refusal while busy and four mouse/touch same-update Cancel handoff cases. Existing session-replacement handoff cases now confirm Restart through the prompt.
- Native scene tests queue touchscreen events through Update and EventSystem for Restart/Leave × Cancel/Confirm. They check action-on-release, board taps behind the dimmer being ignored, safe-area containment/full-screen coverage, selection restoration, no leaked tap on restarted input, and immediate outcome actions remaining valid.
- HUD tests prove the prompt gate never overrides busy/terminal state. Camera/scene tests now check both buttons lie inside the measured bottom row and below the board, and that corner/border picking still works.
- An initial Editor wiring mistake saved the card Canvas without Override Sorting; the dimmer intercepted the four native decision touches. The scene setting was corrected through SerializedObject, saved/reloaded and verified before the passing focused/full suites. No runtime workaround was added.

## Visual review and limits

Both intents were measured after the graphics/layout rebuild in all five cases below. Card/buttons stayed inside their intended bounds, no TMP text overflowed, buttons did not overlap, full-screen dim coverage held, and UI raycasts reached the decision buttons before the dimmer. Bottom row side margins and board/control separation held throughout.

| Screen / safe area | Card bounds in screen pixels (x, y, width, height) | Bottom row bounds |
| --- | --- | --- |
| 1080×1920 full | 220, 760, 640, 400 | 220, 48, 640, 128 |
| 1080×1200 full | 220, 400, 640, 400 | 220, 48, 640, 128 |
| 720×1280 full | 146.67, 506.67, 426.67, 266.67 | 146.67, 32, 426.67, 85.33 |
| 1080×2340 full | 220, 970, 640, 400 | 220, 48, 640, 128 |
| 1080×2340; left 72 / right 24 / top 96 / bottom 48 inset | 244, 946, 640, 400 | 244, 96, 640, 128 |

The small Restart and asymmetric Leave screenshots were visually inspected: titles, warning and actions are readable; the new full-screen dimmer leaves no bright inset strips. Insets are simulated, not device evidence. Preview opening/cancelling used the real flow APIs; one programmatic tap selected (0,0), which remained selected afterward. No moves were played (20 moves, score 0); production progression stayed present with value 2 before and after. No APK build or S23 review was performed. Physical readability/reachability remains Step 15.

Ignored local evidence: `Builds/Validation/Phase16Step11/layout-review.json` (10 settled measurements); `restart-1080x1920.png`, `leave-1080x1920.png`, `leave-1080x1200.png`, `restart-720x1280.png`, `leave-asymmetric.png`. An initial preview probe queried raycasts in the same call that activated the card, before graphic depths rebuilt; those immediate results were discarded and replaced with the settled checks. No code change was needed for that probe timing.

## Changed files and final state

Modified production files: SampleScene, GameFlowController, LevelHudView, SafeAreaLayout, BoardCameraFitter and BoardCameraFraming. New production file: ConfirmationView.cs plus Unity metadata.

Modified test files: GameFlowControllerTests, LevelHudViewTests, SafeAreaLayoutTests, SceneTouchInputTests and TestHud. New test/helper files: ConfirmationViewTests, GameFlowConfirmationTests and TestConfirmation, each with metadata. Docs: this record and RestartConfirmation.md (implementation approved, device check pending). No Core, Bootstrap, board-input, prefab, catalog, package or final project-setting changes.

SampleScene is saved, active and not dirty, outside Play Mode; compilation is idle. StandaloneWindows64 remains active. Phone Game view is restored to index 8, 1080×1920. SafeAreaLayout is enabled; prompt card/dimmer are saved inactive with the Canvas override retained. Run In Background is restored off in runtime/project settings and saved to disk. Only native Keyboard/Mouse devices remain. Final Console error/warning query is empty; test runs emitted the existing expected negative-test exceptions/camera warning, and one MCP WebSocket reconnect warning occurred during reload.

Scene empty-name whitespace was cleaned while the saved scene was unloaded; existing formatting was preserved. The combined change passes `git diff --check`. Commits were requested after review; nothing pushed. Step 12 has not started.
