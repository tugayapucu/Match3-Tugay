# Phase 16 Step 12 — Android Back

Date: 2026-11-11. Status: reviewed and approved by Claude; no blocking findings or production follow-ups.

## Commit reading order

1. `ca227bd` — Handle Android Back through the existing game flow. Read GameFlowController's action lifecycle and state dispatch, then the 11 Back tests in GameFlowBackTests.cs.
2. This separate validation record: device-discovered defect, final policy, Editor and S23 evidence, known warnings and restoration checks.

The implementation and tests were committed unchanged from the approved version. The previously reported tests apply; committing did not require another test run.

## Policy

| Flow state | Back behavior |
| --- | --- |
| Main Menu | Background the Android task; no quit confirmation |
| Settled Playing | Open the existing Leave confirmation |
| Resolving, including a visible outcome before cleanup | Ignore; do not queue navigation |
| Restart or Leave confirmation | Cancel, retaining the session and selected tile |
| Settled Won/Lost, including final completion | Return to Main Menu |

This chooses Step 12 Option A: leaving an unfinished level requires confirmation. It adds no pause system or exit dialog.

## Implementation and learning notes

Only `GameFlowController` production code changes. It creates one `<Keyboard>/escape` button `InputAction` in Awake, enables/disables it with the component, and disposes it on destruction. The performed callback calls `HandleBack()`. There is no new Update method, InputActions asset or scene wiring.

`HandleBack()` first refuses uninitialized or busy flow, then reuses `ReturnToMenu()` or `CancelConfirmation()`. It does not replace sessions, bypass Bootstrap's input gate, change progression or interrupt a coroutine. Existing handoff and selection restoration remain responsible for returning board input safely.

On Android, Awake sets `Input.backButtonLeavesApp = false` once. This is a native platform setting, not legacy key polling; Active Input Handling stays Input System only. Input System 1.20's installed `Documentation~/corresponding-old-new-api.md` lists no replacement for this setting. Back events are read through the new action.

Only a Back received while already in MainMenu calls the current Android Activity's `moveTaskToBack(true)` through Unity's Java bridge. Java wrappers are disposed with `using`. Editor MainMenu Escape does nothing. No `Application.Quit`, broad exception handler or navigation framework is introduced. Unity's [Android lifecycle guidance](https://docs.unity3d.com/6000.0/Documentation/Manual/android-quit.html) recommends this method over Quit when backgrounding programmatically.

### Device-discovered defect in the first prototype

The first APK set `backButtonLeavesApp` true on every transition to MainMenu, and false elsewhere. On Win → Back, the screenshot showed the Android launcher instead of the menu; resuming the app showed that navigation had reached the menu. Loss → Back had appeared correct earlier. Changing the native flag during the Back event was therefore unsafe under the tested Android delivery timing.

The final approach keeps that flag false, and backgrounds explicitly only for a separate Back from MainMenu. The first prototype APK and screenshots are historical evidence, not validation of the final APK. They remain under ignored `Builds/Validation/Phase16Step12/` and `Builds/Android/Phase16-Step12/`.

## Editor reproduction

1. Open SampleScene, enter Play Mode and click Play/Continue.
2. Press Escape when settled: Leave opens. Press Escape again: it cancels.
3. Select a tile, open Restart, then press Escape: the same selection cue returns.
4. Start a move and press Escape during its animation: no prompt or deferred navigation; use a fresh Escape afterward.
5. On a settled outcome, Escape returns to Main Menu. Escape in the Editor Main Menu does not quit the Editor.

No manual Inspector assignment is required. For an Android build: File → Build Profiles → Android → Switch Platform; use the existing IL2CPP/ARM64 development configuration and SampleScene. After device validation, switch back to Windows and disable Development Build, Script Debugging and Autoconnect Profiler.

## Automated checks

The new `GameFlowBackTests.cs` adds 11 PlayMode cases covering uninitialized/menu, held Back, a press/release within one input update, both confirmations and retained selection, Loss/Win/final outcomes, listener disable/enable, busy resolution with no deferred request, and the locked outcome callback. They use isolated preference keys.

One initial asynchronous test needed an extra yielded frame: `InputTestFixture.Press/Release` queues events inside a UnityTest instead of processing them immediately. This was a harness correction, not a gameplay fix. A later rerun was interrupted by a NullReferenceException inside Unity Test Framework's `PlayModeRunTask`; that incomplete run is not counted as passing.

| Run | Result |
| --- | --- |
| Focused Back tests, initial implementation | 11/11 |
| Full EditMode | 632/632 |
| Full PlayMode after the Android fix | 331 passed, 3 explicit hardware-timing skips, 0 failed |

The final PlayMode job is `aa05943a120f44fc96a34d4547ba4d9c`. The Android-only backgrounding branch requires the device checks below; Editor tests cannot prove its native behavior.

## Android build and physical-device checks

