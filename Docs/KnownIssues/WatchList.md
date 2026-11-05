# Watch list (no action needed yet)

Status: Existing maintenance items retained from the Phase 12 review; Phase 15 Step 10 adds a test-environment observation below. Monitor and act when a concrete issue appears.

- `BoardView` is the largest class (about 500 lines): render, lookup, swap, clear, fall, refill and special presentation. Its animation bodies live in small helpers (`ClearAnimation`, `SpecialEffectFeedback`); `BoardView` validates input and resolves views. `BoardView` remains the cohesive visual-board facade; extract more only if additional presentation responsibilities accumulate.
- The "duration must be finite and non-negative" check exists in `BoardView`, `BoardResolutionRunner` and `BoardInputController`. Small and harmless; merge only if a fourth copy appears.
- `ScriptedRandom` exists in both test assemblies (EditMode and PlayMode cannot share code without an extra shared test assembly). Acceptable while it stays this small.
- Interrupted PlayMode tests can leave virtual input state behind. During Phase 15 Step 10, Unity retained only a virtual Touchscreen and logged `KEYS`/`TSCR` format errors after interrupted runs; script reload did not clear that observed state, while restarting the Editor restored native Keyboard/Mouse and a clean smoke check. If this recurs, restart the Editor and verify native devices and the Console before trusting further manual input checks. This is a recovery note, not a proven SDK root cause or a production workaround. Details: `Docs/Validation/PHASE15_STEP10_HUMAN_TOUCH_COMFORT.md`.
