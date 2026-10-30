# Phase 14 Execution Plan — Gameplay Robustness & Adversarial Testing

Phase 14 closes the remaining gameplay robustness gap from Phase 13 before mobile polish begins.

Primary findings carried forward:

```text
DeadBoard
→ MUST FIX BEFORE STORE
→ missing safeguard is certain
→ actual occurrence frequency not yet measured

LockedAfterException
→ SHOULD FIX / investigate
→ consequence is severe
→ no normal-play trigger currently known
→ escalate to MUST if Phase 14 finds a production-reachable trigger
```

Core remains authoritative. Presentation sequences and displays already-decided gameplay facts.

Do not turn this phase into a generic fault-tolerance, state-machine, hint, shuffle-animation or level-generation project.

---

# Goals

By the end of Phase 14:

- every stable Playing board that is waiting for input has at least one legal move;
- a newly generated board is both stable and playable;
- runtime dead-board recovery is deterministic;
- recovery does not change moves, score or goals;
- Win/Loss does not trigger unnecessary recovery;
- restart determinism still works;
- LockedAfterException has an evidence-based final decision;
- adversarial testing challenges the important gameplay boundaries;
- no confirmed MUST gameplay blocker remains open.

---

# Architecture Rules

## Stable vs playable

```text
Stable
= no current match exists.

Playable
= at least one legal player move exists.

Dead
= stable but not playable.
```

Do not redefine “stable” to mean “playable”.

## Legal-move detection

Do not duplicate Match-3 rules.

A candidate move should use the real rule:

```text
adjacent pair
→ apply swap on scratch Board
→ SwapValidator.Analyze(...)
→ legal when result != Invalid
```

This automatically respects:

- normal matches;
- special swaps;
- current/future `SwapValidator` semantics.

The authoritative Board must not be mutated by the query.

Copy strategy:

```text
copy the Board contents into one scratch Board once
→ for each candidate pair: swap → SwapValidator.Analyze(...) → swap back
```

`Analyze` reads the board after the swap, so the swap must be applied to the scratch copy first.

`Board.SetTile` is already public, so the copy can be built from `Width`, `Height`, `GetTile` and `SetTile` without changing `Board`.

Do not add `Board.Clone()` unless a second concrete consumer later justifies it.

## Specials and playability

Current-game fact: on the current full 8×8 board, a special tile with an occupied orthogonal neighbour guarantees a legal `SpecialSwap` (`SwapValidator` returns `SpecialSwap` for any special swapped with a non-empty adjacent cell).

Therefore dead stable states in the current game are expected to be **normal-only boards**.

Do not build that assumption into the generic Core API. A special with no adjacent cell (for example on a 1×1 board) or with only empty neighbours has no legal swap.

`HasAnyLegalMove` stays generic and based on actual adjacency plus `SwapValidator`. An early return for `special + occupied orthogonal neighbour` is acceptable only if it keeps exactly those semantics.

## Board identity

The authoritative `Board` instance is shared by `BoardBootstrap`, `BoardInputController`, `BoardResolutionRunner` and `BoardView`.

Any recovery must mutate that existing instance in place and then re-render it.

Never replace the shared `Board` object during a session.

## Determinism

Do not use `UnityEngine.Random`.

Any recovery randomness must be:

- caller-owned;
- reproducible;
- reset predictably on Restart.

Checking playability must consume **zero randomness** when the board is already playable.

When no recovery occurs, existing seed-42 behavior must remain byte-for-byte unchanged. That includes every existing deterministic test and the Phase 13 S23 9-move Win route (11 moves left, goal 25/25, score 8,300).

If recovery consumes randomness, document which stream it consumes. Subsequent randomness may change only after an actual recovery.

## Recovery failure policy

Dead-board recovery runs inside the locked move window. If it throws there, it recreates the LockedAfterException freeze.

Therefore:

- retries are bounded;
- retry exhaustion during normal runtime recovery must not intentionally throw and leave the move locked;
- a deterministic safe fallback must exist;
- genuine programming or invariant failures stay visible and are not broadly swallowed;
- "fail loudly" is reserved for impossible configuration, validated at startup or restart, where it cannot freeze a move.

## Exception policy

Do not use:

```text
catch everything
→ unlock
→ keep playing
```

