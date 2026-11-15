# Galaxplode release ledger

Phase 17 is in progress. Step 5 established clean-clone reproducibility; Step 6 produced the original upload-key-signed AAB. Code **1** remains an unrolled Internal draft. Step 8 found and corrected its PVRTC filtering defect: replacement **0.1.0 (2)** passed local validation and cleanup, but **has not been uploaded**; the track remains inactive.

## Approved identity and open Console checks

- Galaxplode / Atik Games; package `com.tugaykapucu.match3`; English (United States); free, no ads initially.
- Codex implements; Claude independently reviews; ChatGPT advises; the human owns secrets, irreversible operations and Play Console decisions.
- Outstanding Console checks: trader-status compliance, version-code acceptance, support email and actual PC distribution state; see Step 7 below.

## Upload certificate — human-verified Step 3

- PKCS12, alias `upload`, RSA 4096, SHA256withRSA, certificate `CN=Atik Games`.
- Created November 13, 2026; **valid until March 31, 2054, 20:37:52 TRT**.
- Public upload-certificate SHA-256: `50:85:36:9B:8C:ED:D8:6F:33:53:01:B6:2F:E5:28:E2:02:FD:C8:13:CD:C0:F9:C2:C3:F2:5E:E5:6A:8F:54:17`.
- Human verified the primary outside the repository/synced folders, a USB backup with matching file hashes that opened successfully, and separate password-manager storage. This is the upload certificate, not the later Play app-signing certificate.

## Step 4 — reviewed 2026-11-14

Commit: `df773ab9cd5e7b2c98cef4cf0b899ac1e00e192f` — Configure Galaxplode Android test release.

- Applied Galaxplode / Atik Games, version `0.1.0`, target API 36 and Diagnostics off. Package, code 1 and minimum API 26 were preserved. Tracked settings changed only the four approved PlayerSettings values and the Diagnostics flag.
- Android active; IL2CPP Release, ARM64, portrait, Minimal stripping, Vulkan/OpenGLES3, quality, Internet Access Auto, Back opt-out and the 60 FPS policy unchanged. Local profile: AAB, Public symbols `IncludeInBundle`, Development/Script Debugging/Export Project off.
- Android Diagnostics default/effective getters returned false; override `ProjectSettings (2)` inherited the disabled default. **Settings-only evidence**, without an artifact marker or claim about uploads.
- Validation: 11 invariant checks, 29 focused EditMode tests and 16 real-scene PlayMode tests passed. The initial 1086×607 landscape run had five failures; all passed at 1080×1920 portrait. See [the Game view dependency](../KnownIssues/SceneTestsGameViewDependency.md).
- Nine known URP `TraceVirtualOffset` precision import warnings were recorded. Final scene-test Console contained the declared camera warning and framework messages, with no unexpected project errors. SampleScene was clean; Play Mode stopped; signing fields were off/empty. No build occurred in Step 4.

## Step 5 — reviewed 2026-11-14

