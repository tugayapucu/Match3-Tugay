# Art Provenance

## Phase 11 — Prismatic Energy tile art

Custom AI-assisted artwork created specifically for this project from an original visual direction ("Prismatic Energy"). It is not copied from Royal Match or any other game.

Imported in Phase 11 Step 1 (2026-10-21) from an asset package prepared outside the repository. Files were copied byte-for-byte (verified by SHA-256); no edits were made during import.

Post-import cleanup (Phase 11, after Step 2): two crops contained visible fragments of neighbouring icons from the source sheet, which showed on the board. Those detached islands were erased (made fully transparent); every other pixel is unchanged, verified by comparison:

- `Normal/Green.png` — one 1,332 px fragment at the bottom-left removed;
- `Specials/BeamCrystal_V.png` — two fragments (841 px and 72 px) along the top edge removed.

These two files therefore differ from the external staging package; the repository copies are now the source of truth.

Step 3 readability fix: `Normal/Green.png` was visibly smaller and off-center on the board. Its visible body (alpha ≥ 128) measured 383 × 379 px centered at (275, 241), while the other five gems have a median of 421 × 420 px centered at (255, 254.5). The green shape was uniformly scaled by about 1.10 (bicubic on premultiplied alpha, so edges get no fringes) and re-centered; it now measures 422 × 418 px at (255.5, 254.5). The canvas stays 512 × 512 and the `.meta` file is unchanged. Invisible alpha-1 specks around the shape were dropped in the same pass.

Beam Crystal thickness fix (after Step 7b): both Beam Crystals read as a thin sliver at board size. Their opaque area (alpha ≥ 128) covered about 20% of the canvas (H 426 × 209 px, V 160 × 409 px), against about 60% for the gems. Each body was stretched 1.45× across its beam only, around the canvas centre (bilinear on premultiplied alpha), so its length is unchanged; `BeamCrystal_V.png` was also moved by whole pixels (−3, −8) to sit centred. They now measure H 426 × 303 px and V 232 × 409 px, centred at (255.5, 255–256), and cover about 29% each, close to the Wisp (28%) and still clearly directional. The outline is about 1.45× thicker across the beam, which is invisible at board size. The canvas, `.meta` files, import settings, prefab, collider and code are unchanged. Checked beside all gems and specials at 32, 48 and 64 px cells, and with the row/column effect feedback at 60 fps.

Purple spot cleanup (2026-10-25): a targeted image edit removed the round white blotches in the upper-left facet and cleaned stray edge debris while retaining the purple gem's faceted appearance and bright edge highlights. The built-in image generation tool produced the edit; its output was downsampled to the existing 512 × 512 transparent RGBA canvas. The existing `.meta`, sprite references, PPU, prefab, collider and gameplay code are unchanged.

### Runtime sprites

`Assets/Art/Tiles/PrismaticEnergy/`

| File | Used for (Presentation only) |
| --- | --- |
| `Normal/Red.png` | `TileType.Red` |
| `Normal/Green.png` | `TileType.Green` |
| `Normal/Blue.png` | `TileType.Blue` |
| `Normal/Yellow.png` | `TileType.Yellow` |
| `Normal/Purple.png` | `TileType.Purple` |
| `Normal/Orange.png` | `TileType.Orange` |
| `Specials/BeamCrystal_H.png` | `TileType.HorizontalRocket` (Beam Crystal) |
| `Specials/BeamCrystal_V.png` | `TileType.VerticalRocket` (Beam Crystal) |
| `Specials/Wisp.png` | `TileType.Propeller` (Wisp) |
| `Specials/NovaGem.png` | `TileType.TNT` (Nova Gem) |
| `Specials/SpectrumCore.png` | `TileType.LightBall` (Spectrum Core) |

The player-facing names are design vocabulary only; Core `TileType` names are unchanged.

Source facts (checked at import):

- 512 × 512 RGBA PNG, fully transparent corners;
- content fills about 84% of the canvas (roughly 41 px margin), consistently across files;
- gem bodies use alpha 250–253 rather than 255 (a background-removal residue, not visible at runtime);
- `Wisp.png` has a few near-invisible (alpha 1) pixels around the shape.

Unity import settings:

```text
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 512   (one sprite = one 1×1 board tile)
Alpha Is Transparency: on
Generate Mip Maps: on   (changed in Step 3; see below)
Filter Mode: Bilinear
Compression: None (no platform overrides)
```

Step 1 started with mip maps off. Step 3 measured a concrete reason to enable them: sliding a tile by 1/8 of a screen pixel at 32 px cells made single screen pixels jump by 35–46% of full brightness on normal gems and 66–85% on specials (shimmer). With mip maps on, the worst jump is 12–21%, close to the ideal ~12.5% for smooth sub-pixel motion. Trade-off: still images look slightly softer at 32 px cells, while the default board size (about 57 px cells) stays sharp; memory grows by a third (about 11 MB → 15 MB for all 11 uncompressed sprites). Mip Map Bias was tried as a sharpness compromise but has no effect in this project's Direct3D 12 + URP setup, so it stays at 0.

Note on the `.meta` files: Unity first imported the PNGs in its default `Multiple` sprite mode and auto-sliced some of them (for example `Green_0`…`Green_3`, `BeamCrystal_V_0`…`_2`). Those slice entries remain in the `.meta` files but are ignored in `Single` mode: each file produces exactly one full 512 × 512 sprite. They were left in place rather than churning the main sprite's internal ID with an importer rewrite.

### Concept / reference

`Docs/Art/Concepts/Phase11-Prismatic-Energy-Concept.png` — the source asset sheet (2172 × 724) the runtime sprites were cropped from. Kept under `Docs` so Unity does not import it as a game texture.
