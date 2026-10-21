# Dead board soft-lock

Status: Still deferred after Phase 10; most important gameplay gap.

After resolution the board is stable (no matches), but a normal-only board can still have no legal move: no adjacent swap would create a match. The player cannot continue making moves; Restart is the manual way out.

Phase 10 partly eases this: a special with an occupied orthogonally adjacent neighbor provides a valid special swap, even without a normal match. This does not solve normal-only dead boards.

Revisit with future game-flow work: detect "no legal moves" after the board becomes stable, then decide whether to shuffle or end the game. No legal-move search or shuffle was added in Phase 10.

Notes for that work:
- A legal-move search tries each adjacent swap on a copy of the Board and asks `SwapValidator`. A `Board.Clone()` in Core would then be justified (tests currently have their own copy helpers).
- Keep it separate from resolution: "stable" means no match now; "dead" means no possible move.
