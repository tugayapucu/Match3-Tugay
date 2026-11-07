# Phase 16 Step 6 — Initial level catalog

Date: 2026-11-07. Status: configured and machine-verified; human winnability pending. The production-settings test follow-up from review is complete.

## Catalog and scope

`Assets/Levels/InitialLevelCatalog.asset` contains six inline `LevelDefinition` entries. Zero-based catalog index is identity/order. `LevelCatalog` exposes read-only `Count`, indexed access, and structural `Validate()` (nonempty). Gameplay validation remains in the existing session construction path. There are no new Core rules, IDs, level subclasses, or per-level branches.

| Level | Seed | Moves | Goal | Target | Recorded winning route length | Moves left | Final score |
| --- | ---: | ---: | --- | ---: | ---: | ---: | ---: |
| 1 | 42 | 20 | Red | 25 | 4 | 16 | 8400 |
| 2 | 17 | 22 | Green | 22 | 16 | 6 | 14000 |
| 3 | 73 | 20 | Blue | 25 | 17 | 3 | 15000 |
| 4 | 111 | 22 | Yellow | 24 | 21 | 1 | 12900 |
| 5 | 202 | 20 | Purple | 28 | 16 | 4 | 14800 |
| 6 | 314 | 22 | Orange | 30 | 14 | 8 | 11300 |

Level 1 stays exactly seed 42 / 20 moves / Red / 25. The other entries deliberately use different seeds and all remaining goal colours, 20–22 moves, and targets in the 22–30 range. These choices provide initial content variety, not a difficulty progression. Board dimensions, palette, points, timing and framing remain global and unchanged.

The Yellow entry was initially 28. Its first route lost after 22 moves at 25/28 and score 13,200. Its target was reduced to 24 and the same route replayed: Won on move 21, 24/24 (goal display capped), 1 move left, score 12,900. The failed attempt is retained in the local evidence. One unsuccessful route does not prove the original target impossible.

The catalog is not wired into scene startup yet. The temporary `initialLevel` fallback still starts Level 1; navigation will supply catalog entries in the approved later flow step. No persistence, menu, scene, prefab, package or project-setting changes are part of Step 6.

## Automated checks

- EditMode: five new catalog cases (empty rejected, ordered six entries and Level 1 preserved, two invalid indices, default inline entry rejected by existing gameplay validation).
- Editor PlayMode: six parameterized real-bootstrap cases, one per entry: configured progress, deterministic fresh replacement, stable/playable start, goal colour present, no selection, 64 matching views. Unexpected error logs fail these tests.
- The existing Level 1 SampleScene test also compares the catalog fixture's width, height and ordered palette with the real Bootstrap's serialized settings. Reordering the production palette now fails that check; seed alone does not determine the board.
- Focused runs: 5/5 EditMode and 6/6 PlayMode passed.
- Full suites before the test-only review follow-up: EditMode 608/608 passed; PlayMode 260 passed, 3 skipped (explicit hardware-timing tests), 0 failed.
- After the review follow-up: all seven focused PlayMode cases passed (six catalog entries plus the Level 1 scene/settings check). No test cases were added; only the fixture inputs were shared and existing assertions strengthened. Compilation was clean; the focused run logged only runner housekeeping. A WebSocket reconnect warning during compilation came from MCP tooling.

The asset access in PlayMode tests is Editor-only and guarded by `UNITY_EDITOR`; these are not Android test results. No test attempts to solve a level.

## Recorded play method and limits

These are agent-directed Editor playthroughs, assisted by temporary read-only legal-move inspection and immediate match/special-creation facts. Moves were chosen interactively, not by a solver added to the project. Programmatic `HandlePointerDown`/`HandlePointerUp` pairs exercised production camera projection, Physics2D picking, controller, runner, scoring, targeting, recovery and bootstrap. They bypass native pointer polling and physical finger input. No Board tiles were injected or edited during a route, and the shipped animation durations were retained.

After every recorded settled move, all 64 active TileViews matched Board type and cell. Each of the six final configurations reached Won within its budget. That supplies concrete winning examples using ordinary accepted moves; it does not establish average success rate, human difficulty, final balance, native touch behavior, or S23 approval. Those remain separate reviews. No difficulty ordering is claimed.

Before the Phase 16 completion gate, the human developer must play all six levels once in the Editor or on the S23; the Step 15 device review is the planned checkpoint. Record the outcomes and subjective difficulty, especially for Yellow (one move left on the assisted route). Its revised target was fitted to that same route, so independent human play is still needed. These machine results do not close the plan's normal-play winnability requirement. Do not reorder levels or claim a difficulty curve from this single set of assisted routes.

Step 8 must call `LevelCatalog.Validate()` at flow startup; there is no production catalog consumer in Step 6. Keep the witness routes as documentation, not refill-sequence regression tests.

Local ignored evidence: `Builds/Validation/Phase16Step6/playthroughs.json` contains move-by-move HUD/board snapshots, legal-move inspection results, and the original Yellow failure; `level-1-won.png` through `level-6-won.png` capture outcomes. This Markdown preserves the reproducible routes even if local artifacts are absent from another clone.

