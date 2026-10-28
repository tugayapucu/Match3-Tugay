# Selection feedback after an invalid click

Status: Still deferred after Phase 12; more important on touch, where a tap has no visible confirmation. Keep current behavior for now.

After selecting a tile, clicking a non-adjacent tile keeps the original tile selected, as required by Phase 4. Without a selection highlight, attempting a new pair can appear unresponsive: subsequent clicks are still checked against the original tile. Clicking an occupied orthogonal neighbor of that original tile attempts a swap; selection clears after the move resolves or the invalid swap rolls back.

Phase 11 polish is complete without a selection highlight. Revisit in a future input or polish pass. Changing selection rules would require a separate decision; match validation and rollback do not resolve this feedback issue.
