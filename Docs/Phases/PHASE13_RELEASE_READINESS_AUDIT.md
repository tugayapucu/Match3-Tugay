# Phase 13 — Release Readiness & Product Gap Audit

Audit dates: 2026-10-29 to 2026-10-30. Baseline commit: `08cc8ec`. Unity 6000.5.10f1.

Roles for this phase: Claude executed the audit and drafted this document; Codex challenged it (Step 15); the human developer decides the classifications.

This is an audit only. No finding was fixed during Phase 13.

## Evidence labels

Each claim is tagged with the kind of evidence behind it:

| Label | Meaning |
| --- | --- |
| **[Repo]** | Read directly from the repository, project settings or build output |
| **[Test]** | Proven by the existing automated EditMode/PlayMode tests |
| **[Device]** | Observed on the physical Samsung Galaxy S23 (SM-S911B, Android 16, 1080 x 2340) |
| **[Console]** | Play Console state reported by the developer (developer attestation; not inspected by the auditors) |
| **[Policy]** | Official Google / Android / Unity documentation, with the date it was checked |
| **[Judgment]** | Engineering judgment based on the evidence above |
| **[Estimate]** | A project-planning estimate, not a commitment or guaranteed duration |
| **[Open]** | Not yet verified; listed under Open Verification Items |

---

## Executive Summary

**Question:** can we responsibly distribute this game on Google Play today?

**Answer:** not yet, and that is expected at this point in the roadmap. The gap is mostly distribution setup, plus one gameplay safeguard.

| Classification | Count | What it contains |
| --- | ---: | --- |
| MUST FIX BEFORE STORE | 15 | 1 gameplay safeguard (DeadBoard); 14 distribution, signing, policy and listing items |
| SHOULD FIX BEFORE STORE | 20 | Approved interaction work, product flow, accessibility, presentation, verification and release-quality cleanup |
| CAN DEFER AFTER RELEASE | 4 | Invisible or low-impact items |
| NOT A REAL ISSUE / CLOSE | 11 | Resolved, expected or disproved concerns |

Some findings are separate tasks around one subject, not independent defects:

- the minimum and final store listing;
- Diagnostics, Data Safety and the `INTERNET` investigation;
- packages, build dirtiness and build reproducibility.

Key conclusions:

- **DeadBoard is the only gameplay MUST.** It is an unimplemented safeguard, not a reproduced player incident; how often it happens in play is unknown.
- **LockedAfterException is SHOULD,** with an explicit rule: it becomes MUST if Phase 14 finds a reachable trigger.
- **The non-development APK still declares `INTERNET`.** Unity's generated manifest adds it. That is a confirmed permission, not proof of data collection.
- **The closed test is an applicable external MUST.** This is a personal developer account created after 13 November 2023 [Console][Policy].
- **Account and device verification are complete (CLOSE)**, based on the developer-reported Console state.
- **No failure was observed in the pause/resume scenarios tested on the S23.** Physical evidence comes from that one handset only.
- **Recommended order is unchanged:** Phase 14 → 15 → 16 → 17 → closed test (12 testers opted in for 14 continuous days) → Phases 18–21 during the test → Phase 22.

---

## Audit Scope and Baseline

**Baseline (Step 1)** [Repo]:

- Commit `08cc8ec` on `main`, clean tree.
- The last change outside documentation is `2a85788`, the Android application identifier.
- The final Phase 12 test run already included that commit, so its results describe the current code exactly:
  - **540/540 EditMode** tests passed;
  - **166 PlayMode** tests passed, with 3 explicit hardware-timing tests deliberately skipped.
- Expected Console output during tests:
  - the Purple negative-test exception;
  - the camera no-room warning.
- Android application identifier: `com.tugaykapucu.match3`.

**Scope:**

- all 12 files in `Docs/KnownIssues/`;
- test coverage;
- Android Player settings and build output;
- app lifecycle;
- UI and accessibility;
- Google Play account and policy requirements;
- privacy and data surface;
- release order and risks.

**Out of scope:** implementing any finding. Fixes belong to Phases 14–23.

**Actions taken during the audit, all restored or outside the repository:**

- **Throwaway build (Step 7):** one non-development Android APK. Platform switch → build → restore:
  - the 4 files the build touched were restored with `git checkout`;
  - all 8 checksum-controlled settings files match the pre-build snapshot;
  - git is clean.
- **Device checks (Steps 8–9):** the APK was installed on the S23 for lifecycle and UI checks. The phone now runs this build instead of the Phase 12 development build.
- **Read-only Editor code:** used to compute a deterministic move route. No assets were changed.

---

## Evidence Reviewed

| Source | Used for |
| --- | --- |
| `PLAN.md` §19–30, `Docs/Phases/PHASE13_EXECUTION.md` | Scope, owner phases, entry criteria, Definition of Done |
| `Docs/KnownIssues/*.md` (12 files) | Known-issue inventory |
| `Assets/Scripts/**` | Lifecycle, exception paths, dead-board mechanism, data surface |
| `Assets/Tests/**` (5 assemblies, 39 test files) | Coverage map |
| `ProjectSettings/*.asset`, `Packages/manifest.json`, `packages-lock.json` | Android configuration, services, packages |
| `Library/BuildProfiles/*` (Editor-local) | Development/Debugging/App Bundle/texture state |
| `Docs/Validation/PHASE12_STEP9_ANDROID_BUILD.md`, `Docs/Validation/Phase12TallScreenComparison/` | Phase 12 device and build evidence |
| Phase 12 development APK and Phase 13 non-development APK (`aapt2`, Gradle manifest-merger report, `ScriptingAssemblies.json`, IL2CPP metadata, `llvm-readelf`) | Permissions, components, shipped code, native alignment |
| S23 over ADB | Lifecycle, process death, selection, Android Back, sizes |
| Official Google, Android and Unity pages (listed below) | Current requirements |

