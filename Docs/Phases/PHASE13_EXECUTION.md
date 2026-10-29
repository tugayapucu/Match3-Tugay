# Phase 13 Execution Plan — Release Readiness & Product Gap Audit

Phase 13 determines what is actually missing between the current validated Android prototype and a small Match-3 game that we are comfortable distributing through Google Play.

This phase is intentionally:

```text
audit
→ evidence collection
→ classification
→ sequencing
```

It is **not**:

```text
find issue
→ immediately fix issue
```

No gameplay fix, presentation fix, architecture refactor, release configuration change, signing setup, store submission, or policy declaration should be implemented merely because the audit discovers that it will be needed later.

The phase deliverable is:

```text
Docs/Phases/PHASE13_RELEASE_READINESS_AUDIT.md
```

The audit must classify relevant findings as:

```text
MUST FIX BEFORE STORE
SHOULD FIX BEFORE STORE
CAN DEFER AFTER RELEASE
NOT A REAL ISSUE / CLOSE
```

Every classification must be supported by concrete repository evidence, device/build evidence, or current official Google Play documentation.

---

# Execution Roles

For this audit phase only:

- Claude executes Phase 13 and drafts the audit.
- Codex acts as the independent challenger in Step 15.
- The human developer decides classifications and approves audit corrections.

This intentionally differs from the repository's default implementer/reviewer roles. Do not modify `PLAN.md` to record this temporary assignment.

---

# Phase Boundary

The important boundary for this phase is:

```text
repository / Unity / Android / Play evidence
                ↓
             inspect
                ↓
             classify
                ↓
     release-readiness audit
```

Do not cross into:

```text
audit finding
    ↓
production fix
```

unless a later phase explicitly approves that fix.

Small temporary validation actions are allowed only when they exist to answer an audit question and leave no intended production change behind.

Examples:

- opening/inspecting Unity settings;
- reading Console or test results;
- building a throwaway non-development APK solely to inspect the resulting Android manifest;
- restoring the Editor/build state afterward.

Temporary artifacts must not be committed.

---

# Current Baseline

Phase 12 is complete.

The current validated baseline includes:

- touch-only mobile gameplay;
- portrait safe-area layout;
- responsive camera framing;
- Android 60 FPS target policy;
- successful IL2CPP + ARM64 development APK;
- physical Samsung Galaxy S23 validation;
- deterministic restart behavior;
- broad EditMode coverage;
- broad PlayMode coverage;
- runtime scenario validation;
- known mobile limitations documented rather than hidden.

Phase 13 should reuse this evidence.

Do not repeat Phase 12 validation merely to create new screenshots or larger test totals.

Repeat a check only when:

- the evidence does not answer a Phase 13 release question;
- the relevant project state has changed;
- a policy/release claim requires a different build type;
- the audit discovers a contradiction that must be resolved.

---

# Known-Issue Inputs

At minimum, inspect all 12 current files in `Docs/KnownIssues/`:

```text
BeamCrystalArtThin
ColourOnlyGems
DeadBoard
FrameRatePolicy
LockedAfterException
MissingReadme
MobileLayoutAndPicking
OutcomeDimSafeArea
RefillSeedReuse
SelectionFeedback
TallScreenComposition
WatchList
```

`BeamCrystalArtThin` is recorded as resolved and is a likely `NOT A REAL ISSUE / CLOSE` item, but it must still be inspected and classified rather than silently omitted.

Also search the repository for other explicitly deferred or unresolved findings that are not already represented in those documents.

Do not assume an issue remains valid merely because a file exists.

For each issue, determine:

1. What exactly is the current behavior?
2. Is the behavior reproducible or only theoretical?
3. Can it prevent gameplay completion?
4. Can it crash, lock, corrupt, or mislead the player?
5. Does it violate a release/policy requirement?
6. Is it only visual/polish?
7. Is there existing automated/device evidence?
8. Which later phase should own it if it remains open?

---

# Classification Rules

## MUST FIX BEFORE STORE

Use only when at least one is true:

