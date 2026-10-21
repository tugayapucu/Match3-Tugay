# Refill reuses the generation seed

Status: Deferred; low priority, not a bug.

`BoardBootstrap` passes the same `seed` to `BoardGenerator.Generate(...)` and to the refill `new System.Random(seed)`. Both sequences are deterministic, but they start from the same random numbers, so early refills partly repeat the values used to generate the starting board. The two streams are coupled for no reason.

Phase 10 deliberately keeps this policy unchanged. Propeller targeting uses a separate `Random(unchecked(seed ^ 0x5EED1234))`, recreated alongside the refill stream on every start/restart. Targeting draws do not consume refill randomness, and restart replay is tested.

Possible future change: derive a separate refill seed (for example `seed + 1`). This would change refill sequences, so update deterministic expectations deliberately if that policy is revisited.