An exception may leave Board and BoardView inconsistent.

Prefer:

```text
surface the error
→ do not continue uncertain state
→ allow safe Restart/rebuild
```

only if the evidence justifies implementing recovery.

---

# Explicit Non-Goals

Do not add:

- hints;
- hint timers;
- shuffle spectacle/animation framework;
- swipe;
- selection feedback;
- menus;
- level progression;
- persistence;
- new specials;
- combo redesign;
- generic exception middleware;
- global error handlers;
- generic state-machine framework;
- third-party testing/fuzzing packages;
- unrelated refactors.

---

# Step 1 — Phase Entry Gate

## Goal

Confirm a clean Phase 14 baseline.

Inspect:

- `git status`;
- current HEAD;
- `PHASE13_RELEASE_READINESS_AUDIT.md`;
- PLAN Phase 14 only;
- `AGENTS.md`;
- `DeadBoard.md`;
- `LockedAfterException.md`;
- `Board`;
- `BoardGenerator`;
- `SwapValidator`;
- `MatchFinder`;
- `BoardResolution`;
- `BoardResolutionRunner`;
- `BoardInputController`;
- `BoardBootstrap`;
- relevant tests.

Record:

- current test baseline;
- known negative-test Console output;
- current seed / RNG ownership;
- whether the tree is clean;
- the Phase 13 baseline: audit commit `fbe596a`, followed by the Phase 14 plan commit `987a822`. Record whether local `main` is ahead of `origin/main` at the time of the check.

Do not rerun the full suites merely to re-prove unchanged Phase 13 code.

STOP if unrelated working-tree changes exist.

---

# Step 2 — Define Legal-Move Semantics

## Goal

Agree on the API and algorithm before production edits.

A legal move is an orthogonally adjacent swap where:

```text
SwapValidator.Analyze(...)
```

returns:

```text
NormalMatch
or
SpecialSwap
```

rather than `Invalid`.

Check each adjacent pair once:

```text
for every cell
→ test right neighbour
→ test upper neighbour
```

Preferred minimal API:

```csharp
public static bool HasAnyLegalMove(Board board)
```

Do not add a hint API unless a real consumer requires the actual move.

## Copy strategy

Use one Core-only scratch Board per query:

```text
copy contents once
→ for each candidate pair: swap → Analyze → swap back
```

Do not copy the board once per pair.

Do not add `Board.Clone()` unless a second concrete consumer justifies it. If one is added later, test:

- dimensions copied;
- every tile copied;
- clone mutation does not affect original;
- original mutation does not affect clone.

The legal-move query must leave the input Board unchanged.

## Test cases to define

At minimum:

1. null Board;
2. one known legal normal swap;
3. several legal swaps;
4. known dead normal-only board;
5. special swap makes board playable;
6. original Board unchanged after query;
7. narrow/small boards if Core allows them;
8. repeat calls return the same answer;
9. a special with no occupied orthogonal neighbour does not by itself make the board playable (for example a 1×1 board).

STOP for review if the API differs materially from this.

---

# Step 3 — Find Concrete Dead Boards

## Goal

Turn the theoretical mechanism into concrete reproducible fixtures and measure how it appears.

This is evidence discovery, not a severity decision.

## Initial-board search

Using Core only:

```text
for a deterministic seed range
→ BoardGenerator.Generate(...)
→ confirm no initial match
→ check HasAnyLegalMove(...)
```

If a dead generated seed is found, record:

- seed;
- dimensions;
- palette;
- exact board.

If none is found in a practical range:

- report the searched range;
- keep a hand-authored dead-board fixture;
- do not claim generation can never produce one.

## Post-resolution search

Use deterministic Core simulation:

```text
playable board
→ legal move
→ clear/gravity/refill/cascades
→ stable board
→ legal-move check
```

Search fixed seeds / deterministic move choices.

Do not systematically avoid special swaps. A policy that never swaps specials lets them accumulate, and any special with an occupied neighbour keeps the board playable, so such a search under-reports dead boards.

Vary the move choice deterministically: for example, seeded random legal moves including `SpecialSwap`, alongside first-found normal moves.

Report the move policy with the result, because occurrence depends on it.

Prefer:

```text
broad one-off search
→ first reproducible dead state
→ focused permanent test
```

