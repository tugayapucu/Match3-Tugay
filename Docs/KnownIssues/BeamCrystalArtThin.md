# Beam Crystal art is too thin

Status: Scheduled; fix after Phase 11 Step 7b and before Step 8 (HUD polish).

The Beam Crystal sprites (`BeamCrystal_H.png`, `BeamCrystal_V.png`) read as a thin sliver beside the gems. Their row/column effect is readable (Step 7a review), but the special itself is easy to miss, and its 1.2x origin pop stays small on screen.

Fix it in the source PNGs (a fuller body within the same 512x512 canvas), not by changing the tile root scale, the collider or any gameplay value. Re-check both orientations against the gems at 32, 48 and 64 px afterwards.
