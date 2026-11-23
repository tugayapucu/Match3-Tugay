# Art Provenance

## Phase 18 — Fredoka font (HUD and gameplay controls)

**Fredoka** by Milena Brandão and Hafontia, "Copyright 2016 The Fredoka Project Authors (https://github.com/hafontia/Fredoka-One)", licensed under the **SIL Open Font License 1.1** with no Reserved Font Name. The licence is kept beside the fonts as `Assets/Fonts/Fredoka/OFL.txt`.

| Item | Value |
| --- | --- |
| Official source | [google/fonts](https://github.com/google/fonts) `ofl/fredoka/`, commit `a60a77e14f28abd4ef243a1b5dfc48df0cec5205` |
| Variable source `Fredoka[wdth,wght].ttf` | SHA-256 `2ba02e68b152868aef9ba28e24b3648c7d457fe6f25c761f2c2c53fb61a73fc8` (not stored in the repository) |
| `OFL.txt` | SHA-256 `5c9e7eee5c6b25f4b05b8d53b2e470ea4962f9ced742d044a98f7d95d1375bab` (the LF file as published and as committed; a Windows checkout with `core.autocrlf` may write it with CRLF) |
| Tool | fontTools 4.68.0 (pure-Python wheel, hash-pinned in [requirements-fonts.txt](requirements-fonts.txt)), installed in a virtual environment outside the repository |
| `Fredoka-Medium.ttf` | wght 500, wdth 100 (the font's own Medium instance); SHA-256 `b35d81b4161a69d875971fbf9367bdc73ce0368cd3446fce476dd5ee4a522f81` |
| `Fredoka-SemiBold.ttf` | wght 600, wdth 100 (the font's own SemiBold instance); SHA-256 `2663f28819e41dab4dc7c62dd7526982a024ba443dd3fc87535d955cf44e4cce` |

TextMeshPro reads only a variable font's default instance, which for Fredoka is Light (300), so [GenerateFredokaStatic.py](GenerateFredokaStatic.py) cuts the two static weights. It downloads the pinned files, checks their hashes and keeps the source's timestamps, so every run is byte-identical (verified three times, once in a fresh hash-checked environment). The static cuts are Modified Versions under the OFL: they keep the copyright and licence fields in their name table and stay under the OFL. Their outlines still overlap where the variable font's do; the font assets use SDFAA, which renders from an anti-aliased bitmap, so overlaps do not create artefacts.

**TextMeshPro font assets** (`Assets/Fonts/Fredoka/Fredoka-Medium SDF.asset`, `Fredoka-SemiBold SDF.asset`): Static atlas, SDFAA (the unhinted variant, the usual choice for text drawn at many sizes; LiberationSans uses the hinted one), sampling 90, padding 9, 1024 × 1024, one non-readable Alpha8 atlas texture like LiberationSans SDF, and the same `TextMeshPro/Mobile/Distance Field` shader. Characters: ASCII 32–126 plus U+00B7 "·", added to each asset before it was switched to Static. Fallback: the existing LiberationSans SDF, which is also Static; no further fallback or localisation is planned. (LiberationSans SDF's own fallback list continues to TextMeshPro's pre-existing Dynamic `LiberationSans SDF - Fallback`, committed empty; HUD text never reaches it because every HUD character is in the Fredoka assets' own tables, which `FredokaFontAssetTests` checks without fallbacks.) The underlay material for the goal count is added in the HUD step that uses it.

**Distribution notice (release gate, 18.5):** whether the TTF files or only the generated atlases end up in the Android package is checked on the real AAB, not assumed. If font data is packaged, the copyright notice and OFL must accompany the distribution before shipping; an optional README acknowledgement can wait for Phase 23.

## Phase 17 — Galaxplode app icon and store graphics

Design **A**, chosen by the human on 2026-11-20 from three rendered candidates: the **Spectrum Core** special gem with a soft warm glow on a navy space background. It is composed only from project-owned inputs:

- `Assets/Art/Tiles/PrismaticEnergy/Specials/SpectrumCore.png` (Phase 11 Prismatic Energy art, provenance below), resized only;
- the background colours sampled from the game itself: Main Menu navy RGB (22, 34, 61) and gameplay blue RGB (49, 76, 120);
- a procedural star field with fixed seeds;
- the title text in `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` (SIL Open Font License; licence file beside the font).

No image-generation service, stock asset or other game's listing was used. Source of truth: [GenerateAppIcon.py](GenerateAppIcon.py), run from the repository root with **Pillow 11.3.0** installed outside the repository; running it twice gives byte-identical files.

| Output | Spec | Where |
| --- | --- | --- |
| Play Store icon | 512 × 512, fully opaque 32-bit PNG, full square (Google applies the mask) | `Docs/Art/Store/PlayStoreIcon-512.png` |
| Feature graphic | 1024 × 500, 24-bit PNG without alpha | `Docs/Art/Store/FeatureGraphic-1024x500.png` |
| Android adaptive background / foreground | 432 × 432 (108 dp at xxxhdpi); artwork inside the 66 dp (264 px) safe zone | generated into `Assets/Art/AppIcon/` in Release C step C3 |

The store graphics live outside `Assets/` so Unity does not import them. The listing icon and the launcher icon are separate exports of the same design, as Google treats them as separate assets. No legacy or round launcher PNGs are exported: the minimum Android version is 8.0 (API 26), where launchers use the adaptive icon, and Unity 6 marks the Legacy and Round Android icon kinds obsolete.

### Play Store screenshots (Phase 17 Step 14)

Four real captures of Release C `0.1.0 (4)`, installed from Play on the owner's Samsung Galaxy S23 (1080 × 2340) on 2026-11-21, taken with `adb exec-out screencap`. Gameplay was played on the device (guarded one-move-at-a-time input; every special gem shown was created by real matches). Nothing was drawn, composited or retouched.

[PadStoreScreenshot.py](PadStoreScreenshot.py) turns each capture into **1350 × 2400 (exactly 9:16)**: the capture is pasted unchanged at (135, 30) and only a border is added by extending its own edge pixels. As a heuristic it refuses an image whose edges contain short colour runs; this cannot guarantee that no UI touches an edge, so each output was also inspected visually. No scaling or cropping, so every game pixel is identical to the capture (verified for all four).

| Order | File | Shows | Raw capture SHA-256 (prefix) |
| --- | --- | --- | --- |
| 1 | `Store/Screenshots/01-SpecialGems.png` | Level 6 mid-play, 29/30: Beam Crystals and Wisps on the board | `44e5a281ad2d1750` |
| 2 | `Store/Screenshots/02-LevelStart.png` | Level 6 at the start, 0/30 | `00b054d5ab3b642f` |
| 3 | `Store/Screenshots/03-AllLevelsComplete.png` | Final result card after Level 6 (known undimmed top strip, `Docs/KnownIssues/OutcomeDimSafeArea.md`) | `5a0e20fa726597c7` |
| 4 | `Store/Screenshots/04-MainMenu.png` | Main Menu with the GALAXPLODE title | `91fd2a85ca1d7ddd` |

Raw captures stay ignored under `Builds/Validation/Phase17ReleaseC/`.

Play AI asset declaration (human, 2026-11-21): the Play icon, the feature graphic and all four screenshots are labelled AI-generated in the listing, because they show or are built from the AI-assisted Phase 11 tile art. The composition and captures themselves involve no image generation.

## Phase 15 — Selection highlight

`Assets/Art/Tiles/PrismaticEnergy/SelectionHighlight.png` is original geometric artwork made for this project. It uses no reference images, external artwork or third-party assets. Its reproducible source is [GenerateSelectionHighlight.py](GenerateSelectionHighlight.py), using only Python's standard library; run `python Docs/Art/GenerateSelectionHighlight.py` from the repository root.

The 512×512 RGBA canvas is transparent except for four mirrored L-shaped corner brackets. Each arm is 90 px long with a 22 px overall stroke (4.30% of a cell), comprising a 14 px white core and a 4 px dark border on each side (RGB 24, 29, 39). The 10 px outer inset gives a 492×492 px visible extent: 0.9609375 world units at 512 PPU. The generator writes lossless PNG data directly; no image generation service or reference sheet is involved.

Import: Sprite / Single, centred pivot, Full Rect mesh, 512 PPU, alpha transparency on, bilinear filtering, mipmaps on, no default compression. Android explicitly uses uncompressed RGBA32 at a maximum size of 512. `Tile.prefab` displays it through an inactive-by-default child SpriteRenderer on the tile's sorting layer at order 1; the normal renderer stays at order 0. It has no collider or script and selection never changes the tile root's scale or colour.

Visual acceptance remains provisional: Editor review covers production tiles at 32, 48, 64 and 120 px per cell; physical S23 readability and touch approval belong to the later device steps. This asset does not establish final device approval.

Step 8 review noted that Nova Gem's permanent gold corner brackets resemble the selection cue. Keep the approved artwork for now; explicitly compare selected and unselected Nova Gems during the Step 10 human S23 review, checking whether an unselected Nova reads as selected and whether the two bracket sets are confusing. Reconsider the cue shape only if that review identifies a readability problem.

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
