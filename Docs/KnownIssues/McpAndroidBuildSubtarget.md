# MCP Android build can silently select PVRTC

Status: Open third-party tooling defect; explicit Android build invocation required. Do not modify the MCP package as part of the release workaround.

MCP v10.0.0's unprofiled `ManageBuild.HandleCommand` path calls `BuildTargetMapping.ResolveSubtarget`. That resolver returns `StandaloneBuildSubtarget.Player` (2) when the subtarget is absent, and for every value except `server`. Android interprets 2 as `MobileTextureSubtarget.PVRTC`. `BuildPlayerOptions.subtarget` also persists that choice in the Editor-local Build Profile, so correcting the profile before calling the same helper does not prevent recurrence.

Symptoms:

- The generated manifest adds `<supports-gl-texture android:name="GL_IMG_texture_compression_pvrtc"/>`; Play filters out devices without that format, including the S23's Adreno GPU.
- A fresh import can produce **1,108 obsolete PVRTC warnings** and a splash-compression fallback. Both the original broken Phase 12 build and Phase 17 Step 5 had **1,527 total warnings**. A warm import can omit those warnings while retaining the defect.
- Phase 17 code 1 reported only 3,151 supported devices. It was uploaded as an Internal draft and never rolled out.

Safeguard for every Android build:

1. Keep the intended default in tracked Android Player Settings (**ETC2** for this GLES 3.0+ game); set the local profile to **Use Player Settings** (`MobileTextureSubtarget.Generic`, 0).
2. Bypass the unprofiled MCP resolver. Supply `BuildPlayerOptions.subtarget = (int)MobileTextureSubtarget.Generic` explicitly, or use a verified profile-aware Editor build. Passing `ETC2` or `Generic` as a string to this resolver does not fix it.
3. Record the actual invocation and read back the effective subtarget after the build. Inspect the actual AAB's texture/feature/screen filters against the known-good Phase 16 APK; reject PVRTC. After upload, confirm version-specific Play support for SM-S911B, an Adreno and a Mali device before rollout.
4. Rerun both complete test suites after tracked settings changes. Check sprite/highlight quality through the Play-installed replacement; preserve separate upload, rollout and uninstall approvals.

History: [Phase 12 discovery](../Validation/PHASE12_STEP9_ANDROID_BUILD.md), [Phase 13 safeguard](../Phases/PHASE13_EXECUTION.md), [Phase 17 release checks](../Phases/PHASE17_EXECUTION.md), [release evidence](../Release/RELEASE_LEDGER.md). Review this tooling limitation in subsequent phases. An upstream report is optional.

References: [Unity subtarget persistence](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/buildplayeroptions/subtarget), [Android texture-format filtering](https://developer.android.com/guide/topics/manifest/supports-gl-texture-element).
