# Mobile layout and tile picking

Status: Layout implemented and picking reviewed in Phase 12 Steps 3–5; basic S23 device checks passed in Step 10. Human touch-comfort review remains.

- Layout: SafeAreaLayout positions the HUD inside the usable screen area. BoardCameraFitter frames the fixed 8.2×8.2 world envelope between the actual HUD/Restart edges. Portrait camera clipping from the old fixed-size camera is resolved.
- Picking: retain the 64 BoxCollider2D targets and the shared mouse/primary-touch Physics2D raycast. Tile.prefab now uses root scale 1 and a centred 1×1 collider, covering each full cell without dead gaps. Transparent sprite padding still separates the visible gems. Real-scene touch tests verify cell corners, points just inside either side of shared borders, and outside-board rejection. A point exactly on a shared border may select either neighbour; no particular winner is promised. No measured picking problem justifies replacing this with grid mapping.
- Physics sync timing: with autoSyncTransforms off, an immediate touchscreen event after a zero-duration refill reproduced a missed pick before another physics step. Step 5 adds Physics2D.SyncTransforms immediately before each permitted pointer raycast, after the disabled/busy guards. This covers movement, initial render and restart without enabling automatic or per-frame synchronization. Click/touch helpers no longer hide this responsibility with their own pre-pick syncs.
- Decorative effects: the Wisp flyer has no collider or TileView. Scale effects run while board input is locked and restore the tile scale before picking resumes.

At the 1080×1920 reference framing, each cell and its resting collider are 120 pixels wide. Pixel sizes do not establish physical touch comfort across different device densities. Step 5 used automated touchscreen events in Unity, not a physical phone.

Phase 12 Step 10 also exercised the installed APK on a physical Samsung Galaxy S23
(SM-S911B, Android 16, 1080×2340). Deliberate 80 ms ADB touchscreen presses near a
cell corner, with same/diagonal/distant selections before the adjacent partner,
produced the same final screenshot as the center-press move. The earlier fast
ADB tap attempt was inconclusive; it did not establish a picking defect.
Win/Loss panels and Play Again were checked on the device, and the real cutout
reservation was visible. Core-predicted normal cells matched 1,916 observations
over two deterministic phone routes.

Finger comfort at every corner, one-handed Restart comfort, and other devices
remain unverified. Keep the full-cell colliders and per-permitted-press sync;
`Physics2D.SyncTransforms` cost was not profiled. The cosmetic
[tall-screen composition](TallScreenComposition.md) and
[outcome dimmer strips](OutcomeDimSafeArea.md) remain deferred to UI polish.
