# Outcome dimmer stops at the safe area

Status: Deferred; cosmetic issue confirmed during Phase 12 Step 8.

On Won or Lost, the dark overlay leaves undimmed strips outside the safe area. This is visible with simulated notch, bottom-gesture and asymmetric insets. The outcome card and Play Again remain centered, readable and reachable inside the safe area. Physical-device appearance has not been checked.

Cause: `OutcomePanel` contains the dimming Image and stretches to fill `LevelHud`, which fills `SafeArea`. Its current anchors and zero offsets make the overlay cover only that region; parenting itself does not clip UI.

Decision: record the limitation and leave the layout unchanged for Step 8. Revisit in a focused UI polish pass, informed by device review.

Possible fix: separate the dimming Image into a full-screen backdrop under the Canvas while keeping the card inside SafeArea. Show and hide both together through the existing outcome presentation path. Verify draw order, UI raycasts, both outcomes and Play Again; keep gameplay rules and board framing unchanged.
