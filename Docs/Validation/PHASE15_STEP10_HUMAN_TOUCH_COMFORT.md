# Phase 15 Step 10 — Human touch comfort review

Initial review: 2026-11-05. Approved follow-up: 2026-11-06.
Initial human observations below used the retained S23 candidate t025 from Step 9,
source `9132605`; that APK predates the cancellation adjustment. The updated
tap-to-cancel APK was subsequently built and installed; its evidence is below.

**Complete for the scoped S23 human comfort review.** Same-tile cancellation was
approved, implemented, checked through ADB touch injection and confirmed by the
developer's finger test. Nova's gold artwork and white selection cue were reported
clearly different. The first finger tap after relaunch worked. Restart
confirmation remains explicitly deferred. Step 11 has not started.

## Human observations

| Check | Developer's report | Interpretation |
|---|---|---|
| Centre, edge and corner cells | "comfortable all of them" | Comfortable in the reported finger test |
| Repeated two-tap selection, including quick taps | "comfortable" | Comfortable in the reported finger test |
| Repeated swipes and one-handed use | "comfortable" | Comfortable in the reported finger test |
| Invalid rollback and continuing play | "It is comfortable"; initially reported no way to cancel a first selection | General interaction comfortable; approved cancellation follow-up confirmed below |
| Restart | Easy to reach, possibly too easy: an unintended press immediately restarts | Reachability comfortable; destructive-action protection concern remains |
| Same-tile cancellation on the updated APK | "Works correctly" when asked to select/cancel Nova with moves staying at 19 | Human confirmation of the approved cancellation change |
| Selected versus unselected Nova | "Clearly different" | Retain the corner-bracket cue; no redesign needed from this S23 review |
| First finger tap after closing/reopening | "First tap works" | Initial automated no-cue observation was not reproduced by the developer's finger check |

These are the developer's qualitative observations, not measured tap error rates
or proof of comfort on every handset.

## Selection correction — confirmed current behavior

At the initial review, `BoardInputController.HandlePointerUp` kept selection after a
same-cell or non-adjacent tap: the shared swap attempt returns false. Outside taps
also retain it. This matches the previously approved rules; it is a usability
limitation observed during human testing, not a newly introduced regression.
Swiping from a different tile already uses that tile as its own move origin.

The developer approved the smallest change on 2026-11-06: a classified tap on the already-selected tile cancels
the selection and its cue, without changing the Board, locking input, sending a
busy notification or costing a move. The next tap can select any tile. Keep the
other selection rules unchanged. This is implemented in the existing tap-release
branch, with no new component, Inspector field, prefab or scene change. A swipe
from the selected tile still takes the swipe path before tap classification.

Four focused regression cases cover mouse/touch and normal/special tiles:
press/hold preserves the cue, release clears it, Board/level progress and view
identity are unchanged, no lock/busy/result callback occurs, and a fresh distant
tap can select freely. The three non-adjacent retained-selection cases remain;
the old same-cell-retains case is deliberately replaced. Existing Wisp and scene
corner-picking checks now expect same-cell cancellation while still checking
no activation/move and correct picking. The fresh-level test helper selects and
then cancels, leaving the next move helper unselected.

Focused Unity checks passed 9/9; a targeted rerun covering the Restart helper and
four cancellation cases passed 5/5. Final full PlayMode result: **231 passed,
3 explicit hardware-timing tests skipped, 0 failed** (234 total). EditMode was
not rerun for this Presentation/input change; the prior result remains 603/603.

Two earlier full attempts were interrupted by script recompilation and an
internal `PlayModeRunTask.cs:52` test-runner error. Automatic asset refresh was
temporarily paused for the completed runs and restored afterward. Old-rule
assertions found during the review were updated rather than changing production
behavior back. The final Console also contained two Input System native-input
`KEYS`/`TSCR` format errors outside the named test failures, as well as the
declared startup negative-test exceptions and camera no-room warning. The format
errors' origin has not been established; they are not treated as expected game
output or evidence of a clean Console. The fresh SampleScene handler smoke check
passed: first press unselected, first release selected, second press still
selected, second release cancelled, unlocked, 20 moves / score 0 / goal 0 and
64 views. This used programmatic handlers, not physical input. The SDK format
errors reappeared on entering Play Mode. Script reload did not resolve the
state: device inspection still showed only a virtual, non-native Touchscreen.
The Editor was therefore restarted from a stopped, clean SampleScene to restore
native device discovery. After MCP reconnection, native Keyboard (ID 1) and Mouse
(ID 2) were present, replacing the virtual Touchscreen state. The post-restart
SampleScene handler smoke check passed again, including selecting a distant tile
after cancellation and cancelling that selection too. It left input unlocked,
selection clear, 20 moves / score 0 / goal 0, and 64 views. Console checks before
and during this fresh run had zero errors/warnings. This supports recovery of
the Editor/test environment; it does not establish a physical-device result or
prove the underlying SDK/test-runner cause. SampleScene was stopped afterward.

## Restart protection — confirmed immediate-action path

`LevelHudView.HandleRestartClicked` immediately invokes the restart callback.
There is no confirmation while Playing. The developer reported concern about
accidental activation; no actual accidental-restart incident was reported.