### Known-issue inventory (all 12 files)

| Known-issue file | Status found | Final classification |
| --- | --- | --- |
| `BeamCrystalArtThin.md` | Resolved; PNGs unchanged since the fix (`38eb7d2`) | CLOSE |
| `ColourOnlyGems.md` | Confirmed | SHOULD |
| `DeadBoard.md` | Partially confirmed: the mechanism is certain; not reproduced in play | **MUST** |
| `FrameRatePolicy.md` | The policy is implemented; long-session behavior is unverified | SHOULD (Phase 19 measurement) |
| `LockedAfterException.md` | Partially confirmed: the consequence is certain; no known trigger. **The document understates the impact** (see Open Verification Items) | SHOULD (escalation rule) |
| `MissingReadme.md` | Confirmed | DEFER relative to store |
| `MobileLayoutAndPicking.md` | Main concern resolved; residual items remain | CLOSE + residuals (comfort SHOULD; profiling SHOULD as Phase 19 measurement; optimization DEFER) |
| `OutcomeDimSafeArea.md` | Confirmed on the device | SHOULD |
| `RefillSeedReuse.md` | Confirmed as described; an intentional RNG trade-off, not a bug | DEFER |
| `SelectionFeedback.md` | Confirmed on the device | SHOULD |
| `TallScreenComposition.md` | Confirmed on the device | SHOULD |
| `WatchList.md` | Still accurate; not issues | CLOSE |

Overlaps are flagged, not merged:

- **DeadBoard and LockedAfterException** both mean "the player cannot continue", but the cause and the recovery differ:
  - after a dead board, Restart works;
  - after an exception, Restart is disabled.
- **Performance measurement** items are spread across MobileLayoutAndPicking and FrameRatePolicy. They belong to one Phase 19 measurement list.

---

## Play Account / Current Policy Findings

### Account state (2026-10-30) [Console]

The developer reported this Play Console state; the auditors did not inspect the account.

- **Account type and age:** Personal, created 2026-10-29.
- **Identity verification:** complete; `Create app` is enabled.
- **Device verification: complete.**
  - The "Verify that you have access to an Android mobile device" task is no longer on the Home page.
  - Google's documentation says the task is no longer shown after successful verification [Policy, checked 2026-10-30].
- **The closed-test requirement applies:** personal account, created after 13 November 2023 [Policy].
- **The app has not been created yet.**

### Requirements (checked 2026-10-29 unless noted)

