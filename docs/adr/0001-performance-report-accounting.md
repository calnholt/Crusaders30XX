# Performance report uses leaf-only unaccounted time

Shift+Escape writes a DEBUG-only session report (`logs/performance-report.txt`) from `FrameProfiler`. Scopes are nested (e.g. `Game1.Update` wraps `ECS.World.Update` wraps `InputSystem.Update`; scene draw roots wrap child `*.Draw` calls). Summing every row would double-count and make “unaccounted” negative.

**Decision:** `unaccountedMs = totalFrameMs - sum(leaf scopes only)`. Inclusive scopes (`MeasureInclusive`: `Game1.Update`, `Game1.Draw`, `ECS.World.*`, scene/root draw entry points) appear in the report but are excluded from that sum. Per-system `{TypeName}.Update` and nested draw/event scopes are leaf.

`BeginGameFrame` runs in all builds so rolling overlay frame IDs stay correct; session min/max/avg and `WriteReport` are DEBUG-only. Display snapshot runs skip session accumulation so automated capture does not skew play-session stats.

Each committed frame is tagged with `SceneState.Current` after `World.Update` (`SetActiveScene`), enabling per-scene tables when a scene has at least 60 profiled frames. The report adds P95 (nearest-rank) and slow-frame counts (per-scope samples &gt; 16.67 ms), a session footer, leaf `Game1.Draw.*` breakdown scopes, and a global spike-hotspots section sorted by slow-frame count.