| Check | Result |
| --- | --- |
| Clean source | Exact `df773ab` commit above; no copied Library or embedded MCP package; Android import succeeded |
| Unity / target / test view | 6000.5.10f1 / Android / **1080×1920 portrait**, verified before real-scene tests |
| MCP resolution | Git v10.0.0 at `d49ae2953580f3481beb1e084a1da2682f0b5610` |
| MCP comparison | **699 files each, byte-identical**, including `package.json`; no missing/extra files, excluding VCS/build/cache directories |
| MCP normalized tree SHA-256 | `09d5b4fbf2699b97106f5fadc91380305537942d58eb8f4aa9b0b9e9c77b4d70` in both copies |
| Full EditMode | **632 passed, 0 failed, 0 skipped**, 7.13 s |
| Full PlayMode | **344 passed, 0 failed, 3 explicit skips**, 102.89 s |
| APK build | **Succeeded**, 0 errors, 249.36 s; non-development IL2CPP Release / ARM64 |
| APK | `Builds/Android/Phase17Step5/Galaxplode-Phase17-Step5-df773ab.apk`; **39,881,181 bytes** (38.03 MiB) |
| APK SHA-256 | `7636003303b343118007fd81fb16052ad2dbc8e135ca82f8d18ef14fdcfc0acf` |
| Public symbols ZIP | `Builds/Android/Phase17Step5/Galaxplode-Phase17-Step5-df773ab-0.1.0-v1-IL2CPP.symbols.zip`; 21,209,486 bytes |
| Symbols SHA-256 | `d4670ee7eeeb761d8d15600494c80a31c7bd058b76dc5edae4119e371702073d` |
| Signature | `apksigner verify` passed; `CN=Android Debug`, not the upload key; certificate SHA-256 `c96823360f47a45217a2e94a30c7a206c4b3a2dd4930704edfb5efdf74b93dd1` |
| APK manifest / native code | Galaxplode, `com.tugaykapucu.match3`, `0.1.0 (1)`, SDK 26/36/36, portrait, seven arm64-v8a libraries, not marked debuggable, effective Back opt-out false |
| APK permissions | `INTERNET` and the app's dynamic-receiver permission present; `ACCESS_LOCAL_NETWORK` absent |
| Reopened-Editor check | Approved release settings intact; AAB/Public embedded symbols restored; Diagnostics off with inherited override 2; signing off/empty; preloaded assets empty; 1080×1920; clean SampleScene; Editor idle; zero Console errors/warnings |

- The three PlayMode skips were only `FrameRateTimingTests.Animations_AtReal30Fps`, `Animations_AtReal60Fps` and `Animations_AtReal120Fps` (`Skipped:Explicit`). Both full suites passed before any AAB; no test code changed.
- This APK used AAB off and Public symbols ZIP; Development, Script Debugging and Export Project stayed off. The upload key was untouched. APK/symbol copies were hash-verified in ignored `Builds/` folders. The BuildReport's 891,218,762-byte aggregate includes generated/support files and is not the APK size.
- The build's **1,527 warnings** comprise 1,108 obsolete PVRTC, 414 shader, one disabled-runtime-Pipeline, one splash-logo compression fallback and three TextMeshPro C++-file messages. No C# compiler warnings occurred. The nine distinct known `TraceVirtualOffset` D3D11 messages appeared twice (18 log lines); other warnings were classified separately. No settings were changed to silence them.
- The reviewed four build-noise files and clone-only `.mcp.json` rewrite were restored with the clone Editor closed; reopening verified the result. Only the expected MCP lockfile delta remained: embedded source became Git plus its hash, with unchanged dependencies and other package entries. The developer workspace's embedded copy and lockfile were left unchanged; normalization remains a separate decision.
- The APK includes `MCPForUnity.Runtime.dll` and `Unity.Pipeline.Attributes.dll`, without the runtime Pipeline server or startup records for those packages. **Step 9 finding:** Diagnostics was configured off and `INTERNET` remained; disabling it did not remove the permission. No direct networking API uses were found in the project's C# scripts or MCP's eight Runtime C# files. Engine modules and packages are the next investigation focus. These observations do not settle runtime collection, and the unbuilt release AAB's permissions remain unverified.
- The main workspace retained its release configuration and clean SampleScene. No gameplay, architecture, graphics, quality, stripping, frame-rate or package-version changes, device installation/uninstallation, release AAB, Play Console action or push occurred. Step 6 has not started.
- Detailed comparison, tests, build report, tracked diffs and post-cleanup readback are in ignored `Builds/Validation/Phase17Step5/`. The [execution plan](../Phases/PHASE17_EXECUTION.md) owns procedures and remaining release gates, including the AAB's own warning/permission review.

## Step 6 — reviewed 2026-11-14