Do not add a huge permanent fuzz test merely for impressive numbers.

## Where search code lives

A Core-only post-resolution search has to mirror the resolution loop that lives in Presentation (`BoardResolutionRunner`). A committed copy of that loop could drift from the real one.

Therefore:

- broad dead-board searches remain throwaway scratch or Editor tooling, not committed production or test infrastructure;
- permanent tests use captured fixture boards;
- permanent end-to-end tests go through the real `BoardResolutionRunner`.

## Record

For any dead state:

- starting seed;
- move policy;
- move sequence if relevant;
- final board;
- no current matches;
- no legal moves;
- whether it is reachable from the shipped configuration (seed 42, 20 moves, Red goal 25). A reachable seed plus move sequence can later be replayed on the S23.

STOP for review before recovery design.

---

# Step 4 — Implement Core Legal-Move Detection

## Goal

Add the smallest deterministic Core rule answering:

> Does this Board have at least one legal player move?

Suggested responsibility:

```text
BoardPlayability
or
LegalMoveFinder
```

Choose the clearest name based on the final API.

Requirements:

- Core only;
- no Unity dependency;
- no randomness;
- no mutation of authoritative Board;
- reuse `SwapValidator`;
- special swaps supported automatically;
- adjacent pairs checked once;
- one scratch copy per query (copy once, then swap → Analyze → swap back);
- generic semantics: no assumption that any special makes any Board playable.

Run focused Core tests first, then the full EditMode suite.

## Commit gate

Do not auto-commit.

Show:

- changed files;
- diff summary;
- focused tests;
- regression result.

Wait for approval.

Suggested commit after approval:

```text
Detect legal moves on stable boards
```

STOP.

---

# Step 5 — Choose Dead-Board Recovery Rule

## Goal

Choose recovery based on evidence, before coding it.

### Option A — regenerate from the level palette

```text
dead stable board
→ generate new match-free board
→ require a legal move
→ replace authoritative contents
→ re-render
```

Pros:

- simplest;
- reuses `BoardGenerator`;
- easy to keep deterministic.

Trade-off:

- does not preserve exact current gem counts.

### Option B — shuffle existing normal tiles

```text
copy existing tile multiset
→ deterministic shuffle
→ reject current matches
→ reject another dead board
→ accept first valid candidate
```

Pros:

- familiar Match-3 behavior;
- preserves gem counts.

Trade-offs:

- more code;
- retry/fallback policy required;
- more edge cases.

### Option C — end the level

Simple but likely unfair when moves remain.

Use only with a strong product reason.

In the current game, dead stable states are expected to be normal-only boards (see Specials and playability). Options A and B therefore only need to handle all-normal boards in practice, but must stay correct for any board they receive.

## Decision criteria

Choose the smallest option that guarantees:

- player can continue;
- recovered board is stable;
- recovered board is playable;
- deterministic behavior;
- bounded work / no infinite retry;
- no intentional exception during normal runtime recovery (see Recovery failure policy);
- the existing `Board` instance is mutated in place;
- moves, score and goal are unchanged;
- easy explanation in interview.

## Fallback comparison

Compare the simplest guaranteed fallback options for when bounded retries are exhausted.

One candidate worth considering:

```text
retries exhausted
→ copy a known deterministic playable board,
  produced by the already-guaranteed starting-board generation path,
  into the existing Board instance
→ moves / score / goal unchanged
```

Do not commit to a complicated constructive-pattern algorithm at this stage.

Whatever fallback is chosen must still produce:

- no immediate match;
- at least one legal move;
- deterministic behavior;
- bounded execution.

## RNG decision

Explicitly record:

- which `System.Random` stream recovery consumes;
- why;
- how Restart recreates it;
- that checking an already-playable board consumes zero randomness;
- that subsequent randomness changes only after an actual recovery.

Do not add a third RNG stream unless necessary.

## Outcome ordering

Recovery must not run after an already-decided terminal outcome.

Preferred behavior:

```text
move resolves
→ level progress updates
→ outcome decided
→ if still Playing:
      check playability
      recover if dead
→ unlock input
```

STOP for human approval of the chosen rule.

---

# Step 6 — Guarantee a Playable Starting Board

## Goal

Ensure a new/restarted level satisfies:

