# Phase 18 — Presentation & Accessibility Polish

**Status:** APPROVED for execution 2026-11-22 (human; ChatGPT and Codex reviews). Each step still stops for review; no build, upload, install or push without separate approval  
**Project:** Galaxplode · Unity 6000.5.10f1 · Android portrait · six-level Match-3  
**Date:** 2026-11-22  
**Primary developer:** Codex · **Planning/architecture/coordination:** ChatGPT · **Independent review:** Claude (human role change, 2026-11-27)
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
   - **font distribution gate:** inspect the actual AAB for Fredoka data (the TTF files or the generated TMP atlases); if font data is packaged, make sure the required copyright notice and OFL accompany the distribution before shipping (`Docs/Art/README.md`, Phase 18 Fredoka section). Do not assume the TTFs are absent;
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

## 18.2 design approval and specification corrections (2026-11-24)

**Approved and frozen (human, after ChatGPT review):** B4-A "Top mission HUD", Claude Design **Final Polish v3** export in ignored `Builds/Validation/Phase18Step2/design_final/`: `Galaxplode B4-A Final Polish.dc.html` (1a Tall 1080×2340, 1b Compact 1080×1200, 1f–1i Regular 1080×1920 / 720×1280 / 808 u stress test / goal states), `RegularHUD.dc.html`, `RegularScreen.dc.html` and `handoff/B4A_Visual_Spec.md` (§9 Regular internals). The archived v1/v2 files are not used; some titles inside the export still say "v2". `gems/*.png` are preview crops and are never imported; Unity keeps the original Prismatic Energy sprites.

**Decisions:** three responsive tiers chosen by SafeArea height (Tall: HUD 700 u, controls 2 × 360×144 at 120 u; Regular: 440 u, 2 × 340×136 at 72 u; Compact: 112 u, 2 × 340×112 at 48 u), each keeping today's cell size on every screen; 36 u side margin for the HUD only, `BoardCameraFitter` keeps 48 u; board panel padding 0.2 cell on every tier; Fredoka Medium / SemiBold for the HUD and gameplay controls only (menu, outcome and confirmation need a separate visual checkpoint); selection tint and special-gem glow deferred to 18.3; Core, gameplay, board fitting and the 18.1 outcome dimmer unchanged.

**Corrections to the export (these override it):**

1. **Compact bottom spacing.** The `BottomControls` anchor offset stays **48 u**: the distance from the SafeArea bottom to the bottom of the button rect, lip included. The actual gap to the physical screen edge is 48 u plus any bottom inset (for example a gesture bar). The buttons' soft drop shadow is decorative and counts toward neither. The export's "53" (table) and "29 u" (notes) are superseded; at 1080×1200 the board-to-controls gap becomes about 42 u, the same as the HUD-to-board gap, and cells stay 102.44 px.
2. **No tabular figures.** Fredoka has no `tnum` feature and proportional digits, and TextMeshPro does not apply OpenType number features. Every changing number sits in a fixed-width box with stable alignment (§9 table for Regular; Tall and Compact boxes are sized by the same rule in their steps). The widest digit is "2" (about 0.59 em Medium), slightly wider than "0"; box widths are checked against "22" / "222222" worst cases.
3. **Pressed buttons.** The pressed sprite moves only the button face down by the lip height; the label stays stationary and is not a child of the moving face. The export's §9 sentence and the Regular HTML, which move the label with the face, are preview discrepancies. The button rect and hit area never move.
4. **Font fallback.** The export's "dynamic fallback asset for localisation" is not used: the Fredoka assets fall back to the existing LiberationSans SDF, which is Static. No further fallback font or localisation work is in scope. (LiberationSans SDF itself still lists TextMeshPro's pre-existing Dynamic `LiberationSans SDF - Fallback`; HUD text must never need it, which the Fredoka font-asset tests check.)

## 18.2 Step 1 result — Fredoka fonts and TextMeshPro assets (2026-11-24)

**Changed:** `Docs/Art/GenerateFredokaStatic.py` + `requirements-fonts.txt` (pinned google/fonts commit `a60a77e`, fontTools 4.68.0 by hash, run outside the repository; byte-identical output) and the Fredoka provenance in `Docs/Art/README.md` (`edc6c80`); `Assets/Fonts/Fredoka/` Medium and SemiBold TTFs with `OFL.txt` (`d874b8c`); `Fredoka-Medium SDF` / `Fredoka-SemiBold SDF` font assets, `FredokaFontAssetTests` and the TextMeshPro reference in the Presentation EditMode test assembly (`fcef9a4`). Scene, HUD, TMP Settings, project and Android settings unchanged.