| Check | Result |
| --- | --- |
| Source / Unity | `a569a9e0d9f6d21865777e10ee16e31aee8a1c87` / 6000.5.10f1; developer workspace |
| Full-suite gate | Reused Step 5's **632 EditMode passed; 344 PlayMode passed, 3 approved explicit timing skips, 0 failures** at `df773ab`; no `Assets/`, `Packages/` or `ProjectSettings/` changes between that tested commit and this build's source commit |
| Build | **Succeeded**, 0 errors, **418 warnings**, 289.72 s; non-development IL2CPP Release / ARM64; Minimal stripping; approved graphics and frame-rate settings unchanged |
| AAB | `Builds/Android/Phase17Step6/Galaxplode-InternalA-0.1.0-v1.aab`; **60,945,047 bytes** (58.12 MiB) |
| AAB SHA-256 | `d93d9cea594220222e8bfcd1d70ec8580080842fa4267df3bef4f1f5361b7e81` |
| Identity / SDKs | Galaxplode; `com.tugaykapucu.match3`; `0.1.0 (1)`; minimum 26, target/compile 36; portrait; game category; not debuggable |
| Upload signature | `jarsigner -verify` exited 0, `jar verified`; public certificate matches Step 3's SHA-256 above; all **449 payload entries** separately read and digest/signature-verified against that certificate; no duplicate ZIP entries |
| Public native symbols | Seven `.so.sym` entries embedded under `BUNDLE-METADATA/com.android.tools.build.debugsymbols/arm64-v8a/`, including `libmain`, `libunity` and `libil2cpp`; all have `.symtab`, none have `.debug_info`; artifact remains ignored with its exact AAB |
| 16 KB ELF / RELRO | All seven shipped ARM64 `.so` files pass `LOAD` alignment ≥ `0x4000` and the applicable RELRO rule; generated APK native bytes match the AAB |
| Generated APK packaging | Bundle config: `PAGE_ALIGNMENT_16K`; both locally generated API-36 ARM64 APKs pass `zipalign -c -P 16 -v 4`; all native entries are **compressed**, with `extractNativeLibs=true`, so native ZIP alignment is not applicable |
| Predictive Back | Application declares `enableOnBackInvokedCallback=false`; launcher `com.unity3d.player.UnityPlayerGameActivity` has no override; configuration verified, not device behavior |
| Permissions / components | `INTERNET` and the app's signature-level dynamic-receiver permission only; **no `ACCESS_LOCAL_NETWORK`**. Unity launcher and non-exported AndroidX Startup provider (EmojiCompat/Lifecycle initializers); no services or receivers |
| Runtime package records | `MCPForUnity.Runtime.dll` and `Unity.Pipeline.Attributes.dll` present; runtime Pipeline server absent; neither package has a runtime-initialize record |
| Diagnostics | Android effective setting false, inherited project override `2`; no reliable Diagnostics marker found in `boot.config`/manifest: **settings-only verification**, not proof of absent uploads |
| Cleanup / reopened Editor | Signing flag off, path/alias empty, preloaded assets empty; the four reviewed build-noise files restored only after Editor exit; reopened Android release settings intact, clean SampleScene, idle, **0 Console errors / warnings** and four MCP connection logs |

