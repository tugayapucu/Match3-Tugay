# Dead board soft-lock

Status: Resolved in Phase 14. Verified in tests and in Editor PlayMode; Android device confirmation is pending (Phase 14 Step 12 was deferred because no device was available).

## Terms

- **Stable:** no match on the board now.
- **Playable:** at least one legal move exists.
- **Dead:** stable but not playable. The player has moves left but cannot make one; before Phase 14, Restart was the only way out.

A **legal move** is an orthogonally adjacent swap that `SwapValidator.Analyze`, judging the board after the swap, accepts as a `NormalMatch` or a `SpecialSwap`. Any special next to a non-empty cell therefore always gives a legal move.

## Reproduction (before the fix)

Seed 93 on the shipped level (8x8, six colours, 20 moves, Red goal 25). Ten normal moves, written first click to second click with (x,y) from the bottom-left:

```text
(5,7)-(6,7) (2,7)-(3,7) (5,6)-(5,7) (1,6)-(1,7) (0,7)-(1,7)
(3,7)-(4,7) (2,6)-(3,6) (3,6)-(3,7) (1,6)-(1,7) (4,5)-(5,5)
```

These leave a stable, normal-only board with no legal move, while the level is still Playing (10 moves left, goal 3/25, score 3,300):

```text
PGGOYYOP/BRYBGBGY/OYBPYRPY/PGRPRBGP/PGOGBOYO/OYRGGPPY/BYPRYORR/GRPBGPYR   (top row first)
```

Found by a Core simulation of the resolution rules, then confirmed through the real `BoardResolutionRunner` in Editor PlayMode via programmatic `HandlePointerDown` calls. Across the Phase 14 search, 15 of 59,664 settled boards checked were dead. That ratio is an observation from this search sample, not a measured in-play frequency.

## Detection

`BoardPlayability.HasAnyLegalMove` copies the board once, tries every adjacent pair on that private copy (swap, `SwapValidator.Analyze`, swap back) and never changes the caller's board or draws randomness. Reusing `SwapValidator` keeps one source of truth for what a legal move is.

It runs only at settled-board checkpoints:

1. **Level start and Restart:** `BoardGenerator.GeneratePlayable`.
2. **After every valid move,** once the board is stable, in `BoardBootstrap.HandleValidMoveResolved`. This runs only while the level is still Playing: a Win or Loss is never recovered.

## Recovery

- **Starting board:** `GeneratePlayable` tries seeds `seed+0` to `seed+9` and returns the first playable board. Attempt 0 is the old `Generate(seed)`, so seed 42 still starts on exactly the same board. If all ten are dead it throws; this can only happen at startup or Restart, never mid-move.
- **During play:** `DeadBoardRecovery.RecoverIfDead` does nothing and draws nothing if the board is playable. Otherwise it generates up to ten candidates, each seeded by a draw from the session's refill `Random` (the same instance the resolution runner uses), and copies the first one with no match and a legal move into the board in place (`Board.CopyTilesFrom`). If all ten fail, it falls back to a private copy of the level's validated starting board. A fallback that is itself invalid throws, because only a programming error can cause that.
- The same `Board` object is kept, so input, resolution and view all see the new tiles. `BoardView.Render` then redraws it.
- Moves, score and goal are unchanged. The input lock is still held while recovery runs, so the player never sees an unlocked dead board.
- Deterministic: the same seed and the same moves recover the same way, including after Restart. A game that never needs recovery replays exactly as before, because nothing is drawn.

## Validation

- **EditMode:**
  - `BoardPlayabilityTests` (the pinned seed-42 and seed-93 boards and small fixtures);
  - `BoardGeneratorTests` (`GeneratePlayable`: retries, determinism, budget exhaustion);
  - `DeadBoardRecoveryTests` (zero draws when playable, the seed-93 replacement, determinism, the fallback after ten dead candidates, an invalid fallback, argument checks);
  - `BoardTests` (`CopyTilesFrom`).
- **PlayMode (`BoardBootstrapTests`):**
  - **The seed-93 route through the real runner,** in three variants:
    - with the last move ending the level: Lost, and the dead board is left, because a finished level is never recovered;
    - still Playing: recovered in place before input unlocks, with the same `Board` object, views matching the board and the HUD unchanged; a further legal move and Restart then work normally;
    - replayed after Restart: the same recovery and the same next move.
  - A dead board while Playing is recovered without changing progress.
  - A dead board on a Win is not recovered.
- **Adversarial Play Mode run (Phase 14 Step 8):** 4,000 moves through the real game. At each of the 4,090 points where input unlocked, the board was checked: stable, playable and matching its views, with 0 violations.
- **Not yet done:** the S23 replay of the seed-93 route. A seed-93 build and the expected move-by-move HUD values are prepared for when a device is available.

## Notes

- Recovery replaces the board instantly, without a "no moves" message or a shuffle animation. That is presentation polish, not a correctness gap.
- `Board.CopyTilesFrom` replaced the `Board.Clone()` idea from earlier notes. It copies tiles into an existing board, which is what in-place recovery needs.
