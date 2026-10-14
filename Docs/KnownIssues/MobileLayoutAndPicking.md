# Mobile layout and tile picking

Status: Deferred to the Phase 12 mobile pass.

- Layout: the 8x8 board is about 8 world units wide and fits landscape. With the fixed orthographic camera (size 5), a portrait phone clips the board at the sides. Needs camera sizing or board scaling per aspect ratio.
- Picking: tiles are picked with a `Physics2D` raycast, so every tile carries a `BoxCollider2D` (64 colliders). A screen-to-grid calculation using `BoardRoot`'s transform could replace the colliders; `PLAN.md` asks to avoid unnecessary physics.

Neither affects correctness today.
