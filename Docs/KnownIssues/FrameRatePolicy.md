# Android frame-rate policy

Status: Policy implemented in Phase 12 Step 7; real Android device validation remains pending.

`BoardBootstrap.Start` requests `Application.targetFrameRate = 60` only on Android. This is a fixed baseline, with no adaptive FPS logic or per-frame policy work. Core is unchanged. The target is a request, not a performance guarantee; sustained device performance, thermal behavior and battery cost have not been measured.

## Platform settings and rationale

The Android default in `QualitySettings.asset` is **Medium** (index 2), with `vSyncCount = 1`. The Editor was using **Very Low**, with vSync off and `targetFrameRate = -1`; that Editor selection is not the Android default. Android optimized frame pacing is already enabled and remains unchanged.

Unity 6.5 ignores `vSyncCount` on Android and uses `Application.targetFrameRate`. Without an explicit target, Android defaults to 30 FPS. A requested rate can be rounded down to a display-supported divisor; for example, a 60 FPS request is not a promise of 60 FPS on every refresh-rate configuration. See the [official target-frame-rate documentation](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/application/targetframerate).

On desktop, enabled vSync takes precedence over `targetFrameRate`. See the [official vSync documentation](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/qualitysettings/vsynccount). Production startup deliberately leaves Editor/desktop pacing alone, so the Editor remains uncapped by project code outside validation. This favors development/testing convenience; it is not a desktop release policy. Quality settings, display synchronization and Editor overhead still influence observed FPS.

60 FPS is the current Android baseline for smooth movement and feedback. There is no device evidence yet that justifies a different baseline or adaptive 30/60/120 logic.

## Real-time animation validation

`FrameRateTimingTests` contains three explicit PlayMode tests. Select them individually in Test Runner with the Editor focused. They temporarily disable vSync, request a real frame-rate target, warm up the limiter, measure 60 actual frames, then time production animation iterators using `Time.realtimeSinceStartupAsDouble`. Each phase must be within **one target frame** of its configured duration. Wisp phases are measured separately; rounding can accumulate across a sequence.

These are opt-in, machine-dependent timing checks, not ordinary deterministic regression tests. They reject enabled capture timing or a non-unit time scale, check delivered cadence, and restore the original target and vSync in teardown. They never set `Time.captureFramerate` or fake `deltaTime`.

Measured in Unity **6000.5.10f1**, Windows Editor, approximately **165 Hz** display:

| Phase | Configured | Real 30 FPS | Real 60 FPS | Real 120 FPS |
| --- | --- | --- | --- | --- |
| Measured cadence | requested rate | 30.00 FPS | 60.00 FPS | 120.10 FPS |
| Swap | 200 ms | 199.84 ms | 199.81 ms | 195.27 ms |
| Clear | 100 ms | 132.79 ms | 116.04 ms | 104.75 ms |
| Beam feedback | 120 ms | 133.29 ms | 133.15 ms | 119.77 ms |
| Nova feedback | 120 ms | 132.86 ms | 133.08 ms | 124.54 ms |
| Fall | 150 ms | 166.83 ms | 166.35 ms | 148.86 ms |
| Refill | 150 ms | 166.22 ms | 149.76 ms | 157.26 ms |
| Wisp burst | 120 ms | 133.25 ms | 132.97 ms | 124.31 ms |
| Wisp flight | 250 ms | 266.22 ms | 249.96 ms | 248.95 ms |
| Wisp landing | 120 ms | 133.50 ms | 133.56 ms | 125.11 ms |

All phases passed the unchanged tolerances: about 33.3 / 16.7 / 8.3 ms respectively. Animation code and configured durations were not changed. These Editor measurements do not establish Android frame pacing or physical-device performance; verify the requested baseline in the later Android validation step.
