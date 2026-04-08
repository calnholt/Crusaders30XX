# Component Logger JSON Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `ComponentLoggerService`'s `Console.WriteLine` output with structured JSON written to `logs/session.json`, grouped by context label, flushed periodically, cleared on startup.

**Architecture:** `LoggingService` owns all file I/O, buffering, and flush scheduling. `ComponentLoggerService` serializes entities/components to `JsonObject` via reflection and delegates to `LoggingService.Append`. `Game1.cs` wires the lifecycle (initialize, tick, flush). No existing call sites in `CardPlaySystem.cs` change.

**Tech Stack:** .NET 8.0, `System.Text.Json.Nodes` (JsonObject/JsonNode — built into net8.0, no extra package needed), MonoGame

---

### Task 1: Gitignore `logs/`

**Files:**
- Modify: `.gitignore`

- [ ] **Step 1: Add `logs/` to `.gitignore`**

Open `.gitignore`. It currently ends with:
```
# Debug output
debug/
```

Add below that:
```
logs/
```

- [ ] **Step 2: Commit**

```bash
git add .gitignore
git commit -m "chore: gitignore logs/ directory"
```

---

### Task 2: Create `LoggingService.cs`

**Files:**
- Create: `ECS/Services/LoggingService.cs`

- [ ] **Step 1: Create the file**