- AAB producer: bundletool **1.18.3**, recorded in its `BundleConfig`. Local validation and APK generation used Unity's bundled **1.17.2** (`validate` passed), Build-Tools **36.0.0** (`aapt2`, `zipalign`), bundled OpenJDK and NDK `llvm-readelf`. Generated APKs use local debug signing solely for package inspection; they were not installed. Their layout is not claimed identical to Play's APKs: **App Bundle Explorer confirmation remains required for Play-generated packaging**.
- Signature warnings retained: self-signed/untrusted upload certificate, no timestamp, unsigned ZIP permission attributes, and `JarFile`/`JarInputStream` inconsistency because `META-INF/MANIFEST.MF` is the last entry. The explicit `JarFile` payload check passed; streaming-manifest placement is not silently repaired. [Java documents this ordering requirement](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/util/jar/JarInputStream.html). Play acceptance remains unverified.
- Approved fallback, only if Play rejects the signature layout: preserve the original and re-sign the same built AAB into a separate output using `jarsigner` and the same upload key, with the human entering the password interactively. No Unity rebuild is needed. Verify the replacement's signature and upload-certificate fingerprint, rerun bundle validation, and record its new SHA-256 before any upload; the original hash above must not identify the replacement.
- Build warnings: **414 shader** (including the same nine distinct `TraceVirtualOffset` precision warnings repeated twice), one disabled-runtime-Pipeline warning and three TextMeshPro large-C++-file notices. No new warning category or C# compiler warning identified versus Step 5. Native Unity Console counts after the build: **0 errors, 13 warnings, 5 logs**; MCP mislabels some warning entries as errors/exceptions, so the native counts and BuildReport were checked directly. The BuildReport's 882,693,464-byte aggregate is not the AAB file size.
- The **1,527 → 418** warning drop is exactly **1,108 PVRTC import notices plus one splash-logo compression fallback**. Those warnings arise during asset import; this developer workspace had already imported the assets, unlike Step 5's clean clone. The lower count reflects cached import work, with no importer, graphics or quality setting changed to suppress warnings.
- The matching `Galaxplode-InternalA-0.1.0-v1_BackUpThisFolder_ButDontShipItWithYourGame/` IL2CPP folder is retained beside this AAB under ignored `Builds/Android/Phase17Step6/` (523 files). It is associated with this build and excluded from source control and player distribution.
- Human reports no previous Play upload for this package; code 1 is a candidate based on that history, **not Console-verified availability**. `INTERNET` investigation remains Step 9. App Bundle Explorer confirmation and Play acceptance are pending; **not runtime-tested on 16 KB**. No upload, Play app creation or S23 operation occurred.
- Detailed reports, certificate-only output, per-library headers, symbol hashes, generated-APK inspection, signature checks, build-noise diff and final Editor readback are in ignored `Builds/Validation/Phase17Step6/`. Only this ledger changed in tracked files; no gameplay, scene, prefab, package or settings change remains. Step 6 implementation and local validation are complete and reviewed by Claude; the human's Step 7 Console-app creation has not started. Internal upload/rollout is the separate Step 8 gate.

## Step 7 — completed and reviewed 2026-11-15

| Check | Result |
| --- | --- |
| Evidence | Human readback from Console's Switch apps screen on November 15; no direct agent access |
| Account | Personal; 12-testers/14-continuous-days Closed-test requirement applies before applying for production access |
| App | **Galaxplode / `com.tugaykapucu.match3` / Draft**; last updated November 14, 2026; original package retained |
| Creation approval / form | Explicit human approval preceded submission; **en-US / Game / Free** confirmed |
| Declarations | Human personally accepted Developer Program Policies, Play App Signing terms and US export/encryption-compliance certification |
| Dashboard | Internal available but not started; store setup incomplete; **0 opted-in testers**; production access unavailable; Closed clock not started |
| Artifact acceptance | At Step 7 closure, no AAB was uploaded; subsequent upload/processing evidence is in Step 8 |
| Missing controls | Support email and PC settings absent from creation form and not located afterward; actual settings unverified, no distribution change made |

