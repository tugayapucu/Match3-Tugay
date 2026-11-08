# Phase 16 Step 9 — Main Menu

Date: 2026-11-08. Status: reviewed and approved for Step 9; outcome/in-game Menu controls remain later steps.

## Behavior and ownership

Launch now displays MATCH 3 and one progression-driven action. GameFlow still owns the label and chosen index: 0 gives Play/index 0, unfinished progress gives Continue/that index, and the completion sentinel gives Play Again/index 0 without lowering saved completion.

`MainMenuView.Initialize(Action)` stores the latest callback. Its button listener is registered once in Awake and removed in OnDestroy. `Show(label)` sets the supplied text and activates the background/controls; `Hide()` deactivates only those menu objects. The view does not choose levels or read PlayerPrefs. GameFlow supplies `() => Play()` and shows/hides the menu at initialization, Menu return and session start. No new polling method, navigation state, storage logic or package was added.

The menu's opaque Image covers the whole Canvas, independently of the safe area. Its controls are children of the existing SafeArea, with no new inset calculation. Returning to Menu keeps the old board and gameplay hierarchy active but covers them; Bootstrap gates the controller off. Starting again creates a fresh deterministic session. A Canvas image alone is not relied on to block Physics2D picking.

## Scene hierarchy and layout

```text
Canvas
├── MainMenuBackground       (full stretch; opaque navy Image + MainMenuView)
└── SafeArea                 (existing SafeAreaLayout)
    ├── LevelHud             (existing CanvasGroup)
    └── MenuControls         (full stretch, 48-unit insets, VerticalLayoutGroup)
        ├── Title            (MATCH 3)
        └── PlayButton
            └── Text (TMP)   (Play / Continue / Play Again)
```

Background is first in Canvas sibling order; SafeArea controls draw above it. Both menu objects are saved inactive so the existing HUD remains convenient to edit; runtime GameFlow shows them before player interaction. Hide disables the background and MenuControls only. BoardRoot, GameRoot, HUD layout objects and the Canvas stay active. There is no gameplay-object deactivation during a move.

Title preferred size: 800×160, bold white, font size up to 84. Button preferred size: 480×128, with the existing Restart UISprite, orange colour and button colours; label size up to 48. The centered VerticalLayoutGroup controls child sizes, uses 48 spacing and does not force expansion. Text autosizing allows narrower available widths. No new sprite/font asset or third-party artwork was introduced.

No changes to HUD visual style, board geometry, collider targets, camera fitting, Canvas Scaler, SafeAreaLayout or catalog contents. Outcome Menu actions, final outcome styling and Restart/Leave confirmation remain later steps.

## Validation

- Focused PlayMode: 12/12 passed (six view cases, five loaded-progression flow cases, one real-scene touchscreen case).
- Full EditMode: 632/632 passed.
- Full PlayMode: 288 passed, 3 explicit hardware-timing skips, 0 failed (281 previous + six view cases + one scene case).
- View tests check show/hide without deactivating the owner, replacement callbacks without duplicate listeners, null callbacks and missing wiring.
- Existing progression-parameterized flow tests now check the displayed menu label and invoke its button. Play, Continue and completed replay choose the same expected indices as before. Returning to Menu shows the correct label again.
- The real-scene test uses queued touchscreen events through Update/EventSystem, not a direct OnClick or pointer-handler call. A covered tile tap leaves selection/moves/session unchanged. The menu button acts on release, starts a fresh level and leaves no selected tile or locked gesture behind. Background bounds span the screen and its alpha is 1; the button is inside the safe area.
- Tests continue using unique preference keys and clean them up; no DeleteAll or production-key writes.

## Editor visual review

Actual Game-view dimensions were changed at runtime for these checks. Insets are simulated, not physical-device evidence. After layout, background coverage and title/button bounds were measured, and TMP reported no text overflow in every row:

| Game view / safe area | Button bounds in screen pixels (x, y, width, height) | Result |
| --- | --- | --- |
| 1080×1200, full | 300, 432, 480, 128 | Fits |
| 720×1280, full | 200, 528, 320, 85.33 | Fits |
| 1080×2340, full | 300, 1002, 480, 128 | Fits |
| 1080×2340, left 72 / right 24 / top 96 / bottom 48 inset | 324, 978, 480, 128 | Fits |
| 1080×1920, full (restored reference) | 300, 792, 480, 128 | Fits |

A retained 64-tile board was behind the menu during the previews. The 720×1280 and asymmetric screenshots were visually inspected: title/action readable, solid background to every edge, no board pixels or bright strips showing through. All three action strings were also checked in the asymmetric layout; each fit at font size 48. Those label-length previews supplied strings directly to the view without altering progression; progression-to-label behavior is covered separately by the flow tests.

Ignored screenshots: `Builds/Validation/Phase16Step9/menu-1080x1200.png`, `menu-720x1280.png`, `menu-1080x2340.png`, `menu-asymmetric.png`, `menu-1080x1920.png`. These are local artifacts, not required by another clone to run tests. No new Android build or S23 installation/review was performed.

Review note for the Step 15 device check: inspect whether a retained EventSystem selection makes the menu button's Selected tint noticeable when the menu reappears. This is an unverified cosmetic possibility, not a reproduced defect; no speculative fix was made.

## Manual Unity steps

1. Under Canvas create MainMenuBackground as a full-stretch Image with zero offsets and alpha 1. Set it before SafeArea in sibling order.
2. Under the existing SafeArea create MenuControls, full stretch with offset min (48,48) and max (-48,-48).
3. Add VerticalLayoutGroup: Middle Center, spacing 48, Control Child Size on for width/height, Force Expand off for both.
4. Add Title and PlayButton as direct children, with LayoutElements setting the preferred sizes above. Put the TMP label under the button and stretch it with horizontal 24 / vertical 8 padding. Use the existing HUD font and Restart button visuals.
5. Add MainMenuView to the background and assign MenuControls, PlayButton and its TMP label. Assign that view to GameFlow's Main Menu field. There are no Inspector OnClick callbacks to duplicate; Awake registers the listener.
6. Save the two menu objects inactive. Press Play: GameFlow shows the menu and hides the HUD. Press the menu action: a fresh level appears, with the HUD visible and menu objects inactive.
7. Run the focused tests or full suites. Resize the Game view to short/tall portrait sizes for visual checks; restore the normal Phone 1080×1920 preset afterward.

## Final state

The Phone preset was temporarily resized and restored to 1080×1920, selected index 8. Runtime Run In Background was restored to false; simulated SafeAreaLayout changes were discarded on leaving Play Mode. The production progression key was absent both before and after previews: creating a session and returning to Menu without a move did not save.

SampleScene is active, clean and outside Play Mode; compilation is idle, target StandaloneWindows64, saved HUD alpha 1 and SafeAreaLayout enabled. Only native Keyboard/Mouse devices remain. Final Console is empty. The clean scene was briefly unloaded while trimming Unity-generated empty-name whitespace, then reopened; no layout values were changed by that cleanup.

Changes: four existing files (scene, GameFlowController and two flow/scene test files) and seven new files (MainMenuView, two test/helper files, their metadata and this record). Core, prefabs, project settings, packages and catalog assets are unchanged. Step 10 has not started at this checkpoint.
