# Restart has no confirmation

Status: Deferred by the developer during Phase 15 Step 10 on 2026-11-06.
Schedule a later game-flow/UI step; no implementation phase has been selected.

Restart is easy to reach on the S23, but an accidental press immediately discards
the current session's progress. The developer reported this concern during the
human comfort review; an actual accidental-restart incident was not reported.
`LevelHudView.HandleRestartClicked` directly invokes the restart callback.

The agreed direction is a small Cancel / Restart confirmation during play.
Cancel must preserve the session; Confirm must call the existing restart path
once. Block board input while the confirmation is open and preserve the existing
busy gate. Keep Play Again after a terminal outcome immediate. Do not introduce
a generic dialog framework merely for this prompt.

The developer explicitly chose to defer this work. The existing Restart behavior
is unchanged. Revisit before adding menus/game-flow UI or declaring release
usability complete. See `Docs/Validation/PHASE15_STEP10_HUMAN_TOUCH_COMFORT.md`.