- player can become stuck with no reasonable recovery;
- crash / unrecoverable lock / serious state corruption is confirmed;
- release artifact cannot be produced or updated safely;
- current Google Play requirement would block submission or production access;
- privacy / permission / policy state is materially incorrect or unknown in a way that blocks truthful declaration;
- the game is not reasonably usable on the intended supported mobile experience.

Do not classify something as MUST merely because it would be embarrassing or visually imperfect.

## SHOULD FIX BEFORE STORE

Use when:

- the game remains functional;
- release is technically possible;
- but the issue noticeably harms expected Match-3 interaction, readability, accessibility, player confidence, or portfolio/release quality.

Swipe and selection feedback are already **pre-approved release-scope requirements** by the roadmap. Phase 13 may refine their evidence, priority, or implementation risks, but should not remove them from the release scope.

## CAN DEFER AFTER RELEASE

Use when:

- behavior is acceptable for the intentionally small first release;
- fixing it would add disproportionate complexity;
- profiling/evidence does not show meaningful impact;
- it is a future scalability concern rather than a current release concern.

## NOT A REAL ISSUE / CLOSE

Use when:

- evidence disproves the concern;
- the behavior is intentional and documented;
- a later change already removed the issue;
- the issue duplicates another tracked item;
- the proposed concern is purely theoretical and not actionable for this release.

---

# Step 1 — Phase Entry Gate and Repository Baseline

## Goal

Confirm that Phase 13 starts from the intended completed Phase 12 state and that the audit will not accidentally include unrelated work.

## Inspect

At minimum:

- `git status`;
- current branch;
- local commits ahead/behind remote;
- latest roadmap commits;
- `PLAN.md`;
- `AGENTS.md`;
- relevant Phase 12 completion/validation docs;
- known-issue documents;
- test assemblies and test folders;
- current Android build records.

## Record

In the audit working notes, capture:

- current HEAD SHA;
- whether the tree is clean;
- whether there are intentionally unpushed docs commits;
- Phase 12 final validation totals/evidence;
- current Unity version;
- current Android application identifier if already set.

Do not push merely to make the audit easier.

## Success criteria

The phase baseline is unambiguous and no unrelated working-tree changes are mixed into the audit.

This execution plan is committed as its own docs-only commit before Step 1, so a clean tree is the expected starting state.

STOP and report if the tree is unexpectedly dirty.

---

# Step 2 — Known-Issue Inventory and Evidence Map

## Goal

Build one evidence-backed inventory before assigning release severity.

## Process

For every known issue:

1. locate the source document;
2. read the exact current description;
3. identify existing tests or runtime evidence;
4. inspect the relevant implementation only as needed to understand current behavior;
5. record whether the issue is:
   - confirmed;
   - partially confirmed;
   - theoretical;
   - obsolete;
   - duplicated;
6. identify the likely owning later phase.

Do not fix anything.

## Suggested working table

```text
Issue
Current behavior
Evidence
Player impact
Release/policy impact
Confidence
Candidate classification
Owning later phase
```

Candidate classification is provisional until the full audit is complete.

## Important separation

Do not let severity be determined by the amount of code required.

A one-line bug can be MUST.

A large architectural idea can be DEFER.

---

# Step 3 — Automated-Test and Runtime-Coverage Audit

## Goal

Determine what current testing actually proves and what release-relevant behavior is still unsupported by evidence.

## Inspect

At minimum:

- EditMode tests;
- PlayMode tests;
- input tests;
- special-tile tests;
- cascade/resolution tests;
- restart tests;
- outcome tests;
- mobile layout/picking tests;
- Phase 12 runtime scenario records.

## Record

Summarize:

- what deterministic Core behavior is strongly covered;
- what Presentation/mobile behavior is covered;
- what only has manual/device evidence;
- what has no meaningful evidence;
- intentionally skipped hardware/timing tests;
- expected negative-test Console output.

Do not chase a bigger test count.

If the only changes since the final validated Phase 12 run are documentation changes, do **not** rerun the full EditMode and PlayMode suites merely to reproduce existing totals. Reuse the Phase 12 evidence.

Run tests only if:

- code or project state changed;
- an audit question requires a specific new test;
- existing evidence is contradictory or insufficient.

The question is:

> Does the current evidence support the release-readiness claims we are making?

