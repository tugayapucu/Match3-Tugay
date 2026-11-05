# Phase 15 Step 9 — S23 swipe-threshold comparison

Date: 2026-11-05. Source: `9132605` (approved Step 8).

**Three-candidate human comparison complete: retain 0.25.**
The developer reported no noticeable difference among the candidates. Keep the
existing 0.25 default as the conservative baseline, without claiming it is a
measured optimum or objectively safest value. Step 10 has not started.

## Representation and device

`swipeThresholdFraction` is a fraction of screen-space cell spacing measured once
at press time. A swipe starts when the larger absolute displacement axis reaches
that threshold. It is not a fixed pixel or dp threshold.

ADB confirmed Samsung Galaxy S23 SM-S911B, serial RFCW40R6CVH, Android 16,
1080×2340. Reported density: physical 480, override 450. USB transport is used.
At approximately 120 px cell spacing, the candidates below correspond to about
24, 30 and 42 px respectively; these calculations do not establish finger comfort.

## Candidate builds

All APKs are local, Git-ignored artifacts under `Builds/Android/Phase15-Step9/`.
Each is 70,573,294 bytes. Same application ID: `com.tugaykapucu.match3`.

| Test order | Fraction | Approx. pixels | APK | Human result |
|---|---:|---:|---|---|
| A: baseline | 0.25 | 30 | `Match3-Phase15-t025-Development.apk` | Developer's finger-test feedback: "Okay this looks fine not bad." No specific failure reported; individual checklist results were not separately confirmed. |
| B: lower | 0.20 | 24 | `Match3-Phase15-t020-Development.apk` | Developer's finger-test feedback: "About the same" as 0.25. No specific failure reported; individual checklist results were not separately confirmed. |
| C: higher | 0.35 | 42 | `Match3-Phase15-t035-Development.apk` | Developer's finger-test feedback: "about the same" and "i really could not see adifference." No specific failure reported; individual checklist results were not separately confirmed. |

SHA-256:

```text
t025 77A512058A2311DD345FAE8FAC5C7D1DB5455DF171BF507A43EDB8CC0D634E89
t020 94F65CCF3DC913C923948A3180256F17E671C6DF2E320A8BE2381CAC969025B9
t035 B846B1217C2172EA5DC0BC26DE5222C338560CF74E166CFF87540CF3106C1C22
```

Unity 6000.5.10f1 built each candidate using IL2CPP, ARM64, ETC2 texture subtarget,
Development and DetailedBuildReport, without script debugging, profiler
autoconnection or an in-game debug UI. The serialized fraction was changed and
saved in a temporary scene copy before each build. The production scene was not
edited. All three final BuildReports succeeded with zero errors and one warning:
`No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime).
Pipeline will be disabled in Player builds.` This is recorded without changing
unrelated project configuration.

Reports are `t025-build.json`, `t020-build.json` and `t035-build.json` in the same
artifact folder. An initial delayed build-start diagnostic caused a redundant
build invocation, which Unity rejected because another build was already in
progress. It was tooling activity, not a gameplay exception; all final reports
and artifact hashes above refer to successful builds.

Baseline installation returned Success. ADB confirmed the installed package is
DEBUGGABLE and its UnityPlayerGameActivity was resumed. The short launch-time
Unity/AndroidRuntime error-log query returned no entries; this is not a complete
runtime regression review. No ADB gestures are counted as human testing.

## Human comparison and decision

For each candidate the developer was asked to try deliberate taps, very quick
taps, taps with small finger jitter, short/fast/slow swipes, diagonal-ish swipes,
edge/corner cells and one-handed use, and to report accidental or ignored swipes.
Each candidate was installed and launched after feedback on the previous one.
The replies recorded in the table are overall subjective impressions, not
separate confirmations of every checklist case or a measured error rate.

Final value: **0.25 of cell spacing**, approximately 30 px at 120 px per cell.
The baseline felt fine, while neither 0.20 nor 0.35 had a perceived benefit.
0.25 also lies between the lower travel requirement of 0.20 and the higher travel
requirement of 0.35. Keeping the existing value avoids an unsupported tuning
change; this is a conservative engineering choice, not evidence that 0.25 has
the lowest accidental-swipe or missed-swipe rate. No code or production scene
change was needed. The verified t025 APK was reinstalled and launched after the
comparison, leaving the phone on the retained baseline.

No universal handset-comfort claim is made. Detailed human touch-comfort review
and the Nova/selection-cue comparison remain for Step 10, as recorded in
`Docs/Art/README.md`.

## Editor and repository state after build preparation

The temporary scene folder was deleted through Unity. Original SampleScene is
open, stopped and clean; Windows 64-bit is active again. Development Build,
Script Debugging, Autoconnect Profiler and Run In Background are off.

Four inspected Unity build-generated settings diffs (URP prefilter/runtime lists,
volume serialization, preloaded assets and Android batching) were restored to the
clean pre-build versions. No production code, scene, prefab, settings or package
changes remain. This document is the only tracked change from Step 9.
