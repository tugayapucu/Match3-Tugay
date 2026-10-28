# Tall-screen composition

Status: Deferred to a focused UI polish pass; not part of Phase 12 implementation.

## Problem and cause

Excessive space above and below the board was observed on a physical Samsung
Galaxy S23 (SM-S911B, 1080 x 2340), with the APK running in portrait.
At that resolution, the width-limited board fitting envelope is 984 px square.
The current no-inset calculation leaves about 996 px of spare vertical height,
giving about 530 px between the HUD and envelope and between the envelope and
Restart, including the minimum control gaps. Device insets can alter the exact gaps.

The HUD occupies only about 120 px including its top inset, and Restart is small,
so much of a tall screen remains unused. Centering is correct; the missing visual
structure and content make the space feel empty.

## Deferred direction, in priority order

1. Add a board backdrop panel aligned with the existing 8.2-unit fitting envelope,
   behind BoardRoot in draw order. Keep framing, BoardRoot geometry, colliders
   and picking unchanged; the backdrop should make the full-cell tap areas clear.
2. Use a taller HUD with larger goal and moves text while preserving readability
   and fitting the board from the actual UI edges.
3. Optionally reduce the side margin from 48 to about 16 Canvas units. Without
   horizontal insets at 1080 px width, the envelope would grow from 984 to 1048 px
   (about 6.5%). This is a future option, not an approved Phase 12 setting change.

## Alternative rejected

A vertical bias in `BoardCameraFraming` was prototyped at 0.5, 1/3 and 0.25.
It moved the spare space rather than reducing it: the 1/3 candidate increased
the lower gap from about 530 to 696 px. The mechanism and its tests were discarded;
production retains its original centering without a bias API.
See the [comparison screenshots, measurements and decision](../Validation/Phase12TallScreenComparison/README.md).

## Constraints for a future fix

- Keep Restart in the bottom thumb zone, away from the board: it restarts without
  confirmation, so moving it close to normal gameplay taps increases accident risk.
- Keep Core rules, board geometry and full-cell touch targets intact.
- Preserve safe-area handling and review any deliberate margin or HUD-size change.
- Recheck 1080 x 1200, 720 x 1280 and asymmetric insets, including input and
  special-effect clipping. Do not assume tall-screen composition works on short screens.