## Potential gap categories

Pay particular attention to:

- dead-board detection/recovery;
- exception/lock recovery;
- app lifecycle;
- menu/level flow that does not exist yet;
- swipe, which is approved but not implemented yet;
- release-build-only behavior;
- Play-distributed-build behavior;
- privacy/permissions behavior.

These may be future-phase work rather than Phase 13 fixes.

---

# Step 4 — Android Project-Settings and Build-Configuration Audit

## Goal

Record the current Android release baseline without changing it.

## Inspect

Use the current Unity project and existing Phase 12 build evidence to inspect, where applicable:

- application identifier;
- product/application name — current read-only evidence: `productName: agentic-match3-unity` (shown under the launcher icon);
- company name — current read-only evidence: `companyName: DefaultCompany`;
- version name;
- Android version code;
- minimum API;
- target API;
- scripting backend;
- target architectures;
- orientation;
- Development Build state;
- Script Debugging state;
- Internet Access setting;
- write permission setting;
- graphics APIs;
- managed stripping;
- IL2CPP configuration;
- app icons / adaptive icons;
- permissions implied by project/packages.

Treat the product and company names as current-state evidence, not automatically as defects, until classified.

## Important rule

Do not convert current settings into final release settings during this phase.

The audit should say:

```text
current state
→ release implication
→ later phase owner
```

not:

```text
current state
→ change immediately
```

## Output

Record concrete release gaps such as:

- unset/non-final versioning;
- development-only build status;
- missing signing;
- missing AAB validation;
- target-API uncertainty;
- icon/launcher gaps;
- permission questions.

Classification happens later.

---

# Step 5 — Google Play Account and Current Official-Requirement Audit

## Goal

Establish the external release requirements that actually apply to this developer account and app.

This step must use current official Google / Android documentation and, where account-specific, the actual Play Console state.

Do not rely only on roadmap text or memory.

## Account facts to record

Current account state at plan time:

```text
account type: Personal
created: 2026-10-29
verification: pending
```

Record:

- account type;
- account creation date;
- whether Play Console shows a production-access testing requirement;
- whether identity/contact verification is complete;
- whether the account is otherwise able to create the app.

## Pending verification must not block the audit

Phase 13 may complete while account verification is pending.

If Play Console cannot yet expose the account-specific production-access requirement, record:

```text
PENDING — account verification
```

and use the current official rule as the planning assumption:

- personal account created after 13 November 2023;
- therefore plan as though the qualifying closed-test requirement applies until Play Console proves otherwise.

Do not let an external identity-verification delay block the engineering audit. Update the account-specific line in the audit when Play Console confirms the actual requirement.

Current product intent:

```text
initial release price: FREE
ads: none planned
IAP: none planned
backend/accounts: none planned
```

If any of those decisions change, record the change rather than silently assuming it.

## Verify current Play requirements

At minimum verify:

- current target-API requirement;
- Android App Bundle requirement;
- Play App Signing / upload-key model;
- production-access testing requirements for this account;
- internal-testing prerequisites;
- closed-testing prerequisites;
- minimum store-listing requirements for testing tracks;
- app-content declaration requirements;
- privacy-policy requirement;
- whether the privacy policy must be accessible in-app;
- Data Safety requirements;
- content rating;
- target audience;
- ads declaration;
- app-access declaration;
- required contact information;
- store preview asset requirements.

## Testing-track prerequisites

Distinguish:

- internal testing — currently no app-setup prerequisite;
- closed testing — currently requires app setup to be complete.

Re-verify both against official Play documentation during execution.

## Pricing

Verify and record the current rule:

- paid → free is allowed;
- once this package has been offered free, it cannot later be changed to paid;
- charging later would require a new app/package.

Current decision remains:

```text
FREE
```

## Personal-account public information

Verify and record the current disclosure rules.

Current official guidance indicates that a personal developer profile displays:

- legal name;
- country;
- developer email.

The full legal address is displayed when the developer monetizes through paid apps or in-app purchases, subject to any region-specific additional disclosure rules.

Because this release plans no monetization, record the applicable state accurately rather than assuming either "address is private" or "full address is always public."

## Official-source rule

Prefer:

