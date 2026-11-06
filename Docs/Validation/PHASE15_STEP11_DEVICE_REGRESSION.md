# Phase 15 Step 11 — S23 device regression matrix

Status: **Step 11 completed; ready for Claude review.** The functional device
checks passed with the limits below. No production code changed. Step 12 has not
started, and nothing from Step 11 has been committed or pushed.

Date: 2026-11-06. Production code: `07f663c`, included in `9b2ea22`.
Installed APK: the Step 10 tap-to-cancel development build, SHA-256
`A4984598E81AA42B01E20609BCA92CEBA92608203ACE87A49D550361F77EB3C7`.
Installed `base.apk` hashing confirmed that exact artifact before round 1.
Samsung Galaxy S23 SM-S911B over USB, 1080×2340, density override 450.

## First finger tap after cold launch

For each round, ADB force-stopped and launched the application. `am start -W`
reported COLD, a fresh process ID was verified, and **zero ADB touch events**
were sent before the developer's first finger tap. The developer was instructed
to wait for the board, tap once, lift the finger and report the cue result.

| Round | Previous PID | New PID | Human result |
|---|---:|---:|---|
| 1 | 1217 | 3200 | First tap worked |
| 2 | 3200 | 3668 | First tap worked |
| 3 | 3668 | 4126 | First tap worked |
| 4 | 4126 | 4824 | First tap worked |
| 5 | 4824 | 5131 | First tap worked |

Result: **5/5 reported successful** on this S23. These are separate cold-launch
finger checks, not repeated taps in one session. The small sample does not prove
all launches/devices work or establish the cause of Step 10's initial automated
tap miss. No first-tap production workaround was introduced.

Per-round launch records and responses are retained under the Git-ignored
`Builds/Android/Phase15-Step11/first-tap-round1.json` through `first-tap-round5.json`.

## Functional Android checks

These used Android `input touchscreen` events through ADB on the physical S23.
They exercise the installed game's native input path; they are not human finger
comfort checks. Step 10's human comfort and Nova-cue judgement remain separate.
No board state was injected into the running APK.

Screenshots, input-command timestamps, replay routes and diagnostic programs are
retained under Git-ignored `Builds/Android/Phase15-Step11/`. The offline replay
compiles the current Core source and mirrors the runner's sequencing. A screenshot
classifier compares all 64 visible tile sprites against its predicted settled
board. New H/V sprite templates were visually checked before use. This verifies
visible output, not private Board/View object identity inside the APK. Outcome
cards obscure the board, so terminal states were checked by their visible HUD,
panel and response to input rather than classifying the obscured cells.

Grid coordinates below are zero-based, with `(0,0)` at bottom left. Device cell
centres are `(120 + 120*x, 1610 - 120*y)` in top-left screen pixels.

| Check | Observed result | Evidence filenames / route |
|---|---|---|
| First tap release | Press/hold leaves the screen unchanged; release shows the cue | `11-press-only.png`, `12-release-selected.png` |
| Retained selection / cancellation | Distant tap retains the same cue; same-cell release restores the exact starting screen without a move | `13-distant-retains.png`, `14-cancelled.png` |
| Full-cell picking | Four points one pixel inside the top-left cell's corners select/cancel that cell, including transparent sprite corners | `corner-*-selected.png`, `corner-*-cancelled.png` |
| Outward edges/corners | Eight outward swipes from the four board corners preserve the existing centre selection, board and HUD | `edge-0.png` through `edge-7.png` |
| Rejected gesture remains consumed | Outward swipe followed by movement back onto an in-bounds neighbour still does nothing until release | `offboard-return-selected.png`, `offboard-return-released.png` are identical |
| Right / left swipe | `(0,7)->(1,7)` and reverse visibly animate, roll back and restore the exact starting screen; no move charged | `invalid-{right,left}-moving.png`, `invalid-{right,left}-after.png` |
| Up / down swipe | `(4,2)->(4,3)` and reverse both resolve to the expected Nova-creation board: 19 moves, 9/25, 2,900 points | `valid-{up,down}-settled-held.png` |
| One successful gesture / one attempt | Those vertical swipes were held through resolution/unlock, moved again and released; settled and released screenshots remain identical | `valid-{up,down}-released.png` |
| Adjacent second tap release | `(0,1)` selected; holding the second press at `(1,1)` changes nothing. Release resolves one move: 19 moves, 0/25, 300 points | `two-tap-second-held.png`, `two-tap-second-released-moving.png`, `two-tap-final.png` |
| Cue during movement | Sampled swap/rollback/clear frames show no selection brackets; a subsequent select/cancel works normally | `invalid-right-moving.png`, `rollback-held-busy.png`, `two-tap-second-released-moving.png` |
| Busy swap / rollback | A new press begun during invalid resolution, held past unlock and then moved/released, leaves the exact starting screen unchanged | `rollback-held-{busy,settled,released}.png` |
| Busy cascade / Restart | Fresh Restart press during a seven-round cascade is rejected. A board press held across settlement cannot rearm afterward | `busy-cascade-held.png`, `busy-cascade-settled-held.png`, `busy-cascade-released.png` |
| Special swap / chain | Wisp + Spectrum Core pair and Nova -> Wisp -> Spectrum chain follow Core's predictions. A busy-started press held across their resolution does not select or cause another move on release | `special-{pair,chain}-{busy-held,settled-held,released}.png` |
| Beam effects | H activation after `(4,0)->(4,1)`, then `(3,3)->(3,4)` ends at 18 moves, 10/25, 4,200. The Loss route also creates and activates V | `beam-final.png`; Loss moves 15/16 |
| Win / Loss | Correct titles and final scores; terminal board taps/swipes leave the complete screen unchanged, with no cue | `win-04-settled.png`, `loss-20-settled.png`, `*-terminal-input-ignored.png` |
| Play Again | Both outcomes return to the exact seed-42 starting screen without a leaked selection | `win-play-again.png`, `loss-play-again.png` |

