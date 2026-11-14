# Real-scene tests depend on the local Game view

Status: Open test-environment limitation, recorded during Phase 17 Step 5. No production or test-code fix is included in this step.

Unity keeps separate Game view size selections for different platform groups. Switching to Android can select Free Aspect even though Player Settings specifies portrait. The Editor preview can therefore be landscape; Player Settings does not make Free Aspect a portrait simulation.

During Phase 17 Step 4, `SceneTouchInputTests` ran at 1086×607 landscape: 11 passed and 5 failed. Four failures were exact confirmation-dimmer coordinate comparisons; the fifth assumed that the covered corner tile lay outside the Main Menu Play button. The same 16 tests passed at 1080×1920 portrait without production, scene or test changes. This establishes a local preview dependency; it does not establish an Android-device defect.

Before real-scene PlayMode validation, especially in a clean clone:

1. Switch to the intended build target and wait for import/compilation to finish.
2. Open the Game view size dropdown and choose **1920x1080 Portrait** in the Android group, or another fixed preset verified to render at **1080×1920**.
3. Verify the actual render size is 1080 pixels wide and 1920 pixels high. Merely assigning a size index without applying the dropdown callback can leave the old render size in effect.
4. Run the tests and record the active build target and actual Game view dimensions with the result.

Do not change production framing, suppress assertions or claim a passing portrait run covers arbitrary Editor aspect ratios. Short-screen and asymmetric-safe-area checks remain separate tests, and physical-device validation remains separate evidence.

Future test-fixture fix: set and restore the required resolution, or assert it during setup, so real-scene tests do not silently depend on the local Game view selection.

Evidence and clean-clone results: [release ledger](../Release/RELEASE_LEDGER.md). Execution scope: [Phase 17 plan](../Phases/PHASE17_EXECUTION.md).