Create `ECS/Services/LoggingService.cs` with this content:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Services
{
    public static class LoggingService
    {
        private static Dictionary<string, List<JsonNode>> _buffer = new();
        private static int _callCount = 0;
        private static int _frameCount = 0;
        private static int _seqCounter = 0;
        private const int FlushEveryFrames = 300;
        private const int FlushEveryNCalls = 50;
        private const string LogPath = "logs/session.json";

        private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Initialize()
        {
            _buffer = new();
            _callCount = 0;
            _frameCount = 0;
            _seqCounter = 0;
            Directory.CreateDirectory("logs");
            File.WriteAllText(LogPath, "{}");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Tick()
        {
            _frameCount++;
            if (_frameCount % FlushEveryFrames == 0 || _callCount >= FlushEveryNCalls)
            {
                FlushInternal();
                _callCount = 0;
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Flush()
        {
            FlushInternal();
        }

        // Not marked [Conditional] — called from within [Conditional] methods only,
        // so it is already unreachable in release builds.
        public static void Append(string context, JsonObject entry)
        {
            entry["seq"] = ++_seqCounter;
            entry["frame"] = _frameCount;

            if (!_buffer.TryGetValue(context, out var list))
            {
                list = new List<JsonNode>();
                _buffer[context] = list;
            }
            list.Add(entry);
            _callCount++;
        }

        private static void FlushInternal()
        {
            try
            {
                string json = JsonSerializer.Serialize(_buffer, _writeOptions);
                File.WriteAllText(LogPath, json);
            }
            catch { }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add ECS/Services/LoggingService.cs
git commit -m "feat: add LoggingService for buffered JSON session logging"
```

---

### Task 3: Wire `LoggingService` into `Game1.cs`

**Files:**
- Modify: `Game1.cs` (lines ~117, ~244, ~470)

- [ ] **Step 1: Add using if not present**

Check the top of `Game1.cs` for `using Crusaders30XX.ECS.Services;`. If missing, add it with the other `using Crusaders30XX.*` lines at the top.

- [ ] **Step 2: Wire `Initialize()`**

`Initialize()` is at line ~117. Add `LoggingService.Initialize();` as the first line of the method body:

```csharp
protected override void Initialize()
{
    LoggingService.Initialize();
    CalculateRenderDestination();
    // Initialize ECS World
    _world = new World();
    base.Initialize();
}
```

- [ ] **Step 3: Wire `Update()`**

`Update()` is at line ~244. Add `LoggingService.Tick();` as the first statement:

```csharp
protected override void Update(GameTime gameTime)
{
    LoggingService.Tick();
    WindowIsActive = IsActive;
    // ... rest of method unchanged
```

- [ ] **Step 4: Wire `UnloadContent()`**

`UnloadContent()` is at line ~470. Add `LoggingService.Flush();` before `base.UnloadContent();`:

```csharp
protected override void UnloadContent()
{
    LoggingService.Flush();
    try { _currencyDisplaySystem?.Dispose(); } catch { }
    base.UnloadContent();
}
```

- [ ] **Step 5: Commit**

```bash
git add Game1.cs
git commit -m "feat: wire LoggingService Initialize/Tick/Flush into Game1 lifecycle"
```

---

### Task 4: Rewrite `ComponentLoggerService.cs`

**Files:**
- Modify: `ECS/Services/ComponentLoggerService.cs`

- [ ] **Step 1: Replace the entire file**

Replace `ECS/Services/ComponentLoggerService.cs` with:

```csharp
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Services
{
    public static class ComponentLoggerService
    {
        private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();
        private const int DefaultMaxDepth = 6;
        private const int CollectionCap = 20;

        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogComponent(IComponent component, string context, int maxDepth = DefaultMaxDepth)
        {
            if (component == null) return;

            var compObj = SerializeComponent(component, 1, maxDepth);
            var components = new JsonObject { [component.GetType().Name] = compObj };

            var entry = new JsonObject
            {
                ["entityId"] = component.Owner?.Id ?? -1,
                ["entityName"] = component.Owner?.Name ?? "unknown",
                ["components"] = components
            };

            LoggingService.Append(context, entry);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogEntity(Entity entity, string context, int maxDepth = DefaultMaxDepth)
        {
            if (entity == null) return;

            var components = new JsonObject();
            foreach (var component in entity.GetAllComponents())
                components[component.GetType().Name] = SerializeComponent(component, 1, maxDepth);

            var entry = new JsonObject
            {
                ["entityId"] = entity.Id,
                ["entityName"] = entity.Name,
                ["components"] = components
            };

            LoggingService.Append(context, entry);
        }

        // --- Serialization ---

        private static JsonObject SerializeComponent(IComponent component, int depth, int maxDepth)
        {
            var obj = new JsonObject();
            if (depth > maxDepth) { obj["_truncated"] = "(max depth)"; return obj; }

            foreach (var field in GetFields(component.GetType()))
            {
                if (field.Name == "Owner") continue;
                var node = SerializeValue(field.GetValue(component), depth, maxDepth);
                if (node != null) obj[field.Name] = node;
            }
            return obj;
        }

        private static JsonNode? SerializeValue(object? value, int depth, int maxDepth)
        {
            if (value == null) return null;

            var type = value.GetType();

            if (IsPrimitiveLike(type)) return SerializePrimitive(value);

            switch (value)
            {
                case Vector2 vec:
                    return new JsonObject { ["x"] = MathF.Round(vec.X, 2), ["y"] = MathF.Round(vec.Y, 2) };

                case Color color:
                    return new JsonObject { ["r"] = color.R, ["g"] = color.G, ["b"] = color.B, ["a"] = color.A };

                case Entity entity:
                    return new JsonObject { ["id"] = entity.Id, ["name"] = entity.Name };

                case IComponent component when depth < maxDepth:
                    return SerializeComponent(component, depth + 1, maxDepth);

                case IComponent component:
                    return JsonValue.Create($"[{type.Name}] (max depth)");

                case IDictionary dict:
                    return SerializeDictionary(dict, depth, maxDepth);

                case IEnumerable enumerable when value is not string:
                    return SerializeEnumerable(enumerable, depth, maxDepth);

                case IDisposable:
                    return JsonValue.Create($"[{type.Name}]");

                default:
                    return SerializeObject(value, type, depth, maxDepth);
            }
        }

        private static JsonNode SerializePrimitive(object value) => value switch
        {
            bool b   => JsonValue.Create(b),
            byte by  => JsonValue.Create(by),
            int i    => JsonValue.Create(i),
            long l   => JsonValue.Create(l),
            float f  => JsonValue.Create(f),
            double d => JsonValue.Create(d),
            decimal m => JsonValue.Create(m),
            DateTime dt => JsonValue.Create(dt.ToString("O")),
            TimeSpan ts => JsonValue.Create(ts.ToString()),
            _           => JsonValue.Create(value.ToString())
        };

        private static JsonArray SerializeEnumerable(IEnumerable enumerable, int depth, int maxDepth)
        {
            var arr = new JsonArray();
            int count = 0;
            foreach (var item in enumerable)
            {
                if (count >= CollectionCap) { arr.Add($"...{CountRemaining(enumerable, count)} more"); break; }
                var node = SerializeValue(item, depth + 1, maxDepth);
                arr.Add(node ?? JsonValue.Create("null"));
                count++;
            }
            return arr;
        }

        private static JsonObject SerializeDictionary(IDictionary dict, int depth, int maxDepth)
        {
            var obj = new JsonObject();
            int count = 0;
            foreach (DictionaryEntry entry in dict)
            {
                if (count >= CollectionCap) { obj[$"[{count}]"] = $"...more"; break; }
                var keyNode = SerializeValue(entry.Key, depth + 1, maxDepth);
                var valNode = SerializeValue(entry.Value, depth + 1, maxDepth);
                obj[$"[{count}]key"] = keyNode;
                if (valNode != null) obj[$"[{count}]val"] = valNode;
                count++;
            }
            return obj;
        }

        private static JsonNode SerializeObject(object obj, Type type, int depth, int maxDepth)
        {
            var fields = GetFields(type);
            if (fields.Length == 0) return JsonValue.Create(obj.ToString());

            var result = new JsonObject();
            foreach (var field in fields)
            {
                var node = SerializeValue(field.GetValue(obj), depth + 1, maxDepth);
                if (node != null) result[field.Name] = node;
            }
            return result;
        }

        private static int CountRemaining(IEnumerable enumerable, int alreadyCounted)
        {
            int total = 0;
            foreach (var _ in enumerable) total++;
            return total - alreadyCounted;
        }

        private static bool IsPrimitiveLike(Type type) =>
            type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(TimeSpan);

        private static FieldInfo[] GetFields(Type type) =>
            FieldCache.GetOrAdd(type, t => t.GetFields(BindingFlags.Public | BindingFlags.Instance));
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add ECS/Services/ComponentLoggerService.cs
git commit -m "feat: rewrite ComponentLoggerService to emit structured JSON via LoggingService"
```

---

### Task 5: Manual Verification

**Files:** None modified

- [ ] **Step 1: Run the game**

```bash
dotnet run
```

Expected: game launches without errors. `logs/session.json` is created with `{}` content immediately.

- [ ] **Step 2: Trigger a card play**

In game, navigate to a battle and play a card. After a few seconds (or 50 log calls), `logs/session.json` should update.

- [ ] **Step 3: Verify file structure**

Open `logs/session.json`. Expected shape:

```json
{
  "PlayCardRequested received": [
    {
      "entityId": 42,
      "entityName": "Card_FireBolt",
      "components": {
        "CardData": { ... },
        "Transform": { ... }
      },
      "seq": 1,
      "frame": 1042
    }
  ]
}
```

Verify:
- Top-level keys match the context strings passed in `CardPlaySystem.cs`
- `seq` and `frame` are present on each entry
- No `Owner` field appears inside component objects
- `Entity` references appear as `{ "id": N, "name": "..." }` not as full objects
- `null` fields are absent

- [ ] **Step 4: Verify clean shutdown flush**

Close the game. Re-open `logs/session.json`. All log entries from the session should be present (final flush on `UnloadContent`).

- [ ] **Step 5: Verify file cleared on restart**

Run the game again. Confirm `logs/session.json` is reset to `{}` on startup, not appending to previous session.
