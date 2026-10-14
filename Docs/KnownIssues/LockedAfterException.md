# Input stays locked if resolution throws

Status: Deferred; decide during the Phase 8 game-state review.

`BoardInputController.FinishSwap` unlocks input only at its end. If an exception is thrown during a swap, rollback or resolution (for example a board/view consistency check failing in `BoardView`), the coroutine stops and `isInputLocked` stays `true` for the rest of the session.

Today this can only happen through a real bug, and failing loudly is arguably preferable to hiding it. The question is what the game should do on such a failure (stay frozen, log and unlock, show an error state). An explicit game state would be the natural owner of that decision; a bare `try/finally` unlock could leave the Board and views inconsistent.
