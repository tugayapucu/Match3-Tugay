# No explicit frame-rate policy

Status: Deferred to the mobile/performance phase; deliberately unchanged in Phase 11.

No code sets `Application.targetFrameRate`. The active quality level ("Very Low") has VSync off, so the game runs uncapped: about 1,000 fps was measured in the Editor during Phase 11, which wastes power for a board that only needs a smooth 60. Other quality levels turn VSync on, and mobile platforms fall back to their own default rate when no target is set.

Decide the policy in the mobile/performance phase, for example an explicit `targetFrameRate` (such as 60) on mobile, with a matching VSync choice per platform, then check the frame rate on a real device.

Gameplay is not affected: every animation is time-based (`Time.deltaTime`). Phase 11's effect-feedback reviews captured them at a fixed 60 fps, and its runtime review passed at the uncapped Editor rate.