```text
no initial match
AND
at least one legal move
```

Preserve seed 42 behavior if its first generated board is already playable.

For a dead generated board:

- retry/recover deterministically;
- do not use unbounded loops;
- fail loudly for impossible configuration, at startup or restart, where it cannot freeze a move;
- consume zero randomness when the first generated board is already playable.

Tests:

- current seed 42 remains identical if already playable;
- all existing deterministic tests stay byte-for-byte unchanged when no recovery occurs;
- known dead seed becomes playable;
- no immediate matches;
- at least one legal move;
- same seed → same result;
- Restart remains deterministic.

If real SampleScene validation requires Unity/MCP, follow the teaching protocol before the Editor action.

## Commit gate

Show diff/tests first.

Suggested commit after approval:

```text
Guarantee playable boards after generation
```

STOP.

---

# Step 7 — Runtime Dead-Board Recovery

## Goal

Prevent a Playing level from settling into an unplayable board.

Required logical order:

```text
resolution complete
→ result applied to LevelProgress
→ Win/Loss decided
→ if still Playing:
      HasAnyLegalMove?
      if no:
          deterministic recovery
          update BoardView
→ unlock input
```

Do not recover after Win/Loss.

## Ownership

Core owns:

- playability;
- recovery rule/state.

Presentation owns:

- showing the new authoritative board.

Do not let Presentation invent the shuffle/result.

Recovery mutates the existing authoritative `Board` instance in place, then `BoardView` re-renders it. Never replace the shared `Board` object.

Recovery follows the Recovery failure policy: bounded retries, a deterministic safe fallback, and no intentional exception while the move is locked.

## Presentation scope

Prefer the smallest behavior:

```text
recover board
→ BoardView shows authoritative result
```

A tiny “No moves — reshuffling” message may be considered only if an instant board change proves confusing.

Do not build a shuffle animation system.

## Tests

At minimum:

1. playable board → no recovery;
2. dead board → recovery exactly once;
3. recovered board has no current matches;
4. recovered board has a legal move;
5. deterministic recovery;
6. no extra move consumed;
7. score unchanged;
8. goal unchanged;
9. terminal Win → no recovery;
10. terminal Loss → no recovery;
11. Board/View agree;
12. input remains locked until recovery finishes;
13. Restart after recovery still reproduces intended start;
14. the shared `Board` instance is the same object after recovery;
15. retry exhaustion reaches the deterministic fallback instead of throwing;
16. no recovery → existing deterministic sequences unchanged.

Permanent end-to-end tests go through the real `BoardResolutionRunner`, using captured fixture boards.

## Runtime scenario

Use a controlled dead board.

Verify:

- recovery occurs;
- player can make a move afterward;
- HUD unchanged by recovery;
- no immediate cascade from recovery;
- Console clean.

STOP before exception work.

---

# Step 8 — LockedAfterException Reachability Review

## Goal

Decide whether this SHOULD risk becomes MUST.

Do not implement exception infrastructure first.

Trace production throw points across:

```text
BoardInputController
SwapValidator
onBusyChanged
BoardView
BoardResolutionRunner
onValidMoveResolved
LevelProgress
LevelHudView
dead-board recovery (new in Phase 14)
```

Dead-board recovery runs inside the locked move window, so it is audited as a throw site like the rest.

For each important throw ask:

- can normal player input cause it?
- can a legal board sequence cause it?
- is it only a broken Inspector/config state?
- is it startup-only?
- is it test-only?
- does it happen after Board mutation?

## Adversarial normal-play search

Use supported gameplay only:

- many deterministic seeds;
- valid swaps;
- invalid swaps / rollback;
- cascades;
- every special;
- special chains;
- restart;
- Win/Loss;
- new dead-board recovery.

No deliberate internal corruption in this part.

If a normal production sequence throws:

```text
LockedAfterException → MUST
```

Capture exact seed/board/moves.

## Fault injection is separate

You may deliberately make an existing injected callback throw, e.g.:

```text
onValidMoveResolved
onBusyChanged
```

to prove the architectural lock consequence or later recovery behavior.

Label it clearly:

```text
fault injection
```

not:

```text
production reproduction
```

A fault-injection test does not by itself prove player reachability.

## Decision gate