**Font assets:** each was filled with ASCII 32–126 plus "·" (96 characters, every visible one with atlas pixels) while Dynamic, then switched to Static; Static SDFAA, sampling 90, padding 9, one 1024² non-readable Alpha8 atlas, `TextMeshPro/Mobile/Distance Field` like LiberationSans SDF, fallback LiberationSans SDF only. The runtime source-font field is empty (editor reference only); whether the TTFs are packaged is still checked on the real AAB (18.5 font distribution gate).

**Visual check** (`Builds/Validation/Phase18Step2/step1/`, ignored): temporary unsaved scene, production CanvasScaler (1080×1920 reference, match width) on a camera canvas, default TMP settings; rendered at 720×1280 (scale 0.6667, canvas 1080×1920 u), 1080×1920 and 2× zoom. The QA string at 32 u, every tier's roles and worst cases, and the glyphs whose outlines overlap (`% + 1 A H R X b d f g p q t x y`; HUD text uses 1, A, R, t, g) render without seams, also with underlay and outline. Regular §9 value boxes with worst-case "2" digits (preferred width / box, u): level 55.4/60, "/ 22" 69.5/74, goal count 110.8/120, "/ 22" 92.6/100, "Goal complete!" 218.3/228, moves 60.0/64, score "222222" 180.1/188; "COLLECT · ORANGE"/"YELLOW" 350 u in a 520 u column at the 808 u HUD. Tall and Compact boxes are sized in their steps from the same measurements.

**Found and reverted:** a fallback probe in the temporary scene ("é ü ñ → ✓") made TextMeshPro add glyphs to the pre-existing Dynamic `LiberationSans SDF - Fallback`, and the change was saved; the file was restored to its committed (empty) state and reimported. Production HUD text cannot reach that asset (tested).

**Tests:** EditMode **641 passed** (632 + 9 new); PlayMode **365 passed, 3 explicit timing skips, 0 failed** (unchanged from 18.1). Android target, 1080×1920 Game view; SampleScene reopened and clean afterwards.

## 18.2 Step 2 result — HUD tier selection (2026-11-24)

**Changed:** new `HudTierLayout.cs` (Presentation, pure static math: `HudTier`, per-tier `HudTierMetrics`, `Select(safeSizeInCanvasUnits, boardSideMargin)`, `HudWidth`, `HudSideMargin = 36`) (`a74eb8e`) and `HudTierLayoutTests` (`4f077f8`). Nothing uses it yet; SampleScene, `SafeAreaLayout`, `BoardCameraFitter`, `BoardCameraFraming`, HUD bindings, art and settings are unchanged.

