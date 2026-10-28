# Phase 12 Step 9 - Android development build

Validated: 2026-10-28, Unity 6000.5.10f1 on Windows.

Result: Android development APK successfully built and inspected. No physical Android device was connected during the original build validation. A subsequent installation/launch check is recorded below; it does not establish the remaining device-runtime checks.

## Final Android configuration

| Setting | Value |
| --- | --- |
| Application identifier | `com.tugaykapucu.match3` |
| Version / version code | Existing `1.0` / `1`, unchanged |
| Minimum API | 26 (Android 8.0) |
| Target API | Automatic; resolved to 36 in this APK |
| Orientation | Portrait |
| Android default quality | Medium, index 2; distinct from the Editor's Very Low selection |
| Scripting backend | IL2CPP |
| Target architecture | ARM64 only |
| Texture build subtarget | ETC2 |
| Input handling | Input System Package (New) only, package 1.20.0 |
| Runtime frame target | Existing Android-only 60 FPS request from Step 7; device FPS unmeasured |
| Optimized frame pacing | Already enabled, unchanged |
| Scene | `Assets/Scenes/SampleScene.unity` only |
| Output | APK; App Bundle and Export Project off |
| Build options | Development and DetailedBuildReport; no test assemblies, script debugging or profiler autoconnection requested |
| Signing | Unity debug signing; Custom Keystore off |
| Forced Internet / external-storage permissions | Both off, unchanged |

The only retained project-setting change is the Android application identifier. No source, scene, prefab, art, package or signing/store metadata changed.

## Tooling

Unity's configured external-tool paths point to its bundled `Editor/Data/PlaybackEngines/AndroidPlayer` tools:

- Android SDK platform 36 and Build Tools 36.0.0 were used.
- Android NDK 27.2.12479018 (r27c).
- OpenJDK 17.0.18.
- Gradle 9.1.0.
- Android Build Support, IL2CPP and ARM64 support were already installed. Nothing was reinstalled.

## APK verification

- Path: `Builds/Android/Match3-Phase12-Development.apk` (Git-ignored).
- File size: **51,193,978 bytes**, approximately **48.82 MiB**.
- SHA-256: `891856A9142B4A12B886A5230CA000F016E19E0EDCD54E258C5861791075FB43`.
- Final build job: `build-7c7ffef624`, succeeded in approximately 29.1 seconds with zero errors.
- `aapt` confirmed the identifier, minimum API 26, target API 36, debuggable flag and portrait activity declaration.
- Native code is `arm64-v8a`; the APK contains `libil2cpp.so` and `libunity.so`.
- `apksigner verify` passed, with one signer and APK Signature Scheme v2.
- Declared permissions: `android.permission.INTERNET` and `com.tugaykapucu.match3.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`. The latter comes from AndroidX Core according to the manifest merger report. No camera, microphone, location or storage permission is declared.

Unity's BuildReport reports a larger aggregate size (897,835,709 bytes). That is not the APK file length; the artifact size above was measured directly from the APK.

## Warnings and build-tool workaround

The final build has **415 warnings**, not a warning-free result:

| Category | Count | Disposition |
| --- | ---: | --- |
| Unity/render-pipeline package shader warnings | 414 | Precision conversion, truncation and loop-unrolling warnings; no project shader change made |
| Missing optional `RuntimePipelineConfig` | 1 | The existing `com.unity.pipeline` code-reload package disables its Player runtime feature; this is separate from URP rendering |

Platform-switch compilation also reported an existing MCP package CS0618 warning about an obsolete `EntityId` conversion. No package code was changed to suppress warnings.

The first build also succeeded, but reported 1,527 warnings. The packaged MCP `manage_build` tool defaults `BuildPlayerOptions.subtarget` to desktop `StandaloneBuildSubtarget.Player` (2). Android interprets 2 as PVRTC, causing 1,108 obsolete-compression warnings and a splash fallback. That invocation was replaced by the same Editor build pipeline with the correct Android value, `MobileTextureSubtarget.ETC2` (5). The MCP package itself was not modified. The final APK replaced the first artifact.

