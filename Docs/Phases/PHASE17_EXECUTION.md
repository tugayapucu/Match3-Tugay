# Phase 17 Execution Plan — Google Play Test Bootstrap

Phase 17 is the first release/distribution phase. It does **not** produce the final production release.

Its purpose:

- get a real signed build into Google Play testing early;
- validate the Play delivery and update path;
- complete the minimum policy/setup work a closed test needs;
- start the account-required testing clock while Phases 18–21 continue.

---

# Phase Roles

```text
Claude → main developer / release engineer
Codex  → reviewer / challenger
Human  → owner of irreversible Play Console, legal/policy, pricing and secret decisions
```

**Claude:**
- repository inspection;
- Unity release configuration and builds;
- AAB inspection and automated validation;
- the in-app privacy link;
- release documentation;
- guiding Play Console setup;
- classifying technical findings.

**Codex:**
- reviews release and architecture decisions;
- challenges signing/build assumptions;
- checks manifest and Data Safety evidence;
- finds policy/setup gaps;
- reviews test-track and update evidence;
- challenges any claim stronger than its evidence.

**The human explicitly approves or performs:**
- package identity before app creation;
- free vs paid;
- Play App Signing acceptance;
- upload-key creation/backup;
- any secret or password entry;
- app-content, target-audience, content-rating and Data Safety answers;
- privacy-policy publication;
- every AAB upload or track release;
- tester invitations;
- starting the closed test.

Agents may prepare, inspect and guide these actions, but never make irreversible or policy/legal decisions silently.

---

# Current Policy Baseline — Re-verify in Step 1

Policy facts were checked against official Google/Android documentation on **2026-11-12**. They are a working baseline, not a substitute for the current Play Console UI. Step 1 re-checks each one.