- `support.google.com/googleplay/android-developer`;
- `developer.android.com`.

Record:

- page title;
- URL;
- access/check date;
- the specific requirement relevant to this project.

Avoid blogs as authority for Play policy.

## Account-dependent testing

Do not merely infer the 12-testers / 14-days rule from the account creation date.

Verify what the actual Play Console/account requires.

If the requirement applies, Phase 13 should mark the Phase 17 early closed-test bootstrap as schedule-critical.

If it does not apply, record that evidence and close the concern.

---

# Step 6 — Privacy, Data Surface, Packages and Permissions Audit

## Goal

Understand what must be verified before truthful Data Safety and privacy declarations can be made.

Phase 13 should **not** make final privacy/data-collection claims based only on the development APK.

## Inspect

At minimum:

- Unity package manifest;
- installed packages that may introduce networking, analytics, ads, crash reporting, authentication, or telemetry;
- `ProjectSettings/UnityConnectSettings.asset` — current read-only evidence indicates Unity Analytics, Crash Reporting and Performance Reporting are all disabled;
- the presence and actual use/impact of `com.unity.modules.unityanalytics` and the UnityWebRequest / networking-related modules;
- Android Player settings related to Internet access;
- any explicit networking APIs in project code;
- existing Android manifest overrides, if any;
- Phase 12 development build behavior.

## Questions

1. Does project code intentionally access the network?
2. Is Unity Analytics or another telemetry SDK present/enabled?
3. Are ads/IAP SDKs present?
4. Is any authentication/account SDK present?
5. Is `INTERNET` declared only because of Development Build behavior or an actual runtime dependency?
6. Does a future privacy policy need to describe any data handling beyond a no-data/offline game?
7. What must be re-verified against the Phase 17 release-configuration AAB?
8. What must be re-verified again in Phase 21 against the final build?

## Rule

Do not infer data collection merely because a Unity module or package exists. The throwaway non-development manifest and later release-build inspection remain the stronger evidence.

If evidence is incomplete, write:

```text
UNKNOWN — VERIFY IN PHASE 17
```

instead of guessing.

---

# Step 7 — Throwaway Non-Development Android Build and Manifest Check

## Goal

Answer one narrow audit question:

> Does a non-development Android build still declare `INTERNET`, and what other permissions are present?

This is an approved temporary validation action.

It is **not** the Phase 17 release AAB and **not** the Phase 20 final production build.

## Before every Unity Editor / MCP action

Follow the project learning workflow.

Explain:

1. what will be done;
2. why;
3. the exact manual Unity Editor steps;
4. how the result will be verified.

Only then perform the Editor/MCP action.

After every action, report exactly what changed or was observed.

## Pre-build snapshot

Before changing platform/build state, record:

- active build target;
- Development Build state;
- Script Debugging state;
- current Android build settings needed for the temporary build;
- any Editor focus/run-in-background settings that must be restored;
- current git status;
- the current state of the files the Phase 12 Android build is known to touch:
  - the URP asset;
  - the default batching-cache entry;
  - the Input System preloaded-assets entry.

## Known texture-subtarget trap

An MCP-triggered Android build that uses the wrong/default desktop texture subtarget (`StandaloneBuildSubtarget.Player`, value 2) is interpreted by Android as PVRTC. Phase 12 Step 9 hit this.

Verify the intended Android texture subtarget (ETC2) before treating the throwaway build as valid evidence.

## Temporary build

Create the smallest practical non-development Android APK suitable for manifest inspection.

Requirements:

- Development Build: off;
- do not establish permanent signing configuration solely for this audit;
- do not change package identity;
- do not commit generated artifacts;
- do not alter gameplay;
- do not change release architecture merely to make the build succeed.

If the existing project cannot make the throwaway build without a real production decision, stop and record:

```text
VERIFY IN PHASE 17
```

instead of forcing configuration.

## Inspect

Inspect the final packaged/merged manifest or equivalent reliable build output for:

- `android.permission.INTERNET`;
- other declared permissions;
- package identifier;
- target/min SDK evidence where available;
- unexpected components/providers/receivers that materially affect privacy/policy review.

Record evidence.

## Restore

