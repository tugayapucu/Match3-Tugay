# Dead board soft-lock

Status: Deferred by design (out of Phase 7 scope); most important gameplay gap.

After resolution the board is stable (no matches), but it can still have no legal move: no adjacent swap would create a match. The player then cannot do anything and the game silently stalls.

Revisit with the game-flow work (Phase 8 state review / Phase 9 level loop): detect "no legal moves" after the board becomes stable, then shuffle or end the game.

Notes for that work:
- A legal-move search tries each adjacent swap on a copy of the Board and asks `SwapValidator`. A `Board.Clone()` in Core would then be justified (tests currently have their own copy helpers).
- Keep it separate from resolution: "stable" means no match now; "dead" means no possible move.