| Topic | Baseline | Reference |
| --- | --- | --- |
| Target API | New apps and updates must target Android 16 / API 36 or higher | [answer/11926878](https://support.google.com/googleplay/android-developer/answer/11926878) |
| Format | New apps publish as Android App Bundles (`.aab`) | [app-bundle](https://developer.android.com/guide/app-bundle) |
| 16 KB page size | Apps targeting API 35+ must support 16 KB pages on 64-bit devices. On 2026-11-13 the page said updates without support can't be released from February 1, 2027. Treat it as a required upload validation item now; Step 1 records the then-current wording | [16 KB page sizes](https://developer.android.com/guide/practices/page-sizes) |
| Production access | The Phase 13 audit recorded a **Personal** account created **2026-10-29**. The new-personal-account rule requires at least **12 testers continuously opted in for at least 14 days** in a closed test before applying for production. Confirm it in this account's Console | [answer/14151465](https://support.google.com/googleplay/android-developer/answer/14151465) |
| Internal vs closed | Internal: no app-setup prerequisite, up to 100 testers. Closed: app setup must be complete; this is the track used for the production-access test | [answer/9845334](https://support.google.com/googleplay/android-developer/answer/9845334) |
| Data Safety | Internal-only apps are exempt. Closed, open and production tracks require the form, even when no data is collected, plus a privacy-policy link | [answer/10787469](https://support.google.com/googleplay/android-developer/answer/10787469) |
| Privacy policy | It must be in the Console/listing **and reachable inside the app**. It must be a public HTTP(S) web page (not a PDF), with no login or geo-restriction, and specific to this app | [answer/9859455](https://support.google.com/googleplay/android-developer/answer/9859455) |
| App creation | Asks for app name, package, default language, app or game, free or paid, contact email, policy/export declarations and Play App Signing | [answer/9859152](https://support.google.com/googleplay/android-developer/answer/9859152) |
| Free vs paid | Paid → free is allowed. Once free, the app can never become paid under the same package. The roadmap assumes a **free, non-monetized** first release, and the human reconfirms that before Create app | [answer/6334373](https://support.google.com/googleplay/android-developer/answer/6334373) |
| Signing | **Upload key:** held by the developer; it signs the uploaded AAB and can be reset through Play if lost. **App-signing key:** held by Google; it signs Play-delivered artifacts | [answer/9842756](https://support.google.com/googleplay/android-developer/answer/9842756) |

**Tester-track rule (keep it even if the Console wording differs):**
- Never use the main closed-test cohort for Internal testing.
- Someone in Internal who needs Closed must leave Internal first, then opt into Closed.
- Their closed-test continuity starts from the Closed opt-in, not from Internal use.

Step 1 records Play's current wording for users in more than one track. The rule does not depend on it.

---

# Phase 13 Carry-overs

**Release baseline recorded by Phase 13 (re-inspect in Step 1):**
- Unity 6000.5.10f1;
- min SDK 26; target SDK Automatic (resolved to 36);
- IL2CPP, ARM64 only, portrait, ETC2;
- `versionName 1.0` / `versionCode 1`;
- no custom keystore and no launcher icon;
- `productName: agentic-match3-unity`, `companyName: DefaultCompany`.

Change only what the first Play test needs. Phase 20's final optimization/build pass is out of scope.

| Carry-over | Phase 17 obligation |
| --- | --- |
| **Package identity** `com.tugaykapucu.match3` | It becomes irreversible when Play registers it. Do not create the app until the human confirms it. |
| **`INTERNET` permission** | Unity's generated manifest declared it, and project scripts show no networking use. That is **not proof of data collection or of non-collection**. Investigate it in Step 9. |
| **Unity services / SDK surface** | Phase 13 found no Ads, IAP, login, third-party analytics or crash SDK. Unity analytics/diagnostic/web-request modules exist, but there is no Cloud link and no active services. Re-check against the current repo and the built artifact. |
| **`AssetPackManager` `ClassNotFoundException` at cold start** | Triage it on a **Play-installed** build, and don't suppress the log. A crash or broken feature → Phase 17 blocker. A Play-delivery issue with concrete impact → focused fix. A harmless but unexplained log → document it and keep the Phase 20 closure item. |
| **Embedded MCP package** | It is a local, git-ignored copy, while `manifest.json` points to v10.0.0 via a git URL. Compare the two and build from a clean clone (Step 5). Never commit the embedded copy to make the difference disappear. |
| **16 KB page size** | The audit routed this to Phase 20, but it is checked on every upload. Phase 17 validates it from the first AAB (Step 6). |

---

# Scope

**In scope:**
- release identity and package registration;
- versioning convention and upload signing;
- the first signed release AAB and its inspection;
- clean-clone reproducibility;
- Internal Releases A and B, with an S23 smoke test and the Play update path;
- the privacy-policy page and in-app access;
- Data Safety evidence;
- app-content declarations;
- a minimum truthful listing;
- closed-test cohort and launch;
- the first Play reports.

**Out of scope:**
- final store screenshots, art, copy and launcher icon (Phase 21);
- the final production AAB (Phase 20);
- production-access application and rollout (Phase 22);
- ads, IAP, analytics SDKs, backend or accounts, cloud save;
- broad performance work (Phase 19);
- presentation polish (Phase 18).

---

# Key Release Strategy

```text
Internal Release A   → prove signing / package / AAB / Play delivery quickly
                       (internal can start before full app setup)
privacy page + in-app link + app-content setup
Internal Release B   → higher versionCode, installed as a real Play update
Closed test          → promote/reuse the verified Release B; start the 12 × 14-day clock
```

Prefer promoting the same tested AAB to Closed over an untested rebuild, unless Play Console requires otherwise.

---

# Versioning Convention

- Testing uses `versionName 0.1.0`, starting at `versionCode 1`, provided code 1 is unused in Play.
- **Every upload increases `versionCode`.** A code is never reused, even for a rejected upload.
- `versionName` may stay `0.1.0` across testing uploads until the product meaningfully changes.

Example sequence:
- Internal A: `0.1.0 (1)`;
- Internal B: `0.1.0 (2)`;
- later test builds: `(3)` onwards;
- Phase 20 production candidate: `1.0.0 (next unused code)`.

Every upload is recorded in the Release Ledger (see Documentation).

---

# Secret / PII Rules

**Never commit, serialize, or paste into agent prompts:**
- keystore or key passwords;
- private keys;
- recovery codes;
- Play account credentials;
- tester emails or names;
- personal legal/account screenshots;
- private contact information;
- local machine paths, including keystore paths.

**Keystore files** live outside the repository. Add defensive `.gitignore` entries `*.jks` and `*.keystore` anyway.

**Safe to record:**
- upload certificate SHA-256 fingerprint;
- alias, optionally;
- creation date;
- backup verified yes/no and the generic backup *category*;
- Play App Signing status;
- anonymous tester IDs (`Tester 01`, …) and counts.

---

# Step 1 — Phase Entry Gate

**Goal:** confirm Phase 17 may begin. Read-only; no production file changes.

**Required:**
- Phase 14 complete;
- Phase 16 formally closed;
- no open MUST-FIX gameplay blocker;
- clean working tree with HEAD recorded;
- package still `com.tugaykapucu.match3`.

**Record the current state of:**
- the test baseline;
- Unity version;
- Android Player settings: package, `productName`, `companyName`, version name and code, target/min SDK, IL2CPP/ABI, Development Build and Script Debugging, Internet Access, Predictive Back support, symbols setting;
- signing and icon state;
- packages;
- known release issues.

**Account verification (human, in Play Console):**
- account type Personal, with creation date and eligibility state;
- identity and device verification complete;
- Create app available;
- the production-access testing requirement is shown;
- no existing app owns the package.

Record only non-sensitive state.

**Policy re-check:** re-verify every row of the Policy Baseline, including the current 16 KB enforcement wording and Play's wording about users in more than one testing track. Note any differences.

STOP if Phase 16 is not actually closed.

---

# Step 2 — Irreversible Product / Play Identity Gate

**Goal:** decide the values an agent must not guess. Produce **one identity table** for human approval before Create app.

| Decision | Proposal / constraint |
| --- | --- |
| Package | `com.tugaykapucu.match3` |
| Working Play app name | Shown to testers. The current in-game title is `MATCH 3`. Testers must not see a technical placeholder. |
| Android launcher/display name | Unity `productName`, currently `agentic-match3-unity`; see the Step 4 rename note |
| `companyName` | Currently `DefaultCompany`. Decide whether it is acceptable for the first Play test or should change before release identity is committed. It also affects Editor paths (Step 4). |
| Default language | Likely English, matching the current UI. The human confirms the exact locale. |
| App vs game | Game |
| Free vs paid | Free. If the human might ever charge for the base app: **STOP before Create app.** |
| Contact email | Supplied by the human; never invented |
| Google Play Games on PC | Review the default distribution state. If PC isn't in scope, record the choice and opt out where Play allows. No PC work in Phase 17. |

---

# Step 3 — Upload-Key and Signing Design

**Goal:** a recoverable upload key, with no secrets and no local paths in Git.

## Upload key
Create it with standard Java/Android tooling, human-guided:
- RSA 2048+ with long validity;
- a unique alias and a strong password;
- the password stored only in the human's secret manager;
- the keystore **outside the repository and the Unity project**;
- avoid echoing the password into terminal history.

## Unity keystore serialization
Configuring a custom keystore in Unity writes local information into the committed `ProjectSettings/ProjectSettings.asset`:
- `AndroidKeystoreName`, the keystore's local path;
- `AndroidKeyaliasName`;
- `androidUseCustomKeystore`.

**Approved Phase 17 approach:**

```text
set custom keystore for the build session
→ build / sign
→ inspect the ProjectSettings diff
→ restore the local keystore path / alias / custom-keystore serialization
→ never commit local machine paths or signing configuration that exposes them
```

The restore step also runs after a **failed or cancelled** build. Unity has already written the fields by then.

- **Passwords and private keys:** never serialized or committed.
- **No build script yet:** no Editor build script or environment-variable framework in Phase 17. Reconsider only if repetition later makes it worthwhile.

## Backup gate (before the first upload)
The human confirms:
- the primary keystore exists;
- at least one separate secure backup exists;
- the password and recovery information are stored separately.

Do not proceed on "I'll back it up later."

---

# Step 4 — Release-Test Build Configuration Decision

**Goal:** define the exact first Play-test build.

```text
Build App Bundle (.aab): ON      Development Build: OFF     Script Debugging: OFF
IL2CPP · ARM64 · portrait        target API ≥ 36             min API 26 unless evidence requires change
signed with the upload key (Step 3 session approach)
```

**Decisions to make:**
- **Target SDK:** either keep Automatic and prove `targetSdk ≥ 36` from the AAB, or serialize API 36 explicitly for reproducibility. Prefer the smallest setting that gives deterministic evidence.
- **Version:** use the approved convention (`0.1.0 (1)`), and only if code 1 is unused.
- **Native debug symbols:** for IL2CPP, evaluate generating native symbols for test releases so Play's crash/ANR reports can be symbolicated.
  - Preferred: **Create symbols.zip = Public** (Unity Android build setting). Upload the zip with the AAB where Play accepts it, and record which artifact and version it belongs to.
  - A deferral needs an explicit, concrete reason.
  - Symbol files never go into source control.
- **`productName` rename:** replace `agentic-match3-unity` with the approved launcher name before closed testing.
  - This changes Editor-local paths: the Windows Editor PlayerPrefs location, `persistentDataPath`, and test/Editor data locations.
  - Existing **Editor** progression may therefore look reset. That is expected, not a gameplay persistence bug.
  - Android players are unaffected, because Android PlayerPrefs are keyed by package name.
  - Apply the Step 2 `companyName` decision at the same time; it affects the same paths.
- **App icon:** no final art pass. A temporary, truthful icon is acceptable if Play or the build requires one.

**Build-side effects:**
- Before building, snapshot the tracked files Unity builds are known to rewrite: `ProjectSettings.asset`, the URP assets and global settings, `DefaultVolumeProfile.asset`, and any tracked Build Profiles.
- Afterwards:
  - keep and review intentional release settings;
  - inspect Unity build dirtiness file by file, including the Step 3 keystore fields;
  - never commit it blindly.

---

# Step 5 — Clean-Clone Reproducibility and MCP Comparison

**Goal:** prove the build depends on no hidden local state.

**Clean-clone build:** in a temporary directory outside the repo, at the exact reviewed commit:
1. clone;
2. let Unity resolve packages;
3. import with the same Unity version;
4. build the equivalent non-development Android artifact;
5. confirm compilation and the build succeed.

The clone needs no committed keystore. Configure signing locally, or use debug signing with its purpose documented. The real release AAB still requires the upload key.

**MCP comparison:** compare the workspace's embedded MCP package with the clone's resolved v10.0.0, checking:
- the version;
- `package.json`;
- a source-content diff or checksum (excluding VCS and cache noise).

No material difference → document and close. A material difference → determine which copy is authoritative. Never auto-commit the embedded package.

**Gate:** a clean-clone failure is a Phase 17 blocker until understood.

---

# Step 6 — First Signed Internal AAB

**Goal:** the first real Play-uploadable artifact. It may precede privacy/app-content setup, since Internal can start early.

**Pre-build:**
- full EditMode and PlayMode runs;
- Console review;
- record the commit SHA.

**Build:**
- a signed release-config `.aab`, stored outside source control;
- symbols per the Step 4 decision;
- then restore the keystore serialization (Step 3).

**Record in the ledger:**
- filename, size, SHA-256;
- commit and Unity version;
- `versionName` / `versionCode`;
- build settings and warning count;
- symbols artifact, if any.

## AAB inspection
Inspect the **actual AAB**; never infer from older APK evidence. Use bundletool, the SDK tools or analyzers available locally, and validate the bundle.

| Check | Evidence required |
| --- | --- |
| Identity | package, `versionName`, `versionCode`, launcher activity, label |
| SDKs | min SDK; target SDK ≥ 36 |
| Debuggability | `debuggable` false |
| Native ABIs | arm64-v8a only, as configured |
| **16 KB page size** | All four checks in the 16 KB section below. Don't assume Unity 6 guarantees any of them. |
| **Predictive Back opt-out** | The **effective** value for the launcher activity is `false`: either the activity declares `android:enableOnBackInvokedCallback="false"`, or it declares nothing and `<application>` declares `false`. An activity-level `true` overrides an application-level `false` and **fails** the check. Read the built manifest, not PlayerSettings. This proves configuration only; the S23 Back checks (Steps 8, 17) prove behavior. |
| Permissions | the full list, especially `INTERNET`; no unexpected entries |
| Components | no unexpected services, providers, receivers or SDK metadata |
| App category | game (`android:appCategory`), if present |
| Signature | signed by the intended upload certificate; record its fingerprint |

## 16 KB validation
Follow [Android's page-size guide](https://developer.android.com/guide/practices/page-sizes). Static checks:

1. **Bundle packaging:** `bundletool dump config --bundle=<aab>` shows `PAGE_ALIGNMENT_16K`. `PAGE_ALIGNMENT_4K` fails the check.
2. **ELF `LOAD` alignment:** run `llvm-objdump -p` or `llvm-readelf -Wl` (NDK) on **every `.so`**. Every `LOAD` segment must have align ≥ `2**14` (`0x4000`).
3. **RELRO:** for every `.so` with a `GNU_RELRO` segment, one of these must hold:
   - `(VirtAddr + MemSiz) % 0x4000 == 0`;
   - or RELRO ends exactly where its containing `LOAD` segment ends.
4. **Generated APKs:** build the arm64 device APKs from the AAB with `bundletool build-apks`. Run `zipalign -c -P 16 -v 4` (Build-Tools 35+) on each APK that contains `.so` files, and expect "Verification successful".

**Independent confirmation:** after upload, Play Console's App Bundle Explorer must show no 16 KB issue. Play's result confirms the local checks; it doesn't replace them.

**Static vs runtime:** these checks prove the package is laid out correctly. They don't prove the game runs on a 16 KB kernel.
- The S23 runs 4 KB pages; record `adb shell getconf PAGE_SIZE` once.
- A 16 KB runtime test, for example Samsung Remote Test Lab, is optional in Phase 17.
- If it isn't done, record "not runtime-tested on 16 KB" and never claim full compatibility.

---

# Step 7 — Create the Play Console App

**Goal:** register the app, only after the identity (Step 2) and signing (Step 3) are approved. This is a human step.

- Before clicking **Create app**, show the human the final identity table, with **PACKAGE** and **FREE/PAID** highlighted. Don't create the app while either is ambiguous.
- Accept the policy, export and Play App Signing terms only after human review.
- After the first artifact is accepted, record the public signing certificate metadata and status in the ledger.

---

# Step 8 — Internal Test Release A

**Goal:** prove Play accepts and distributes the bundle.

**Testers:** the developer's own account or a very small internal-only list. The closed cohort is never used here.

**Before rollout:**
- the artifact SHA matches the inspected AAB;
- the version code is correct;
- release notes are truthful;
- the Console shows no blocking error;
- App Bundle Explorer shows no 16 KB or native-library warning.

The human approves the release.

**S23 pre-check (before installing A):** check whether `com.tugaykapucu.match3` is already installed, and with which signing certificate.
- A locally built (debug or upload-key) install can't be updated by the Play-signed A.
- If an uninstall is needed, the human explicitly approves it and acknowledges that the S23's saved progression is lost.
- This is the only planned uninstall. A → B must stay an in-place update.

**Play install:** install on the S23 **from Google Play**; no sideloading. Verify:
- package, version and launcher label;
- launch, portrait and Main Menu;
- first board tap, swipe and two-tap;
- safe area;
- Restart/Leave confirmations;
- Win/Loss and specials;
- **Android Back in every Phase 16 state** (the manifest opt-out proven on device);
- persistence after relaunch;
- no unexpected permission prompt.

**Logcat:** capture one controlled cold start. Inspect it for:
- crash or fatal lines;
- ANR indicators;
- the `AssetPackManager` exception (classify it per the carry-over table);
- unexpected networking or service errors.

---

# Step 9 — Data-Surface / INTERNET Evidence Review

**Goal:** evidence for the Data Safety answers, gathered after the real release AAB exists.

**Start here:** Player Settings → **Internet Access = Auto** (`ForceInternetPermission: 0`). Yet the build declares `android.permission.INTERNET`. Find out why:

- which assemblies or packages in the **player build** reference networking APIs (`UnityWebRequest`, sockets, HTTP clients, networking modules);
- how Unity's generated manifest decides to add the permission under Auto;
- which Unity services are configured, the Cloud link state, `Packages/manifest.json` and the lockfile, third-party packages and Android plugins;
- what remains after stripping: the actual built artifact and observed runtime behavior;
- any actual use of analytics, crash upload, ads, authentication, location, microphone/camera, identifiers, contacts, or files outside app storage.

**Rules:**
- **Do not change Internet Access to make the permission disappear.** Understand the cause and its Data Safety implications first.
- A module name doesn't prove runtime collection, and a lack of explicit game code doesn't prove that the engine or an SDK collects nothing.
- Where engine diagnostics or analytics behavior stays ambiguous, check current Unity documentation.

Record the findings in the ledger's Data Safety section. Keep five things separate: **permission present / SDK or module present / feature configured / runtime behavior observed / proposed answer**. Codex challenges the evidence before any answer is proposed.

---

# Step 10 — Privacy Policy Source and Hosting

**Goal:** a public privacy page based on Step 9 evidence. No claims before that evidence exists.

**Content:**
- the app name and package;
- developer contact;
- data collected and shared, if any;
- local-only progression storage;
- relevant platform/third-party services;
- retention and deletion, where applicable;
- target-audience implications;
- an effective date.

If the evidence supports "no user data collected or shared by the app", say exactly that. Still explain the local storage and the platform boundaries. Never claim that Google Play itself collects nothing.

**Hosting:**
- the human chooses a public HTTPS host with a stable URL;
- no login, no geoblock;
- a normal web page, not a PDF;
- GitHub Pages is acceptable if deliberately configured.

**Verify** in a logged-out/private browser:
- the page loads over valid HTTPS;
- no login is required;
- the app and package are identifiable;
- the contact works;
- it reads well on mobile.

Record the URL in the ledger.

---

# Step 11 — In-App Privacy Policy Access

**Goal:** close the Phase 13 → 16 carry-over. Add a real **Privacy Policy** action to the Main Menu once the hosted URL exists.

**Architecture:** it stays scoped to Presentation and game flow, and Core is unaware of it. Smallest shape: `MainMenuView` forwards the click, and GameFlow (or a narrow helper) opens the approved HTTPS URL. No browser or navigation framework.

**UI:**
- secondary to Play / Continue / Play Again;
- inside SafeArea;
- readable and tappable;
- cannot be triggered through board input.

**Automated tests** (never launch a real browser):
- the wiring exists;
- a click forwards once;
- the URL is non-empty HTTPS;
- the menu still gates board input;
- the primary action is unchanged;
- re-initializing does not stack listeners.

**S23 device check:**

```text
Main Menu → Privacy Policy → browser opens the correct HTTPS page → Android Back → game returns to Main Menu
```

Verify:
- board input is still gated;
- there is no stale gesture;
- the menu state and label are correct;
- no session started unexpectedly.

**Phase 16 evolution:** "Main Menu has exactly one real button" was true for Phase 16. Phase 17 deliberately adds a second real action. Document it as an evolution, not a regression.

---

# Step 12 — Internal Release B / Play Update Path

**Goal:** verify a real Google Play update.

**Build:**
- after the privacy link and any approved fixes, build Release B with a **higher `versionCode`**;
- inspect it again with the full Step 6 table, including the 16 KB and Back checks;
- add it to the ledger.

**Update** on the S23:
1. keep Release A installed;
2. wait for Play to offer B;
3. update through Play, without uninstalling first.

**Verify:**
- progression survives as intended;
- the Privacy action appears;
- launch and gameplay work;
- the version changed;
- no signature mismatch;
- no new permission prompt.

**Data Safety delta (before any declaration is submitted):** B is the artifact Closed testers receive, so Step 9's evidence must cover B, not only A. Briefly compare A → B, then confirm Step 9 still holds or update it, and record the result in the ledger:
- code diff;
- package/lockfile changes;
- manifest permissions and components;
- new runtime behavior, such as opening the privacy URL in the browser.

---

# Step 13 — Play Console App Setup for Closed Testing

**Goal:** the minimum real setup Closed needs. The Console's current Dashboard tasks are authoritative over this list.

| Item | Expectation / rule |
| --- | --- |
| Privacy policy | The verified URL from Step 10 |
| App access | All gameplay without login. The human confirms the answer. |
| Ads | No ads. The human confirms. |
| Target audience | The human chooses age groups from the actual game; never "children" just because it's colorful. If a child-directed group is selected, **STOP and review the Families policy** first. |
| Content rating | Answer the questionnaire honestly; don't predict the rating. |
| Data Safety | From the reviewed Step 9 evidence, updated by the Step 12 B delta. Codex challenges it before the human submits. |
| Closed countries/regions | Include every country the cohort is in. Internal access may bypass country availability, so Internal success doesn't prove Closed availability. |
| Category / tags | Only what setup requires; optimization is Phase 21. |
| Contact | The approved public email. No private phone or address unless Play requires it. |

---

# Step 14 — Minimum Truthful Store Listing

**Goal:** satisfy closed-test setup without Phase 21's marketing pass.

**Prepare:**
- the working name;
- a short and a full description;
- a temporary compliant icon and feature graphic, if required;
- the minimum screenshots.

**Truthfulness:**
- describe only features that exist;
- no rankings, awards, multiplayer, leaderboards, large level counts or daily content;
- no broad device-support claims from one S23.

**Screenshots:**
- use real current screens;
- raw S23 1080×2340 captures exceed Play's aspect-ratio limit (Phase 13), so crop or pad them to accepted dimensions without distorting gameplay.

**Provenance:** all art is original, project-owned or licensed, and its provenance is recorded. Never copy another Match-3 listing.

---

# Step 15 — Closed-Test Cohort Preparation

**Goal:** enough testers that normal drop-off can't break the requirement.

**Cohort:**
- invite **15–20**, aiming for **at least 12 continuously opted in** for the whole period;
- apply the tester-track rule from the Policy Baseline: the closed cohort stays separate from Internal, and an Internal user leaves Internal before opting into Closed, with their clock starting at the Closed opt-in.

**Privacy:**
- track testers privately (ID, invited, opted-in date, still opted in, feedback);
- the repo holds anonymous counts only.

**Feedback:**
- one simple channel: email, a private group, or a form;
- no analytics SDK added for this;
- ask for device and Android version, crashes/freezes, touch problems, confusing UI, difficulty, readability, and update/install issues.

---

# Step 16 — Closed Release Launch

**Goal:** start the clock as soon as the verified build and app setup are ready. Promote/reuse Release B where Play supports it; never ship an untested rebuild just to change track.

**Pre-launch gate:**
- app setup is complete: privacy page live, in-app link present, Data Safety, content rating, target audience, ads and app-access declarations, and the listing accepted;
- the artifact has been Play-tested;
- no blocking internal-test crash;
- the cohort is ready.

The human approves the start.

**Record in the ledger:**
- version name and code;
- SHA-256 and commit;
- publication timestamp;
- invite count;
- first observed opted-in count.

Continuity is per tester: publication time is not each tester's 14-day start, so track it conservatively.

**"Clock started" means all of:**
- the Closed release is published and available, not just submitted or in review;
- **at least 12 eligible accounts are actually opted into Closed**. Evidence is the Console's count where shown, otherwise each tester's private confirmation of the opt-in page. Invitations and publication time don't count.
- none of those 12 is still in Internal.

Fourteen qualifying days only **allow** the production-access application (Phase 22); they don't guarantee approval.

**Clock rule:**
- Phase 17 may complete once the clock has started as defined above. It does **not** wait 14 days.
- If opt-ins stay below 12, Phase 17 stays open on this item.
- Phases 18–21 continue in parallel.
- The production-access application is Phase 22.

---

# Step 17 — Closed-Track Install / S23 Smoke

**Goal:** validate Closed-track delivery, as a distribution smoke test rather than a full Phase 16 retest.

**Delivery evidence:** B already installed from Internal does **not** prove Closed delivery of the same B, because the same `versionCode` produces no new install. Verify all of:
- the S23's active Play account has left Internal;
- that account is opted into Closed;
- Play shows the app as available on the Closed track for that account.

A fresh install from Closed would need an uninstall. That only happens with explicit human approval and the progression loss acknowledged; otherwise record "Closed delivery verified by account and enrollment, not by reinstall".

**On the S23, verify:**
- track and version;
- launch, Main Menu and the Privacy action;
- progression, gameplay, touch and safe area;
- Restart/Leave and Win/Loss;
- **Android Back in every Phase 16 state** (the release-manifest opt-out proven on the Closed build);
- background/resume;
- no unexpected permission prompt.

**Log:** capture one controlled cold-start log. Re-check `AssetPackManager` and record whether Play distribution changes it.

---

# Step 18 — Play Automated Reports and Compatibility

**Goal:** review the Play-side evidence once it exists. Don't invent results while processing is pending.

**Inspect:**
- the pre-launch report;
- crashes and ANRs (symbolicated if Step 4's symbols were uploaded);
- device compatibility and exclusions;
- bundle/device delivery;
- policy and target-API warnings;
- 64-bit, native-library and **16 KB** findings.

**Classify each finding:**

| Class | Meaning |
| --- | --- |
| **MUST** | Blocks or risks the test/release |
| **SHOULD** | A real issue, fixable in a later planned phase |
| **DEFER** | Not impacting, or belongs to Phases 18–21 |
| **FALSE POSITIVE / EXTERNAL** | Evidence supports no project fix |

A Play finding is evidence, not automatically a bug; Codex challenges the classification. `AssetPackManager`: connect any related Play-reported failure to the behavior it affects. If it stays an isolated log with no functional impact, record that and keep the Phase 20 closure item.

---

# Documentation (Steps 19–20)

Phase 17 produces exactly **two** documents. Create them during execution, not before Step 1.

## Step 19 — `Docs/Release/RELEASE_LEDGER.md` (living)

A release-engineering record that outlives Phase 17; Phases 20–22 extend it. Sections:

1. **Uploads:** a table of `versionName`, `versionCode`, commit, AAB SHA-256, size, symbols artifact, track, upload/publication date.
2. **Signing:** upload certificate fingerprint; Play App Signing status and certificate metadata. **Never secrets or local paths.**
3. **Manifest and permissions:** the Step 6 inspection results per artifact, including 16 KB alignment and the Predictive Back opt-out.
4. **Data Safety evidence:** Step 9's findings, in its five separate columns, plus the submitted answers.
5. **Privacy:** the public URL and its verification date.
6. **Testing tracks:** internal/closed status, anonymous tester counts and continuity status, feedback themes, bugs, and builds sent. If little feedback arrived, say so; never manufacture engagement evidence.
7. **Play reports:** each finding with its classification.

No tester PII, ever.

## Step 20 — `Docs/Validation/PHASE17_SUMMARY.md` (phase end)

About 1,500 words, following the Phase 15/16 summaries:
- what was set up;
- the key decisions and why;
- the major validation evidence (pointing to the ledger rather than copying it);
- remaining and deferred issues.

---

# Step 21 — Learning Review

Before Phase 17 closes, the developer should be able to explain:

- **AAB vs APK:** an APK is an installable package; an AAB is a publishing bundle from which Play generates optimized APKs.
- **Upload key vs app-signing key:** the upload key proves who uploaded and is held by the developer; the app-signing key signs what Play delivers and is managed by Play App Signing.
- **`versionCode` vs `versionName`:** the code orders updates and is never reused; the name is the user-visible string.
- **Internal vs closed:**
  - Internal is fast and small, and may start before setup, but doesn't satisfy the personal-account requirement.
  - Closed requires setup and is the required test.
- **Why 15–20 testers when the minimum is 12:** a buffer against non-opt-ins and drop-off. The minimum is policy; the buffer is risk management.
- **Why Data Safety comes from build evidence:** combine the actual manifest, dependencies, configured services, runtime behavior and official SDK documentation. Neither a permission nor the absence of code settles it alone.
- **Why 16 KB alignment and the Back opt-out are checked in the built artifact:** settings describe intent; the artifact is what ships. Static checks prove the layout, not runtime behavior on a 16 KB device.
- **Why the privacy policy appears inside the app:** it is a user-facing requirement, not store decoration.
- **Why the closed-test clock starts now:** later phases run in parallel with the external requirement.

---

# Step 22 — Completion Gate

Phase 17 is complete when every applicable item is true. Details live in the referenced steps.

**Entry and identity (Steps 1–2)**
- [ ] Phase 16 closed; baseline clean; Play account status and closed-test requirement confirmed in Console.
- [ ] Policy baseline re-verified, including current 16 KB wording and multi-track tester wording.
- [ ] Identity table approved: package, app/display name, `companyName`, language, Game, Free, contact email, PC distribution.

**Signing and versioning (Steps 3–4)**
- [ ] Upload key outside the repo; backup and separate password storage verified; fingerprint recorded.
- [ ] `*.jks` / `*.keystore` gitignore defense in place.
- [ ] Keystore path/alias serialization restored after every build, including failed or cancelled builds; no local paths or secrets committed.
- [ ] Play App Signing accepted; certificate metadata recorded.
- [ ] Versioning convention applied; first code verified unused.
- [ ] Native-symbol decision made and applied (or deferral justified).
- [ ] `productName` / `companyName` decision applied; Editor path change noted.

**Reproducibility (Step 5)**
- [ ] Clean clone resolves packages and builds; MCP copy compared; no hidden local dependency.

**Release artifacts (Steps 6, 12)** — for every uploaded AAB:
- [ ] Release config (no Development Build or Script Debugging), IL2CPP, ARM64, `targetSdk ≥ 36`, min SDK, package and version confirmed from the AAB.
- [ ] Upload signature confirmed; bundle validation passes.
- [ ] **16 KB static validation passes:**
  - `bundletool` reports `PAGE_ALIGNMENT_16K`;
  - every `.so` passes the `LOAD` alignment and RELRO checks;
  - the generated APKs pass `zipalign -c -P 16`;
  - App Bundle Explorer shows no issue;
  - runtime 16 KB testing is recorded as done or explicitly not done.
- [ ] **Effective Predictive Back value is `false` for the launcher activity**: no activity-level `true` overrides the application value.
- [ ] Permissions/components reviewed; `INTERNET` documented; SHA-256 recorded in the ledger.

**Internal and update path (Steps 7, 8, 12)**
- [ ] App created; Release A accepted; S23 prior-install certificate checked, with any uninstall approved by the human; S23 installed from Play; smoke passes, including Android Back.
- [ ] No unexpected permission prompt; cold-start log reviewed; `AssetPackManager` triaged.
- [ ] Release A → B updated through Play: higher code, same upload key, progression kept, no signature mismatch.

**Data and privacy (Steps 9–11)**
- [ ] `INTERNET` cause explained from the Internet Access = Auto starting point; no answer based on assumption alone.
- [ ] Data Safety evidence reconciled against Release B (A → B delta) before submission.
- [ ] Privacy page live, public, app-specific.
- [ ] In-app Privacy action implemented and tested; S23 browser → Back → Main Menu check passes.

**Closed test (Steps 13–17)**
- [ ] Console setup complete: privacy, Data Safety, ads, app access, target audience, content rating, listing, Closed countries covering the cohort.
- [ ] Cohort of 15–20 kept separate from Internal; no tester PII committed.
- [ ] Verified artifact promoted/reused; closed release published and available; S23 account verified as out of Internal and opted into Closed; Closed smoke passes, including Android Back.
- [ ] **Clock started:** at least 12 eligible accounts actually opted into Closed, with evidence and not invitations; publication time recorded. The developer understands that Phase 18 starts without waiting 14 days, and that 14 days enables, but doesn't guarantee, production access (Phase 22).

**Play reports (Step 18)**
- [ ] Available reports reviewed; every finding classified; no confirmed testing-blocking crash open; `AssetPackManager` disposition recorded.

**Documentation and learning (Steps 19–21)**
- [ ] `RELEASE_LEDGER.md` and `PHASE17_SUMMARY.md` complete; no secret or PII committed.
- [ ] The developer can explain the Step 21 topics.
- [ ] The human reviewed the final Phase 17 diff.

Do not start Phase 18 automatically.

---

# Git / Artifact Discipline

**May be committed:**
- release build settings (without keystore paths);
- working identity;
- `.gitignore` defenses;
- privacy-policy source;
- the in-app privacy UI, code and tests;
- the ledger and summary;
- temporary listing source assets, if deliberately tracked.

**Never committed:**
- AAB/APK binaries and symbol files;
- keystores, keys and passwords;
- keystore paths;
- tester emails;
- private Play screenshots;
- local machine paths;
- credentials.

Build artifacts live in ignored locations.

**Per slice:**

```text
Claude implements → focused validation → exact diff/evidence → human review → Codex challenge → approved fixes → commit
```

- Do not push unless asked.
- A successful build never implies an upload; the human approves immediately before every Play release action.
- No co-author or attribution trailers.

**Possible commit boundaries** (use only when they match real work):
- Prepare Android test-release identity
- Add in-app privacy policy access
- Record release ledger evidence
- Add Play test listing assets
- Complete Phase 17 Play test bootstrap

---

# Unity / Release Teaching Protocol

**Before** every meaningful Unity Editor or Play Console state change, explain:
1. what changes;
2. why;
3. the exact manual steps;
4. what is irreversible;
5. how to verify;
6. what must stay private.

**After** it, report:
- the state changed;
- evidence;
- warnings/errors;
- files modified, including keystore-serialization restoration;
- a secrets/PII check;
- whether it can be rolled back.

Examples: package/product identity, version code, custom keystore, Build App Bundle, symbols, release build, Play app creation, Play App Signing, a track rollout, Data Safety submission.

Read-only inspection needs no ceremony.

---

# Device Automation Rule

```text
verify foreground package → inspect screenshot → perform ONE action → inspect again
```

No unattended coordinate sequences. This matters especially for Play install/update, the privacy browser round trip, opt-in links and background/resume.

---

# First Developer Assignment — Steps 1–4 Only

Inspection and decisions only.

**Not yet:**
- create the Play app;
- generate a keystore;
- change PlayerSettings;
- build or upload anything;
- publish a privacy page;
- modify the Main Menu;
- submit declarations;
- create the ledger or summary.

**Inspect and report:**
1. whether Phase 16 is formally closed; HEAD and clean status; the full-test baseline;
2. current package, `productName`, `companyName`, and version name/code;
3. target/min SDK behavior; IL2CPP/ABI/build flags; icon and signing state;
4. the repo's manifest/package/plugin risk surface;
5. `INTERNET` evidence, starting from Internet Access = Auto;
6. Unity services and Cloud state;
7. the embedded MCP vs manifest situation;
8. existing build side effects from prior validation;
9. the Predictive Back setting and how it should appear in a built manifest;
10. locally available tools for the AAB and 16 KB checks: bundletool, Build-Tools 35+ `zipalign`, and the NDK's `llvm-objdump`/`llvm-readelf`, plus the currently installed S23 app's signing certificate (read-only);
11. Unity's options for native debug symbols and a recommendation;
12. the proposed versioning convention;
13. the proposed working Android/Play name, and the `companyName` recommendation, including the Editor path consequences;
14. recommendations for package confirmation, default language, and Game/Free, with the irreversible trade-offs;
15. the Play Games on PC / default-distribution concern;
16. the upload-key creation and backup procedure (without creating anything), plus the keystore-serialization restore procedure;
17. the clean-clone reproduction procedure;
18. exact Release A build settings and AAB inspection commands;
19. the Internal A → Internal B → Closed sequencing;
20. a privacy-hosting recommendation and the smallest in-app link implementation;
21. the app-content/Data Safety questions that need evidence;
22. the minimum truthful listing assets needed before Closed;
23. the tester cohort strategy;
24. any conflict between the repo state and the entry criteria;
25. any policy fact to re-verify in Play Console, including the 16 KB enforcement wording.

Then **STOP**. No repository, Unity or Play Console changes until the human and Codex review the report.
