# Selection feedback after an invalid click

Status: Deferred; keep current behavior for now.

After selecting a tile, clicking a non-adjacent tile keeps the original tile selected, as required by Phase 4. Without a selection highlight, attempting a new pair can appear unresponsive: subsequent clicks are still checked against the original tile. Clicking a neighbor of that original tile swaps successfully and clears the selection after animation.

Revisit during polish: consider a visual selection highlight. Changing selection rules would require a separate decision; match validation and rollback will not resolve this feedback issue automatically.