### Path A — production trigger found

- escalate to MUST;
- implement minimal safe recovery;
- add exact regression test.

### Path B — no production trigger found

Decide whether a small recovery change is still justified.

Do not add a framework simply to make the issue disappear from a list.

STOP and report evidence before coding recovery.

---

# Step 9 — Exception Recovery, Only If Justified

## Safety rule

Do not:

```text
catch
→ unlock
→ continue uncertain board
```

Preferred principle:

```text
error remains visible
→ normal gameplay does not resume on uncertain state
→ clean Restart/rebuild becomes available
```

If implemented, required behavior:

- original exception remains visible in logs;
- normal board input stays unavailable;
- Restart becomes available;
- Restart can run despite interrupted move lock;
- restart uses normal session creation path;
- stale selection clears;
- stale move/coroutine state does not continue;
- restarted board is deterministic.

## Coroutine caution

Do not assume Unity runs `finally` the way we need around nested yielded routines.

If the design depends on:

```csharp
try { yield return ...; }
finally { ... }
```

prove it with focused PlayMode tests for the relevant failure location.

Test separately when practical:

- exception thrown directly in controller flow;
- exception originating from yielded/nested work.

If the simple mechanism does not behave reliably:

STOP before creating a generic coroutine executor.

## Recovery test

Prove:

```text
move starts
→ controlled fault
→ error visible
→ normal input unavailable
→ Restart usable
→ restart rebuilds clean board
→ normal input works again
```

## Commit gate

Show reproduction, design, diff and tests first.

Possible commit after approval:

```text
Allow restart after a failed move
```

Only if a production change is justified.

---

# Step 10 — Codex Adversarial Review

Claude remains the main developer.

Codex is reviewer/challenger and must not edit files.

Ask Codex to propose concrete board states / move sequences that challenge:

- legal-move detection;
- initial playability;
- dead-board recovery;
- lines/squares/L/T matches;
- invalid swaps/rollback;
- gravity/refill;
- cascades;
- specials;
- chains;
- Wisp/Spectrum targeting;
- special+special swaps;
- terminal outcomes;
- restart;
- input locking;
- exception decision.

For each finding:

```text
already covered?
→ close

not covered + reproducible?
→ focused test

not reproducible?
→ document, no speculative fix
```

Avoid giant combinatorial test matrices.

---

# Step 11 — Full Regression Validation

Run:

- full EditMode suite;
- full PlayMode suite;
- all new Phase 14 tests.

Record:

- pass count;
- skipped tests;
- expected negative-test output;
- unexpected warnings/errors.

Required runtime scenarios:

- controlled dead-board recovery;
- move after recovery;
- restart after recovery;
- Win/Loss does not recover;
- cascade;
- at least one special;
- special chain if practical;
- fault recovery only if implemented.

Verify deterministic behavior:

```text
same seed
+ same moves
→ same recovery behavior
```

Final Console should contain only declared expected negative-test messages.

---

# Step 12 — S23 Smoke Test

Do not repeat the full Phase 12 campaign.

Use the S23 to smoke-test changed robustness behavior.

If Step 3 found a real seed + move sequence that reaches a dead board in the shipped configuration, prefer replaying that sequence on the S23 (as Phase 13 replayed the Win route) over a special test build.

If a controlled dead-board scenario can be reached safely:

- show dead state;
- show recovery;
- make a valid move afterward;
- verify HUD did not change because of recovery.

Also smoke-test:

- ordinary swap;
- cascade;
- special;
- Restart;
- Win or Loss if practical.

If exception recovery was implemented, validate only through a safe controlled test seam/build.

One S23 pass proves one tested device/build, not all Android devices.

---

# Step 13 — Known-Issue Documentation

Only update known-issue docs after validation.

## `DeadBoard.md`

If resolved, record:

- legal-move definition;
- when detection runs;
- chosen deterministic recovery;
- validation;
- resolved status.

## `LockedAfterException.md`

In every case, correct the impact the current document understates (Phase 13 audit, Open Verification Items):

- after an exception inside the move, input stays locked **and** Restart and Play Again stay disabled, because the busy notification that re-enables them never runs;
- `RestartLevel` also refuses to run while input is locked;
- a throw after the input lock but before the busy notification leaves Restart visibly enabled, but clicking it is refused;
- the only recovery today is relaunching the app (nothing is saved, so relaunch fully recovers).