Samsung Galaxy S23, SM-S911B; Android 17 / API 37; 1080×2340. The user physically checked Main Menu Back/resume, settled play, both confirmations and busy play. Outcome routes use individually screenshot-guarded ADB Back and touchscreen swipes on the real player. They bypass no game handlers or Core rules. Predictive-Back animation quality and different Android versions/devices are unverified.

Final APK: `Builds/Android/Phase16-Step12/Match3-Phase16-Step12-Development.apk`, 70,587,883 bytes (about 67.32 MiB), Android IL2CPP ARM64, Development, ETC2, no test assemblies or profiler/debugger attachment. Source: `de810e7` plus this uncommitted GameFlow change. Build succeeded in 37.26 seconds with 0 errors and 4 warnings.

SHA-256: `ae31dffd4fff85ba65a6d399bdecbeb989d3cf3bba7d7194b8d2b7850904eb3b`.

Warnings: "Pipeline: No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime). Pipeline will be disabled in Player builds", plus three IL2CPP notices that large TMP methods were split into their own C++ files. These are reported as build warnings, not attributed to Back or silently removed. No pipeline/package settings are changed to suppress them. Final device results follow below.

Human checks on the final APK: Main Menu Back backgrounds the app, reopening shows the menu; settled Playing Back opens Leave; Back cancels Leave and restores the selected tile cue; Back cancels Restart without resetting. All confirmed by the user. The user also confirmed Back was ignored during animation, the move finished, and a fresh Back worked afterward.

The first automated attempt on the final APK is invalid: its initial screenshot still showed the Unity splash screen, then its taps continued on the launcher and opened Spotify. A fixed two-second delay was incorrectly treated as readiness. That run is excluded. No unattended coordinate sequence will be reused: verify the foreground package and visible expected game screen, act once, then inspect; stop on any mismatch and ask the user.

| Final APK device check | Result / evidence |
| --- | --- |
| Main Menu Back and reopen | User: Home screen, then Main Menu |
| Settled Playing Back | User: Leave confirmation opens |
| Back on Leave | User: cancels and same selection cue returns |
| Back on Restart | User: cancels without resetting |
| Back while animating, then fresh Back | User: ignored while animating; fresh Back works after settlement |
| Settled Loss Back | Guarded ADB: Out of Moves, 0 moves / 23/25 / 9,400; Back returns to Main Menu, game remains foreground |
| Settled Win Back | Guarded ADB: Level Complete, 16 moves / 25/25 / 8,400; Back returns to Main Menu with Continue, game remains foreground |

Final outcome evidence is `l20-lost.png`, `l21-loss-back-menu.png`, `w04-won.png`, `w05-win-back-menu.png`, and `verified-device-results.json` under ignored `Builds/Validation/Phase16Step12/`. The screenshots and foreground checks were inspected after each action. The single-action helper refused input when a notification changed the expected screen. No game input was sent until the previously inspected game screen returned. The retired unattended script is disabled.

Final-completion Back shares the Outcome branch and is covered by an Editor test; the six-level final ending was not replayed on the phone in this step. The exact locked outcome callback boundary is covered in PlayMode; the human busy check is an observation, not frame-level phone instrumentation.

The unchanged Level 1 losing witness uses 20 moves and ends at 0 moves, Red 23/25, score 9,400. The winning witness is `(4,0)→(4,1)`, `(3,1)→(3,2)`, `(1,0)→(1,1)`, `(3,5)→(4,5)`, ending at 16 moves, 25/25, score 8,400. Coordinates start at bottom-left. The losing route was prepared using a runtime-only Editor probe; it does not establish human difficulty. Its move list and screenshots remain in the ignored evidence folder.

The device log reproduces the already documented `AssetPackManager` ClassNotFoundException at cold startup ([Phase 13 audit](../Phases/PHASE13_RELEASE_READINESS_AUDIT.md)). It is not a new Back finding, and is not assumed harmless. No fix or package change is attempted in this step.

## Final repository and Editor state

Exactly four intended paths: `Assets/Scripts/Presentation/GameFlowController.cs`, `Assets/Tests/PlayMode/GameFlowBackTests.cs`, its `.meta`, and this report. No Core, scene, prefab, catalog, board/input-controller, package or project-settings changes. All 42 backed-up scene/settings/package files match their pre-build bytes after restoring the four build-generated settings diffs. `git diff --check` passes.

Editor: StandaloneWindows64, SampleScene active and unmodified, Play Mode off, compilation idle; Development Build, Script Debugging, Autoconnect Profiler and Run In Background false. Only native Keyboard/Mouse remain. Host progression is unchanged at 2. Original phone preferences were restored byte-for-byte after the test win; no DeleteAll or uninstall was used. The final APK remains installed and the game was reopened without any coordinate input.

Console: no C# compiler or game runtime errors found in the final checks. Build/URP messages remain: the four build warnings above, the volume-profile override notice, URP build inclusion and debug-shader stripping messages. This is not an empty/warning-free Console claim. The known AssetPackManager startup error remains in device logs; no Back-related managed exception was found.

The user requested separate implementation and documentation commits and a push after Claude's approval. Step 13 has not started.
