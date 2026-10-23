# Beam Crystal art is too thin

Status: Resolved and validated (2026-10-24, after Phase 11 Step 7b, before Step 8).

The Beam Crystal sprites (`BeamCrystal_H.png`, `BeamCrystal_V.png`) read as a thin sliver beside the gems. Their row/column effect is readable (Step 7a review), but the special itself is easy to miss, and its 1.2x origin pop stays small on screen.

Fix it in the source PNGs (a fuller body within the same 512x512 canvas), not by changing the tile root scale, the collider or any gameplay value. Re-check both orientations against the gems at 32, 48 and 64 px afterwards.

Resolution: each body was stretched 1.45x across its beam (length unchanged), and the vertical one was recentred. Canvas coverage went from about 20% to about 29% (close to the Wisp). Only the two PNGs changed. Both orientations were checked against all gems and specials at 32, 48 and 64 px, and with the row/column feedback at 60 fps; tests pass. Details are in `Docs/Art/README.md`.
