# Mobile layout and tile picking

Status: Deferred to the Phase 12 mobile pass.

- Layout: the 8x8 board is about 8 world units wide and fits landscape. With the fixed orthographic camera (size 5), a portrait phone clips the board at the sides. Needs camera sizing or board scaling per aspect ratio.
- Picking: tiles are picked with a `Physics2D` raycast, so every tile carries a `BoxCollider2D` (64 colliders). A screen-to-grid calculation using `BoardRoot`'s transform could replace the colliders; `PLAN.md` asks to avoid unnecessary physics.
- Physics sync timing (Phase 12 Step 1 review): `Physics2D.autoSyncTransforms` is off and the fixed timestep is 20 ms. A scripted tap immediately after refill reproduced a missed selection; the touch-test helper now calls `Physics2D.SyncTransforms()` before picking, matching the existing `Click` helper. During Step 5, measure whether real input between animation completion and the next physics sync can miss or hit stale collider positions, then decide whether a single sync when the board settles is needed. Production picking is unchanged in Step 1.

These remain open mobile-pass review items.