After inspection:

- restore the intended editor/build target state;
- restore temporary build toggles;
- leave Development Build in the approved normal state;
- inspect and restore, if changed, the URP asset, the default batching-cache entry and the Input System preloaded-assets entry;
- confirm no unintended serialized settings diff;
- confirm git status;
- keep generated APK/build output ignored/untracked.

## Success criteria

The audit can state either:

```text
non-development build declares INTERNET
```

or:

```text
non-development build does not declare INTERNET
```

with concrete evidence.

If the answer remains uncertain, explicitly defer final verification to Phase 17.

Do not make permanent fixes in Phase 13.

STOP for approval if this step reveals a project-setting mutation that would need to be committed to restore correctness.

---

# Step 8 — App Lifecycle and Player-Stuck-State Audit

## Goal

Identify release-relevant lifecycle and recoverability risks without implementing fixes.

## Inspect

At minimum consider:

- app background → foreground;
- focus loss / regain;
- app pause/resume during:
  - idle board;
  - swap;
  - resolution/cascade;
  - special animation;
  - Win/Loss outcome;
- screen/device interruption where practical;
- terminal input state;
- exception paths;
- current locked/busy flags;
- Android process death / cold restoration.

## Process death

Treat process death as a distinct case. Current expected behavior may simply be:

```text
Android kills the process
→ app later starts fresh
→ current board/session is lost
```

Do not automatically classify that as a bug for the current single-session prototype. Record it as a product/state-persistence decision.

Phase 16 will need to decide which progression state, such as unlocked/current level, must survive process death.

## Evidence rule

Do not invent a lifecycle bug from code reading alone.

Classify as:

- confirmed;
- plausible / needs Phase 19 stability validation;
- not a current issue.

Phase 19 owns deeper sustained stability/performance testing.

## Special attention

`LockedAfterException` should be evaluated for release impact here, but any fix belongs to Phase 14 if classified as a real gameplay robustness blocker.

---

# Step 9 — UI / Interaction / Accessibility Gap Audit

## Goal

Separate functional release blockers from approved interaction/presentation work.

## Review

At minimum:

- current two-tap swapping;
- approved future swipe input;
- selection feedback;
- outcome flow;
- tall-screen composition;
- outcome dimmer;
- colour-only gem identification;
- touch target comfort;
- restart placement;
- current absence of main menu / level progression;
- current absence of final store-facing polish;
- Android Back behavior.

## Android Back

Android Back behavior is not yet defined and is owned by Phase 16.

Audit the need for behavior at:

- active gameplay;
- board resolution / input-locked state;
- Win/Loss outcome;
- main menu.

Do not decide the final behavior in Phase 13.

## Roadmap awareness

Do not classify approved future features as accidental regressions.

Examples:

```text
Swipe
→ approved Phase 15 release-scope work

Main menu / levels
→ approved Phase 16 release-scope work

Final presentation/accessibility polish
→ Phase 18
```

The audit should still explain why each matters and whether it is release-critical.

---

# Step 10 — Release-Gap Classification Workshop

## Goal

Convert the evidence inventory into one coherent release-readiness decision table.

Do not classify each issue permanently while reading it in isolation.

Review all evidence first, then compare issues against the same severity rules.

## Required table columns

Use at least:

| Finding | Type | Classification | Evidence | Player / release impact | Rationale | Owner phase |
| --- | --- | --- | --- | --- | --- | --- |

`Type` is separate from severity. Use these values where appropriate:

- gameplay bug;
- release blocker;
- polish;
- policy/compliance;
- performance;
- product feature.

Examples:

```text
DeadBoard                  → gameplay bug       + MUST
No signed Play AAB yet     → release blocker    + MUST
Tall-screen composition    → polish             + likely SHOULD
Closed-test requirement    → policy/compliance  + MUST
```

This distinction is part of the Phase 13 learning goal.

Optional useful columns:

- confidence;
- dependency;
- policy source/date;
- verification still needed.

## Required ownership mapping

Every open finding must have a later owner.

Expected owner phases include:

