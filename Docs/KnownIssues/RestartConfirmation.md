# Restart confirmation

Status: Implemented and reviewer-approved in Phase 16 Step 11 on 2026-11-10;
automated and Editor validation passed. The planned S23 check remains pending.

Restart was easy to reach on the S23, and an accidental press would immediately
discard the current session's progress. The developer reported this concern during the
human comfort review; an actual accidental-restart incident was not reported.
The HUD now forwards that request to GameFlow, which opens a Restart confirmation.

The implemented Cancel / Restart prompt gates board input through Bootstrap,
preserves the logical selection and session on Cancel, and restarts the same
level once on Confirm. It cannot open during a move. A separate Leave intent
uses the same view for the in-game Menu action. Terminal Retry / Play Again stay
immediate; no generic dialog framework was introduced.

The original deferral is recorded in
`Docs/Validation/PHASE15_STEP10_HUMAN_TOUCH_COMFORT.md`. Implementation, tests and
manual setup are recorded in `Docs/Validation/PHASE16_STEP11_CONFIRMATIONS.md`.
Recheck the physical touch experience during Phase 16's device review and revisit
this status in its final known-issue review.
