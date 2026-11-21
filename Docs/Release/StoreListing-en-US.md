# Galaxplode store listing — en-US (default)

Source of truth for Play Console → Grow users → Store presence → Main store listing (Phase 17 Step 14). Copy each field exactly. Every claim was checked against shipped Release C `0.1.0 (4)`; change the text here first if the game changes.

## App name (30 max)

```text
Galaxplode
```

## Short description (80 max; 78 used)

```text
Match colorful gems, unleash cosmic powers and complete six fun puzzle levels!
```

## Full description (4000 max)

```text
Galaxplode is a casual match-3 puzzle game. Swap neighbouring gems to line up three or more of the same colour, and collect the gems each level asks for before you run out of moves.

What's in the game
• Six levels, each with its own gem goal and move limit
• Four special gems made from bigger matches:
  – Beam Crystal (four in a line) clears a full row or column
  – Wisp (a 2×2 square) clears the gems around it, then flies to another part of the board
  – Nova Gem (an L or T shape) clears a 5×5 area
  – Spectrum Core (five in a line) clears every gem of one colour
• Your level progression is saved on your device, so you can continue from your next unlocked level.
• Offline gameplay: no account, no ads and no in-app purchases

Galaxplode is an early version and is still in development.
```

## Graphics

| Asset | File |
| --- | --- |
| App icon, 512 × 512 | `Docs/Art/Store/PlayStoreIcon-512.png` |
| Feature graphic, 1024 × 500 | `Docs/Art/Store/FeatureGraphic-1024x500.png` |
| Phone screenshots, 1350 × 2400 (9:16), upload in this order | `Docs/Art/Store/Screenshots/01-SpecialGems.png`, `02-LevelStart.png`, `03-AllLevelsComplete.png`, `04-MainMenu.png` (provenance in `Docs/Art/README.md`) |

## Claim evidence

| Claim | Evidence in Release C |
| --- | --- |
| Six levels, each with a gem goal and move limit | `Assets/Levels/InitialLevelCatalog.asset`: six entries, each with `goalTileType`, `goalTarget`, `startingMoveCount` |
| Special gems and their shapes | `SpecialCreationRules`: 2×2 square → Propeller (Wisp); run of 5+ → LightBall (Spectrum Core); run of 4 → Rocket (Beam Crystal); crossing runs → TNT (Nova Gem) |
| Special gem effects | `SpecialEffects.GetFootprint`: Rocket full row/column; TNT 5×5; Propeller 3×3 plus a remote target; LightBall every gem of one colour |
| Level progression saved on the device | `ProgressionStore`: saves only the next unlocked level (or "all complete") in `PlayerPrefs` after an accepted win; no in-level session is saved. After all six levels the menu offers "Play Again" |
| Offline gameplay; no account, ads or purchases | Step 9 / Step 13 records: no account, no ads SDK, Ads/Purchasing disabled. Play needs no network (Step 12 offline check on Release B; no runtime code change in C); the Privacy Policy link opens in the browser and needs a connection |

Deliberately absent: rankings, awards, multiplayer, leaderboards, daily content, larger level counts, device-support claims, future-content promises and child-directed wording (the declared audience is 13+).
