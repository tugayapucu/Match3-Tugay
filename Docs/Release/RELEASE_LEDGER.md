# Galaxplode release ledger

Phase 17 is in progress. Step 5 established clean-clone reproducibility; Step 6 produced a locally validated upload-key-signed AAB, independently reviewed by Claude. No Play upload has occurred.

## Approved identity and open Console checks

- Galaxplode / Atik Games; package `com.tugaykapucu.match3`; English (United States); free, no ads initially.
- Codex implements; Claude independently reviews; ChatGPT advises; the human owns secrets, irreversible operations and Play Console decisions.
- Account verification, version-code availability, contact email and actual PC distribution state remain unverified in Console.

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