```text
Phase 14 — Gameplay Robustness & Adversarial Testing
Phase 15 — Mobile Interaction Polish
Phase 16 — Level System & Game Flow
Phase 17 — Play Test Bootstrap
Phase 18 — Presentation & Accessibility Polish
Phase 19 — Performance & Stability Review
Phase 20 — Final Android Production Build
Phase 21 — Google Play Store & Policy Readiness
Phase 22 — Release Candidate, Production Access & Submission
Phase 23 — Portfolio README & Engineering Story
```

Avoid vague ownership such as:

```text
later
eventually
future polish
```

---

# Step 11 — Proposed Release Order, Dependencies and Schedule Risks

## Goal

Explain which findings constrain the order of later work.

## Required dependency review

At minimum consider:

### Gameplay robustness first

Confirmed player-stuck / unrecoverable gameplay states should be addressed before investing heavily in presentation.

### Interaction before level-flow presentation finalization

Swipe / selection behavior should be known before final UI polish.

### Level flow before final presentation polish

Main menu, level indicator, Next/Retry/Menu outcome actions can change HUD/outcome layouts.

### Play-test bootstrap as early parallel work

If the account requires a closed-test period:

```text
Phase 17 starts test
        ↓
Phase 18 / 19 / 20 / 21 continue while test clock runs
```

Record this as a schedule dependency, not merely a policy note.

### Earliest useful closed test

Explicitly compare when the earliest useful closed test should start. At minimum compare:

- after Phase 14 — robustness complete, but before level flow;
- after Phase 16 — testers receive the more complete game flow.

Recommend the earliest point that gives useful tester feedback without unnecessarily delaying the 14-day clock. This execution plan does not decide the answer.

### Performance after presentation

Profile the near-final presentation rather than UI that will immediately change.

### Final declarations after final build inspection

Data Safety / permissions / privacy statements must be re-checked against final build behavior.

---

# Step 12 — Risks and Explicit Non-Goals

## Goal

Prevent the audit from turning release readiness into product expansion.

## Explicit non-goals

Phase 13 must not recommend adding, merely to appear store-ready:

- ads;
- IAP;
- economy;
- coins/shop;
- daily rewards;
- backend;
- login/accounts;
- cloud save;
- live-ops;
- push notifications;
- analytics without a concrete need;
- multiplayer/networking;
- dozens of levels;
- ECS/DOTS;
- heavy DI;
- a generic gesture framework;
- a generic animation framework;
- object pooling without profiling evidence;
- architectural rewrites unrelated to a confirmed release blocker.

The target remains:

> one small, coherent, releasable Match-3 game.

## Risk categories

Record at minimum:

- gameplay/recoverability;
- schedule/testing-track;
- Android build/signing;
- privacy/policy;
- device/performance evidence;
- presentation/accessibility;
- repository/documentation.

---

# Step 13 — Draft `PHASE13_RELEASE_READINESS_AUDIT.md`

## Goal

Produce the phase deliverable without implementing findings.

## Required structure

Suggested:

```text
# Phase 13 — Release Readiness & Product Gap Audit

## Executive Summary

## Audit Scope and Baseline

## Evidence Reviewed

## Play Account / Current Policy Findings

## Android Build / Permission Findings

## Test and Validation Coverage

## Release-Gap Classification Table

### MUST FIX BEFORE STORE

### SHOULD FIX BEFORE STORE

### CAN DEFER AFTER RELEASE

### NOT A REAL ISSUE / CLOSE

## Proposed Order of Work

## Schedule / Dependency Risks

## Open Verification Items

## Explicit Non-Goals

## Phase 13 Conclusion
```

## Writing rules

The document must clearly separate:

```text
confirmed fact
observed evidence
policy requirement
engineering judgement
open question
```

Do not present a likely concern as a confirmed defect.

For current policy claims, include:

- official URL;
- check date;
- concise interpretation relevant to this project.

Avoid copying large passages from policy pages.

## No fixes

The audit may say:

```text
DeadBoard → MUST FIX → Phase 14
```

but must not implement dead-board handling.

---

# Step 14 — Self-Review Before External Review

## Goal

Ensure the audit is internally consistent before asking Codex to challenge it.

Check:

