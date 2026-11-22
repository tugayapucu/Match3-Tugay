# Phase 18 — Presentation & Accessibility Polish

**Status:** APPROVED for execution 2026-11-22 (human; ChatGPT and Codex reviews). Each step still stops for review; no build, upload, install or push without separate approval  
**Project:** Galaxplode · Unity 6000.5.10f1 · Android portrait · six-level Match-3  
**Date:** 2026-11-22  
**Primary developer:** Claude · **Planning/review:** ChatGPT · **Independent review:** Codex (as requested)  
**Human approval:** visual direction, scene edits, physical-device actions, build/signing, push, Play Console operations and release

> **Planning basis:** The Phase 18 roadmap in `PLAN.md` §24, the Phase 17 handoff, the earlier Phase 18 draft, and Claude's 2026-11-22 repository inspection. Repo-specific statements below reflect **Claude's inspection report** and must be reconfirmed during Step 18.0. This document is a proposed execution plan, not authorization to implement or release anything.

## 1. Goal, scope and completion gates

Make the existing small game feel deliberate, visually coherent, accessible and comfortable on supported portrait phones, without enlarging the product or changing game rules. Address evidence-backed defects from the Phase 13 audit and known-issue files; preserve the **Prismatic Energy** art family.

**Scope:** Outcome overlay presentation; final HUD and level indicator **if approved**; short/tall-screen composition; non-color cues for normal gems; decision and, if approved, minimal original audio; regression and controlled device validation. Minor selection-cue styling may be considered only with demonstrated benefit.

**Out of scope:** New levels, match or special-gem rule changes, progression changes, economy/shop/coins/ads/IAP, daily rewards, accounts/backend/live-ops, localization, major VFX/animation architecture, landscape/tablet/XR support, telemetry/profiling (Phase 19), final Android production build (Phase 20), routine store listing/policy work (Phase 21), production submission (Phase 22), and README/portfolio story (Phase 23). `MissingReadme.md` remains open for Phase 23. `MobileLayoutAndPicking.md` is triaged in 18.0, not a standing implementation step.

**Baseline and invariants**

