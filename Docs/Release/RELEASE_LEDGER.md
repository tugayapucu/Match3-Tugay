# Galaxplode release ledger

Phase 17 is in progress. No release AAB or Play upload has been produced. Step 5's debug-signed APK establishes clean-clone reproducibility.

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
