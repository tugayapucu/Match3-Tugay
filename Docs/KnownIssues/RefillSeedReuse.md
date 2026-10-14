# Refill reuses the generation seed

Status: Deferred; low priority, not a bug.

`BoardBootstrap` passes the same `seed` to `BoardGenerator.Generate(...)` and to the refill `new System.Random(seed)`. Both sequences are deterministic, but they start from the same random numbers, so early refills partly repeat the values used to generate the starting board. The two streams are coupled for no reason.

Suggested fix: derive a separate refill seed (for example `seed + 1`). One line in `BoardBootstrap`; the game stays reproducible from a single Inspector seed.