- Current Play-delivered release on both Internal and Closed is **0.1.0 (versionCode 4)**. Codes 1–4 are consumed. Keep code 4 available to testers while Phase 18 is developed.
- The existing menu, gameplay HUD, outcome and confirmation interfaces live in **one scene**, `Assets/Scenes/SampleScene.unity` (per Claude's inspection).
- Phase 16 already provides Retry, Next Level / Play Again, Menu, confirmation and relevant flow tests. Phase 18 may **restyle**, but must not rebuild their logic or alter control semantics.
- Core/Presentation boundary, saved-level progression, level catalog, special-gem effects, board input gating, portrait lock, and safe-area-protected interactive UI remain unchanged.
- The S23 runs a **Play-installed, Google app-signing-key-signed** build. A differently signed local APK cannot update it in place; do **not** uninstall, replace, clear app data or switch testing track for development. The LG G7 may similarly be Play-installed through Closed.
- Each numbered step ends in a review checkpoint. Before a meaningful Unity Editor change: explain **what / how / why / how it will be verified**. Make small single-purpose commits, no AI attribution trailers, no push without approval. No keystore passwords or tester identifiers in repo.

**Phase exit requires:** approved audit/scope, reviewed visual decisions, verified fixes and deliberately accepted deferrals, full regression results, the simulated device matrix, human-reviewed physical device batch if a build is authorized, accurate known-issue dispositions, and `Docs/Validation/PHASE18_SUMMARY.md`. Phase 18 completion **does not** itself authorize Internal/Closed uploads.

## 2. Device validation strategy (applies to every step)

**During iteration:** use Unity Editor/Game view and scene-based tests, **not a local APK installed over the S23 Play version**. Keep existing S23 captures as the physical *before* baseline: Release C code 4 (`Builds/Validation/Phase17ReleaseC/`: menu, starting board, mid-game board, final outcome) and Phase 16 Step 15 (`Builds/Validation/Phase16Step15/`: Won and Lost, from a pre-Play build; its outcome and gameplay UI layout matches code 4 (level values may differ after the Phase 16 tuning), while its menu predates the Privacy link and the GALAXPLODE title). These folders are ignored local evidence. Do not capture new physical baseline screenshots solely for the audit.

**Before the test baseline:** set Android as the active build target and Game view to **1080×1920**, as required by `SceneTestsGameViewDependency.md`. Record actual EditMode/PlayMode counts; the **expected current baseline** from Claude's inspection is **632 EditMode passes; 354 PlayMode passes and 3 approved skips**, to be verified rather than assumed. Additional tests may increase counts.

**Mandatory Editor visual matrix:**

| Simulation | Purpose |
| --- | --- |
| 1080×1920 | Reference portrait and test fixture baseline |
| 1080×2340 | Galaxy S23 tall portrait |
| 1080×2400 | Common 20:9 tall portrait (the LG G7 is 19.5:9, the same shape as the S23; its notch is covered by the inset cases and its real-device check stays in 18.5) |
| 720×1280 | Smaller portrait |
| 1080×1200 | Short portrait, known-issue edge case |
| Asymmetric SafeArea insets | Use the pattern/case already exercised by `SafeAreaLayoutTests` |

At each relevant step compare **board visibility/geometry, HUD and title legibility, touch targets, SafeArea boundaries, clipping/overlap, decorative edge coverage and screen balance**. Capture actual screenshots and list resolution/insets. UI must not silently require a device-specific camera offset.

**Physical validation is a separate batch at the end (18.5), on two independent routes:**

- **S23 (Internal):** after explicit build and Internal-upload approval, a **guarded in-place Play update to code 5** (or higher): app foreground check, one deliberate action at a time, screenshot after each; persisted progression verified through the update.
- **LG G7 (Closed):** its account is deliberately not on Internal (Phase 17 Step 17), so it cannot receive the Internal code 5. It may establish a **code 4 Closed baseline** now through Phase 17 Step 17, and validates code 5 only **after** a separately approved Closed promotion.

Never replace a Play-installed build with a differently signed local artifact.

**Default release strategy:** one Internal code 5 at the end of Phase 18. An earlier update requires separate human approval for a critical issue (for example a tester-reported blocking defect).

**Accepted risk:** visual approvals in 18.1–18.4 are Editor-only; the final S23 batch may reveal device-specific issues. A fix after code 5 has been uploaded uses **code 6 or higher**. Version codes are never reused; one build is not promised regardless of findings.

## 3. Work priority and review rules

| Item | Default disposition | Gate |
| --- | --- | --- |
| Outcome dimmer undimmed strip | **MUST** fix if reproduced | Existing full-screen confirmation pattern; tests and visual review |
| HUD level indicator | **RECOMMEND** implementing, subject to design approval | Show `Level X / N` or equivalent, with N taken from the level catalog count; no new flow logic |
| HUD + composition | **SHOULD** improve based on evidence | One combined design gate, then independent small implementation commits |
| Normal-gem secondary cues | **MUST evaluate**, implement approved minimal solution if feasible | Human approves art approach before any asset/scene edits |
| Residual picking/touch comfort | **DEFER unless reproduced** | Reopen only with evidence or tester report |
| Audio feedback | **DECIDE**; optional minimal implementation | Human approves events, sounds, mute behavior before changes |
| Haptics | **DEFER by default** | Requires a new, explicit decision and Android-specific validation |
| README / portfolio | **DEFER to Phase 23** | No duplicate Phase 18 work |

**Provisional product decisions (human, 2026-11-22; each still passes its step's design gate):**

- **Level indicator:** `Level X / N` (N from the catalog) is the intended direction, confirmed or adjusted at the 18.2 mockup gate.
- **Gem cues:** explore **option A, the reversible overlay**, first; B or C only if A fails the readability gate.
- **Audio:** minimal **AUDIO ONLY with a persistent mute**, subject to the 18.4 sound previews and the privacy-policy update.
- **Haptics:** deferred.

No implicit scope expansion. Each step report must state (1) changes and rationale, (2) tests and evidence, (3) commits/tree state, (4) risks and rollback, (5) human decisions required.

## 4. Numbered execution

### 18.0 — Repository audit, scope decisions and baseline

**Goal:** Convert the roadmap/known issues into a verified, bounded implementation scope.

**Inspect:** `PLAN.md` §24 and §29, Phase 13 audit, Phase 16 implementation, `SampleScene.unity`, current UI hierarchy/scripts, `Docs/KnownIssues/OutcomeDimSafeArea.md`, `TallScreenComposition.md`, `ColourOnlyGems.md`, `MobileLayoutAndPicking.md`, `MissingReadme.md`, `SceneTestsGameViewDependency.md`, relevant EditMode/PlayMode/scene-touch/SafeArea tests, asset provenance and current branch state.

**Deliverables:**

1. Audit matrix: each candidate labeled `MUST / SHOULD / DEFER / NOT REPRODUCED`, with repo evidence, minimal approach, test coverage, rollback and owner.
2. Verify **one-scene setup** and existing Outcome control behavior; confirm exactly which screens lack a level number. Present level indicator as an **explicit human choice**; recommend a compact `Level X / N` treatment (current level and catalog count from the existing flow), not a new progression system.
3. Review `MobileLayoutAndPicking.md`: touch/picking were previously approved on S23 and full-cell collider tests exist; set `NOT REPRODUCED / DEFER` absent fresh evidence. Keep profiling for Phase 19.
4. Record whether normal gem source consists of AI-assisted imported PNGs with no reproducible original-generation pipeline; identify options for cue overlays versus source-PNG edits.
5. Confirm audio state: enabled setting, one listener, no AudioSource/clips/feedback hooks/haptic calls (per Claude), and flag any discrepancy.
6. Set Android/1080×1920, run baseline suites and record actual pass/skip counts. Establish the specified Editor screenshot matrix and link the **existing** S23 baseline captures listed in section 2.
7. Confirm LG G7 Android version, physical resolution/insets, availability and Closed-track account/device constraints *before* any device action.

**No code, art or scene edits. No push.**  
**Stop:** Human/ChatGPT review of audit and explicit scope choices before 18.1.

### 18.1 — Full-screen outcome dimmer

**Goal:** Remove the undimmed top/outside-safe-area strip for **Won, Lost and All Levels Complete** without changing the outcome flow.

**Approach:** Reuse the existing Phase 16 `ConfirmationDimmer` geometry: decorative dimming fills the entire root Canvas/screen; the outcome card and buttons remain inside SafeArea. Inspect actual anchors, hierarchy and raycast configuration. No new generic overlay architecture.

**Acceptance criteria:**

- For all three outcomes, the dim rect matches the full canvas/screen bounds in the relevant coordinate space, even with asymmetric safe-area insets.
- Outcome cards and their interactive controls remain within SafeArea; no top/bottom undimmed band, clipped buttons or accidental click-through.
- Existing Retry / Next Level / Play Again / Menu behavior, Back behavior and board-input gating are unchanged.

**Validation:** Follow the `SceneTouchInputTests` confirmation geometry pattern with targeted assertions for each outcome; confirm existing scene-touch and flow tests still pass. Compare outcome screenshots across reference, tall, short and asymmetric simulations. S23 comparison occurs in the later physical batch, not via local APK installation now.

**Changes:** Small scene/presentation commit(s) only. **Rollback:** revert those focused changes and re-open known issue.  
**Stop:** Review test report and before/after screenshots before 18.2.

### 18.2 — HUD, level indicator and portrait composition (one design gate)

**Goal:** Complete readable HUD/outcome styling and better tall-screen balance in one coordinated pass, avoiding two successive HUD redesigns.

**Existing state:** HUD shows **Moves, Score and Goal**, not the level number. Menu shows Play/Continue/Play Again. Outcome controls and titles (`Level Complete!`, `Out of Moves`, `All Levels Complete`) already exist; their flow is not being recreated.

**First: present design choices without edits.** Mockups (screenshots annotated or disposable visual previews) must cover level label, goal text/icon, HUD height, board-area backdrop/framing, safe margins, visual hierarchy, outcome styling and optional selection-cue restyle. Compare the original centered composition with lightweight framing/art changes; do not resurrect the rejected vertical-bias camera hack. Keep Restart accessible in the thumb zone *with its existing confirmation*, correcting the historical note in `TallScreenComposition.md`.

**Recommended choice:** Add a compact visible `Level X / N` indicator, N being the level catalog count (not a hardcoded 6), while keeping moves, score, goal and the catalog unchanged. Human may accept/defer it after previews. **Optional proposal only, not approved:** a menu label such as "Continue — Level X".

**Acceptance criteria:**

- No HUD/goal/level/outcome text clipping or overlap across the full Editor matrix; all controls within SafeArea and legible on short screens.
- Board remains fully visible, centered/appropriately framed and pickable; normal/special gems and selection cue stay readable; no camera geometry modifications without a documented reproducible defect.
- Level number, if approved, reflects the actual current level and the catalog count without off-by-one errors (tests derive the expected total from the catalog); progression, Retry, Play Again, Next Level and Menu semantics do not change.
- Decorative areas can fill the whole display; interactive areas remain safe-area aware. Visual balance is acceptable to the human, not merely technically non-clipping.

**Tests:** Existing scene-flow/touch/SafeArea tests; focused UI binding/level-number assertions if implemented; simulation screenshot matrix. Group related visual decisions in **one approval**, but implement in **separate small commits** (e.g., level label, HUD hierarchy, board framing, outcome styling) with checks after each.

**Out of scope:** New level logic, per-device offsets, gameplay animation/VFX redesign and performance profiling. **Rollback:** each focused styling commit independently reversible.  
**Stops:** Human approves mockup direction **before** Unity scene edits; review final screenshot set before 18.3.

### 18.3 — Non-color visual cues for normal gems

**Goal:** Make normal gem colors more distinguishable without relying on color alone while preserving the Prismatic Energy identity and keeping special effects legible.

**Mandatory design gate:** Show 2–3 subtle alternatives with *real art at in-game scale*, including the **HUD goal icon**, because it uses the normal gem sprites. Candidate methods:

- **A — reversible overlay sprite** generated by a committed reproducible script (like `GenerateSelectionHighlight.py`), attached consistently to board gem and HUD goal icon; original PNGs untouched, but both presentation paths need wiring and tests.
- **B — minimal direct edits to imported normal-gem PNGs**: both board and HUD inherit changes, but provenance/history of manual asset edits expands.
- **C — another demonstrably smaller option** only if supported by actual asset/layout constraints (e.g., restrained facet/material variation), not speculative rearchitecture.

Avoid confusion with the existing selection brackets and **Nova Gem corner brackets**. Preserve the gem's silhouette and clear color; cues must be visible inside a roughly **120-pixel** S23 board cell and at the HUD goal-icon size, about **44 px** (current `GoalIcon` height of 44 Canvas units; 18.2 may change it, which is why 18.2 precedes 18.3). The goal icon is the harder constraint. Define stable mappings from gem identity to cues without modifying TileType or match logic.

**Acceptance criteria:**

- Readability is judged at the **actual rendered size** of the cue on every screen in the Editor matrix (measured in pixels on the captured screenshots, after Canvas scaling and camera framing), not inferred from Canvas units or sprite size.
- Real screenshot previews at game size remain distinguishable in **grayscale, protan, deutan and tritan** simulations (a practical design check, not a medical certification).
- Board and HUD goal display the same mapping; cues remain readable during selection, animation and cascades without visually imitating special gems.
- No matching, scoring, spawn, palette or progression behavior changes; asset provenance and generator/hand-edit steps are documented.
- Identify whether Phase 21 store screenshots need refresh; if shipped visuals would materially contradict the current listing, **do not publish those visuals without updating the listing**.

**Tests:** Focused sprite/cue mapping and scene binding tests where meaningful, unchanged Core tests, Editor screenshot matrix including special-gem states.  
**Rollback:** isolated overlay removal or PNG restoration, not Core changes.  
**Stops:** Human chooses an art option **before implementation** and approves actual board/HUD screenshots afterward.

### 18.4 — Audio decision; minimal approved feedback only

**Goal:** Determine whether a small sound layer materially improves the game's feel. There are currently no audio clips, AudioSources, or haptic calls according to the repository review; do not create a reusable audio framework in anticipation of future games.

**Audit:** Identify existing game/presentation events for selection/swap, match clear, special activation, win/loss and button navigation. Flag events that can repeat and require one-shot guarding. Check current Android audio behavior and lifecycle.

**Present human choices:** `NONE / AUDIO ONLY / HAPTICS ONLY / SMALL BOTH`, with **HAPTICS DEFERRED as the default**. Recommend a small `AUDIO ONLY` option *only if* original previews demonstrate value and can be implemented without breaking the flow.

**If sound is approved:**

- Prefer short, original sound clips from a **reproducible generator script** under version control; document provenance. No unverified third-party sound licensing, no procedural audio engine or multi-layer mixer architecture.
- Use only justified cues, fired **once per actual event** and never required for gameplay logic or board progression.
- Include an accessible **Mute** toggle (location to be visually approved), persist one preference via a focused `PlayerPrefs` key; verify state survives relaunch. Do not create a general settings system.
- **Privacy policy before shipping a persistent mute preference:** the live policy says the game stores "a progression number in the app's preferences". Review and update that wording in both the game-repo template (`Docs/Release/PrivacyPolicy/index.html`) and the live website, keeping them consistent, with human approval of the text and of the website commit. This is a disclosure-accuracy update, not data collection; recheck Data safety at the same time.
- Do not assume device silent mode reliably mutes the Unity media stream; human listening/mute tests are required on the later Internal build.

**Haptics:** Default `DEFER`. Unity `Handheld.Vibrate` is a fixed vibration and normally adds `android.permission.VIBRATE` to Android manifest. Android `View.performHapticFeedback` can avoid that permission but adds Android-specific integration and must be validated on actual devices; do not implement either without a new explicit approval and review of manifest/privacy impact. A new permission alone is **not proof of data collection**, but manifest/runtime changes require checking Data safety truthfulness.

**Acceptance if implemented:** No duplicate cues or noticeable audio spam; mute works throughout menus and gameplay and persists; no effect on Core state, transitions, pause/Back, or privacy declarations without review. 

**Tests:** Focused event/mute/persistence tests; Editor preview as feasible; guarded physical sound/haptics check in 18.5.  
**Rollback:** remove focused Presentation hooks, audio assets, stored setting and manifest changes, if any.  
**Stops:** Human approves exact design before modifications; human approves behavior after physical listening or chooses defer.

### 18.5 — Full regression, physical-device batch and phase summary

**Goal:** Establish a verified Phase 18 candidate while keeping Phase 17 testers and code 4 undisturbed until explicit release approval.

**Before any new build:**

1. Run complete EditMode and PlayMode suite with Android/1080×1920 Game view; report exact results and compare with the verified 18.0 baseline.
2. Run focused scene-geometry, SafeArea, level indicator, gem cue, audio and interaction tests applicable to the chosen scope.
3. Capture and independently review the full Editor simulation matrix, including Won/Lost/All Levels Complete, HUD/goal icon, selection, special effects, menus, confirmation and privacy-link affordance.
4. Check Core/Presentation boundaries and unchanged gameplay/progression; inspect commits, tree, known issues and any screenshots that may now misrepresent the release.

**Release candidate / physical batch — *separate human decisions*, not automatic Phase 18 steps:**

1. Approve candidate versionName and the next unused **versionCode >= 5** (ordinarily `0.1.1 (5)`); the **human confirms in Play Console that the code is unused** before upload.
2. Build and validate with the **complete Release C procedure recorded in `Docs/Release/RELEASE_LEDGER.md`**:
   - the human loads the upload keystore in the Editor; no password, keystore path or key material passes to the agent;
   - guarded build with explicit `BuildPlayerOptions` **`subtarget = Generic`** (never the MCP default that previously selected PVRTC) and the preflight asserts: release AAB, ETC2, version code/name, package, IL2CPP Release ARM64, alias, symbols, Diagnostics off, no overwrite, all 6 adaptive icon sizes;
   - AAB validation: bundletool, signature and upload certificate, manifest and permission diff against code 4, native libraries and 16 KB static alignment (no claim of 16 KB device validation without such a device);
   - signing fields cleared afterwards; the **human closes Unity before** the four known build-noise files are restored; readback after reopening.
3. **Immediately before the Internal rollout**, record the S23's current saved progression as seen in the app (menu label Play / Continue / Play Again and, where shown, the level), with a screenshot, so the in-place update can be checked against it.
4. **Human-approved Internal upload and rollout**, then an **in-place Google Play update** on the S23. Verify installer/signature, save persistence, launch, menu, board input, confirm/Back, outcomes, level flow, privacy link, and any new cues or sounds. Use the guarded foreground/one-action/screenshot procedure.
5. **Internal validation of code 5 takes priority over a pre-launch report.** Do not upload straight to Closed to obtain one; record whether the Internal upload produces a report.
6. If fixes are necessary after code 5 is uploaded, assign a **new** versionCode (6+) and repeat verification; never reuse a consumed code.
7. Only after Internal approval, the human may separately authorize promoting the **same verified AAB** to Closed from the library; expect another review. Existing tester opt-ins normally persist across updates. Reevaluate Data safety, the privacy policy and store listing accuracy whenever runtime, stored data, assets, SDKs or manifest meaningfully change.
8. After the Closed promotion, and with separate approval, validate code 5 on the **LG G7** through its Closed enrollment (layout, notch/SafeArea, touch), compared with its code 4 baseline. Report if the device or route is unavailable.

**Documentation/exit:** Update known issues with evidence, note Phase 19 profiling / Phase 21 asset refresh / Phase 23 README deferrals, then write `Docs/Validation/PHASE18_SUMMARY.md` with decisions, commit hashes, test results, screenshot references, physical observations, permissions, risks and next steps. Close Phase 18 only after explicit human summary review.

**Stop:** Human approval before build, Internal update, Closed update and each push. A code-complete phase is not automatically deployed.

## 18.0 result — audit and baseline (2026-11-22)

No code, scene, asset or setting changes. Evidence (screenshots and JSON layout records) is ignored local evidence under `Builds/Validation/Phase18Step0/`.

**Baseline:** Android active, Game view 1080×1920, `SampleScene` clean, `0.1.0 (4)`, Generic subtarget. EditMode **632 passed**; PlayMode **354 passed, 3 explicit timing skips, 0 failed** (matches the expected baseline). Console 0 errors/warnings afterwards.

**Capture method (repeatable):** temporary fixed Game view presets for 1080×2340, 1080×2400 and 1080×1200 (720×1280 and 1080×1920 are built-in); Play Mode paused and advanced with `EditorApplication.Step()`, because Play Mode frames do not run while the Editor is unfocused (an early unstepped reading was stale and discarded). Asymmetric insets as in the tests: `SafeAreaLayout` disabled, then `ApplyLayout(60, 70, 940×2170)` at 1080×2340. Afterwards presets removed, 1080×1920 reselected, Play Mode stopped, scene clean, Run In Background unchanged (off).

**Measured layout (Level 1, actual rendered pixels):**

| Screen | Canvas scale | Cell | Board | HUD bar | Gap HUD→board / board→buttons | Goal icon | Clipped text / off-screen controls |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1080×1920 | 1.00 | 120 px | 960 px | 96 px | 332 / 332 | 44 px | none |
| 1080×2340 (S23) | 1.00 | 120 px | 960 px | 96 px | **542 / 542** | 44 px | none |
| 1080×2400 (20:9) | 1.00 | 120 px | 960 px | 96 px | **572 / 572** | 44 px | none |
| 720×1280 | 0.67 | 80 px | 640 px | 64 px | 221 / 221 | **29 px** | none |
| 1080×1200 (short) | 1.00 | 102 px | 820 px | 96 px | **42 / 42** (height-limited) | 44 px | none |
| 1080×2340 asymmetric | 1.00 | 103 px | 823 px | 96 px | 525 / 525; HUD, buttons and board inside SafeArea | 44 px | none |

The Main Menu (title, Play, Privacy Policy) fits inside the screen with no clipped text at all five sizes.

**Classification:**

| Item | Class | Evidence and consequence |
| --- | --- | --- |
| Outcome dimmer strip (`OutcomeDimSafeArea.md`) | **MUST** | Reproduced on the S23 (Phase 12/16 captures, store screenshot 03); cause unchanged in the scene. Editor "before" captures of Won/Lost/All Levels Complete are taken in 18.1 |
| Level indicator | **SHOULD (recommended)** | No screen shows the level number (HUD: Moves, Goal, Score; menu: Play/Continue/Play Again) |
| Tall-screen composition (`TallScreenComposition.md`) | **SHOULD** | 542 px (S23) and 572 px (20:9) empty bands above and below the board; the HUD is a 96 px strip. Constraint for 18.2: the short screen is already height-limited (42 px gaps, 102 px cells), so a taller HUD shrinks the board there unless the design accounts for it |
| Normal-gem colour reliance (`ColourOnlyGems.md`) | **MUST evaluate** | Simulation of the S23 board: under protan/deutan, red, green, orange and yellow collapse into olive/yellow tones and blue/purple into blue; in grayscale only yellow and orange stand out. Cue sizes to design for: **80–120 px cells** and a **29–44 px goal icon** |
| Picking / touch comfort (`MobileLayoutAndPicking.md`) | **NOT REPRODUCED / DEFER** | Full-cell colliders and touch tests pass; S23 finger review approved in Phase 15; profiling is Phase 19 |
| README (`MissingReadme.md`) | **DEFER to Phase 23** | `PLAN.md` §29 |
| Audio | **DECIDE in 18.4** | Audio enabled, one AudioListener, no clips, AudioSources or vibration calls |
| `SceneTestsGameViewDependency.md` | Unchanged | Baseline run with the required Game view; no action |
| `FrameRatePolicy.md`, `LockedAfterException.md` | Phase 19 | Not Phase 18 work |
| `RefillSeedReuse.md`, `WatchList.md` | Unchanged | Low priority / monitor only |
| `McpAndroidBuildSubtarget.md` | Release gate | Covered by the 18.5 build procedure |

**Product decisions** remain as recorded in section 3 (level indicator, overlay cues first, audio only with mute, haptics deferred). **Open fact:** the LG G7's Android version and display mode are not yet known; they are needed only for Phase 17 Step 17 and 18.5, not for 18.1.

## 18.1 result — full-screen outcome dimmer (2026-11-22)

**Changed:** `SampleScene.unity` (new root-level `OutcomeDimmer`; `OutcomePanel` Image removed; `Card` gets an override-sorting Canvas and GraphicRaycaster, copied from `ConfirmationCard`), `LevelHudView.cs` (`outcomeDimmer` field and startup check; panel and dimmer active only while the level is over and the HUD is visible), `TestHud.cs`, and tests. Commits `3beca88` (scene + code) and `e69add5` (tests).

**Finding during implementation:** with only the dimmer gated, the hidden card's buttons still received UI raycasts on the menu (`Graphic.Raycast` stops consulting parent CanvasGroups at an override-sorting Canvas), overlapping the lower part of the Privacy button. The approved fix deactivates the panel together with the dimmer. No exception or input lock was introduced.

**Tests:** EditMode **632 passed**; PlayMode **365 passed, 3 explicit timing skips, 0 failed** (baseline 354 + 11 new: both Show/SetVisible call orders, the missing-reference case, Won / Lost / All Levels Complete × normal / asymmetric geometry with real UI raycasts on the card buttons, and EventSystem touches on Privacy and Play after outcome → Menu with a raycast check at the hidden card's button positions).

**Editor evidence** (`Builds/Validation/Phase18Step1/`, ignored): before, a 100 / 70 / 60 / 80 px undimmed strip with asymmetric insets for all three outcomes; after, full-screen dimming, card and buttons inside SafeArea and no text overflow for all three outcomes at all five sizes plus asymmetric insets (18 captures), and an undimmed menu after an outcome. Temporary presets and the temporary progression key were removed; the Editor's real progression key was never written.

**Notes for later steps:** "All Levels Complete" nearly fills the card width (no overflow); consider it in the 18.2 outcome styling. Physical S23 check in 18.5.

## 5. Parallel Phase 17 work (independent of Phase 18)

- Recruit **12 or more** independent eligible testers each continuously opted in for **14 days** before production-access application; prefer 15–20 for buffer and real feedback. Current checkpoint from the handoff: **1 non-owner opt-in**. Count is a time-sensitive operational fact and must be rechecked, not assumed unchanged.
- Complete Step 17: a second-device Closed-track install (LG G7 if compatible), using a distinct account that has never joined Internal. This is also the G7's code 4 baseline for 18.5. The S23 stays on Internal. Installer/certificate checks support but do not alone prove track eligibility.
- Review Play pre-launch report availability, Android vitals and Policy status. Report absent code-4 pre-launch report as `EXTERNAL / DEFER`; do not imply an unrun report passed. The earlier idea of uploading the next build straight to Closed for a report is superseded by the Internal-first rule in 18.5.
- Capture feedback themes anonymously; only bring new work into Phase 18 after scope/priority approval. Preserve confidential tester identities outside the repository.
- Phase 17 stays open until its documented milestones and `PHASE17_SUMMARY.md` are complete. Test submission/approval/publication/opt-in/14-day qualification are separate milestones.

## 6. Step report format and approval checklist

Every step ends with:

1. **Changes and rationale** — exact files/scene, what/why; or `no edits` for audit.
2. **Evidence** — test counts and known skips; screenshots with screen profile/insets; device route when applicable.
3. **Git** — focused commit hash(es), working-tree state, whether pushed (default no).
4. **Risks and rollback** — residual issues and cheapest safe revert.
5. **Human decisions required** — design choices or next-phase/build/Console gates; stop rather than assume approval.

**Review status for this file:** Claude's repository review and corrections applied and human-accepted 2026-11-22; Codex's independent assessment approved it with no blocking issues, and its two execution reminders (S23 progression record before the code 5 rollout; cue readability at actual rendered sizes) are included above. Approval covers executing the steps with their stops, not builds, uploads, installs or pushes.
