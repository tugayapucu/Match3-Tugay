# Art Provenance

## Phase 11 — Prismatic Energy tile art

Custom AI-assisted artwork created specifically for this project from an original visual direction ("Prismatic Energy"). It is not copied from Royal Match or any other game.

Imported in Phase 11 Step 1 (2026-10-21) from an asset package prepared outside the repository. Files were copied byte-for-byte (verified by SHA-256); no edits were made during import.

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
Generate Mip Maps: off
Filter Mode: Bilinear
Compression: None (no platform overrides)
```

These are the initial Phase 11 settings; Step 3 may adjust them only for a concrete runtime reason.

Note on the `.meta` files: Unity first imported the PNGs in its default `Multiple` sprite mode and auto-sliced some of them (for example `Green_0`…`Green_3`, `BeamCrystal_V_0`…`_2`). Those slice entries remain in the `.meta` files but are ignored in `Single` mode: each file produces exactly one full 512 × 512 sprite. They were left in place rather than churning the main sprite's internal ID with an importer rewrite.

### Concept / reference

`Docs/Art/Concepts/Phase11-Prismatic-Energy-Concept.png` — the source asset sheet (2172 × 724) the runtime sprites were cropped from. Kept under `Docs` so Unity does not import it as a game texture.
