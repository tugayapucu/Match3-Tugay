# Outcome dimmer stops at the safe area

Status: Resolved in Phase 18 Step 1 (2026-11-22), verified in the Editor and by tests. Physical S23 confirmation is pending in the Phase 18 Step 5 device batch (Internal code 5); store screenshot 03 still shows the old strip until the Phase 21 listing refresh.

On Won or Lost, the dark overlay leaves undimmed strips outside the safe area. This is visible with simulated notch, bottom-gesture and asymmetric insets. The outcome card and Play Again remain centered, readable and reachable inside the safe area. Phase 12 Step 10 also confirmed the undimmed top cutout strip in Win/Loss screenshots on a physical Samsung Galaxy S23 (Android 16, 1080×2340). Other devices and inset shapes remain unverified physically.

Cause: `OutcomePanel` contains the dimming Image and stretches to fill `LevelHud`, which fills `SafeArea`. Its current anchors and zero offsets make the overlay cover only that region; parenting itself does not clip UI.

Decision: record the limitation and leave the layout unchanged for Step 8. Revisit in a focused UI polish pass, informed by device review.

Possible fix: separate the dimming Image into a full-screen backdrop under the Canvas while keeping the card inside SafeArea. Show and hide both together through the existing outcome presentation path. Verify draw order, UI raycasts, both outcomes and Play Again; keep gameplay rules and board framing unchanged.

Phase 16 update (2026-11-12): still deferred; the S23 Step 15 screenshots show the same top strip on Won and Lost. The Phase 16 confirmation prompt already uses this split (full-screen `ConfirmationDimmer` under the Canvas, `ConfirmationCard` under SafeArea with its own sorting Canvas) and covers the whole S23 screen. It is the in-repo reference if this is fixed.

Phase 17 update (2026-11-21): still deferred. The strip appears in Play Store screenshot `Docs/Art/Store/Screenshots/03-AllLevelsComplete.png` (Release C on the S23) and is deliberately left visible there rather than concealed.

Phase 18 Step 1 fix (2026-11-22): the Phase 16 confirmation pattern is reused. A new full-screen `OutcomeDimmer` (black 55%, raycasts on) sits under the root Canvas between `SafeArea` and `ConfirmationDimmer`; `OutcomePanel` keeps only its container role; its `Card` has its own Canvas (override sorting, order 1) and GraphicRaycaster, so it draws and takes touches above the dimmer while staying inside SafeArea. `LevelHudView` activates the panel and the dimmer only while the level is over **and** the HUD is visible. Deactivating (not just fading) is required: a real-raycast test showed that the hidden card's buttons still received UI raycasts on the menu, because `Graphic.Raycast` stops consulting parent CanvasGroups at a Canvas with override sorting. Editor evidence: before, a 100 / 70 / 60 / 80 px undimmed strip with asymmetric insets for Won, Lost and All Levels Complete; after, full-screen dimming with card and buttons inside SafeArea at 1080×1920, 1080×2340, 1080×2400, 720×1280, 1080×1200 and asymmetric insets, and an undimmed menu after an outcome (`Builds/Validation/Phase18Step1/`, ignored).
