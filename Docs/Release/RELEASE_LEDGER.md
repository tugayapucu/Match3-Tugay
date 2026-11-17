# Galaxplode release ledger

Phase 17 Step 8 is complete and reviewed: Play code **2** is available to owner-only Internal testers, with installation and smoke checks verified. The Step 10 policy is published under **Atik Gameworks** and passed automated live checks plus human phone/contact verification; the listing-name display check is deferred to Step 14.

## Approved identity and open Console checks

- Galaxplode / **Atik Gameworks** (public Play developer identity); package `com.tugaykapucu.match3`; English (United States); free, no ads initially.
- Codex implements; Claude independently reviews; ChatGPT advises; the human owns secrets, irreversible operations and Play Console decisions.
- Step 9 evidence reviewed; installer checks on, anti-tamper/telemetry controls not shown. Step 10 contact approved; see live policy; human chose **tugaykapucu.com** hosting. PC absent from Form factors; trader readback **Non-trader**.

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

- **Trader status:** at Step 7, outstanding with a **February 1, 2027** deadline and no declaration reported. Step 8's current human readback shows **Non-trader**, superseding that pending-status label; submission timing and any remaining restriction message were not reported. [Google's guidance](https://support.google.com/googleplay/android-developer/answer/17594752?hl=en-GB) lists paid apps, IAP and ads as profit signals. A hobby/non-commercial activity is "unlikely to be a trader"; no revenue alone settles nothing. A personal-account trader's phone number and address are publicly displayed (announced from March 1, 2027). Adding ads later requires rechecking the declaration. Classification remains the human's; no agent selected or submitted it.
- **Deferrals:** support email to **Step 13**, subject to any earlier Console requirement; PC distribution to **Step 8 before rollout approval**. Read-only route: select Galaxplode → **Test and release → Advanced settings → Form factors → Google Play Games on PC → Manage**, if present. [Console Help](https://support.google.com/googleplay/android-developer/answer/13295490) uses “Test and release”; [Google's PC guide](https://developer.android.com/games/playgames/development-submit) labels the parent “Release.” If absent, resolve the actual state before rollout; do not add/remove a form factor or save changes without human approval. The AAB is **ARM64-only, portrait/touch**; PC compatibility and opt-out are unverified.
- **Step 14:** human approved adding a Turkish (`tr-TR`) store listing later, retaining en-US as the default; no translation implemented here.

## Step 8 - complete and reviewed 2026-11-16

Console evidence is human-reported; artifact, device and log checks were performed locally. Detailed chronology and raw evidence remain ignored under `Builds/Validation/Phase17Step8/`, including the pre-condensation ledger snapshot.

### Original upload and compatibility correction

| Check | Result |
| --- | --- |
| Code 1 | Step 6 AAB accepted as draft; **never rolled out**, replaced by code 2; original AAB/IL2CPP backup retained |
| Play signing | Upload certificate matches Step 3; Google-managed in-use SHA-256 **`66:25:7F:76:FA:79:1B:62:6D:B6:B7:D2:B5:77:82:C0:DC:DF:C4:F2:17:C1:A2:F3:B7:0D:9B:B9:96:26:DC:B0`** |
| Defect / cause | **PVRTC**, only **3,151 supported devices**; MCP `ResolveSubtarget` supplied desktop **Player (2)** to Android, where **2 = PVRTC**, in Step 5/6 builds |
| Safeguard | Tracked Android **ETC2**, profile **Generic (0)**, explicit Android subtarget; MCP unchanged; [known issue](../KnownIssues/McpAndroidBuildSubtarget.md) |
| Warning correction | Step 5's **1,108 PVRTC warnings were a defect signal**, not acceptable noise; same trap appeared in Phase 12 |
| Version plan | Replacement A **0.1.0 (2)**; update B planned **0.1.0 (3)**, subject to unused-code confirmation |

### Replacement A - local validation

| Check | Result |
| --- | --- |
| Source / settings | **`90f35ad4e5313384e931368cecb93634e9efe928`**; tracked settings changed only Android ETC2 and code **1 to 2** |
| Test environment | Unity **6000.5.10f1**, Android, **1080x1920 portrait**; SampleScene clean; `runInBackground=false` |
| Full suites | **632 EditMode passed; 344 PlayMode passed, 0 failed, 3 approved explicit hardware-timing skips** |
| Test Console | Expected camera negative-test warning and runner/tooling messages; no project errors or unexpected warnings |
| AAB | `Builds/Android/Phase17Step8/Galaxplode-InternalA-0.1.0-v2.aab`; **60,939,391 bytes** |
| SHA-256 | **`41c68a46b59a8ca8eded9421e3144e6f79ba470ae1b4b69f0042398542477b66`** |
| Build | Explicit Generic subtarget bypassed resolver; **59.79 s, 0 errors, 1 known Pipeline warning**; subtarget read back **0** |
| Manifest | **No `supports-gl-texture`**; screen/feature/GLES filters match Phase 16; SDK **26/36**, ARM64, portrait, non-debuggable, Back callback **false** |
| Permissions / Diagnostics | `INTERNET` and app dynamic-receiver permission; **no `ACCESS_LOCAL_NETWORK`**; Diagnostics off/inherited, settings-only evidence |
| Signature | `jarsigner` passed; **449 payload entries** verified against upload certificate; no duplicate entries; Play accepted without re-signing |
| Static 16 KB | **7 libraries** pass LOAD/RELRO; **PAGE_ALIGNMENT_16K**; 2 generated APKs pass `zipalign`; compressed libraries, `extractNativeLibs=true` |
| Symbols / tools | **7 Public symbol files**, no full DWARF; producer bundletool **1.18.3**, local validator/APK generator **1.17.2** |
| Artifacts / cleanup | IL2CPP backup **522 files**; signing cleared, four noise files restored with Unity closed; reopened settings/scene clean, **0 Console errors/warnings** |

### Replacement A - Play acceptance and installation

| Check | Result |
| --- | --- |
| Release / cohort | **Internal A - 0.1.0 (2)**, Available to internal testers, **Not reviewed**, owner-only; **Nov 16, 9:48 PM**, timezone unspecified; Closed clock not started |
| Preview | Only code **2**; **0 errors**, only Java mapping warning; R8 minification off, native symbols recognized (**21.2 MB**) |
| Notes, en-US | "Initial internal test build of Galaxplode with touch gameplay, menus and saved level progression." |
| Device coverage | Draft **13,323**, published **13,286**; 37-model decrease unexplained; Supported export **3,080 Adreno / 6,809 Mali** |
| Representative devices | Supported export: **dm1q / S23 / Adreno 740**, **a34x / A34 / Mali G68**; code-2 Downloads lists **dm1q / Android 17 / 38.7 MB** |
| Catalog discrepancy | Unscoped SM-S911B detail said Unsupported; cause unproven, but actual Play installation succeeded |
| Play 16 KB | Code 2 **Supports 16 KB**, with standard caveat; **not runtime-tested on 16 KB** |
| PC / trader | PC **not listed** on Form factors; account **Non-trader**; no PC-compatibility claim or agent classification |
| Debug removal | Approved human uninstall erased normal-profile progression; package absence verified; managed profile 10 had no copy and was unchanged |
| Device | **SM-S911B / dm1q / Android 17 (SDK 37) / 4,096-byte pages**, normal user 0 |
| Verified Play install | **Galaxplode / com.tugaykapucu.match3 / 0.1.0 (2)**, non-debuggable, 2 APKs; installer/initiator **`com.android.vending`**, signer matches full in-use certificate above |
| Delivered SDK | S23 base min **32**, target **36**; AAB min **26**; consistent with SDK-specific generation, not global API-32-only support |
| Protection / network lead | Installer checks **on**, Play adds code; delivered `CHECK_LICENSE` and LicenseClient service request observed; not packet/data-field evidence; carry to Step 9 |

SDK-specific generation is a qualified inference from [bundletool 1.18.3](https://github.com/google/bundletool/releases/tag/1.18.3) and its [variant configuration](https://github.com/google/bundletool/blob/1.18.3/src/main/proto/config.proto). [Automatic protection](https://support.google.com/googleplay/android-developer/answer/10183279) changes delivered code; the exact permission-injection path is not independently proven.

### Play-installed S23 smoke

Automated input was **screenshot-gated by the machine, not inspected by a person**; agent visual review and machine reference comparisons supplied result evidence.

| Check | Result |
| --- | --- |
| Launch / permissions | Human opened from Play; portrait **MATCH 3 / Play** visible; human confirmed **no permission prompt** |
| First tap / selection | First injected tap showed cue; same-tile tap cancelled; **20 / Red 0/25 / 0**, exact starting image restored |
| Two-tap / swipe / Loss | **20** settled board/HUD images match Phase 16 references (**1,021,200 pixels each**); loss **0 moves / Red 23/25 / 9,400 points** |
| Restart / Retry | Restart Confirm and Loss Retry each restored exact fresh Level 1, without stale cue/outcome |
| Win / specials | Four-move win **16 moves / Red 25/25 / 8,400 points**; settled states match references; flying Wisp captured; Next displayed, not pressed |
| Back: Playing / Confirmation | Playing opens Leave; Back cancels and restores selection exactly; Cancel button preserves settled state |
| Back: busy | Resolving-board/disabled-button witness before Back; finishes **19 / 9/25 / 3,500**, no prompt; fresh settled Back opens Leave |
| Back: Outcome / Menu | Won Back returns to Continue without exiting; Main Menu Back leaves game; reopening retains Continue |
| Persistence / Leave | Force-stop/relaunch retains Continue; fresh Level 2 **22 / Green 0/22 / 0**, first injected tap works; Menu Leave returns to Continue |
| Visuals | ETC2 gems/specials/cue and controls clear in captures; known outcome-dimmer strips remain; no smoothness/all-tile/all-device claim |
| Human real-finger check | **One-minute gameplay check on the Play build not done**; human opened the app, board smoke used ADB injection |
| Log triage | 990 app-UID lines: **2 AssetPackManager exceptions, 2 dex-finalizer assertions; 0 fatal-exception/fatal-signal/ANR-text matches**; game alive |

- Known startup exceptions remain unexplained, **not declared harmless**; no observed crash, frozen input or gameplay failure. AssetPackManager closure remains Phase 20; app-UID logs cannot exclude all system ANRs. Exit history records the intentional force-stop.
- Pre-launch-report findings requested but not supplied: **pending Step 18**, without blocking Step 8. No full six-level run or 16 KB runtime test.
- Raw screenshots, action journal, image comparisons, manifests and logs remain ignored under `Builds/Validation/Phase17Step8/`; incidental notification captures discarded.
- Step 8 approved by Claude; validated production/settings unchanged. Step 9's separately authorized evidence review follows below.

## Step 9 - evidence gathered 2026-11-16; reviewed by Claude 2026-11-17

Scope: unchanged release **0.1.0 (2)**, AAB SHA-256 **`41c68a46b59a8ca8eded9421e3144e6f79ba470ae1b4b69f0042398542477b66`**, and the retained, certificate-verified Play-installed base APK from Step 8. This step reuses Step 8's runtime observations; it does **not** add a packet capture or prove absence of traffic.

### Why INTERNET is present

Unity's actual **`IntermediateLibraryManifestDiag.txt`** attributes `INTERNET` to `UnityEngine.Networking` in **Unity.InputSystem** and **Unity.RenderPipelines.Core.Runtime**. Feature files and retained assembly references identify **PlayerConnection** tooling, rather than game HTTP calls. The installed Editor's manifest-builder IL confirms the Auto rule; Gradle's merger traces the permission to Unity's generated library manifest, before Play processing. This is stronger evidence than inferring the cause from package names or Diagnostics settings.

The release's retained **InputSystem.InitializeInPlayer** has no remoting setup call, and **DebugUpdater.RuntimeInit** contains only `ret`; package source gates automatic remoting/debug initialization to development builds. These checks explain the permission without establishing that every engine/native path is incapable of networking. **Internet Access remains Auto; no setting was changed.**

### Data surface and provisional dispositions

| Surface | Permission | Module / SDK evidence | Configuration | Runtime evidence | Proposed answer / limit |
| --- | --- | --- | --- | --- | --- |
| Progression | No special permission | GameFlow + ProgressionStore; one integer `Match3.Progression.v1` in PlayerPrefs | Save on accepted win; no app sync/server code | Step 8 cold-relaunch Continue starts fresh Level 2 | No app-implemented upload identified; local-only processing is outside Play's collection definition, subject to the platform-backup caveat below |
| Engine networking | `INTERNET` in AAB and delivered APK | PlayerConnection references trigger Unity's manifest rule | Auto, non-development; automatic remoting/debug startup absent in inspected IL | No new packet observation | Permission alone is not a collection declaration or proof of traffic |
| Unity services | No service-specific permission distinguishes collection | Engine **UnityAnalyticsCommonModule** support types retained; UGS Analytics SDK not identified | Build input: Unity Connect, Analytics, Performance Reporting, Crash Reporting, Insights **all false**; project unlinked; Android Diagnostics disabled/inherited | No upload established by retained logs; logs are not exhaustive network evidence | No configured Unity collection identified; no blanket no-collection claim from settings alone |
| MCP / Pipeline | No additional permission attributed to their runtime code | Names in registry, but **no retained MCPForUnity.Runtime or Unity.Pipeline.Attributes image**; runtime Pipeline server absent | No corresponding runtime initializer | No runtime collector identified in inspected artifacts | Registry names do not prove shipped executable SDK code |
| Ads / IAP / auth / sensitive APIs | No AD_ID, location, camera, microphone, contacts or external-storage permission in inspected manifests | No matching game API calls, custom Android plugins, Firebase SDK or ads SDK definitions identified | Ads/Purchasing disabled; no authentication implementation | No permission dialog reported in Step 8 | No implemented feature basis for these collection categories; does not rule out normal-permission/native identifiers |
| Play installer protection | Delivered APK adds `CHECK_LICENSE` | **38 additional DEX classes**, including `com.pairip.licensecheck` | Installer checks on; no change proposed | LicenseClient request observed; inspected report writes package name and a zero flag to a Binder parcel | Reviewed interpretation: package-only installer IPC and the Store's own processing are not app collection/sharing; Google does not explicitly confirm this classification |
| Optional Play telemetry | Permission alone cannot establish it | Google describes anonymized environment/performance telemetry for anti-tamper protection | Human readback **2026-11-17: control not shown**, anti-tamper also not shown; installer checks on | No telemetry payload captured | **Not shown is not verified off**; no toggle available in reported UI; recheck Release B |

### Artifact checks and qualifications

- **Registry versus compiled code:** `ScriptingAssemblies.json` lists **121 names**, while actual IL2CPP metadata has **68 images / 8,843 types** (67 stripped assemblies plus `__Generated`). Uploaded and delivered metadata are byte-identical, SHA-256 **`a32bcb5d3a3757b9c32006065cc6e7c4745307e66277aabef2920fab0b59cb3a`**. All 67 backup assemblies match current stripped output. No retained UnityWebRequest, UnityAnalytics service-module, MCP Runtime or Pipeline Attributes image; engine common analytics types remain. Earlier Steps 5/6 package-presence statements used the registry and must not be read as proof of retained executable code.
- **Firebase helper is not Firebase SDK evidence:** Unity's Java compatibility helper reflectively looks for `com.google.firebase.analytics.FirebaseAnalytics`; no `com.google.firebase.*` class definition exists in either inspected DEX. No game identifier/analytics calls were found in the retained Match3 assemblies.
- **Build flags versus labels:** `submitAnalytics: 1` remains serialized; do not treat it as proof of enabled runtime collection. The actual release build input records all five service switches above as false; `m_EngineDiagnosticsEnabled: 0`, unlinked Cloud settings and inherited Android override support the configuration finding. No reliable built Diagnostics marker was found.
- **Play boundary:** local DEX **4,719 classes**, delivered **4,757**; added licensing code binds to `com.android.vending` services, consistent with Step 8 logs. No direct HTTP/socket invocation was found in those inspected added classes; this does not exclude Store-mediated network use or prove no collection/sharing. Do not substitute the separate Play Integrity API's data list for this installer-check implementation.
- **Platform backup:** neither inspected manifest explicitly sets `allowBackup` or backup-exclusion rules. Android system backup/transfer may include preferences depending on OS/user settings; not tested here. Privacy wording can say the game stores progression in app preferences, not that data can never leave the phone. No backup operation or configuration change performed.

### Policy basis and review gate

Unity/Android sources checked **2026-11-16**; Google protection/Data Safety guidance rechecked **2026-11-17**:

- [Unity Android Player settings](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/android/getting-started/class-player-settings): Auto adds permission for detected networking APIs; the build diagnostic supplies this artifact's concrete reason.
- [Unity Diagnostics settings](https://docs.unity.com/en-us/engine/6000.5/manual/unity-editor/editor-settings-reference/comp-manager-group/diagnostics-settings): collection can be disabled, profiles can override defaults, and unlinked projects have Diagnostics disabled.
- [Google Automatic protection](https://support.google.com/googleplay/android-developer/answer/10183279?hl=en): Play injects code; installer checks can periodically require connectivity; optional anti-tamper telemetry controls require a separate status check.
- [Google Data Safety guidance](https://support.google.com/googleplay/android-developer/answer/10787469?hl=en): permissions alone do not require declaring collection; local-only processing is excluded; third-party collection must be assessed, including on-device sharing. No automatic exemption inferred for Play-added code.
- [Android application settings](https://developer.android.com/guide/topics/manifest/application-element) and [Auto Backup](https://developer.android.com/identity/data/autobackup): platform backup is distinct from app-implemented synchronization.

### Reviewed Play boundary and outstanding human decision

- **Installer check:** the inspected report passes the package name to the Play Store over on-device IPC. Codex and Claude assess that this installer check and the Store's own onward processing do not constitute collection/sharing by this app. This is a **reasoned interpretation, not an explicit Google exemption**, and is not extended to optional telemetry or arbitrary SDK transfers. Play developer support is optional for additional certainty.
- **Human readback, 2026-11-17:** **Prevent unauthorized access / Installer checks on** is the entire reported list; the human explicitly confirms **Anti-tamper protection** and **Share app telemetry with Google** are **not shown**. This completes the requested readback, not verification that telemetry is off. No setting changed; installer protection stays on. If the telemetry control becomes available/enabled later, recommend disabling only telemetry after separate human approval.
- **With telemetry verified off:** propose **No collection / No sharing** on the inspected evidence and installer/platform interpretation above, subject to human approval of the declaration. Neither silence in logs nor the permission alone supplies that conclusion.
- **If telemetry stays on:** do not propose No collection. Conservative starting proposal: **App info and performance → Diagnostics**, purpose **Fraud prevention, security and compliance**, and **not shared** only if a relevant sharing exception is established. Confirm exact fields/purposes before submission; collection has no blanket anonymization exemption, while fully anonymized sharing has a separate exception in Google's guidance.
- **Release B (planned code 3):** recheck control availability and status, retaining **not shown** if still absent; inspect/test the delivered release before claiming a verified off configuration. Google's page does not state whether a toggle changes already-live **code 2**; no retrospective effect is claimed. Reconcile the declaration with applicable distributed versions; record any remaining code-2 uncertainty rather than silently carrying a code-3 result backward.

### Step 10 privacy wording to carry forward

Progress is stored on the device in app preferences and may be included in Android's own backup/transfer. Include a separate line that **Google Play performs licensing and installer checks under [Google's privacy policy](https://policies.google.com/privacy)**; do not claim Google itself collects nothing. If optional telemetry remains enabled, revise the policy to describe its assessed data/purposes instead of using blanket no-collection wording. These are handoff requirements, not a published privacy policy or a hosting decision.

Raw build diagnostics, Cecil IL inspection, stripped-assembly inventory, DEX comparison and version-107 metadata inspection stay ignored under `Builds/Validation/Phase17Step9/`. This update changes only the ledger; no production, package, settings or Editor-state change, rebuild, install, Console change or push. Full suites were not repeated for this evidence/documentation step; no Data Safety form submitted or Step 10 implementation started.

## Step 10 - approved publication and verification 2026-11-17

| Check | Result |
| --- | --- |
| Source | `Docs/Release/PrivacyPolicy/index.html`; HTML/CSS template with **PUBLIC_CONTACT_EMAIL** placeholder; actual public-contact copy belongs in the website repository |
| Reviewed content | Galaxplode / **Atik Gameworks** / package; device progression, Android backup/transfer, Google Play checks, deletion, proposed support-correspondence retention and contact sections |
| Public contact | Contact approved; see live policy; personal address excluded from game Git files; dedicated support address/alias offered; no email sent |
| Proposed audience | **13–15, 16–17, 18+ targeted; 5 and under, 6–8, 9–12 not targeted**; human's final clarification supersedes the earlier under-13 answer |
| Policy status | Step 10 approved; website content reviewed and published as **Atik Gameworks**, effective **2026-11-17**; no draft banner or contact placeholder in deployed copy; game-repo source remains a placeholder template; no blanket no-collection claim |
| Local validation | Template/content and local HTTP checks passed; isolated previews at **320, 390 and 1080 px** fit without horizontal overflow; no local Next.js production build run |
| Website publication | Human-authorized single-file commit **[`d6d7b1f`](https://github.com/tugayapucu/tugaykapucu.com/commit/d6d7b1f3b59f936286aa3edbf09deef554b3731b)** pushed to website `main`; only `public/galaxplode/privacy.html` added; website tree clean and synced |
| Live URL | **[Galaxplode Privacy Policy](https://tugaykapucu.com/galaxplode/privacy.html)**; checked **2026-11-17 17:25 UTC** |
| Live response | **HTTP 200**, valid HTTPS, no redirects/login wall; **7,108 bytes**, byte-identical to local website file |
| SHA-256 | **`fafb48742a3e7d30ed9d97620f8f27f67830826fae3b71ef66bb0957bbec489f`** for both local file and served response |
| Live browser | Fresh isolated logged-out Chrome; **320/390/1080 px** fit; zero scripts/forms/iframes; mobile capture inspected; no injected content |
| Contact check | Automated target/click checks passed; human confirmed **page and email link work on the phone** on **2026-11-17**; no test email sent |
| Step 14 follow-up | Human rechecked on **2026-11-17**: developer name **still absent** from the listing; account rename human-confirmed, display unverified; recheck during Step 14 |
| Console privacy URL | **Awaiting human confirmation that it is saved** in App content > Privacy policy; agent made no Console change |

**Human hosting decision, 2026-11-17:** the human chose the existing **tugaykapucu.com** site on **Vercel**, replacing the proposed separate GitHub Pages site, and confirmed connected repository **`tugayapucu/tugaykapucu.com`** with Production branch **`main`**. The Next.js repository serves the single added public HTML file at the URL above; portfolio code, dependencies, DNS and deployment configuration were unchanged.

The standalone source avoids the portfolio's scripts/forms/layout. It discloses potential Vercel technical-request processing separately from game storage; [Vercel's Privacy Notice](https://vercel.com/legal/privacy-notice) distinguishes its own processing from processing for customers. The published response/body passed the exact-content check above; this does not establish that all hosting telemetry is disabled.

**Audience review, 2026-11-17:** the initial under-13 answer triggered the plan's read-only [Families review](https://support.google.com/googleplay/android-developer/answer/9893335?hl=en). The human then clarified the actual design/marketing intent: teenagers and adults **13+**, not young children; colorful space art is for casual players. Record this as **proposed**, not submitted, and reassess if design, marketing or monetization changes. [Google's audience guidance](https://support.google.com/googleplay/android-developer/answer/9867159?hl=en) requires selections to reflect intended design and notes that local definitions of children vary. No blanket legal/Families-compliance claim or new age/consent framework was introduced. Existing Step 9 data/SDK limits remain; disabled Diagnostics is supported by [Unity's privacy documentation](https://docs.unity.com/en-us/cloud/developer-data/privacy-overview), not a universal privacy exemption.

**Unintended appeal to children:** Google may judge the actual listing/design as child-directed despite a proposed 13+ declaration. Mitigate in Step 14 with truthful teen/adult casual-puzzle copy and representative screenshots, avoiding contradictory child-directed claims; reassess the audience if design/marketing actually changes, rather than relying on age selections alone.

**Public identity update, 2026-11-17:** the human confirms successfully changing the public Google Play developer name to **Atik Gameworks** because **Atik Games** was unavailable. The policy template now uses Atik Gameworks throughout. Unity **`companyName: Atik Games`**, package **`com.tugaykapucu.match3`**, existing upload certificate **`CN=Atik Games`** and built artifacts stay unchanged; no rebuild or key change is needed or authorized. Earlier certificate/build records retain their historical names.

**Publication approval and execution, 2026-11-17:** Claude approved the exact template-to-website differences: draft removal, effective date, hosted wording and approved contact. The human then explicitly authorized one website commit and push to `main`, triggering Vercel production publication. No game-repository commit/push, Play Console action or Step 11 implementation was included.

**Live-check limits and human readback:** automated checks used a new isolated browser profile without login, cookies or authorization and emulated mobile viewports. The only extra request was the browser's same-origin `/favicon.ico`; no scripts, remote assets or injected resources appeared. The automated mailto click was intercepted; separately, the human confirmed the page and email link work on the phone. Email delivery was not tested. The human also confirmed that the Play listing still has **no developer name displayed**; the cause is unverified, and listing-name display is deferred to **Step 14**, rather than remaining a Step 10 gate. Saving the verified URL in **App content > Privacy policy** remains a human Console action; no saved-status confirmation has been received, and the agent has not performed it.

No target-audience Console declaration, Step 11 UI change, Unity operation or new build. Raw local/live response evidence, hashes and screenshots remain ignored under `Builds/Validation/Phase17Step10/`. Game-repo changes are the placeholder policy template, the execution-plan identity note and this ledger; no personal contact address or deployed policy copy is included. The preceding Step 9 readback was committed as **`ea80e3c`** and pushed with the six earlier reviewed commits; Step 10 documentation approved for two scoped commits; no game-repository push or Step 11 implementation authorized.
