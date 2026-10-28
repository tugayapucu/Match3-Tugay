# Tall-screen board position comparison

Status: **Decided: remain at 0.5; bias mechanism not adopted.**

The original centering calculation is retained without a bias parameter or
constant. The three experimental source/test changes were restored to `HEAD`.
No replacement APK was built or installed for this experiment.

## Decision

A bias only redistributes empty space: at 1/3, our upper gap shrinks from 530 to
364 px while the lower gap grows to 696 px, so it does not address both gaps.
For comparison, the developer measured Royal Match on the same 1080 x 2340 S23:
220 px above and 155 px below its board, totaling 375 px (16% of screen height,
equivalent to about 0.59 top-spare bias), versus our 996 px (43%).
Its board remains near-centered while a roughly 470 px HUD, 250 px booster bar,
framed backdrop and background art occupy the height; our content/composition
work is therefore [deferred to UI polish](../../KnownIssues/TallScreenComposition.md),
outside Phase 12.

These reference measurements were supplied by the developer, not independently
measured by the agent. No third-party screenshots or art are included.

## Saved screenshots

All screenshots are stored here, outside Unity's `Temp` directory.

| Preview | Bias | HUD-to-envelope gap | Envelope-to-Restart gap |
| --- | --- | --- | --- |
| [Current centering](bias_050.png) | 0.5 | 530 px | 530 px |
| [Moderate upward shift](bias_0333.png) | 1/3 | 364 px | 696 px |
| [Stronger upward shift](bias_025.png) | 0.25 | 281 px | 779 px |

The three previews are actual Unity Game view captures at 1080 x 2340, with no
simulated insets. They use the same generated board, HUD and Restart positions.
The fitting envelope is 984 x 984 px in every preview; cells remain 120 px apart.
Gaps are measured to the envelope, not the gems' opaque pixels.

[S23 installed-build baseline](s23_original_device.png) was captured through ADB
from the connected SM-S911B after bringing the existing game to the foreground.
Only this baseline is a physical-device capture. The candidate positions have
not been tested on the phone.

All JSON records and the 1/3 and 0.25 previews were produced with the discarded
experimental Editor implementation, not the retained production code or an
updated Android APK. They remain here as historical evidence, not current APIs
or acceptance criteria.

## Discarded prototype

The experimental `CalculateCamera` accepted a finite bias between zero and one.
It assigned that fraction of the region's spare height above the board envelope.
The content region already included the control gaps, so they were not added again.

Only camera position changes between candidates; orthographic size stays 9.75
at this resolution. `SafeAreaLayout`, `BoardCameraFitter`, BoardRoot, tile spacing,
colliders, scene assets, project settings and Core code are unchanged on disk.

To reproduce the visual positions manually, select a fixed 1080 x 2340 Game view
and enter Play Mode.
Temporarily disable Main Camera's BoardCameraFitter, retain orthographic size
9.75 and camera Z -10, and compare camera Y -0.2333333, -1.6166667 and -2.3083333.
These Y values apply only to this particular screen and inset configuration;
they are not production camera settings. Stop Play Mode to discard edits.

## Historical experiment validation

- EditMode: 550 passed. Tests cover bias extremes, the three candidates, existing
  screen sizes, asymmetric insets, centered compatibility and invalid biases.
- PlayMode: 166 passed, 3 explicit hardware timing tests skipped. An earlier
  interrupted run encountered a Test Runner exception after an assembly reload;
  it was discarded and the full suite was rerun successfully.
- Live scene queries passed for all three biases at 1080 x 2340, 1080 x 1200,
  720 x 1280 and an asymmetric 1080 x 2340 safe area. JSON records are alongside
  the screenshots. Each checks envelope containment, four corner collider picks,
  Restart-center clearance and unchanged BoardRoot geometry.
- At 1080 x 1200, every candidate has the same height-limited framing and 32 px
  control gaps. At 720 x 1280, the envelope remains 656 px wide for every bias.
- Compilation succeeded. Final Console inspection found no errors or warnings.
- SampleScene is stopped and clean. Temporary Game view presets were removed,
  the original Phone preset restored, and Run In Background restored to off.

The 550-test EditMode result above includes the discarded experimental tests.
The retained code's regression results are recorded separately below.

## Validation after discarding the prototype

Validated on 2026-10-28 after restoring all three experimental files to `HEAD`:

- EditMode: **540/540 passed**, job `419df38fb03e4eaab014d1ace21cf9e6`.
- PlayMode: **166 passed, 3 explicit timing tests skipped**, job
  `28f69142e95844c38473312dca8ec7f3`. The first attempt was interrupted by an
  assembly reload and a Test Runner `NullReferenceException`; its stale job and
  generated scene were cleared before the complete successful rerun.
- Compilation completed. Final Console contains the expected Purple negative-test
  exception and no-room warning, with no unexpected gameplay errors. The MCP
  console tool also labels ordinary result-saving/cleanup messages as errors or
  warnings; those are Test Runner bookkeeping.
- SampleScene is open, stopped and unmodified; only one scene is loaded.
  StandaloneWindows64 remains active and Run In Background is restored to off.
- `git diff` contains no changes under `Assets/` or `ProjectSettings/`.

No Phase 12 Step 10 work or UI polish implementation is part of this decision.
