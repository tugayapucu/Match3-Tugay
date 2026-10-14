# Watch list (no action needed yet)

Status: Monitor; act only if the situation below changes.

- `BoardView` is the largest class (about 370 lines): render, lookup, swap, clear, fall and refill visuals. Still one cohesive job ("the visual board"). If special tiles (Phase 10) add more animations, extract an animation helper then.
- The "duration must be finite and non-negative" check exists in `BoardView`, `BoardResolutionRunner` and `BoardInputController`. Small and harmless; merge only if a fourth copy appears.
- `ScriptedRandom` exists in both test assemblies (EditMode and PlayMode cannot share code without an extra shared test assembly). Acceptable while it stays this small.
