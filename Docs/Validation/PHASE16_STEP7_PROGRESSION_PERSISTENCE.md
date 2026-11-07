# Phase 16 Step 7 — Minimal progression persistence

Date: 2026-11-07. Status: reviewed and approved in isolation; game-flow integration remains Step 8 work.

## Stored value and API

Production PlayerPrefs key: `Match3.Progression.v1`. It stores one integer, using catalog list indices:

| Value with six levels | Meaning |
| --- | --- |
| 0 | Start Level 1 |
| 1–5 | Continue from that zero-based catalog index |
| 6 | All current levels completed; replay starts Level 1 without resetting completion |

`ProgressionStore` is a plain sealed C# class in Presentation, not a MonoBehaviour. Construction takes the positive catalog count and optionally a separate preference key for isolated tests:

```csharp
var store = new ProgressionStore(catalog.Count);
int progression = store.Load();
// Later, game flow supplies accepted completed-level progress:
progression = store.Save(completedLevelIndex + 1);
```

`Load()` uses `PlayerPrefs.GetInt(key, 0)`, clamped to `0 .. levelCount`. Loading never writes. Missing data stays missing until a save. Invalid stored values cannot become invalid indices.

`Save(progression)` checks the caller's requested value is inside `0 .. levelCount`, then stores the maximum of that value and the clamped existing progression. It calls `PlayerPrefs.Save()` explicitly and returns the saved integer. Earlier/same-level saves cannot reduce valid progress, including final completion during replay. If the raw stored value is corrupt (for example 99 for six levels), the next save normalizes it to the policy's valid range; the monotonic guarantee concerns valid progression, not preserving corrupt out-of-range numbers.

A nonpositive count, blank key or out-of-range save request is a programming/configuration error and throws before writing. Storage exceptions are not swallowed. No interface, storage framework, singleton, callbacks or per-frame work were added.

## Scope and ownership

Only the helper and its tests exist in this step. No production code calls the helper yet; normal gameplay still does not save progression. Step 8 must load it at flow startup and save on an accepted Won callback, before Next, with no saves on Loss/Retry/Restart. Those event-level invariants are not claimed as tested here.

No Board, RNG, score, remaining moves, selection or animation state is saved. No scene wiring, prefab, Inspector, package, project settings or Core changes are needed.

## Tests and validation

- 24/24 focused EditMode cases passed.
- Full EditMode: 632/632 passed, 0 failed, 0 skipped (608 previous plus 24 new).
- Coverage: missing default, negative/oversized clamping including integer extremes, ordered progress advancement, lower/equal saves, final sentinel for one- and six-level catalogs, normalization on save, fresh store reads and argument guards before writes.
- Every test uses a new `Match3.Tests.Progression.<GUID>` key and deletes only that key in teardown, followed by Save. Tests never use the production key or `PlayerPrefs.DeleteAll()`.
- Fresh-store reads exercise the real PlayerPrefs backend in the same Editor process. They do not establish a physical Android cold-relaunch result or prove disk durability under interruption; those checks need the later integrated flow/device validation.
- PlayMode was not rerun: this isolated helper has no production consumer and no session/input/UI code changed. The previous Step 6 baseline remains 260 passed plus 3 explicit timing skips; that is not a new run.
- Compilation completed without errors. Final Console and scene state are recorded below.

## LockedAfterException review

This helper does not fix or recover a locked move. Its data shape cannot serialize a partially failed Board. With the approved Step 8 wiring, only an accepted completed-level result will be persisted; relaunch will reconstruct a fresh deterministic session from that index, rather than resume the failed session. An interrupted unfinished level may lose that attempt's moves/score, as intended.

This is the persistence-design review, not evidence that every future storage failure or move-time exception is recoverable. Saving will run inside the outcome callback and storage failures remain visible; Step 8 still needs the accepted-outcome and busy-timing tests. The existing exception risk stays accepted and unresolved. Update the known-issue record with integrated evidence during the phase's later review.

## Unity learning / manual verification

Create the C# file under `Assets/Scripts/Presentation`, without `MonoBehaviour`, then let Unity import and compile it. No component is attached to a GameObject. In Window → General → Test Runner → EditMode, select `ProgressionStoreTests` and Run Selected, or run the full EditMode suite. Tests create and clean their own preferences; do not clear the application's other saved preferences.

The code uses `GetInt` to read, `SetInt` to change the cached value and `Save` to explicitly flush modified preferences. Unity documents that Save can cause a hitch, so the planned call site is the accepted-win checkpoint, not an Update loop. The write cost has not been profiled.

Verified against the installed Unity API and the official [GetInt](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.GetInt.html) and [Save](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.Save.html) documentation.

## Final state

SampleScene is active, clean and out of Play Mode. The test runner re-saved two empty scene-name fields with trailing spaces; only that whitespace was removed and the clean scene reloaded, leaving no scene diff. Editor target remains StandaloneWindows64, Run In Background is false, and only native Keyboard and Mouse devices remain.

Console contains no project errors or warnings: only routine runner messages and one MCP WebSocket reconnect warning from the domain reload. Step 7 introduces five files: the helper and test with Unity-generated metadata, plus this record. No existing file is modified; Core, scene, prefab, settings and packages are unchanged. Step 8 has not started at this checkpoint.