For scripted Android builds, explicitly select the Android texture subtarget; do not pass a desktop subtarget. For manual builds, use Android's ETC2 texture-compression selection in Build Profiles. The texture subtarget is an Editor-local build choice, not a PNG or prefab edit.

Raw first/final warning lists are in `Builds/Android/FirstBuildWarnings.txt` and `FinalBuildWarnings.txt`; those generated files are Git-ignored. The categories and counts above are the permanent record.

## Manual reproduction

1. Open **File > Build Profiles**, select Android and **Switch Platform**.
2. In **Edit > Project Settings > Player > Android > Other Settings**, check the identifier, API levels, IL2CPP, ARM64 and Input System-only settings listed above. Keep Portrait under Resolution and Presentation and Custom Keystore off under Publishing Settings.
3. In Android Build Profiles, select ETC2 texture compression, enable Development Build, and leave Script Debugging, Autoconnect Profiler, Build App Bundle and Export Project off. Include only SampleScene.
4. Select **Build**, not Build And Run, and save to the APK path above.
5. Inspect the Build Report and APK manifest/signature. Build success does not establish device-runtime correctness.
6. Restore Windows 64-bit with **Switch Platform** and restore the previous Development Build toggle (off).

References: [Unity Android build/signing guide](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/building-and-delivering/build-process), [Android Player settings](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/getting-started/class-player-settings), [platform-specific build subtargets](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/buildplayeroptions/subtarget).

## Editor restoration and regression validation

- Policy B: **StandaloneWindows64 restored**. Android is not left active. This is the expected final Editor platform before Step 11 unless a later approved task changes it explicitly.
- Development Build restored to off; script debugging and profiler autoconnection remain off. Android's local texture subtarget is ETC2 for future builds.
- SampleScene open, stopped, unmodified; one scene loaded. Editor compilation completed.
- Build-generated URP asset changes and the default batching-cache entry were restored. Build processors cleaned up temporary performance-test resources; the Input System preload entry was also restored.
- **540/540 EditMode** and **166/166 regular PlayMode** tests passed after restoration. The three explicit frame-timing tests were intentionally skipped; Step 9 does not repeat Step 7's timing measurements.
- Final test Console contains only the expected Purple negative-test exception and no-room warning as errors/warnings. They are separate from the successful Android build's zero errors and recorded build warnings.

## Physical device check

The developer installed the APK and confirmed that it runs on a Samsung Galaxy
S23 (SM-S911B, 1080 x 2340) in portrait. An ADB screenshot of the running game is
saved with the [tall-screen comparison record](Phase12TallScreenComparison/README.md).
The [tall-screen composition issue](../KnownIssues/TallScreenComposition.md) was
observed on this device and is deferred to UI polish; the original centered
framing is retained. No experimental-bias APK was installed.

| Check | Verified status |
| --- | --- |
| APK installation, launch and portrait presentation | Confirmed by developer and running-game screenshot |
| Device resolution/model | 1080 x 2340 / SM-S911B, confirmed through ADB |
| Android version / active display refresh rate | Unverified; the device was not ADB-authorized when writing this follow-up |
| Delivered game FPS | Unverified; the 60 FPS request is not a measurement |
| Edge taps, valid/invalid moves and input locking on the device | Unverified |
| Cascades, all specials and Restart behavior on the device | Unverified |
| Win/Lose and Play Again on the device | Unverified |
| Outcome dimmer strips on the physical display | Unverified |

## Remaining device checklist (not executed)

When a device is available, record its model, Android version, resolution, refresh rate and actual delivered FPS. Check launch and portrait orientation; safe-area layout; touch selection, valid swaps and invalid rollback; cascades and all special effects including Wisp flight; HUD, Win/Lose, Restart and Play Again; and input/restart locking during resolution. No keyboard should be required.

On a 90 Hz or other high-refresh device, measure the actual rate rather than assuming the 60 FPS request guarantees 60 or switches the display mode. Check the [known outcome-dimmer limitation](../KnownIssues/OutcomeDimSafeArea.md) on the physical screen. Device installation is optional for Step 9; this checklist does not claim Step 10 runtime review has been performed.
