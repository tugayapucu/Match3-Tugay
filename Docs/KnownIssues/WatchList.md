# Watch list (no action needed yet)

Status: Reviewed after Phase 12: unchanged; monitor and act only if the situation below changes.

- `BoardView` is the largest class (about 500 lines): render, lookup, swap, clear, fall, refill and special presentation. Its animation bodies live in small helpers (`ClearAnimation`, `SpecialEffectFeedback`); `BoardView` validates input and resolves views. `BoardView` remains the cohesive visual-board facade; extract more only if additional presentation responsibilities accumulate.
- The "duration must be finite and non-negative" check exists in `BoardView`, `BoardResolutionRunner` and `BoardInputController`. Small and harmless; merge only if a fourth copy appears.
- `ScriptedRandom` exists in both test assemblies (EditMode and PlayMode cannot share code without an extra shared test assembly). Acceptable while it stays this small.
