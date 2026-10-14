# Decision: Board interaction state (Phase 8)

Status: Accepted. Keep the current design.

## Context

After Phase 7, `BoardInputController` represents the interaction phase with one `bool isInputLocked` plus coroutine sequencing (`FinishSwap` → forward swap → rollback **or** `ResolveUntilStable`). The Phase 8 audit found:

- three interaction situations: ready, one tile selected, move running;
- one lock point and one asynchronous exit point, all inside `BoardInputController`;
- no production code that needs to know *why* input is locked (swapping vs resolving);
- clear / fall / refill are private steps of `BoardResolutionRunner`;
- win / lose does not exist yet.

The audit kept four concerns separate: interaction phase (`isInputLocked`), selection (`GridPosition? selectedPosition`), resolution substeps (private to the runner), and future level outcome (Phase 9).

## Options

| Question | A. Current bool | B. Simple enum | C. State classes |
| --- | --- | --- | --- |
| Shows why input is blocked | No (nothing needs it) | Yes | Yes |
| New types | 0 | 1 enum | ~4 (interface + 3 states) |
| Transition clarity | One class, one exit | Same points, named | Spread across classes |
| Testing benefit | Behaviour already tested | Marginal (a label) | None; more setup |
| Boilerplate | None | Very small | Significant |
| Helps current requirements | Fully meets them | No new capability | No new capability |
| Justified only by future needs | No | Partly | Yes |

## Decision

**A. Keep `isInputLocked` + coroutine sequencing.** A yes/no question ("is a move running?") needs a yes/no value. An enum would mostly rename information nothing reads; state classes would mainly wrap two existing coroutine calls.

## Revisit when

- A real consumer needs to know *why* input is blocked (UI showing "resolving", hints waiting for a stable board, Phase 9 logic that differs during resolution) → introduce a minimal enum (`Idle / Swapping / Resolving`) owned by `BoardInputController`, with `IsInputLocked` derived from it.
- Each state gains substantial behaviour of its own, or `switch`es on state spread across classes → reconsider state classes.

Not decided here (separate concerns): level outcome (win / lose) in Phase 9, the dead-board policy (`Docs/KnownIssues/DeadBoard.md`), and the failure policy when a move throws (`Docs/KnownIssues/LockedAfterException.md`).