Also update the outdated owner wording in the known-issue docs this phase touches (for example "future game-flow work" → Phase 14).

If a trigger was found/fixed:

- record exact reproduction;
- final severity;
- recovery policy;
- tests;
- resolution.

If no production trigger was found:

- record adversarial evidence;
- fault-injection evidence if used;
- whether production recovery was deliberately added or not;
- remaining accepted risk.

Do not claim it is impossible.

---

# Step 14 — Learning Gate

At the end I should be able to explain:

### Stable vs playable vs dead

```text
stable = no match now
playable = at least one legal move
dead = stable but no legal move
```

### Why legal-move detection reuses `SwapValidator`

One source of truth for move legality.

### Why the query must not mutate Board

A question should not change authoritative gameplay state.

### Why recovery must be deterministic

Reproducible tests, bugs and restart behavior.

### Why DeadBoard is MUST

The player can otherwise become unable to continue while the level still says Playing.

### Why LockedAfterException is different

Its consequence is severe, but production reachability is not established.

### Why blindly unlocking after an exception is unsafe

Board and BoardView may no longer agree.

No notebook note is required unless one of these is genuinely new/useful.

---

# Step 15 — Completion Gate

Phase 14 is complete only when:

- [ ] legal-move semantics are defined;
- [ ] legal-move detection exists in Core;
- [ ] playability queries do not mutate the authoritative Board;
- [ ] at least one concrete dead-board fixture exists;
- [ ] initial boards are stable and playable;
- [ ] runtime Playing boards cannot remain dead;
- [ ] recovery is deterministic;
- [ ] recovery mutates the shared `Board` instance in place;
- [ ] recovery has bounded retries and a deterministic fallback, with no intentional exception while the move is locked;
- [ ] checking an already-playable board consumes zero randomness;
- [ ] existing seed-42 deterministic behavior is unchanged when no recovery occurs;
- [ ] recovery changes no move/score/goal values;
- [ ] no recovery occurs after Win/Loss;
- [ ] Board/View consistency survives recovery;
- [ ] restart determinism still works;
- [ ] LockedAfterException reachability was investigated;
- [ ] any production trigger found has a focused regression test;
- [ ] exception recovery policy is explicitly decided;
- [ ] no broad exception swallowing was added;
- [ ] Codex adversarial review is complete;
- [ ] high-value reviewer findings are tested/resolved/documented;
- [ ] all EditMode tests pass;
- [ ] all PlayMode tests pass;
- [ ] runtime recovery scenarios pass;
- [ ] Console has no unexplained errors/warnings;
- [ ] S23 smoke validation is complete where practical;
- [ ] known-issue docs reflect final evidence;
- [ ] no confirmed MUST gameplay blocker remains;
- [ ] human reviewed final diff.

Do not start Phase 15 automatically.

---

# Git Discipline

For each implementation slice:

```text
implement
→ focused tests
→ required regression
→ show diff
→ human approval
→ commit
```

Do not commit before review.

Do not push unless explicitly requested.

No co-author / attribution trailers.

Possible focused commits, only if the final implementation actually matches them:

```text
Detect legal moves on stable boards
Guarantee playable boards after generation
Recover deterministic dead boards
Allow restart after a failed move   # only if justified
Complete Phase 14 gameplay robustness
```

---

# Unity Learning Protocol

Before every Unity Editor / MCP action that changes or meaningfully exercises Editor state, explain:

1. what will be done;
2. why;
3. exact manual Unity steps;
4. how to verify.

After the action report:

- what changed;
- what was observed;
- tests / Console result;
- what temporary state must be restored.

Read-only code inspection does not need repetitive ceremony.

---

# First Developer Assignment

Claude should begin with **Steps 1–3 only**:

```text
baseline
→ legal-move semantics/API proposal
→ dead-board evidence search
```

Do not implement final recovery yet.

First report should include:

- clean baseline;
- proposed legal-move API;
- clone/no-mutation strategy;
- focused test cases;
- concrete dead-board fixture if found;
- initial-board seed search evidence;
- post-resolution search evidence;
- recommended recovery option with trade-offs.

Then STOP for human review before production implementation.
