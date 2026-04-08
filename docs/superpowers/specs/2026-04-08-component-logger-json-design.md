# Component Logger JSON Redesign

**Date:** 2026-04-08  
**Status:** Approved

## Goal

Improve `ComponentLoggerService` to write structured JSON logs to `logs/session.json` for LLM-assisted debugging. File is cleared on startup and flushed periodically. Logs are grouped by context label for fast navigation.

## Architecture

Two files changed, two new:

| File | Change |
|------|--------|
| `ECS/Services/ComponentLoggerService.cs` | Rewritten — serializes to `JsonObject`, delegates to `LoggingService` |
| `ECS/Services/LoggingService.cs` | New — owns in-memory buffer, file I/O, flush scheduling |
| `Game1.cs` | 3 call sites added: `Initialize`, `Update`, `UnloadContent` |
| `logs/session.json` | Output file, cleared on startup, gitignored |

All existing `ComponentLoggerService` call sites in `CardPlaySystem.cs` are untouched. `[Conditional("DEBUG")]` stays on all public methods — zero cost in release builds.

## JSON Output Format

Top-level object keyed by context label. Each value is an array of log entries:

```json
{
  "PlayCardRequested received": [
    {
      "seq": 1,
      "frame": 1042,
      "entityId": 42,
      "entityName": "Card_FireBolt",
      "components": {
        "CardData": {
          "CardId": "fire_bolt",
          "Type": "Attack",
          "Cost": 1
        },
        "Transform": {
          "Position": { "x": 320.50, "y": 240.00 },
          "Scale": 1.0
        }
      }
    }
  ],
  "Executing card OnPlay effect": []
}
```

### Token-Efficiency Rules

- Null fields are omitted entirely
- `Entity` references render as `{ "id": N, "name": "..." }` — no recursion
- Collections (IEnumerable, IDictionary) capped at 20 items; truncated with a `"...N more"` tail entry
- `Owner` field always skipped (circular reference)
- Max depth: 6 (down from 8)
- `seq` + `frame` provide global temporal ordering across context groups

## `LoggingService` Design

```csharp
public static class LoggingService
{
    private static Dictionary<string, List<JsonNode>> _buffer;
    private static int _callCount;
    private static int _frameCount;
    private const int FlushEveryFrames = 300;  // ~5s at 60fps
    private const int FlushEveryNCalls = 50;
    private const string LogPath = "logs/session.json";

    public static void Initialize();  // clears file, resets buffer
    public static void Tick();        // called each frame; flushes on threshold
    public static void Flush();       // final write on shutdown
    public static void Append(string context, JsonObject entry);
}
```

**Flush behavior:** Overwrites `logs/session.json` with the full buffer on every flush. File is always a complete, valid JSON document — safe to read mid-session.

`Initialize()` creates the `logs/` directory if missing and writes `{}` to reset the file.

## `ComponentLoggerService` Serialization

Reflection walk produces a `JsonNode` tree instead of a `StringBuilder`. Type dispatch:

| Input type | Output |
|------------|--------|
| `null` | omitted by caller |
| primitive / string | `JsonValue` |
| `Vector2` | `{ "x": F2, "y": F2 }` |
| `Color` | `{ "r", "g", "b", "a" }` |
| `Entity` | `{ "id": N, "name": "..." }` — no recursion |
| `IComponent` | recurse public fields via reflection (skip `Owner`, respect `maxDepth`) |
| `IDictionary` | `{ "[0]key": ..., "[0]val": ... }` capped at 20 |
| `IEnumerable` | `[...]` capped at 20, tail `"...N more"` if truncated |
| `IDisposable` | `"[TypeName]"` opaque |
| other struct/class | recurse public fields |

`LogEntity` entry shape:
```json
{ "seq": N, "frame": N, "entityId": N, "entityName": "...", "components": { "TypeName": {...} } }
```

`LogComponent` uses the same shape with a single-key `components` map.

Field cache (`ConcurrentDictionary<Type, FieldInfo[]>`) is retained for reflection performance.

## `Game1.cs` Wiring

```csharp
// Initialize()
LoggingService.Initialize();

// Update()
LoggingService.Tick();

// UnloadContent()
LoggingService.Flush();
```

## `.gitignore` Addition

```
logs/
```