**Rule:** the largest of Tall / Regular whose reserved height (HUD + offsets + buttons + 2 × 32 u fitter gap) still leaves the board region at least as tall as it is wide (safe width − 2 × the fitter's 48 u margin); otherwise Compact. The cell is min(region width, height) / 8.2 and the width does not depend on the HUD, so the cell never changes; Compact reserves exactly today's 360 u (24 + 112 + 48 + 112 + 64 = 24 + 96 + 48 + 128 + 64). Reserved heights: Tall 1052 u, Regular 736 u, so Tall from safe height ≥ width + 956 and Regular from ≥ width + 640 (1080 wide: 2036 / 1720; 940: 1896 / 1580; 880: 1836 / 1520).

**Tests (42 new):** the approved matrix (1080×2340 / 2400 / 2160, 1440×2960, 1080×1920 and 720×1280, both 1920 inset cases, 1080×1440 / 1200, S23 insets, both test-suite inset cases) with expected tier and cell from the real `BoardCameraFraming.ContentRegion`; exact boundaries and ±0.1 u at three widths; the threshold at every whole width 700–1080; a sweep of 26 000 safe sizes (9 widths × heights 300–3200) proving the cell equals today's or both layouts have no room; metrics, HUD-width independence from the 48 u board margin, no-room and invalid input. EditMode **683 passed** (641 + 42); PlayMode **365 passed, 3 explicit skips, 0 failed**.

**Review outcome (human, ChatGPT, Codex):** approved. Carried into layout integration: (1) keeping the cell size does not prove the controls fit horizontally (for example a 700 u wide SafeArea selects Tall, whose 760 u button row is wider than the SafeArea); before tiers are activated, every control must fit inside the SafeArea, with horizontal-fit tests at narrow widths and tier boundaries and, if needed, a stated minimum width plus an explicit fallback or adaptive button-size policy (falling back to Compact alone is not enough); (2) the Compact HUD width is calculated from the actual board panel (8 cells + 0.2 cell padding per side) and centred on the board, respecting asymmetric insets, not hardcoded.

## 18.2 Step 3 result — HudStatsView wrapper (2026-11-24)

**Changed:** new `HudStatsView` (writes moves, score, goal count and goal icon only) on the existing `TopBar`; `LevelHudView` replaces its `movesText` / `scoreText` / `goalText` / `goalIcon` fields with one `stats` reference, checks it at startup and delegates `Show` to it after resolving the goal sprite. Outcome visibility, confirmation, buttons and actions stay in `LevelHudView` / `GameFlowController`. `HudTierLayout` is still unused; no level or progress bindings yet (`b99fc39` code and scene, `64772cc` tests).

**Scene:** `HudStatsView` references the same four objects (same fileIDs) the old fields did; every other reference is unchanged. The scene file also now stores the live values of the two rects `GoalRow`'s HorizontalLayoutGroup drives (GoalIcon 33.47 × 44, GoalText 258.53 × 56), previously saved as 0; the Editor showed these values before the change too.

**Evidence** (`Builds/Validation/Phase18Step2/step3/`, ignored): reference and RectTransform dumps before and after; Play mode at 1080×1920 from Level 2's start, stepped 240 frames — **before and after captures are pixel-identical** (0 differing pixels).

**Tests:** fixture builds the stats view like the scene; missing `stats` and each missing stats reference fail at `Initialize`; null arguments leave the display unchanged; a scene test checks `stats` is the TopBar's and shows the session's values. EditMode **683 passed**; PlayMode **370 passed** (365 − 2 replaced cases + 7 new), **3 explicit skips, 0 failed**.

## 18.2 Steps 5.1–5.2 result — HUD formatting and Compact geometry (2026-11-26)

**Changed (pure Presentation code, no scene):** `HudFormat` (`708d581`, tests `e26d9ac`): invariant-culture counts, `"/ N"` totals and a goal fill clamped to 0–1; negative values are caller bugs and throw. `HudTierLayout` gains `CompactHud` and `FitControls` (`4ac9d8e`, tests `585b4e8`).

**Compact width contract** (SafeArea W × H in Canvas units): cell c = min(W − 96, H − 360) / 8.2 only when both are positive, otherwise 0 (arithmetic only, never `ContentRegion` on unusable sizes); board panel P = 8.4c; widest HUD Wh = max(0, W − 72); minimum content width 722 u. **Aligned** when P ≥ 722 (W ≥ 800.8095… and H ≥ 1064.8095…): width P. **Expanded** when P < 722 and W ≥ 794: 722 u, wider than the panel (1080×1000, and the 200/220 u no-room cases). **Scaled** when W < 794: content laid out at 722 u and scaled by Wh / 722. The HUD height never changes. **Controls:** design size while it fits Wh (Compact / Regular / Tall from W 784 / 792 / 832); otherwise two equal buttons (Wh − gap) / 2 with gap and height kept; the 220 u minimum holds down to W 544 (Compact) / 552 (Regular, Tall); the row fits only while Wh ≥ gap, which is all it claims.

**Tests (52 new):** boundaries derived from the constants, each below / at / above (the exact point asserts the 722 u width, not the mode); the approved matrix; ~433 000 SafeArea sizes; button thresholds, the floor and the narrower-than-gap case. EditMode **735 passed**; PlayMode **370 passed, 3 explicit skips, 0 failed**. Approved by Codex and ChatGPT with one correction: **rollback reverts each test commit before its code** (`585b4e8` before `4ac9d8e`, `e26d9ac` before `708d581`); the 5.1 and 5.2 pairs are independent.

## 18.2 Step 5.3 result — Compact geometry in the scene (2026-11-27)

**Changed:** `SafeAreaLayout` sizes the HUD from the Compact tier on every screen (tier selection still inactive): `TopBar` width from `CompactHud`, height 112 and offset −24, `BottomControls` from `FitControls` at offset +48, and both buttons' size; the 900 u rule is gone. New `menuButton` / `restartButton` references must be children of `BottomControls` or `Awake` throws. Content scale is not applied yet: there is no Compact content until 5.5, so **the finished Scaled layout is not validated**. `BoardCameraFitter`, Core, gameplay, progression, confirmation and outcome code are unchanged (`016ff92` code, scene and fixture; `5c747ee` tests).

**Scene (the six approved operations only):** TopBar 900×96 → 1008×112; BottomControls 640×128 → 712×112; Menu anchored and pivoted bottom-left (0,0) and Restart bottom-right (1,0), both at position (0,0) and 340×112 (gap 32 u between them); both labels auto-size 24–32 u with NoWrap (font size 32, colour, sprite and ColorTint transition unchanged); the two layout references. Verified by a component dump before and after and by the scene diff. **Correction:** the committed scene also resets the two rects `GoalRow`'s HorizontalLayoutGroup drives (GoalIcon and GoalText anchors, positions and widths back to the zero values saved before Step 3). Unity re-saved the scene after the diff review; these values are recalculated by the layout group at runtime, and the after captures show the goal row unchanged.

**Visible change:** the bar spans the board panel (1008 u at 1080 wide) and the buttons grow and move to the row edges. Cell sizes are unchanged in every case; **the board sits 16 u lower** (bar bottom 120 → 136, button top 176 → 160). The shift and the Expanded geometry are accepted in principle; final human visual acceptance is pending. The old Moves / Goal / Score groups stretch into the bar until 5.5.

**Labels:** checked by their drawn glyph quads, not `textBounds`. At the 220 u minimum both stay 32 u on one line, inside their buttons. Below it there is no fit guarantee: at 98 u buttons "Restart" auto-sizes to 26.8 u and its **padded SDF glyph quads** end 0.86 u past the button edge. The quads include transparent SDF padding, so this does not establish visible lettering outside the button. Kept as a documented below-minimum limitation; no further scene edit (decided after review).

**Evidence** (`Builds/Validation/Phase18Step2/step5_3/`, ignored): before/after captures of Level 2's start in Play mode stepped frame by frame. These are **geometry simulations** (SafeAreas applied inside a 1080×1920 Game view: full, 880×1730 inset, 1080×1200, 1080×1000 Expanded, 700 wide), not actual-resolution renders; the actual-resolution matrix stays planned for 5.6. Measured cells 120 / 95.61 / 102.44 / 78.05 / 73.66 px before and after; board centre 16 px lower in each.

**Tests:** fixture updated in the same commit (bar 1008 / 864.59 × 112, row 712 × 112, HUD margin 36 u). New: Expanded, 700 and 500 wide bar and row sizes, edge alignment and gap, recovery to the full layout, missing or misplaced button references; in the scene, cell sizes of the previous layout, bar width during no-room (722 u) and after recovery (Aligned), button wiring and kept style, and label quads at 220 u and 98 u. EditMode **735 passed**; PlayMode **383 passed** (370 + 13), **3 explicit skips, 0 failed**.

**Rollback:** revert `5c747ee` before `016ff92`; close Unity before reverting `016ff92` (it contains the open scene).

## 18.2 Step 5.4 result — Compact HUD sprite import (2026-11-27)

**Changed (independently reviewed by Claude; approved, no blocking findings):** generated the five PNGs into `Assets/Art/Hud/Compact/` using the already-committed `GenerateHudSprites.py` (`571b4c2`). Before requesting import, every PNG's SHA-256 matched `step5_4/run1/`; `run1/` and `run2/` also match. Unity created both folder metas (`Hud.meta`, `Hud/Compact.meta`) and five PNG metas. Exact dimensions, borders, hashes and the design-export SHA-256 are recorded in `Docs/Art/README.md`. This plan's role line now reflects the human's switch to Codex developer / Claude reviewer / ChatGPT planning.

**Import and readback:** Sprite / Single, centered pivot, Full Rect, 200 PPU, `NPOTScale.None`, input alpha with Alpha Is Transparency, sRGB, no mipmaps, non-readable, Bilinear / Clamp, uncompressed, maximum 512; Android RGBA32 override at 512, uncompressed. Crunch and fallback physics shapes are off. Actual imported texture and sprite sizes equal the source sizes for all five; borders and pivots match. No scene, prefab, runtime, Core, ProjectSettings or TMP/font asset changes.

**Tests:** `CompactHudSpriteImportTests` adds five parameterized EditMode cases covering the imported result and every setting above, including the absence of physics shapes. Focused **5/5 passed**. Full EditMode **740 passed** (735 + 5), **0 failed**, 8.93 s. Full PlayMode **383 passed, 3 explicit hardware-timing skips, 0 failed**, 104.67 s (unchanged). Android active and actual Game view **1080×1920** verified before the full suites and afterwards. No compilation errors. The final PlayMode Console contains four expected missing/misplaced-button exceptions declared by `SafeAreaLayoutTests`, the expected no-room camera warning declared by `SceneTouchInputTests.CameraFit_NoRoom_WarnsOnceKeepsCamera_AndRecoversWhenLayoutChanges`, and MCP / test-framework messages; no unexpected project errors. An MCP WebSocket warning appeared during the earlier EditMode runs.

**Evidence:** importer readback, full-suite XML and job results, final Console and Editor state in `Builds/Validation/Phase18Step2/step5_4/` (ignored). Unity ended on clean `SampleScene`, Play Mode stopped, not compiling/updating, still `0.1.0 (4)`. No settings were restored through Git. No Android build or device/Console actions were performed. After Claude's approval, the human separately authorized three local commits (sprites/metas, import tests, documentation) and a push; check the exact staged diff immediately before each commit. Step 5.5 remains separately gated.

**Risks / next gate:** these assets are not bound to the HUD until 5.5. Linear-space appearance, stretched seams and small fill fractions still require that visual gate; the shadow/track/fill are fixed-height Compact sprites. The 5.3 board shift and Expanded HUD still await final human visual acceptance. **Rollback before any consumer exists:** remove only this step's new sprites/metas and test/metafile, and revert its documentation changes; stop Unity before removing imported assets. Stop for separate 5.5 approval.

## 18.2 Step 5.5 result — Compact prefab and bindings (2026-11-27)

**Status:** base Compact visuals, the 16 u board shift, Expanded geometry and Scaled fallback accepted by the human. Claude independently approved all five corrections. The three Step 5.5 commits (`e956a7e`, `45bc32b`, `0f881e2`) were pushed with human approval. Step 5.6 uses `0f881e2` as its baseline.

**Changed:** `CompactHud.prefab` under the existing `TopBar`, with the imported shell/shadow/halo/track/fill and Fredoka Medium / SemiBold. One HorizontalLayoutGroup: padding 10 each side, three 2 u dividers (`#96A5FF` at 16%), column minimums 140 / 276 / 130 / 150 (722 u total); preferred widths 176 / 276 / 140 / 196 (814 u total), only Goal flexible. Numeric pairs use TMP BaselineRight / BaselineLeft at the same vertical center. Fixed number boxes are 60 / 70 (level), 64 / 70 (goal), 64 (moves), 140 (score, auto-size 40–48). Labels are 32 u; LEVEL and SCORE retain character spacing 10, while MOVES uses the separately approved refinement to 5. Values, totals and all geometry retain the approved sizes. All 18 decorative graphics (including nine TMP texts) have raycasts off; sliced Images use PPU multiplier 1 against the existing Canvas reference PPU 100. No new Canvas, raycaster, mask, layout Update or font/material edit.

**Bindings:** `HudStatsView.Show` computes every string and fraction before any display write; goal count and total replace the combined text. `ShowLevel` validates the pair and formats both strings before writing. `LevelHudView.SetLevel` delegates; `GameFlowController.StartSession` calls it immediately after successful `StartLevel` with `index + 1, catalog.Count`. The fill is a sliced Image whose horizontal anchor follows the true fraction; zero disables it, tiny fractions are not enlarged, completion fills the inner 14 u bar. No interpolation or new per-frame work. `SafeAreaLayout` gets the centered content reference and assigns width and uniform scale on every layout, including scale 1 on recovery; TopBar remains 112 u and Compact is forced.

**Scene audit:** removed the three old stats groups and their children, plus TopBar's old stats view, Image and CanvasRenderer; added the prefab and rewired stats/content. The existing serialized documents with semantic changes are only TopBar's GameObject/RectTransform, LevelHudView's stats reference and SafeAreaLayout's content reference. The new prefab instance includes Unity-written layout-driven rect overrides and TMP default-style/color-cache initialization. Code, tests and docs pass whitespace checks; the raw scene diff also has six Unity-written blank-field trailing spaces, left in the Editor's serialized format. Core, BoardCameraFitter, source art/fonts, ProjectSettings, gameplay, progression, outcome and confirmation behavior are unchanged.

**Validation:** focused PlayMode **111/111 passed**. Full EditMode **740/740 passed**, 8.87 s (unchanged baseline); full PlayMode **414 passed, 3 explicit timing skips, 0 failed**, 103.84 s (383 + 31 cases). New coverage: every required reference; invalid formatting and level pairs leave the display unchanged; catalog-derived scene numbering; all nine text roles at 722 u after forced layout with `22`, `/ 22`, `222222`, plus `22222` / `999999`; baseline alignment, score font floor, font/sprite/raycast wiring; zero / 1/30 / partial / complete fills and reset; all width modes, recovery, unchanged TopBar height and actual scene cell size/picking. The ink-fit check uses glyph metrics, not transparent SDF quad padding. The final Console contains only declared negative-test exceptions/warnings and tooling/framework messages. The first new focused attempt hit a test-only GetMesh API compile error and MCP initialization timeout; the API was corrected and a fresh focused run passed before the final suites.

**Visual evidence:** 14 accepted direct actual-resolution captures under `Builds/Validation/Phase18Step2/step5_5/screenshots/`, indexed by its README and `capture-records.json`. Real resolutions: 1080×1920, 1080×2340, 1080×2400, 720×1280, 1080×1200 and 1080×1000 Expanded. Two are explicitly simulated SafeAreas: asymmetric (60,70,940,2170) inside a 1080×2400 render, and 700 u width inside 1080×1920. Separate HUD-only QA frames use synthetic worst-case values and the four fill states without changing the board/session. Minimum-bar widths are 0 / 4.7333 / 71 / 142 u. Linear-space blending, sliced seams/corners and the short-screen shadow were inspected by Codex; the base visual direction and geometry were subsequently accepted by the human. Current MOVES spacing and settled-button replacements for 01-03 are in review-corrections/. One early 720×1280 capture had stale layout after Canvas scale changed; retained as discarded evidence and replaced by `04b` after additional stepped frames. It is not accepted layout evidence.

**Cleanup / gate:** eight temporary Game view presets removed; Android and actual 1080×1920 restored, Play Mode stopped, SampleScene clean, no compilation/update in progress, still `0.1.0 (4)`. The normal Editor progression key was read back unchanged; no temporary save key was written. Full-suite XML, final Console/Editor readback, one-off Editor operation and scene-diff audit are ignored local evidence in `step5_5/`. Recheck the exact staged diff immediately before each of the three authorized local commits. No Android build, device or Play Console actions. The Step 5.3 shift, Expanded HUD and Scaled fallback now have human visual acceptance; the corrections have independent review approval. The broader Step 5.6 validation remains separately gated. Rollback: close Unity, revert the code/scene/fixture integration together and remove this step's prefab, retaining the approved 5.4 assets.

**Review corrections (independently reviewed and approved):** wrapped `CompactHudViewTests` in `UNITY_EDITOR` like `CatalogLevelSessionTests`, removed its unused Reflection import and extended the existing prefab test to lock MOVES at spacing 5 / font size 32 / minimum column 130, with LEVEL and SCORE still at 10. Existing flow tests now assert both level texts against the expected route index and `flowCatalog.Count` after Play, Continue, Next, Retry, confirmed Restart and final Play Again. The outcome-related comment is on `SetOutcomePresentation`; `SetLevel` has its own display-only comment. No runtime logic changed in this correction pass.

**MOVES refinement and recaptures:** the prefab's only serialized change relative to the initial 5.5 implementation is `Content/Moves/Label.m_characterSpacing: 10 → 5`; the scene remains byte-identical to its pre-correction snapshot. Controlled 722 u and Scaled A/B captures use identical HUD-only widest values (`22`, `/ 22`, `222222`). Preferred label width drops from 122.32 to 115.92 u; Codex retained 5 after visual comparison for the additional clearance and legibility. Replacements for screenshots 01–03 are actual 1080×1920 / 1080×1200 / 1080×1000 renders, not simulated SafeAreas. Only A/B captures 06–07 use a simulated 700 u SafeArea inside an actual 1080×1920 render. Capture is refused until both buttons' renderer tints match their normal tint (white); the authored Image color remains RGBA (0.95,0.55,0.20,1), fade duration 0.1 s. Both buttons sample `#E8862F` in every new PNG; seven dimensions and fourteen color samples were checked independently. No button settings, gaps, background or board panel changed.

**Latest correction validation:** affected focused PlayMode **121/121 passed**, 2.62 s; full EditMode **740/740 passed**, 8.39 s; full PlayMode **414 passed, 3 explicit hardware-timing skips, 0 failed**, 103.75 s. No unexpected Console entries. Evidence and exact correction scope are indexed in `Builds/Validation/Phase18Step2/step5_5/review-corrections/README.md`; it contains the new screenshots, capture/color records, latest suite XML and pre-correction prefab/scene snapshots. Four temporary Game view presets removed, normal Editor progress unchanged, SampleScene clean and stopped at Android/1080×1920. Claude independently confirmed the corrections and evidence with no new findings. The human authorized the reviewed three-commit split; no push, Step 5.6 or release actions are included.

## 18.2 Step 5.6 result — Compact HUD validation (2026-11-28)

**Status:** validation complete and independently reviewed by Claude with no findings. The Step 5.5 commits were pushed through `0f881e2`, the baseline for this validation.

**Live move:** `Scene_RealGoalMove_UpdatesCompactHudFromTheResolvedSession` finds a goal-producing swap on a scratch copy of the starting board and performs it through queued scene touchscreen events. The test observes the busy state and waits for resolution. It verifies one move was spent and that score and goal progress both increased on the same session. The displayed moves, score, goal count, goal total and fill fraction match that session. The new test and the existing flow, geometry, recovery and picking tests passed.

**Capture matrix:** 28 new actual-resolution Unity Editor PNGs and three reused Step 5.5 Playing captures. Reference 1080×1920, short 1080×1200 and simulated S23 insets at 1080×2340 cover Playing, Won, Lost, All Levels Complete, Menu and Restart Confirmation. The other six profiles cover Playing and All Levels Complete, with an additional Menu capture for the 880×1730 SafeArea. The new undimmed Playing captures at 1080×2340, 1080×2400 and 720×1280 show MOVES spacing 5. The exact simulated SafeAreas are (60,70,940,2170) in 1080×2340, (120,90,880,1730) in 1080×1920 and (190,0,700,1920) in 1080×1920. Outcomes use injected resolved-move facts and the normal Next route under an isolated progression key. Menu and Confirmation use normal commands within the fixture session. Filenames and the evidence index identify fixture-driven states and simulated SafeAreas.

**Inset checks:** outcome dimmer rect filled the render, the card sorted above it, and the card and buttons stayed within SafeArea. Pixel samples confirmed darkening in all four outside strips and at the HUD shell and shadow for both inset profiles. The Menu captures show neither HUD nor shadow, with HUD alpha zero and the outcome card and dimmer inactive. TopBar remained 112 u throughout. The Scaled case used 722 u content at 0.8698 scale and 73.66 px cells. Existing tests verified recovery to scale 1 and 120 px cells in the reference layout. Physical S23 verification remains in 18.5.

**Tests:** affected PlayMode **112/112 passed**. Full EditMode **740/740 passed** (same baseline). Full PlayMode **415 passed, 3 explicit hardware timing skips, 0 failed** (414 + 1). Console output matched the declared negative-test logs and test runner messages. Evidence includes PNG originals, review sheets, per-profile geometry and dimming records and suite XML in `Builds/Validation/Phase18Step2/step5_6/` (ignored).

**Cleanup:** all nine temporary Game view presets and the isolated fixture key were removed. The normal Editor progression key's presence and value matched the pre-run readback after both suites. SampleScene was clean with Play Mode stopped and Android/1080×1920 restored. The tracked diff is this record and the single scene integration test.

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