- all 12 known-issue files appear somewhere;
- every finding has a Type;
- every MUST/SHOULD open item has an owner phase;
- no item is simultaneously classified two ways;
- policy claims have current official evidence;
- account-specific claims are based on actual account state, or explicitly marked `PENDING — account verification`;
- Phase 12 evidence is not overstated;
- one-device validation is not described as broad Android compatibility;
- development APK permissions are not treated as final release permissions;
- no release feature was silently added;
- no fix was implemented;
- git diff contains only the intended Phase 13 audit document.

Run:

```text
git status
git diff -- Docs/Phases/PHASE13_RELEASE_READINESS_AUDIT.md
```

Do not commit yet.

STOP for human review.

---

# Step 15 — Cross-Agent Audit Review

## Goal

Use Codex as challenger after the first audit draft exists.

Codex should review, not edit.

Suggested review questions:

```text
Review PHASE13_RELEASE_READINESS_AUDIT.md against the current repository.

Do not modify files.

For every MUST / SHOULD / DEFER / CLOSE classification:

- agree / disagree;
- cite repository evidence;
- identify overstated or missing release risk;
- identify any issue assigned to the wrong later phase;
- challenge unsupported Google Play assumptions;
- flag duplicated findings;
- flag any audit conclusion that actually requires a production fix to prove.

Then list the five highest-value changes to the audit document itself.

Do not propose scope expansion.
```

Human developer decides which review feedback is accepted.

Update only the audit document for approved corrections.

Do not start Phase 14.

---

# Step 16 — Phase 13 Completion Gate

Phase 13 is complete only when:

- [ ] all 12 known-issue files were reviewed;
- [ ] other unresolved/deferred repository findings were searched;
- [ ] automated/runtime evidence was summarized accurately;
- [ ] Android settings/build state was audited;
- [ ] Play account status and applicable testing requirement were verified, or recorded as `PENDING — account verification` with the closed-test requirement assumed to apply;
- [ ] current official Play requirements were checked;
- [ ] privacy-policy requirements were verified;
- [ ] package/SDK/data-surface questions were recorded;
- [ ] the non-development manifest question was answered or explicitly deferred;
- [ ] lifecycle/recoverability risks were reviewed;
- [ ] every release gap has one classification and one Type;
- [ ] every open finding has a later owner phase;
- [ ] proposed order and schedule dependencies are documented;
- [ ] risks and non-goals are explicit;
- [ ] Codex challenge review was completed;
- [ ] human developer approved the final audit;
- [ ] no production fix was made during the audit;
- [ ] repository is clean except for the approved audit-document change before commit.

Only after approval should the Phase 13 audit be committed.

Suggested commit:

```text
Complete Phase 13 release-readiness audit
```

Then STOP.

Do not start Phase 14 automatically.

---

# Unity Learning Requirement

Phase 13 is mostly read-only, but any Unity Editor / MCP action must still follow the project teaching protocol.

Before each Unity Editor / MCP action, explain:

1. what will be done;
2. why it is necessary for the audit;
3. the exact manual Unity Editor steps;
4. how the result will be verified.

After the action, report exactly:

- what changed;
- what was observed;
- whether anything needs restoration.

Do not batch silent Editor changes behind a generic “audit” label.

---

# Learning Gate

At the end of Phase 13, I should be able to explain the difference between:

## Gameplay bug

A defect in game rules or runtime behavior.

Example:

```text
a valid game can enter a state where the player has no legal move and no recovery
```

## Release blocker

Something that prevents safe/valid distribution even if gameplay itself works.

Example:

```text
no safe upload-signing setup
```

## Polish issue

A quality/readability/feel problem that does not make the game fundamentally non-functional.

Example:

```text
outcome dimmer composition is visually incomplete
```

## Policy/compliance requirement

An external platform rule the app/account must satisfy.

Example:

```text
required Play declarations or account-specific testing gate
```

## Performance concern

A suspected cost that must be measured before optimization.

Example:

```text
Physics2D.SyncTransforms may be worth profiling on device
```

## Deferred product feature

A feature that could improve or expand the game but is not required for the intentionally small first release.

Example:

```text
large level catalogue
economy
cloud systems
```

The audit succeeds only if these categories remain distinct.