Smallest proposed change: a Cancel / Restart confirmation for restarting during
play, with board input blocked while the confirmation is open. Cancel preserves
the current session; Confirm uses the existing restart callback once. Keep
terminal-state Play Again immediate and preserve the existing busy gate. This
would change approved behavior and require UI work. **The developer requested
confirmation but explicitly deferred implementation on 2026-11-06.** No dialog
or scene changes have been made. The deferred work is recorded in
`Docs/KnownIssues/RestartConfirmation.md`; the exact later step is not assigned.

## Updated Android APK and device checks — 2026-11-06

- APK: `Builds/Android/Phase15-Step10/Match3-Phase15-Step10-TapCancel-Development.apk`;
  70,571,959 bytes, SHA-256
  `A4984598E81AA42B01E20609BCA92CEBA92608203ACE87A49D550361F77EB3C7`.
- Source at build time: `084249d` plus the approved uncommitted cancellation change.
  `BoardInputController.cs` SHA-256:
  `CE29CBDF4FE9F156B370CD740C8045853232DF7E3A1F905B3FA737A091173C1B`.
  Source hashing before/after the build confirmed that file did not change.
- IL2CPP / ARM64 / ETC2 development APK built from the unchanged production
  SampleScene with the retained 0.25 threshold. No script debugging, profiler
  autoconnection, temporary scene edit or in-game debug UI was added.
- BuildReport: Succeeded, 0 errors, 6 warnings, approximately 50.2 seconds.
  Warnings: the existing missing RuntimePipelineConfig warning, three TextMeshPro
  native-compilation notices and two MCP WebSocket connection warnings. The MCP
  call returned before the build completed; the final BuildReport/status file
  was checked rather than starting a second build.
- ADB installation succeeded and UnityPlayerGameActivity resumed on the S23.
- After a tile was visibly selected, a further tap on that tile removed the cue
  without using a move. The cancelled PNG was byte-identical to the initial
  screenshot (20 moves, goal 0/25, score 0). Selecting and cancelling a distant
  tile also returned the byte-identical initial screenshot. These checks use
  Android touch injection, not programmatic game-handler calls or human fingers.
- The first automated tap captured after launch produced no cue; its PNG was
  identical to the initial state. The next tap selected normally. This is a
  concrete observation, but its cause and reproducibility have not been
  established. In the follow-up finger check after closing/reopening, the
  developer reported "First tap works". ADB readback showed process ID 26408,
  different from the pre-check ID 22711, supporting a fresh-process relaunch.
  Keep the original ADB observation; do not claim its cause is fixed, that every
  automated first tap succeeds, or that no future first-tap failure is possible.
- App-process logcat contained the already recorded
  `com.google.android.play.core.assetpacks.AssetPackManager` ClassNotFoundException
  on startup. It also appears in the Phase 14 S23 log and Phase 13 audit; see
  `Docs/Phases/PHASE13_RELEASE_READINESS_AUDIT.md`. The game continued through the
  checks; this does not establish that the log is harmless or newly introduced.

BuildReport, source fingerprint, app error log and screenshots `00-start.png`
through `06-nova-unselected.png` are under the Git-ignored
`Builds/Android/Phase15-Step10/` directory. Screenshots distinguish the initial
no-cue observation, selected state and actual cancelled state. The APK is left
installed; the developer subsequently completed the requested finger review.

## Nova creation aid — device route and human readability confirmed

A temporary, Git-ignored console probe compiled the current Core source with
Unity's Mono compiler and replayed the Core calls used by the resolution runner.
It found this route from the shipped seed-42 starting board, with refill seed 42
and the unchanged separately salted targeting stream:

1. Restart the installed build (seed 42, retained 0.25 threshold).
2. Count columns from the left and rows from the top, both starting at 1.
3. Swipe column 5, row 6 upward to column 5, row 5: Blue exchanges with Red.
4. Allow the cascades to finish.

The Core replay leaves a Nova at column 6, row 3, with Red goal progress 9/25.
This was subsequently reproduced in the updated APK using an ADB-injected swipe
from screen (600,1370) to (600,1250). The resulting screenshot shows the predicted
settled board, 19 moves, goal 9/25 and score 2900. The Nova was left unselected
for human comparison. The settled top-to-bottom Core board is:

```text
RYRRPYRG
GPYBRYBY
PPYGYTPR
GRFLGPYP
PYRPPRYY
YRPYBOPP
GPGGYGBG
PRRYGGPY
```

`T` is Nova, `F` is Wisp, `L` is Spectrum Core, and `P` is Purple. The developer
compared Nova before/after selection on the updated APK and answered "Clearly
different" for its permanent gold corners versus the white selection cue.
Selecting/cancelling it with the move count unchanged was reported as "Works
correctly". These are explicit human results, separate from ADB observations.
Keep the current selection artwork. This is S23 approval, not a universal
small-screen readability claim.

## Scope and state

The approved follow-up changes only the tap-release branch in
`BoardInputController`, the corresponding PlayMode tests, the execution-plan
exception and these review/deferral notes. No Core, scene, prefab, settings or
package changes were made. The diagnostic probe is under Git-ignored `Temp/`.
The phone now has the updated
tap-to-cancel APK. After building, Windows 64-bit was restored and inspected
build-generated settings diffs were reverted without touching the approved
implementation. The requested human cancellation, Nova comparison and first-tap
follow-up have been answered, so Step 10 is complete with the documented limits
and deferred Restart confirmation. Stop here for review; Step 11 remains unstarted.