- **Trader status:** outstanding, due **February 1, 2027**. [Google's guidance](https://support.google.com/googleplay/android-developer/answer/17594752?hl=en-GB) lists paid apps, IAP and ads as profit signals. A hobby/non-commercial activity is "unlikely to be a trader"; no revenue alone does not settle the classification. A personal-account trader must provide a publicly displayed phone number and address (announced display from March 1, 2027). Adding ads later requires rechecking the declaration. The choice is the human's; no trader declaration submitted. Recheck any enforcement before publishing.
- **Deferrals:** support email to **Step 13**, subject to any earlier Console requirement; PC distribution to **Step 8 before rollout approval**. Read-only route: select Galaxplode → **Test and release → Advanced settings → Form factors → Google Play Games on PC → Manage**, if present. [Console Help](https://support.google.com/googleplay/android-developer/answer/13295490) uses “Test and release”; [Google's PC guide](https://developer.android.com/games/playgames/development-submit) labels the parent “Release.” If absent, resolve the actual state before rollout; do not add/remove a form factor or save changes without human approval. The AAB is **ARM64-only, portrait/touch**; PC compatibility and opt-out are unverified.
- **Step 14:** human approved adding a Turkish (`tr-TR`) store listing later, retaining en-US as the default; no translation implemented here.

## Step 8 — uploaded Internal draft 2026-11-15; post-upload verification pending

| Check | Result |
| --- | --- |
| Release A | Exact Step 6 file/hash rechecked and unchanged; no tracked production-source/settings drift since `a569a9e`; original draft uploaded without a rebuild. The subsequently identified local texture-subtarget defect requires a separately approved replacement |
| Console version history | Empty history confirmed by the human before upload; afterward, code **1** processed and appeared as **0.1.0 (1)** |
| Release / track | Human confirms **Internal A - 0.1.0 (1)** / **Draft** / Internal **Inactive**; no publication or rollout |
| Reported upload certificate | Human-reported SHA-256 `50:85:36:9B:8C:ED:D8:6F:33:53:01:B6:2F:E5:28:E2:02:FD:C8:13:CD:C0:F9:C2:C3:F2:5E:E5:6A:8F:54:17` matches the Step 3/Step 6 upload certificate |
| Active Play app-signing certificate | Human copied SHA-256 from the **App signing key — In use** panel: `66:25:7F:76:FA:79:1B:62:6D:B6:B7:D2:B5:77:82:C0:DC:DF:C4:F2:17:C1:A2:F3:B7:0D:9B:B9:96:26:DC:B0`; matches Console's Digital Asset Links snippet and differs from the upload certificate |
| Other signing keys | Screenshot also shows a Previous app signing keys row marked Quantum-ready (beta). No further beta-key investigation required: accept an installed certificate matching Console's **in-use** key or one explicitly listed as applicable, together with installer `com.android.vending` |
| Play 16 KB / native details | For **0.1.0 (1)**, human reports **Memory page size: Supports 16 KB**, ARM64-v8a, API 26+, target 36, GLES 3.0+. Play's generic caveat about undetected libraries/page-size assumptions is retained; independent static confirmation obtained, **16 KB runtime remains untested** |
| Other Play details | 3,151 supported devices; `GL_IMG_texture_compression_pvrtc` (also present in local AAB). Human supplied a device-catalog CSV after personally accepting its terms; agent made no Console change |
| S23 compatibility concern | CSV has **3,151 rows**, matching the reported supported total; no Galaxy S23/SM-S911/dm1q match, no Qualcomm/Adreno GPU entries, and 2,990 PowerVR entries. It contains no targeting-status column and the export's tab/filter is unconfirmed. This is consistent with PVRTC filtering but does not establish the exact S23 exclusion reason. Resolve the Console's S23 targeting status before rollout; no rebuild or exclusion change authorized from this inference |
| S23 model readback | The Console row **SC-51D / Android 16 / Adreno 740 / Unsupported** is a different S23 variant. Claude's read-only ADB check on **2026-11-13** confirmed the physical phone as **SM-S911B / Android 17 / 4,096-byte pages**, with a debug certificate reported in abbreviated form as `c968…3dd1`. Check **SM-S911B for code 2** specifically; re-read the current full installed certificate before any separately approved uninstall |
| Effective texture-setting defect | Read-only Unity inspection found **Android build-profile subtarget PVRTC (2)**, player texture-format list `ETC`, import-compression override `NoOverride`. The built AAB and Play metadata advertise only PVRTC. This confirms a local build-profile override missed in release preflight; [Unity documents the per-build texture selection](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/building-and-delivering/build-settings). It strongly explains the PowerVR-heavy targeting/unsupported Adreno model, while the exact Console exclusion reason remains unavailable |
| Root cause, traced 2026-11-16 | The Step 5 build call (2026-11-13 22:45:44 UTC) and Step 6 call (2026-11-14 15:13:47 UTC) both invoked MCP `ManageBuild.HandleCommand` for Android without a texture subtarget. `ManageBuild` → `BuildTargetMapping.ResolveSubtarget` defaults to **desktop Player (2)** → `BuildRunner.CreateBuildOptions` passes that value to `BuildPlayerOptions.subtarget`. Android interprets **2 as PVRTC**. Unity persists this build option into Editor-local settings. No Step 4 texture-setting assignment was found; the two build calls account for both workspaces. This is the same trap already documented in Phase 12 Step 9 and Phase 13's execution plan, missed again in Phase 17 |
| Manifest comparison | Re-inspected the known-good Phase 16 Step 12 APK: **no `supports-gl-texture`**. Step 5's APK adds **PVRTC**, while the remaining screen/feature/GLES filtering declarations match. Fresh extraction from the unchanged Step 6 AAB confirms PVRTC too. Independently checked all ten available final Phase 12–16 APKs: none has PVRTC, confirming Claude's comparison. Phase 17's build calls introduced the regression; the older S23 smoke evidence is not invalidated |
| Approved correction, applied 2026-11-16 | **Tracked Android Player Settings Texture Compression Formats = ETC2**, local Build Profile **Use Player Settings (Generic, 0)**. The texture-only diff was inspected before changing version code **1 → 2**; these are the only two tracked settings changes. The build invocation must explicitly preserve inheritance, bypassing MCP's desktop-default resolver. Human supplied approval of the fix, full tests, commit and replacement rebuild; no replacement upload or rollout authorized |
| Replacement/version plan | Code **1** was uploaded, **never rolled out**, and is blocked from rollout by PVRTC. Replacement A **0.1.0 (2)** built and passed local checks; **not uploaded**. Later update B remains planned as **0.1.0 (3)**, rechecking availability before upload. Code 1's AAB and IL2CPP backup preserved. Draft bundle removal/addition requires separate human upload approval |
| Required replacement evidence | **Both full suites** after the tracked settings change; repeat signature, manifest, symbols and static 16 KB checks. Compare manifest filtering with the Phase 16 APK; no PVRTC restriction. After approved upload/before rollout, verify materially broader support plus **SM-S911B, an Adreno and a Mali device** in the replacement bundle's device view. Check sprite/highlight quality on the phone after a separately approved Play installation. These checks are now explicit in Step 6 |
| DEX metric | Human reports Low, 6.44 MB uncompressed, 8% obfuscation, no R8 metadata. Below Google's published 50 MB game DEX alert threshold; no R8/build change proposed solely for this metric ([guidance](https://developer.android.com/google/play/vitals/code-optimization)) |
| Play permission list | Human expanded Details: `android.permission.INTERNET`, `com.android.vending.CHECK_LICENSE`, `com.tugaykapucu.match3.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`; no `ACCESS_LOCAL_NETWORK` in the list |
| Automatic protection | Human reports **1 of 1 service active → Prevent unauthorized access → Installer checks on**. Keep this observed setting unchanged for replacement A. [Google documents runtime installer-check code added to Play-generated APKs](https://support.google.com/googleplay/android-developer/answer/10183279): delivered code differs from the uploaded AAB's code. Device evidence must come from a **Play install**, and Step 9 includes licensing/installer traffic as a candidate network source |
| Permission delta | `CHECK_LICENSE` is absent from the local uploaded-AAB manifest. [Google documents it for Play license checks](https://developer.android.com/google/play/licensing/client-side-verification); active installer protection is consistent with a Play-processing explanation, but the exact injection path/runtime behavior is not independently verified. Step 9 must include Play-added protection in the distributed-build evidence; no collection/non-collection conclusion from this permission or setting alone |
| Remaining post-upload checks | Replacement local validation is recorded below. Separate upload/rollout approvals, code-2 Play device/symbol checks, installed signing-certificate comparison and phone sprite quality remain pending. User-provided CSV/screenshot moved intact to ignored `Builds/Validation/Phase17Step8/`; neither belongs in a commit |
| Investigation housekeeping | During the read-only investigation, human closed Unity; process exit confirmed before restoring only MCP-generated `.mcp.json`. No production changes were made in that investigation. The subsequently approved correction and validation are recorded below |
| Reviewer additions | All-profile removal/verification, own-account-only Internal testing, Google-generated signing, Play-installer/certificate checks and available-report triage incorporated into execution-plan Step 8 |
| Gates | Human explicitly approved creating the Internal draft and uploading the unchanged Step 6 AAB with Google-generated signing defaults. Rollout, S23 uninstall/install, PC changes, private-key export and push remain unauthorized |

Signing clarification: the Google-held private key is not retrievable, but [Play supports limited key changes/upgrades](https://support.google.com/googleplay/android-developer/answer/9842756); it is not accurate to call the key fixed forever after the first release. Upload and processing are human-reported evidence, not direct agent access to Console. No key export/change, S23 uninstall/install or rollout has been reported.

Texture-inspection evidence is in ignored `Builds/Validation/Phase17Step8/ReadOnlyTextureInspection.json`. The corrected read-only probe returned an idle Editor, clean SampleScene and zero native Console errors/warnings; no project source/settings change occurred. One tool-only probe first referenced an obsolete enum and failed in-memory compilation, then was corrected using reflected metadata. Release A's compatibility/configuration issue must be reviewed before proceeding to rollout; the previous no-rebuild recommendation is superseded by this concrete finding.

Root-cause evidence and normalized manifest-filter comparison are retained beside it as `TextureBuildCallEvidence.json` and `ManifestFilterComparison.json`. The Step 5/6 warning classifications above are historical: **the 1,108 PVRTC notices indicated a build-configuration defect, not acceptable release noise**. Their absence on a warm import did not clear that defect. [Unity documents subtarget persistence](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/buildplayeroptions/subtarget), and [Android documents Play's texture-format filtering](https://developer.android.com/guide/topics/manifest/supports-gl-texture-element).

### Replacement A — code 2 pre-build validation, 2026-11-16

| Check | Result |
| --- | --- |
| Settings | Android ETC2 serialized under `m_BuildTargetDefaultTextureCompressionFormat`; code **2**, name **0.1.0**. Profile **Generic (0)**. Package, SDK 26/36, IL2CPP Release, ARM64, graphics and frame-rate policy unchanged |
| Test environment | Android target; existing Game view preset 5 read back as **1080×1920** before tests and confirmed afterward. SampleScene active and clean; `runInBackground=false` |
| Full EditMode | **632 passed, 0 failed, 0 skipped**; 7.30 s |
| Full PlayMode | **344 passed, 0 failed, 3 `Skipped:Explicit`** (30/60/120 FPS hardware-timing tests); 102.98 s |
| Console | No project errors or unexpected warnings. Expected no-room camera negative-test warning plus test-runner/tooling messages; MCP again labels the normal results-saving line as Exception while native error count is zero |
| Release configuration | Non-development AAB, script debugging off; Public/SymbolTable symbols included in bundle. Diagnostics project default false, Android profile state 2 (inherit). Before human signing setup: custom keystore false, path and alias empty |
| Evidence / gate | Raw test results and inspection helpers in ignored `Builds/Validation/Phase17Step8/ReplacementV2/`. Validated changes committed before the build; human entered signing passwords locally. Replacement upload, rollout and device uninstall remain separate gates |

### Replacement A — code 2 local artifact validation, 2026-11-16

| Check | Result |
| --- | --- |
| Build source | **`90f35ad4e5313384e931368cecb93634e9efe928`**; settings commit `f9801b2`, guardrails `85067f4`, pre-build evidence `90f35ad`. Both full suites above passed before these commits/build; no production change between tests and build |
| AAB | `Builds/Android/Phase17Step8/Galaxplode-InternalA-0.1.0-v2.aab`; **60,939,391 bytes**; SHA-256 **`41c68a46b59a8ca8eded9421e3144e6f79ba470ae1b4b69f0042398542477b66`** |
| Build invocation / result | Explicit `BuildPlayerOptions.subtarget = MobileTextureSubtarget.Generic` through `BuildRunner.ScheduleBuild`; bypassed `ManageBuild`'s resolver. **Succeeded**, 59.79 s, 0 errors, **1 warning** (known disabled Unity Pipeline runtime). Post-build readback: **Generic (0)**, tracked **ETC2** |
| Filtering / metadata | **No `supports-gl-texture`**. Screen, feature and GLES declarations match the Phase 16 Step 12 APK. Package `com.tugaykapucu.match3`, **0.1.0 (2)**, SDK **26/36**, non-debuggable, portrait, effective Predictive Back callback **false**. Components, permissions, package assemblies and startup hooks match code 1; no unexpected component or package startup hook |
| Permissions / Diagnostics | Local AAB requests only `INTERNET` and the package's dynamic-receiver permission; **no `ACCESS_LOCAL_NETWORK`**. Diagnostics remains supported by settings/inheritance evidence only; no artifact marker or no-networking claim. Play-added installer checks/licensing remain part of Step 9's delivered-build review |
| Signature | `jarsigner` verified; all **449 payload entries** independently digest/signature-verified against the approved upload certificate **`50:85…54:17`**, no duplicate entries. Same signed-entry ordering/self-signed certificate caveats as code 1 retained; no re-signing needed locally. Existing human-password `jarsigner` fallback applies only if Play rejects signature layout |
| Static 16 KB / APK packaging | **All 7 ARM64 libraries** pass LOAD alignment and RELRO. Config **`PAGE_ALIGNMENT_16K`**; two local arm64/API36 APKs generated, both `zipalign -c -P 16 -v 4` checks pass. Native libraries are **compressed**, `extractNativeLibs=true`, so ELF/extraction checks carry native compatibility. **Not runtime-tested on 16 KB** |
| Symbols / tool versions | **7 Public/SymbolTable files** in `BUNDLE-METADATA/...debugsymbols/arm64-v8a/`, symbol tables present, no full DWARF debug info. Producer bundletool **1.18.3**, local APK generator/validator **1.17.2**: Play-generated APKs still need independent Console/device checks |
| Retained artifacts | Code 1 unchanged; code 2's adjacent **`BackUpThisFolder_ButDontShipItWithYourGame`** retained (**522 files**). Raw inspections, symbol hashes, manifest comparison and invocation in ignored `Builds/Validation/Phase17Step8/ReplacementV2/`; nothing installed |
| Build hygiene / final gate | Signing flag, path, alias and password fields cleared through Editor APIs after the build. Temporary Resources files self-cleaned. Human closed Unity; process exit confirmed before restoring **only the four reviewed build-noise files**. Restored settings hash equals the tested baseline; no tracked production/settings diff remains. Reopened Editor confirms ETC2, Generic (0), code 2, Diagnostics disabled/inherited, signing off/path and alias empty, zero preloaded assets, clean SampleScene, stopped Play Mode, 1080×1920, `runInBackground=false`, **zero Console errors/warnings**. No replacement upload, rollout, uninstall or push |

The incremental build reported fewer warnings than code 1's 418; the earlier import/compiler messages did not recur. The count alone is not compatibility evidence: no warning was suppressed, and the effective subtarget plus **actual manifest comparison** prove the PVRTC correction. Console also showed the known URP volume/runtime-list preprocessing messages; native Console error count was zero. Device-catalog acceptance for **code 2 / SM-S911B, Adreno and Mali**, protection/Play delivery, sprite quality and publication remain unverified until the separately approved Play actions.