The four-move Win route ends **Won, 16 moves, 25/25, 8,400 points**:
`4,0->4,1; 3,1->3,2; 1,0->1,1; 3,5->4,5`.
The twenty-move Loss route ends **Lost, 0 moves, 17/25, 6,800 points**.
Their full sequences and per-move predictions are in `win-route.json` and
`loss-route.json`; screenshots were captured at each settled state. Every
unobscured settled board on those routes matched the prediction.

For the special cases, first use `(4,2)->(4,3)` to create the natural special
layout. Then `(2,4)->(3,4)` activates the Spectrum/Wisp pair, ending at
**18 moves, 20/25, 7,600**. Alternatively `(5,5)->(4,5)` produces the
Nova/Wisp/Spectrum chain, ending at **18 moves, 22/25, 7,400**.

## Recovery with the current input code

A temporary scene copy changed only Bootstrap's seed to 93. No test hook,
debug UI or production source change was added. It was built with the current
code, IL2CPP ARM64, Development and ETC2; the old Phase 14 APK was not used.

Artifact: `Builds/Android/Phase15-Step11/Match3-Phase15-Step11-Seed93-Development.apk`.
Size: **70,571,959 bytes**. SHA-256:
`B5592FD1377AF9A07F98DF926A485CE6A83E9423FD758498FFA1393B434FF9EA`.
Build succeeded in **28.27 s**, zero errors, one warning: missing
`RuntimePipelineConfig` / Pipeline disabled in Player builds. The BuildReport's
aggregate size includes other outputs; the APK size above is from the actual file.

Native swipes replayed the ten-move seed-93 route:

```text
(5,7)->(6,7) (2,7)->(3,7) (5,6)->(5,7) (1,6)->(1,7) (0,7)->(1,7)
(3,7)->(4,7) (2,6)->(3,6) (3,6)->(3,7) (1,6)->(1,7) (4,5)->(5,5)
```

All settled boards matched Core's predictions. Move 10 recovered to the expected
playable replacement with **10 moves, 3/25, 3,300 points**. A fresh press begun
during the last resolution remained held through recovery and unlock; moving
and releasing it afterward left the recovered screen exactly unchanged.
`recovery-10-busy-held.png` visibly shows the clear phase and disabled Restart;
`recovery-10-settled.png` and `recovery-10-released.png` are identical.

The next legitimate swipe `(2,0)->(3,0)` resolved normally:
**9 moves, 3/25, 3,600 points**, with all 64 visible tiles matching prediction.
Select/cancel then worked, and Restart restored the exact seed-93 starting screen.
See `recovery-route.json`, `recovery-11-settled.png` and `recovery-restart.png`.

Recovery itself is synchronous. ADB cannot inject a callback inside that instant;
this is evidence of holding across it, not a claim of precisely timed injection
inside `RecoverIfDead`. The existing PlayMode test
`Seed93Swipes_RecoveryFinishesInsideTheLockedProductionCallback` checks the exact
callback ordering and locked state.

## Limits and diagnostics

- Device screenshots sample animation frames; they do not prove every individual
  frame boundary. Before-first-frame cue ordering remains covered by automated
  controller tests. Source-based animation duration estimates were used only to
  wait for settlement, not as new real-time performance measurements.
- The five cold-launch checks are human finger evidence. The other checks above
  are native Android events injected by ADB. No new comfort claim is inferred.
- The original automated first-tap miss from Step 10 remains unexplained. No
  workaround was added after the five successful human checks.
- No new functional input/recovery regression was reproduced. Android logs are
  **not error-free**: the previously recorded startup `AssetPackManager`
  `ClassNotFoundException` is present. Process logs also contain
  `AHardwareBuffer` allocation errors and Android `DexFile.finalize` assertion
  messages. No observed gesture failure, stuck lock or process termination was
  associated with these samples; their causes are not established or fixed here.
  Logs are retained as `production-logcat-before-fixture.txt`, `seed93-logcat.txt`
  and `final-production-logcat.txt` for review.
- Tall-screen whitespace, outcome dimmer strips and Restart confirmation remain
  deferred. Step 11 does not alter those decisions or establish device-wide
  performance or reliability guarantees.
- No EditMode/PlayMode suite was rerun in this device-only step. The previously
  recorded results remain **603 EditMode**, **231 PlayMode passed / 3 explicit
  timing skips**; they are earlier results, not a fresh Step 11 run.

## Restored state and repository scope

The temporary scene/folder was removed through Unity's asset tools. Four inspected
files automatically rewritten by the build (three rendering settings assets and
`ProjectSettings.asset`) were restored to HEAD. There is no remaining change under
`Assets/`, `Packages/` or `ProjectSettings/`.

The normal seed-42 APK was reinstalled and launched; its on-device SHA-256 matches
the production artifact above. The final visible board matches the seed-42
prediction. Editor: **Windows64**, clean SampleScene at seed 42, stopped, no pending
compile/import; Development Build, Script Debugging, Connect Profiler and Run In
Background off. Final Editor Console read: **zero errors/warnings**.

Only this report and the execution plan's Step 11 status are changed. Diagnostic
programs, APKs, captures and logs stay ignored under `Builds/`. No commit or push;
stop for Claude review before Step 12.