| Requirement | Current rule (paraphrased) | Applies to us | Source |
| --- | --- | --- | --- |
| Closed test before production | At least 12 testers opted in **continuously** for the 14 consecutive days before applying for production. The period is tied to tester opt-in, not to publication. A tester who leaves breaks their own continuity. Then a questionnaire (recruitment, engagement, feedback, changes made); review usually ≤7 days | **Yes** | [Testing requirements](https://support.google.com/googleplay/android-developer/answer/14151465) (re-checked 2026-10-30) |
| Internal testing | No prerequisites; can start before app setup; up to 100 testers; does not count toward the 12/14 rule | Yes | [Set up a test](https://support.google.com/googleplay/android-developer/answer/9845334) |
| Closed testing | A closed test can start only after app setup is complete; the opt-in link appears only once the release is Published | Yes | [Testing requirements](https://support.google.com/googleplay/android-developer/answer/14151465) (re-checked 2026-10-30), [Set up a test](https://support.google.com/googleplay/android-developer/answer/9845334) |
| App creation | Name (changeable), **package name (permanent, never reusable)**, default language, app or game (changeable), free or paid, contact email; policy, export-law and Play App Signing acceptances. The store category is set later | Yes | [Create and set up your app](https://support.google.com/googleplay/android-developer/answer/9859152) |
| Pricing | Paid → free is allowed. **Once offered free, never paid**; charging later needs a new package | Yes (we plan FREE) | [App prices](https://support.google.com/googleplay/android-developer/answer/6334373) |
| Public developer information | Personal account: legal name, country and developer email shown. Full address when monetizing; some regions require additional disclosures | Yes: no full address while free, unless a regional rule requires more | [Account information](https://support.google.com/googleplay/android-developer/answer/13628312) |
| Device verification | New personal accounts verify a real Android device; the task disappears once done | **Done** [Console] | [Device verification](https://support.google.com/googleplay/android-developer/answer/14316361) (checked 2026-10-30) |
| Target API | New apps and updates must target API 36 | Met (APK targets 36) | [Target API](https://support.google.com/googleplay/android-developer/answer/11926878) |
| App Bundle | New apps publish as AAB | Yes (App Bundle currently off) | [Android App Bundle](https://developer.android.com/guide/app-bundle) |
| Signing | Play App Signing by default; the developer keeps the upload key; a lost upload key can be reset | Yes | [Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756) |
| Version code | Must increase with every release; never reused; maximum 2,100,000,000 | Yes | [Versioning](https://developer.android.com/studio/publish/versioning) |
| 64-bit | Native code must include 64-bit libraries | Met (arm64-v8a only) | [64-bit](https://developer.android.com/google/play/requirements/64-bit) |
| Privacy policy | Required for **every** app, even one that collects nothing. Must be in Play Console **and** reachable in the app (a link or text). Public HTTPS page, not a PDF, no login or geo-block, app-specific | **Yes** | [User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311), [App content](https://support.google.com/googleplay/android-developer/answer/9859455) |
| Data Safety | Required for closed, open and production tracks, even when no data is collected; engine/SDK collection must be declared | Yes | [Data safety](https://support.google.com/googleplay/android-developer/answer/10787469) |
| Content rating, target audience, ads declaration | Required for every app; a child audience brings in the Families policy | Yes | [App content](https://support.google.com/googleplay/android-developer/answer/9859455) |
| App access | Console asks about restricted access; login-gated apps must provide access details | Yes: declare that all functionality is available without restrictions if Console asks; no credentials needed | Same |
| Permissions declaration | Only for high-risk or sensitive permissions | No (none in the APK) | Same |
| Store assets | 512x512 icon, 1024x500 feature graphic, at least 2 screenshots whose **long side is ≤ 2x the short side** | Yes. **Raw S23 screenshots (2.17:1) are invalid** | [Preview assets](https://support.google.com/googleplay/android-developer/answer/9866151) |
| Tablets (Android 16/17) | Apps targeting 36+ lose orientation/resizability restrictions on large screens; **games identified by `android:appCategory` are exempt**, and Android 17 still lists that exemption | Exempt from this override (our manifest category is game) | [Android 16 changes](https://developer.android.com/about/versions/16/behavior-changes-16), [Android 17](https://developer.android.com/about/versions/17/changes/ff-restrictions-ignored) |
| Predictive Back | Apps targeting 36 lose the classic Back callback unless they opt out. No removal date for the opt-out is announced | We opt out today | [Android 16 changes](https://developer.android.com/about/versions/16/behavior-changes-16) |

**Consequence for the schedule** [Policy]: a closed test can start only after app setup is complete, so the following must exist in Phase 17, not only Phase 21:

- the privacy-policy page;
- the first Data Safety answers;
- the content rating, target audience, ads and app-access declarations;
- a minimum store listing.

---

## Android Build / Permission Findings

### Project configuration (Step 4) [Repo]

**Identity:**

- **Launcher label:** `productName` is `agentic-match3-unity`.
- **Company name:** `DefaultCompany`. No Android-visible effect was found.
- **Windows identifier:** still the template default, `com.DefaultCompany.2D-URP`. No Android effect.
- **Version:** `1.0`, version code 1.

**Build settings:**

- **SDK levels:** minimum API 26; target API Automatic, which resolves to 36.
- **Code:** IL2CPP Release; ARM64 only.
- **Managed stripping:** Minimal, the effective value read through the API.
- **Orientation:** portrait.
- **Graphics API:** Auto. Vulkan is optional in the manifest.

**Android build profile (Editor-local):**

- Development Build, Script Debugging and App Bundle are all off.
- Texture compression is ETC2.

**Permissions and signing:**

- **Internet Access:** Auto.
- **Write permission:** Internal.
- **Signing:** no custom keystore exists, so builds use Unity's debug key.

**Launcher and app behavior:**

- **Icon:** none assigned in any of the 18 Android slots, so the Unity default icon is used.
- **Overrides:** none. No custom manifest, Gradle template, plugin or keystore exists in the repository.
- **Predictive Back:** opted out.
- **Backup:** no `allowBackup` attribute, so Android's default (on) applies.

**Unity Editor UI note:** in this Unity 6.5 project, `Hide Classic Platforms` is on in `EditorSettings`, so the Build Profiles window does not show the Platforms list.

### Phase 12 development APK vs Phase 13 non-development APK (Step 7) [Repo]

| Evidence | Development APK (Phase 12) | Non-development APK (Phase 13) |
| --- | --- | --- |
| Path | `Builds/Android/Match3-Phase12-Development.apk` | `Builds/Android/Match3-Phase13-NonDev.apk` (both git-ignored) |
| Size | 51,193,978 bytes | 39,792,256 bytes |
| SHA-256 | — | `054f37edae047fcadfcc48b8651f5f0b001dffe937b919c8a710365f19c9928f` |
| Build | — | Succeeded, 3 min 18 s, 0 errors, ETC2 confirmed. 4 warnings: 1 `com.unity.pipeline` missing-configuration warning; 3 IL2CPP notes that large TextMeshPro methods got their own C++ files |
| Debuggable | yes | no |
| Package / version | `com.tugaykapucu.match3`, 1.0 / 1 | same |
| Min / target / compile SDK | 26 / 36 / 36 | same |
| ABI | arm64-v8a | arm64-v8a |
| Native alignment | — | All 7 native libraries have 16 KB (0x4000) LOAD-segment alignment (`llvm-readelf`) |
| Permissions | `INTERNET`; AndroidX `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` | **same** |
| `appCategory` | game | game |
| Components | GameActivity (launcher); private AndroidX startup provider | same; no services, no receivers |

The 4-warning count is not an improvement over Phase 12's 415: Phase 12's warnings were mostly shader compilation, which was cached this time.

**16 KB page sizes:** the alignment result is positive evidence for [Android's 16 KB page-size guidance](https://developer.android.com/guide/practices/page-sizes). The libraries are compressed and the manifest enables extraction, so their ZIP offsets alone are not a failure. AAB and on-device 16 KB verification remain with the build phases [Open].

**Finding 1: where `INTERNET` comes from.**

- The Gradle manifest-merger report shows that `INTERNET` is added by **Unity's generated manifest** (`unityLibrary/src/main/AndroidManifest.xml`), not by AndroidX or any SDK [Repo].
- Unity's documentation says Auto adds the permission "if you are using a networking API" but does not list which ones [Policy, Unity 6000.5 Android Player settings].
- The project's scripts use no networking APIs [Repo].

**Finding 2: what code shipped** [Repo].

- `ScriptingAssemblies.json` lists 121 assemblies, including the web-request, analytics, Insights (diagnostics), crash-reporting and curl/TLS modules. A listing alone does not show use.
- In the IL2CPP metadata, those modules appear only as module names and engine update-step names.
- **No obvious managed networking classes were found in the stripped IL2CPP evidence**: no `DownloadHandler`, `HttpClient` or `TcpClient`.
- MCP, Newtonsoft, Visual Scripting and `Unity.Pipeline` code was stripped entirely.

This does **not** prove that no native engine networking path exists. The exact trigger stays unknown until a controlled experiment in Phase 17/20 [Open].

**Finding 3: Diagnostics** [Repo + Policy].

- The project default has Engine Diagnostics enabled. That setting alone does not establish collection.
- The project is **not linked to Unity Cloud**: `cloudProjectId` is empty and `cloudEnabled` is 0.
- Unity documents a Cloud link as a prerequisite for collection ([Diagnostics prerequisites](https://docs.unity.com/en-us/cloud/developer-data/prerequisites)).
- If Diagnostics were active, Unity's guidance is to declare crash logs and app diagnostics ([Unity Data safety guidance](https://docs.unity.com/en-us/cloud/developer-data/google-data-safety)).
- All other Unity services (Analytics, Crash Reporting, Performance Reporting, Ads, Purchasing) are off.

**Finding 4: build side effects** [Repo]. The build modified 4 tracked files, all restored afterwards:

- the URP asset (shader-prefilter flags);
- `ProjectSettings.asset` (the Android batching entry, and the Input System actions asset added to preloaded assets);
- the URP global settings;
- **`DefaultVolumeProfile.asset`**, new compared with Phase 12: URP adds a missing bloom field on every build and logs that the profile was modified.

**Finding 5: packages** [Repo].

- No separate third-party ads, IAP, login, analytics or crash SDK is installed. Unity's built-in analytics and diagnostics modules are present (Findings 2–3).
- **The Unity MCP package in `Packages/` is an embedded, git-ignored copy** (`.gitignore` line 91). It overrides the manifest's git-URL package, so a fresh clone resolves the package from the git URL instead.
  - Both identify as version 10.0.0.
  - Whether their contents differ is unverified [Open].
- `com.unity.pipeline` (experimental) logs a missing-configuration warning on every build.

**Finding 6: device log** [Device]. Every cold start logs one Unity error:

> `ClassNotFoundException: com.google.android.play.core.assetpacks.AssetPackManager`

No gameplay effect was observed. It is neither assumed to be a crash nor declared harmless noise. Triage it during the first Phase 17 Play-build smoke test; closure stays with Phase 20.

---

## Test and Validation Coverage

Summary of the coverage map (Step 3). Code is unchanged since the last validated Phase 12 run, so no tests were rerun.

| Area | Evidence | Proves | Does not prove |
| --- | --- | --- | --- |
| Core rules: swaps, matching, specials, cascades, refill | Strong [Test] | Rules, special creation/activation, cascades, determinism | Statistical behavior over long play |
| Moves, score, goal, outcomes, restart | Strong [Test] + [Device] | Rules, HUD updates, Win/Loss, identical restart | — |
| Input locking, invalid swap rollback | Strong [Test] | Locking through swap, cascade and specials | Lock after an exception |
| Touch wiring | Partial [Test] (virtual touchscreen in the real scene) + [Device] (ADB) | The Input System path | A physical screen with a human finger |
| Layout and picking | Strong [Test] + [Device] | Corners, borders, sync after refill | Finger comfort |
| Safe area and framing | Partial [Test] (3 simulated safe areas, pure framing math) + [Device] (S23 only) | Containment, no-room recovery | Other handsets and cutout shapes |
| Exceptions | Partial [Test] | Configuration failure at Start; invalid input rejected before changes; restart guarded mid-move | **An exception during a move: not exercised** |
| DeadBoard | **None** | — | Detection or recovery |
| Lifecycle | No code, no tests; [Device] no failure in the tested scenarios (below) | — | Untested timings; long sessions; other devices |
| Timing / hardware | Explicit tests skipped; Editor measured once; S23 short samples | About 60 FPS for a few seconds | Sustained performance, heat, battery |

Helper vs end-to-end: most `BoardInputControllerTests` call `HandlePointerDown` directly. They prove the selection logic. Input wiring is proven separately by the `Update_*` tests and `SceneTouchInputTests`.

### Physical-device results (S23, non-development APK) [Device]

Physical validation comes primarily from **one Samsung Galaxy S23**. These results do not establish general Android compatibility. Each timing-sensitive scenario was tried once.

| Scenario | Result |
| --- | --- |
| Idle → Home → return | Same process; board and input fine |
| Screen off → lock → unlock | Same process; next move worked |
| Home about 0.25 s into a Beam-creating move | Move completed coherently; HUD correct |
| Home during a Beam activation | Row cleared; board full; HUD correct |
| Win screen → Home → return | Screen identical; Play Again returned the exact launch board |
| Restart after several background cycles | Exact launch board (identical checksum) |
| Process death (background kill, force-stop) | New process, cold start, fresh launch board. **Expected:** nothing is saved |
| Selection feedback | The screen is pixel-identical after a first tap and after an invalid tap; the selection is silently kept |
| Android Back | Ignored on the idle board, mid-resolution and on the Win screen |
| Determinism | A 9-move route computed with Core in the Editor matched the device exactly, at move 8 and at the Win (11 moves left, 25/25, 8,300) |
| Sizes | Cell 120 px ≈ 43 dp ≈ 7 mm. Android's 48 dp touch-target guidance is a design guideline, not an automatic Play rejection threshold. Restart ≈ 105 x 44 dp, bottom center |
| Colour | Grayscale brightness of the gem sprites: **Red 0.195 ≈ Blue 0.200**, **Orange 0.294 ≈ Green 0.307**; Purple 0.129; Yellow 0.467. This is a brightness measurement, not a colour-vision-deficiency usability study |

**ADB note** [Device]:

- **Observed:** an instant `input tap` was not registered; 80 ms presses were.
- **Suspected cause:** the timing of ADB's injected events. Not proven: the Input System tracks press events, so "same frame" alone is not a sufficient explanation.
- **Consequence:** no production fix is justified by this observation.

---

## Release-Gap Classification Table

**Type values:**

- gameplay bug;
- release blocker;
- polish;
- policy/compliance;
- performance;
- product feature;
- **release quality:** build and packaging work that improves the release without preventing distribution. Used by five cleanup items.

Type is separate from severity. A **process gate** is a project rule rather than a platform policy.

### MUST FIX BEFORE STORE

| Finding | Type | Confidence | Evidence | Player / release impact | Rationale | Owner phase |
| --- | --- | --- | --- | --- | --- | --- |
| DeadBoard | gameplay bug | High that the safeguard is missing; frequency unknown; not reproduced | [Repo] the generator prevents initial patterns but does not guarantee at least one legal move; no detection or shuffle; [Test] none | Player stuck with moves left; neither Win nor Loss | An unimplemented safeguard with an unacceptable consequence; PLAN gate for Phase 17 | 14 |
| Play App Signing acceptance | release blocker | High | [Policy]; [Console] no app created yet | Required for new apps | Accept at app creation | 17 |
| Upload-key signing | release blocker | High | [Repo] debug-signed; no custom keystore | Cannot upload | Create the key and back it up outside the repo | 17 |
| AAB production path | release blocker | High | [Repo] App Bundle off; only APK evidence; [Policy] | Play requires AAB; an APK does not prove Play-generated installs or updates | Not yet exercised; Phase 20 re-verifies | 17 |
| Version-code convention | release blocker (process gate) | High | [Repo] code 1 is valid; [Policy] codes must increase and never repeat | Reusing an uploaded code or mishandling updates blocks uploads | A project process gate, not a policy rule: decide the convention before the first upload | 17 |
| Diagnostics / Data Safety ambiguity | policy/compliance | Medium | [Repo] Engine Diagnostics enabled, not Cloud-linked, other services off; [Policy] Unity prerequisites. Does not establish collection | Data Safety cannot be answered truthfully while it is ambiguous | A verification gate. Resolve before the first Play test build: disable explicitly, or keep it only if documented behavior plus build evidence support truthful answers | 17 |
| Privacy policy URL | policy/compliance | High | [Policy]; no policy page exists | Blocks app setup | A hosted HTTPS page | 17 |
| In-app privacy-policy access | policy/compliance | High | [Policy] a link or text within the app; [Repo] the HUD has no access point | Policy violation | The policy does not mandate a particular UI. Phase 16's menu is its natural home, delivered before Phase 17 | 16 |
| Data Safety form | policy/compliance | High that it is required; answers pending | [Policy] | Blocks the closed test | Answers must include applicable engine/SDK behavior. First answers from the Phase 17 AAB; Phase 21 re-verifies | 17 |
| Content rating | policy/compliance | High | [Policy] | Blocks setup | Questionnaire | 17 |
| Target audience | policy/compliance | High | [Policy] | Blocks setup; a child audience brings in the Families policy | Decide the age group deliberately; do not infer it from the colourful artwork | 17 |
| Ads declaration | policy/compliance | High | [Policy]; [Repo] no ad integration | Blocks setup | Declare accurately: no ads | 17 |
| Minimum closed-test store listing | policy/compliance | High | [Policy]; [Device] raw screenshots are 2.17:1 | Blocks the closed test | Icon, feature graphic, ≥2 screenshots at ≤2:1, descriptions. The same listing is finalized in Phase 21 | 17 |
| Closed test (12 testers x 14 days) | policy/compliance (external, schedule) | High | [Policy]; [Console] personal account created after 2023-11-13 | Gates production access | Eligibility depends on 12 testers' continuous opt-in, not on publication. Phase 22 applies for production | 17 |
| Final store listing | policy/compliance | High | [Policy] | Needed for production | Finalizes the Phase 17 minimum listing (a later milestone of the same listing, not a separate missing system). Final art and text after Phase 18 | 21 |

### SHOULD FIX BEFORE STORE

**Gameplay / usability**

| Finding | Type | Confidence | Evidence | Player / release impact | Rationale | Owner phase |
| --- | --- | --- | --- | --- | --- | --- |
| LockedAfterException | gameplay bug (latent) | High on consequence; likelihood low/unknown | [Repo] input cleanup happens only at the end of a move; no `try`/`finally`; Bootstrap rejects restart while input is locked; [Test] not exercised. **Not a confirmed runtime bug** | If triggered: frozen game, buttons disabled, app must be reopened. Reopening fully recovers (nothing saved) | Severe but needs a real bug; no reachable normal-play trigger shown. **Escalates to MUST if Phase 14 finds a reachable trigger** | 14 |
| Swipe | product feature | High | [Repo] two-tap only | Most Match-3 players expect swiping | Already approved release scope; feed the same first/second-cell request through the existing guards | 15 |
| SelectionFeedback | product feature | High | [Repo] selection stored without a visual update; [Device] pixel-identical after a tap | Taps feel ignored | Already approved release scope | 15 |
| HumanTouchComfort | polish (validation) | Medium | [Device] 43 dp cells; ADB only | Unknown for real fingers | Full-cell colliders and ADB presses establish geometry and input, not comfort. Needs a human check | 15 |
| Android Back | product feature | High | [Repo] no Back handling; [Device] ignored everywhere | Breaks a basic Android expectation | **Release-scope usability requirement, not an established Play-policy violation** | 16 |
| Main Menu | product feature | High | [Repo] Bootstrap starts directly into gameplay | Feels like a demo | Our release scope, not Play policy | 16 |
| Level progression | product feature | High | [Repo] one configuration, seed 42 | Same board every session | Our release scope, not Play policy | 16 |
| Persistence / Continue | product feature | High | [Repo] no persistence; [Device] process death resets | Nothing carries across sessions | Persist progression, such as the unlocked level. Saving an in-progress board is not required | 16 |
| Phase 19 measurements: sustained performance, heat, battery, `SyncTransforms` cost, per-move allocations | performance | Medium | [Device] two short ~60 FPS samples; [Repo] sync cost and allocations not profiled | Unknown | Measure near-final presentation. Optimization is a separate DEFER item | 19 |
| Device coverage beyond the S23 | performance | High that it is a gap | [Device] one handset | Unknown on other devices | A second device and/or Play's pre-launch report (using Phase 17 results when available). A pre-launch report does not replace human comfort testing | 19 |

**Accessibility / presentation**

| Finding | Type | Confidence | Evidence | Player / release impact | Rationale | Owner phase |
| --- | --- | --- | --- | --- | --- | --- |
| ColourOnlyGems | polish (accessibility) | High | [Repo] same shape for all six; [Device] grayscale brightness Red≈Blue and Orange≈Green (not a colour-vision study); Red is the goal colour | Colour-blind players may misread the board and the goal | **A small targeted fix**, such as an inner symbol per colour plus the HUD goal icon; no accessibility framework | 18 |
| TallScreenComposition | polish | High | [Device] about 530 px gaps at 1080 x 2340 | Empty look on tall phones; layout functionally correct | First impression, not a functional failure. The rejected vertical-bias decision stays | 18 |
| OutcomeDimSafeArea | polish | High | [Repo] dimmer inside the safe-area hierarchy; [Device] strip in Win screenshots | Cosmetic; controls work | Do after Phase 16 redesigns the outcome screen | 18 |
| Launcher name | polish | High | [Repo] `agentic-match3-unity` | Looks unfinished | A trivial change during identity setup | 17 |
| Launcher icon | polish | High | [Repo] Unity default | Looks unfinished | Needs art. Separate from the 512 px store icon, which is part of the MUST listing | 18 |

**Release-quality cleanup**

| Finding | Type | Confidence | Evidence | Player / release impact | Rationale | Owner phase |
| --- | --- | --- | --- | --- | --- | --- |
| `INTERNET` trigger investigation | release quality | High it is present; trigger unknown | [Repo] Unity-generated manifest; project scripts show no networking use | **A confirmed permission, not proof of data collection** | Narrow objective: attribute the inclusion trigger with a controlled experiment. Not itself a policy requirement; truthful data-behavior verification is part of the Phase 17 Data Safety MUST | 20 |
| Cold-start `AssetPackManager` error | release quality | High it occurs; impact unknown | [Device] logcat on every cold start | No observed gameplay effect; possible noise in crash and pre-launch reports | **Investigation item; not assumed to be a crash or harmless.** Triage during the first Phase 17 Play-build smoke test; closure here | 20 |
| `DefaultVolumeProfile.asset` dirtied by builds | release quality | High | [Repo] Step 7 diff | Every build dirties git | Review the exact URP diff before choosing the fix (for example, committing the field) | 20 |
| Development and experimental packages | release quality | High | [Repo] embedded, git-ignored MCP copy (10.0.0) overrides the manifest's git URL (also v10.0.0); contents may differ (unverified); `com.unity.pipeline` experimental | No player impact (stripped); reproducibility risk | Check first-build reproducibility in Phase 17; align or remove before the final build | 20 |
| Release build procedure and reproducibility | release quality | Medium | [Repo] platform settings live only in `Library`; builds dirty files; the Phase 12 build doc assumes a visible Platforms list | Hard to reproduce the final build | First procedure in Phase 17 (signing, first AAB), finalized here. If a clean clone cannot build, escalate that concrete failure | 20 |

### CAN DEFER AFTER RELEASE

| Finding | Type | Confidence | Evidence | Player / release impact | Rationale | Owner phase |
| --- | --- | --- | --- | --- | --- | --- |
| RefillSeedReuse | intentional RNG-policy trade-off | High | [Repo] Bootstrap deliberately creates separate generators from the same seed | No visible effect | Not a bug; revisit only if Phase 16 changes the seed policy | 16 |
| Restart without confirmation | polish | Medium | [Repo] bottom center, separated, disabled while busy | Accidental restart possible; never observed | Revisit inside the approved Phase 16 flow work | 16 |
| Optimizing `SyncTransforms` / per-move allocations | performance | Medium | [Repo] not profiled | None observed | Measurement is in the Phase 19 SHOULD row; optimize only if it shows a cost | 19 |
| MissingReadme | product feature (portfolio) | High | [Repo] no README | None for players | **Store readiness ≠ portfolio readiness.** Still required in Phase 23; the project Definition of Done is not satisfied without it | 23 |

### NOT A REAL ISSUE / CLOSE

| Finding | Type | Evidence for closing |
| --- | --- | --- |
| BeamCrystalArtThin | polish | Resolved 2026-10-24; PNGs unchanged since [Repo] |
| WatchList | maintainability note | All three notes still accurate and harmless [Repo]. CLOSE means no current action; the notes stay |
| MobileLayoutAndPicking (main concern) | gameplay bug (resolved) | Full-cell colliders, sync before picking, Phase 12 tests and 1,916 S23 observations [Test][Device]. Comfort and profiling remain separate items |
| No failure in the tested S23 pause/resume scenarios | gameplay bug (not observed) | Every tested scenario passed [Device]. This closes those scenarios only; broader lifecycle verification stays with Phase 19 |
| Process death (current-session reset) | product feature | A fresh start is the current intended behavior [Device]; progression persistence is a separate Phase 16 item |
| Target API 36 / ARM64 | policy/compliance | Met in the non-development APK [Repo][Policy]; re-checked in the AAB |
| Forced tablet orientation override | policy/compliance | Manifest category is game, which Android exempts [Repo][Policy]. This closes that override only, not tablet compatibility generally |
| Backup default | policy/compliance | Acceptable while nothing is stored; revisit when Phase 16 introduces stored progression |
| Account verification | policy/compliance | Complete; `Create app` enabled [Console] |
| Device verification | policy/compliance | Complete: the task is gone, which Google documents as the sign of success [Console][Policy, checked 2026-10-30] |
| ADB instant tap not registered | test-tool observation | 80 ms presses work; suspected injection timing, not proven; no production fix justified [Device] |

---

## Proposed Order of Work

**Default roadmap (unchanged):**

```text
Phase 14 → Phase 15 → Phase 16 → Phase 17 → first test release Published
                                                   │
                                   ≥12 testers opted in continuously for 14 days
                                                   ║
                                    Phases 18 → 19 → 20 → 21 during the test
                                                   ║
                                               Phase 22
```

**MUST items in execution order:**

1. **DeadBoard** (Phase 14). Decide LockedAfterException's escalation inside Phase 14 too, because Phase 17's entry gate depends on it.
2. **In-app privacy-policy access** (Phase 16).
3. **Phase 17:**
   1. app creation;
   2. Play App Signing;
   3. version-code convention;
   4. the Diagnostics decision;
   5. upload key and first signed AAB, with its manifest inspected;
   6. privacy URL;
   7. content rating, target audience, ads and app-access declarations;
   8. first Data Safety answers;
   9. minimum listing;
   10. **publish the closed test and get at least 12 testers opted in.**
4. **Final store listing** (Phase 21), with Data Safety re-checked against the final build.
5. **Production-access application and submission** (Phase 22).

**Can start now, alongside Phases 15/16** (non-engineering lead time):

- **Testers:** recruit 15–20, a buffer above 12.
- **Privacy policy:** draft and host the page.
- **Store copy and art:** draft the store texts; plan the 512 px icon and the feature graphic.
- **Phase 17 prep:**
  - read through the content-rating and target-audience questionnaires;
  - choose the version-code scheme;
  - choose where the upload key will be backed up.

**Accelerated alternative (not recommended):** move Phase 17 directly after Phase 14.

- It would **not** satisfy Phase 17's current entry criteria, which require the Phase 16 level flow to be complete (`PLAN.md`, Phase 17 entry criteria).
- Using it would mean changing that gate as well as the phase order, with explicit approval.
- Benefit: at most about 0–4 days [Estimate], and none if Phases 18–21 take 14 days or more.
- Costs:
  - testers get a build with no swipe, selection cue, menu, levels or Back;
  - the privacy link needs a temporary home;
  - an extra AAB and declarations cycle when Phase 16 lands;
  - weaker engagement evidence for Google's production questionnaire.

---

## Schedule / Dependency Risks

**Critical path:**

P14 → P15 → P16 → P17 → first test release Published → ≥12 testers opted in → max(14 continuous opted-in days, P18→P21) → production-access review → production review → launch.

**Planning estimates** [Estimate]: these are historical/project-planning estimates, **not commitments or guaranteed durations**.

- Phases 11–12 took about 3–4 calendar days each.
- On that basis, Phases 18–21 would take about 10–14 days.
- So the qualifying 14 days are on or near the critical path, adding about 0–4 days under the default order.
- That holds only if 12 testers opt in promptly after publication.

| Dependency | Type |
| --- | --- |
| P14 DeadBoard → P17 | Hard blocker (PLAN entry gate) |
| P16 level flow, privacy link, Back → P17 | Hard blocker (PLAN entry gate: Phase 16 level flow complete) |
| P17 app setup → closed test | Hard blocker (policy) |
| Diagnostics decision → first Play test AAB | Hard blocker (Data Safety truthfulness) |
| First test release Published → testers can opt in | Schedule (Google review time not precisely documented) |
| ≥12 testers opted in for 14 consecutive days → P22 | Hard blocker (policy). The period follows tester opt-in, not publication |
| P16 outcome redesign → P18 dimmer fix | Quality |
| P18 → P19 measurements; P18 → P21 screenshots | Quality / schedule |
| P17 first Play build → AssetPackManager triage, reproducibility check | Verification |
| P20 final AAB → P21 Data Safety | Re-verification |
| P17 first AAB → P20 (API, category, permissions, 16 KB) | Re-verification |

**Top five release risks:**

1. DeadBoard.
2. The closed-test schedule: recruitment, drop-outs, 14 continuous opted-in days and Google's review times.
3. Data Safety truthfulness against the *final* build: Diagnostics and the unknown `INTERNET` trigger.
4. The first signed AAB and the upload-key handling, never exercised so far.
5. Single-handset evidence: device variety, weak API-26 devices, long sessions.

Close behind: Phase 16 scope creep, because Phase 16 sits on the critical path.

**External / schedule risks** (not engineering defects):

- the testing requirement;
- recruitment and drop-outs;
- time to publish the first test release;
- the production-access decision;
- policy changes before submission.

**Accepted for the first release:**

- RefillSeedReuse;
- restart without confirmation until the Phase 16 flow replaces it;
- losing an in-progress level on process death;
- no `SyncTransforms` or allocation optimization unless Phase 19 measurement shows a cost;
- the WatchList items;
- a small level set;
- no colour-blind *mode*, only one small cue;
- relying on the Back opt-out;
- Android's default backup;
- README after store submission.

---

## Open Verification Items

| Item | How it will be verified | Phase |
| --- | --- | --- |
| Which setup tasks Console marks mandatory before closed testing | Play Console during setup (policy already requires completed app setup) | 17 |
| The production-access requirement as shown in Console | Console after app creation (policy already says it applies) | 17 |
| Data Safety answers | Inspect the Phase 17 AAB, then the final build | 17 → 21 |
| Diagnostics behavior | The explicit Phase 17 decision, plus build evidence | 17 |
| What makes Unity add `INTERNET` | Controlled build experiment | 20 |
| Impact of the `AssetPackManager` error | Triage in the Phase 17 smoke test; investigation and Play pre-launch report | 20 |
| Whether the embedded MCP copy differs from the git-URL package | Compare contents; check that a clean clone builds | 17 → 20 |
| The `DefaultVolumeProfile.asset` fix | Review the exact URP diff | 20 |
| 16 KB page-size compatibility of the AAB and on device | Inspect the AAB; device or pre-launch evidence | 20 |
| DeadBoard frequency | Core-only simulation | 14 |
| A reachable LockedAfterException trigger | Adversarial testing; decides the escalation | 14 |
| Human finger comfort | The developer's own hands on the S23 | 15 |
| Devices beyond the S23; long-session performance; sync cost and allocations | Second device and/or pre-launch report; measurement | 19 |
| Time to publish the first test release | Observe in Phase 17 | 17 |
| `LockedAfterException.md` understates the impact | Update the known-issue doc in Phase 14. It omits that Restart and Play Again stay disabled, and that a throw before the busy notification leaves Restart clickable but refused. Several known-issue docs still use pre-reorder owner wording | 14 |

---

## Explicit Non-Goals

The first release does **not** add:

- **Monetization:** ads, IAP, an economy, a currency or shop, daily rewards.
- **Online services:** login/accounts, a backend, cloud save, push notifications, analytics for its own sake, multiplayer, live ops.
- **Content and social:** dozens of levels, social systems, achievements or leaderboards.
- **Frameworks and architecture:** a generic gesture framework, a generic animation framework, ECS/DOTS, heavy dependency injection, object pooling without profiling evidence, large architecture rewrites.
- **New design:** new special-tile systems, a combo-special redesign, a new art direction.

**Product target:** a small, coherent, offline Match-3 game that can be responsibly released, tested and explained in an interview.

**Scope-creep rule:** when a finding can be solved by a small targeted fix or by a new generalized system, prefer the targeted fix, unless the repository shows a reusable system is already necessary.

Expansions considered and rejected during the audit:

| Rejected | Targeted alternative |
| --- | --- |
| A full colour-blind mode with settings | One inner symbol per gem colour |
| A generic gesture system | Dominant direction plus a threshold, feeding the existing swap request |
| An exception-recovery or state-machine framework | The minimal recovery decided in Phase 14 |
| A hint system or elaborate shuffle animation | Detect, plus a simple shuffle or end rule |
| An analytics or crash SDK | Play Console's built-in crash data and the pre-launch report |
| Cloud save or accounts | A tiny local save |
| A level editor | A few hand-written level definitions |

---

## Phase 13 Conclusion

The game is functionally sound for a small first release:

- **Gameplay rules:** strongly tested.
- **Restarts:** deterministic.
- **Pause/resume:** no failure in the scenarios tested on the S23.

What separates it from a responsible Google Play release:

- **One gameplay safeguard:** DeadBoard (Phase 14).
- **Distribution and policy setup:** Phases 16–17.
- **The external closed-test schedule:** at least 12 testers opted in for 14 continuous days.
- **Release-scope quality work** that is already planned: Phases 15, 16 and 18–21.

No production change was made during Phase 13, and the roadmap order is unchanged.

**Learning-gate categories, with an example of each from this audit:**

| Category | Example |
| --- | --- |
| Gameplay bug | DeadBoard |
| Release blocker | No signed AAB |
| Polish issue | Tall-screen composition, outcome dimmer |
| Policy/compliance requirement | Closed test, privacy policy, Data Safety |
| Performance concern | Sustained FPS, `SyncTransforms` cost |
| Deferred product feature | README for the portfolio; anything in the non-goals list |

---

## Audit Corrections After Review

Codex's challenge review (Step 15) found two factual errors in the first draft. Both were verified and corrected:

1. **Phase 17 entry gate.** The draft said the accelerated path satisfied Phase 17's entry criteria. `PLAN.md` also requires the Phase 16 level flow to be complete, so the accelerated path would need that gate changed.
2. **Closed-testing setup rule.** The draft listed "closed testing requires completed app setup" as unverified. Google's testing-requirements page states it explicitly: a closed test can start after completing app setup.

The other review changes narrowed evidence claims or added missing evidence; none changed a classification or an owner phase:

- **Narrowed claims:** lifecycle closure, the ADB tap explanation, package-version difference, Diagnostics presence, regional address disclosure.
- **Added evidence:** 16 KB alignment, full APK path and hash, warning categories.