## Replay routes

Coordinates are zero-based `(x,y)`, left-to-right and bottom-to-top. Each pair is first tap → second tap. Start a fresh session with the corresponding catalog definition, and wait until input unlocks after each pair. Refill and targeting streams are the unmodified Bootstrap streams derived from that level's seed.

### Level 1

```text
1. (4,0) -> (4,1)
2. (3,1) -> (3,2)
3. (1,0) -> (1,1)
4. (3,5) -> (4,5)
```

### Level 2

```text
1. (3,0) -> (3,1)
2. (3,2) -> (3,3)
3. (2,5) -> (3,5)
4. (2,3) -> (2,4)
5. (2,4) -> (3,4)
6. (1,3) -> (2,3)
7. (5,3) -> (6,3)
8. (6,1) -> (6,2)
9. (5,3) -> (5,4)
10. (0,1) -> (1,1)
11. (1,5) -> (1,6)
12. (3,0) -> (4,0)
13. (2,2) -> (2,3)
14. (0,2) -> (1,2)
15. (1,0) -> (1,1)
16. (3,2) -> (3,3)
```

### Level 3

```text
1. (4,1) -> (4,2)
2. (4,5) -> (4,6)
3. (5,4) -> (5,5)
4. (2,5) -> (3,5)
5. (4,4) -> (4,5)
6. (5,6) -> (5,7)
7. (1,3) -> (2,3)
8. (3,5) -> (3,6)
9. (5,5) -> (5,6)
10. (2,0) -> (3,0)
11. (2,1) -> (3,1)
12. (1,1) -> (2,1)
13. (3,5) -> (3,6)
14. (3,4) -> (3,5)
15. (2,5) -> (3,5)
16. (5,3) -> (5,4)
17. (3,3) -> (3,4)
```

### Level 4

```text
1. (5,0) -> (5,1)
2. (4,0) -> (4,1)
3. (2,7) -> (3,7)
4. (4,3) -> (4,4)
5. (6,6) -> (7,6)
6. (1,4) -> (1,5)
7. (1,4) -> (1,5)
8. (4,2) -> (5,2)
9. (3,2) -> (3,3)
10. (3,5) -> (3,6)
11. (3,5) -> (3,6)
12. (3,4) -> (3,5)
13. (4,3) -> (5,3)
14. (4,2) -> (5,2)
15. (5,5) -> (6,5)
16. (5,3) -> (5,4)
17. (2,3) -> (2,4)
18. (2,2) -> (2,3)
19. (3,1) -> (3,2)
20. (3,6) -> (3,7)
21. (3,5) -> (4,5)
```

### Level 5

```text
1. (4,2) -> (5,2)
2. (3,0) -> (4,0)
3. (1,6) -> (2,6)
4. (1,5) -> (1,6)
5. (5,6) -> (6,6)
6. (5,6) -> (6,6)
7. (1,3) -> (1,4)
8. (1,0) -> (1,1)
9. (1,1) -> (1,2)
10. (2,0) -> (2,1)
11. (7,5) -> (7,6)
12. (2,1) -> (2,2)
13. (1,5) -> (2,5)
14. (0,1) -> (0,2)
15. (0,0) -> (0,1)
16. (0,5) -> (1,5)
```

### Level 6

```text
1. (5,0) -> (5,1)
2. (0,5) -> (1,5)
3. (4,5) -> (4,6)
4. (4,5) -> (5,5)
5. (6,7) -> (7,7)
6. (7,1) -> (7,2)
7. (4,4) -> (5,4)
8. (5,6) -> (6,6)
9. (5,4) -> (5,5)
10. (5,3) -> (5,4)
11. (5,1) -> (5,2)
12. (0,3) -> (1,3)
13. (5,6) -> (6,6)
14. (4,3) -> (4,4)
```

## Manual authoring

1. In the Project window open `Assets/Levels`.
2. Right-click → Create → Match3 → Level Catalog; name the new asset.
3. Select the asset; set the Levels array size and fill each entry's moves, goal type, target and seed. Entry order determines level order.
4. Save the project. Do not edit this asset to store runtime progress.
5. Run the catalog content/session tests before using edited entries. Positive moves/targets and a palette goal are checked by the existing session construction path, not duplicated in the catalog.

## Final state

Compilation is clean. The full PlayMode Console contains only expected negative-test exceptions (goal colour, clear duration, highlight wiring and TNT sprite), the expected camera no-room warning, and runner/MCP housekeeping. No unexpected project errors or warnings were observed.

SampleScene is active, unmodified, and out of Play Mode; the Editor target remains StandaloneWindows64. Runtime Run In Background was restored to false and the serialized setting remains 0. Only native Keyboard and Mouse devices remain. The test runner removed its temporary scene. The only previously existing tracked file edited is `SceneTouchInputTests.cs`, for the production-settings assertions. Core, scene, prefab, settings and packages are untouched. Step 7 has not started at this checkpoint; human playthroughs remain pending before phase completion.
