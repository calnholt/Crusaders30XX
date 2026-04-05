# Parallax System Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make ParallaxLayerSystem 100% plug-and-play by removing all external cooperation requirements — no BasePosition, no cooperation flags, no exposed internal state.

**Architecture:** New PositionTween component + system for smooth position animations (decoupled from parallax). ParallaxLayerSystem refactored with internal anchor tracking via private dictionary. Transform.BasePosition removed entirely. 24+ systems migrated from BasePosition protocol to direct Position writes.

**Tech Stack:** C# / .NET 8.0 / MonoGame DesktopGL

---

> **IMPORTANT — Atomic batch:** Tasks 2-8 form a breaking batch. The project will not compile until ALL are complete. Task 1 is independently buildable. Task 9 is the build verification point.

---

### Task 1: Create PositionTween Infrastructure

**Files:**
- Create: `ECS/Components/PositionTween.cs`
- Create: `ECS/Systems/PositionTweenSystem.cs`
- Modify: `Game1.cs:169` (add field + registration)

- [ ] **Step 1: Create PositionTween component**

Create `ECS/Components/PositionTween.cs`:

```csharp
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Components
{
    public class PositionTween : IComponent
    {
        public Entity Owner { get; set; }

        // Where the layout system wants this entity (written by layout systems each frame)
        public Vector2 Target { get; set; } = Vector2.Zero;

        // Internally-tracked smooth position (owned exclusively by PositionTweenSystem)
        public Vector2 Current { get; set; } = Vector2.Zero;

        // Exponential decay rate for smoothing
        public float Speed { get; set; } = 10f;

        // False until first update; lets layout systems set Current to a spawn point before the system takes over
        public bool Initialized { get; set; } = false;
    }
}
```

- [ ] **Step 2: Create PositionTweenSystem**

Create `ECS/Systems/PositionTweenSystem.cs`:

```csharp
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    public class PositionTweenSystem : Core.System
    {
        public PositionTweenSystem(EntityManager em) : base(em) { }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<PositionTween>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var tween = entity.GetComponent<PositionTween>();
            var t = entity.GetComponent<Transform>();
            if (tween == null || t == null) return;

            if (!tween.Initialized)
            {
                tween.Current = t.Position;
                tween.Initialized = true;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float alpha = 1f - (float)System.Math.Exp(-tween.Speed * dt);
            tween.Current = Vector2.Lerp(tween.Current, tween.Target, MathHelper.Clamp(alpha, 0f, 1f));
            t.Position = tween.Current;
        }
    }
}
```

- [ ] **Step 3: Register PositionTweenSystem in Game1.cs**

In `Game1.cs`, add the field alongside the parallax field (around line 46):

```csharp
private PositionTweenSystem _positionTweenSystem;
```

In the constructor area (around line 169):

```csharp
_positionTweenSystem = new PositionTweenSystem(_world.EntityManager);
```

Register it immediately BEFORE the parallax system (around line 198):

```csharp
_world.AddSystem(_positionTweenSystem);
_world.AddSystem(_parallaxLayerSystem);
```

Add the using directive at the top of Game1.cs if not already present:

```csharp
using Crusaders30XX.ECS.Systems;
```

- [ ] **Step 4: Build to verify no regressions**

Run: `dotnet build`
Expected: Build succeeds with no errors. PositionTween exists but nothing uses it yet.

- [ ] **Step 5: Commit**

```bash
git add ECS/Components/PositionTween.cs ECS/Systems/PositionTweenSystem.cs Game1.cs
git commit -m "feat: add PositionTween component and system for smooth position animation"
```

---

### Task 2: Refactor Core Parallax Components and System

**Files:**
- Modify: `ECS/Components/CardComponents.cs:230-316` (Transform + ParallaxLayer)
- Rewrite: `ECS/Scenes/WorldMapScene/ParallaxLayerSystem.cs`

> **NOTE:** After this task, the project will NOT compile until Tasks 3-8 are complete. All references to `BasePosition`, `CaptureBaseOnFirstUpdate`, `UpdateBaseFromCurrentEachFrame`, `LastAppliedOffset`, `LastAppliedPosition`, and `AffectsUIBounds` will be broken.

- [ ] **Step 1: Strip Transform.BasePosition**

In `ECS/Components/CardComponents.cs`, remove `BasePosition` from the Transform class. Transform becomes:

```csharp
public class Transform : IComponent
{
    public Entity Owner { get; set; }

    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0f;
    public Vector2 Scale { get; set; } = Vector2.One;
    public int ZOrder { get; set; } = 0;
}
```

- [ ] **Step 2: Strip ParallaxLayer to pure config**

In `ECS/Components/CardComponents.cs`, replace the entire ParallaxLayer class with:

```csharp
/// <summary>
/// Parallax configuration for UI/scene entities that should subtly move opposite the cursor.
/// Attach this component and the ParallaxLayerSystem handles everything automatically.
/// </summary>
public class ParallaxLayer : IComponent
{
    public Entity Owner { get; set; }

    public float MultiplierX { get; set; } = 0.03f;
    public float MultiplierY { get; set; } = 0.03f;
    public float MaxOffset { get; set; } = 48f;
    public float SmoothTime { get; set; } = 0.08f;

    public static ParallaxLayer GetUIParallaxLayer()
    {
        return new ParallaxLayer
        {
            MultiplierX = 0.025f,
            MultiplierY = 0.025f,
            MaxOffset = 48f,
            SmoothTime = 0.08f
        };
    }

    public static ParallaxLayer GetLocationParallaxLayer()
    {
        return new ParallaxLayer
        {
            MultiplierX = 0.01f,
            MultiplierY = 0.01f,
            MaxOffset = 12f,
            SmoothTime = 0.01f
        };
    }

    public static ParallaxLayer GetCharacterParallaxLayer()
    {
        return new ParallaxLayer
        {
            MultiplierX = 0.01f,
            MultiplierY = 0.01f,
            MaxOffset = 48f,
            SmoothTime = 0.08f
        };
    }
}
```

- [ ] **Step 3: Rewrite ParallaxLayerSystem**

Replace the entire contents of `ECS/Scenes/WorldMapScene/ParallaxLayerSystem.cs` with:

```csharp
using System;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Parallax Layer System")]
    public class ParallaxLayerSystem : Core.System
    {
        private readonly GraphicsDevice _graphics;
        private Vector2 _cursorPos;

        private struct ParallaxState
        {
            public Vector2 Anchor;
            public Vector2 LastWrittenPos;
            public bool Initialized;
        }

        private readonly Dictionary<int, ParallaxState> _states = new();

        public ParallaxLayerSystem(EntityManager em, GraphicsDevice graphics)
            : base(em)
        {
            _graphics = graphics;
            EventManager.Subscribe<CursorStateEvent>(OnCursor);
            EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
        }

        private void OnCursor(CursorStateEvent evt)
        {
            _cursorPos = evt.Position;
        }

        private void OnDeleteCaches(DeleteCachesEvent evt)
        {
            _states.Clear();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<ParallaxLayer>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var layer = entity.GetComponent<ParallaxLayer>();
            var t = entity.GetComponent<Transform>();
            if (layer == null || t == null) return;

            int id = entity.Id;
            if (!_states.TryGetValue(id, out var state))
            {
                state = new ParallaxState();
            }

            // First frame: anchor on current position
            if (!state.Initialized)
            {
                state.Anchor = t.Position;
                state.Initialized = true;
            }
            // Detect external write: something else moved the entity since our last write
            else if (t.Position != state.LastWrittenPos)
            {
                state.Anchor = t.Position;
            }

            // Compute parallax offset from cursor
            int w = Game1.VirtualWidth;
            int h = Game1.VirtualHeight;
            var center = new Vector2(w / 2f, h / 2f);
            Vector2 delta = center - _cursorPos;
            Vector2 raw = new Vector2(delta.X * layer.MultiplierX, delta.Y * layer.MultiplierY);
            float max = Math.Max(0f, layer.MaxOffset);
            Vector2 offset = ClampMagnitude(raw, max);

            // Smooth toward target
            Vector2 target = state.Anchor + offset;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float smooth = layer.SmoothTime;
            float a = (smooth <= 0f) ? 1f : (1f - (float)Math.Exp(-dt / smooth));
            Vector2 newPos = Vector2.Lerp(t.Position, target, MathHelper.Clamp(a, 0f, 1f));

            t.Position = newPos;
            state.LastWrittenPos = newPos;
            _states[id] = state;

            // Keep UI bounds aligned with parallax-adjusted position
            var ui = entity.GetComponent<UIElement>();
            if (ui != null)
            {
                int tileW = ui.Bounds.Width;
                int tileH = ui.Bounds.Height;
                ui.Bounds = new Rectangle(
                    (int)Math.Round(newPos.X - tileW / 2f),
                    (int)Math.Round(newPos.Y - tileH / 2f),
                    tileW,
                    tileH);
            }
        }

        private static Vector2 ClampMagnitude(Vector2 v, float maxLen)
        {
            float len = v.Length();
            if (len <= maxLen || len == 0f) return v;
            return v * (maxLen / len);
        }
    }
}
```

---

### Task 3: Migrate EntityFactory and Inline Entity Creation (Category C)

**Files:**
- Modify: `ECS/Factories/EntityFactory.cs`
- Modify: Various systems that create entities with `BasePosition` in Transform constructors

- [ ] **Step 1: Fix EntityFactory.cs**

Search for all instances of `BasePosition` in `ECS/Factories/EntityFactory.cs` and remove the `BasePosition = ...` from Transform object initializers. For example:

```csharp
// Before:
new Transform { Position = position, BasePosition = position, ZOrder = 0 }

// After:
new Transform { Position = position, ZOrder = 0 }
```

```csharp
// Before:
new Transform { Position = new Vector2(-1000, -1000), BasePosition = new Vector2(-1000, -1000), ZOrder = 10002 }

// After:
new Transform { Position = new Vector2(-1000, -1000), ZOrder = 10002 }
```

Also remove `AffectsUIBounds` references from any ParallaxLayer configuration in EntityFactory:

```csharp
// Before:
var pl = ParallaxLayer.GetUIParallaxLayer();
pl.AffectsUIBounds = true;
entityManager.AddComponent(e, pl);

// After:
entityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
```

- [ ] **Step 2: Fix inline entity creation across systems**

Apply the same pattern to every system that creates entities with `BasePosition` in a Transform constructor or sets `AffectsUIBounds`, `CaptureBaseOnFirstUpdate`, or `UpdateBaseFromCurrentEachFrame` on a ParallaxLayer. Delete all such references. These are found in:

- `LocationSelectDisplaySystem.cs:173` — remove `BasePosition = position` from `new Transform { ... }`
- `CustomizeButtonDisplaySystem.cs:86,104` — remove `BasePosition = position` from `new Transform { ... }`
- `AchievementButtonDisplaySystem.cs:154,172` — remove `BasePosition = position` from `new Transform { ... }`
- `AmbushDisplaySystem.cs:358` (and similar anchor entity creation lines) — remove `BasePosition` from Transform constructors
- `GuardianAngelDisplaySystem.cs:424` — remove `BasePosition = Vector2.Zero` from `new Transform { ... }`
- `EndTurnDisplaySystem.cs:205` — remove `BasePosition` from `new Transform { ... }`
- `DialogDisplaySystem.cs` — any entity creation with `BasePosition` in Transform constructors
- `EnemyDamageMeterDisplaySystem.cs` — any entity creation with `BasePosition` in Transform constructors
- `QuestRewardModalDisplaySystem.cs` — any entity creation with `BasePosition` in Transform constructors
- `ForSaleDisplaySystem.cs` — any entity creation with `BasePosition` in Transform constructors

The pattern is always the same: delete the `BasePosition = ...` from the object initializer.

---

### Task 4: Migrate Simple Update-Path Systems (Category A)

**Files:** 12 systems that write `BasePosition` in their Update path.

- [ ] **Step 1: Bulk replace BasePosition writes with Position writes**

For each system below, find all lines that write `t.BasePosition = ...` or `transform.BasePosition = ...` and change them to write `t.Position = ...` or `transform.Position = ...` instead.

Also delete any lines that read `BasePosition` (like `var baseCurrent = transform.BasePosition`).

**DrawPileDisplaySystem.cs** — change `BasePosition = center` to `Position = center`

**DiscardPileDisplaySystem.cs** — change `BasePosition = center` to `Position = center`

**PlayerDisplaySystem.cs** — change `BasePosition = pos` to `Position = pos`

**QueuedEventsDisplaySystem.cs** — change `BasePosition = screenCenter` to `Position = screenCenter`

**EquippedWeaponDisplaySystem.cs** — change `BasePosition = pos` to `Position = pos`

**LocationSelectDisplaySystem.cs** — change `t.BasePosition = position` (line 132) to `t.Position = position`

**PointOfInterestDisplaySystem.cs** — change `t.BasePosition = screenPos` (line 142) to `t.Position = screenPos`

**CustomizeButtonDisplaySystem.cs** — change `transform.BasePosition = position` (line 99) to `transform.Position = position`

**AchievementButtonDisplaySystem.cs** — change `transform.BasePosition = position` (line 167) to `transform.Position = position`

**AssignedBlockCardsDisplaySystem.cs** — remove any `BasePosition` references

**DeckManagementSystem.cs** — delete the three `BasePosition = Vector2.Zero` lines (lines 124, 277, 319). The adjacent `Position` resets already exist.

**AchievementGridDisplaySystem.cs** — change `x.T.BasePosition = baseCenter` to `x.T.Position = baseCenter`. Delete lines that read `BasePosition` for offset computation (lines 235, 237, 241, 246). Replace offset computation:

```csharp
// Before:
if (x.T.BasePosition == Vector2.Zero)
{
    x.T.BasePosition = baseCenter;
    x.T.Position = baseCenter;
}
x.T.BasePosition = baseCenter;
var cand = pos - x.T.BasePosition;

// After:
x.T.Position = baseCenter;
```

The manual parallax offset extraction (`pos - BasePosition`) is no longer needed — the parallax system handles it internally. Remove the offset variable and any code that applies it to draw rects. Draw rects should use `x.T.Position` directly (which will already include parallax offset by the time Draw runs).

---

### Task 5: Migrate HandDisplaySystem to PositionTween

**Files:**
- Modify: `ECS/Scenes/BattleScene/HandDisplaySystem.cs:237-257`

- [ ] **Step 1: Replace BasePosition tween with PositionTween**

In `HandDisplaySystem.cs`, in the card layout loop (around lines 237-257), replace the BasePosition tween logic with PositionTween.Target writes:

```csharp
// Before (lines 237-257):
// If this card just appeared, spawn its base offscreen to the right so it flies in
if (transform.BasePosition == Vector2.Zero)
{
    float spawnX = Game1.VirtualWidth + ((cvs?.CardWidth ?? 250) * 1.5f);
    float spawnY = pivot.Y + HandFanCurveOffset;
    var spawn = new Vector2(spawnX, spawnY);
    transform.BasePosition = spawn;
    transform.Position = spawn;
}

// Hover lift
var ui = entity.GetComponent<UIElement>();
bool hovered = ui?.IsHovered == true;
if (hovered) { y -= HandHoverLift; }

// Smooth tween the BasePosition toward the layout target; ParallaxLayer will set current Position
var baseCurrent = transform.BasePosition;
var baseTarget = new Vector2(x, y);
float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
float alpha = 1f - (float)Math.Exp(-HandTweenSpeed * dt);
transform.BasePosition = Vector2.Lerp(baseCurrent, baseTarget, MathHelper.Clamp(alpha, 0f, 1f));

// After:
// Hover lift
var ui = entity.GetComponent<UIElement>();
bool hovered = ui?.IsHovered == true;
if (hovered) { y -= HandHoverLift; }

var tween = entity.GetComponent<PositionTween>();
if (tween != null)
{
    // If this card just appeared, spawn offscreen to the right so it flies in
    if (!tween.Initialized)
    {
        float spawnX = Game1.VirtualWidth + ((cvs?.CardWidth ?? 250) * 1.5f);
        float spawnY = pivot.Y + HandFanCurveOffset;
        tween.Current = new Vector2(spawnX, spawnY);
        transform.Position = tween.Current;
    }
    tween.Target = new Vector2(x, y);
    tween.Speed = HandTweenSpeed;
}
else
{
    // No tween component — write position directly
    transform.Position = new Vector2(x, y);
}
```

Add `using Crusaders30XX.ECS.Components;` at the top if not already present (it likely is).

- [ ] **Step 2: Ensure EntityFactory adds PositionTween to card entities**

In `EntityFactory.cs`, where card entities are created and given a `ParallaxLayer`, also add a `PositionTween` component. Find the card entity creation (around line 367 where `ParallaxLayer.GetUIParallaxLayer()` is added):

```csharp
entityManager.AddComponent(entity, ParallaxLayer.GetUIParallaxLayer());
entityManager.AddComponent(entity, new PositionTween { Speed = 12f });
```

Also in the card clone method (around line 699):

```csharp
entityManager.AddComponent(clonedEntity, ParallaxLayer.GetUIParallaxLayer());
entityManager.AddComponent(clonedEntity, new PositionTween { Speed = 12f });
```

---

### Task 6: Migrate GuardianAngelDisplaySystem to PositionTween

**Files:**
- Modify: `ECS/Scenes/BattleScene/GuardianAngelDisplaySystem.cs:189,424`

- [ ] **Step 1: Replace BasePosition write with PositionTween.Target**

At line 189, replace:

```csharp
// Before:
gt.BasePosition = _pos;

// After:
var tween = guardian.GetComponent<PositionTween>();
if (tween != null)
{
    tween.Target = _pos;
}
else
{
    gt.Position = _pos;
}
```

- [ ] **Step 2: Add PositionTween to guardian entity creation**

At line 424 in `EnsureGuardianEntity()`, after the Transform and ParallaxLayer are added:

```csharp
EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, ZOrder = 0 });
EntityManager.AddComponent(e, ParallaxLayer.GetCharacterParallaxLayer());
EntityManager.AddComponent(e, new PositionTween { Speed = 10f });
```

---

### Task 7: Migrate Draw-to-Update Systems (Category B)

**Files:** 10 systems that write Position/BasePosition in Draw functions.

The pattern for each system is: move the layout position computation from Draw into Update (or a helper called from Update), so the parallax system can process it. Draw reads `Transform.Position` for rendering but never writes it.

- [ ] **Step 1: EquipmentDisplaySystem — move BasePosition write to Update**

In `ECS/Scenes/BattleScene/EquipmentDisplaySystem.cs`:

The current `UpdateEntity` is a no-op (line 128-131). The layout logic that sets `BasePosition` lives in `Draw()` (line 189).

Move the layout computation into `UpdateEntity`. The Update path needs to iterate equipment entities and set their `Position`:

Replace the no-op UpdateEntity with logic that computes layout positions. Keep the same layout math (baseX, y, rowHeight, etc.) currently in Draw:

```csharp
protected override void UpdateEntity(Entity entity, GameTime gameTime)
{
    // Layout equipment positions in Update so parallax can process them
    var allEquipmentForPlayer = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
        .Where(e => e.GetComponent<EquippedEquipment>().EquippedOwner == entity)
        .Select(e => e.GetComponent<EquippedEquipment>())
        .ToList();
    var equipment = allEquipmentForPlayer
        .Where(eq => (eq.Owner.GetComponent<EquipmentZone>()?.Zone ?? EquipmentZoneType.Default) == EquipmentZoneType.Default)
        .ToList();

    if (equipment.Count == 0) return;

    EquipmentSlot[] order = [EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Arms, EquipmentSlot.Legs];
    int baseX = LeftMargin;
    int y = TopMargin;
    int rowHeight = (IconSize + BgPadding * 2) + RowGap;
    foreach (var type in order)
    {
        var items = equipment.Where(eq => eq.Equipment.Slot == type).ToList();
        if (items.Count > 0)
        {
            int x = baseX;
            foreach (var item in items)
            {
                var tEquip = item.Owner.GetComponent<Transform>();
                if (tEquip != null)
                {
                    tEquip.Position = new Vector2(x, y);
                    tEquip.ZOrder = 10001;
                }
                x += (IconSize + BgPadding * 2) + ColGap;
            }
            y += rowHeight;
        }
    }
}
```

In `Draw()`, remove the `tEquip.BasePosition = ...` line (189) and the `tEquip.ZOrder = ...` line (190). In Draw, build the draw rect from `tEquip.Position` directly (already read at line 193 — just remove the fallback):

```csharp
// Before:
tEquip.BasePosition = new Vector2(x, y);
tEquip.ZOrder = 10001;
var curPos = tEquip != null ? tEquip.Position : new Vector2(x, y);

// After (remove BasePosition/ZOrder writes, keep Position read):
var curPos = tEquip != null ? tEquip.Position : new Vector2(x, y);
```

Also remove the UIElement bounds manual sync in Draw if present — the parallax system now handles this.

- [ ] **Step 2: EndTurnDisplaySystem — move entity setup to Update**

In `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs`:

The entity creation and position writes (lines 200-218) happen in Draw. Move entity creation/position sync into `UpdateEntity`.

Add to UpdateEntity (or a helper called from Update):

```csharp
// Ensure the end-turn button entity exists and position is set
var btnRect = GetButtonRect();
var endBtn = EntityManager.GetEntity("UIButton_EndTurn");
if (endBtn == null)
{
    endBtn = EntityManager.CreateEntity("UIButton_EndTurn");
    EntityManager.AddComponent(endBtn, new Transform { Position = new Vector2(btnRect.X, btnRect.Y), ZOrder = ButtonZ });
    EntityManager.AddComponent(endBtn, new UIElement { Bounds = btnRect, IsInteractable = true, IsHidden = true, EventType = UIElementEventType.EndTurn });
    EntityManager.AddComponent(endBtn, new HotKey { Button = FaceButton.Y });
    EntityManager.AddComponent(endBtn, ParallaxLayer.GetUIParallaxLayer());
}
else
{
    var tr = endBtn.GetComponent<Transform>();
    if (tr != null)
    {
        tr.ZOrder = ButtonZ;
        tr.Position = new Vector2(btnRect.X, btnRect.Y);
    }
}
```

Remove the corresponding entity creation and position write block from Draw. Draw should only read Position for rendering:

```csharp
var t = endBtn?.GetComponent<Transform>();
Vector2 drawPos = (t != null) ? t.Position : new Vector2(btnRect.X, btnRect.Y);
var drawRect = new Rectangle((int)drawPos.X, (int)drawPos.Y, btnRect.Width, btnRect.Height);
```

- [ ] **Step 3: MedalDisplaySystem — move BasePosition write to Update**

Same pattern as EquipmentDisplaySystem. Find the per-medal `BasePosition` writes in Draw, move the layout position computation to Update. In Draw, read `Position` for rendering.

- [ ] **Step 4: ForSaleDisplaySystem — simplify by removing manual offset logic**

In `ECS/Scenes/ShopScene/ForSaleDisplaySystem.cs`:

The Draw method (lines 229-255) manually extracts the parallax offset (`Position - BasePosition`) and applies it to draw rects. In the new design, all this manual offset logic disappears.

Move the layout position write into Update:

```csharp
protected override void UpdateEntity(Entity entity, GameTime gameTime)
{
    var scene = entity.GetComponent<SceneState>();
    if (scene == null || scene.Current != SceneId.Shop) return;

    EnsureInventoryEntities();

    var items = EntityManager.GetEntitiesWithComponent<ForSaleItem>()
        .Select(e => new { E = e, T = e.GetComponent<Transform>(), FS = e.GetComponent<ForSaleItem>() })
        .Where(x => x.FS != null)
        .ToList();

    if (items.Count == 0) return;

    int cols = Math.Max(1, MaxColumns);
    for (int i = 0; i < items.Count; i++)
    {
        var x = items[i];
        int row = i / cols;
        int col = i % cols;
        int px = PanelMarginX + col * (TileWidth + HorizontalGap);
        int py = PanelMarginTop + row * (TileHeight + VerticalGap);
        var baseCenter = new Vector2(px + TileWidth / 2f, py + TileHeight / 2f);

        if (x.T != null)
        {
            x.T.Position = baseCenter;
            x.T.ZOrder = 10002;
        }
    }
}
```

In Draw, remove the entire BasePosition/offset extraction block (lines 229-255). Replace with simply reading Position:

```csharp
// Build draw rect from the entity's current Position (includes parallax offset)
var pos = x.T != null ? x.T.Position : new Vector2(tileRect.X + tileRect.Width / 2f, tileRect.Y + tileRect.Height / 2f);
var drawRect = new Rectangle(
    (int)System.Math.Round(pos.X - TileWidth / 2f),
    (int)System.Math.Round(pos.Y - TileHeight / 2f),
    TileWidth,
    TileHeight);
```

- [ ] **Step 5: EnemyDamageMeterDisplaySystem — move entity creation to Update**

Same pattern. Find entity creation with BasePosition in Draw, move to Update. Draw reads Position only.

- [ ] **Step 6: DialogDisplaySystem — move EndButton creation to Update**

Find the EndButton entity creation in Draw. Move to a helper method called from Update. Remove BasePosition references.

- [ ] **Step 7: EnemyAttackDisplaySystem.Draw.cs — move banner anchor position to Update**

Move the banner anchor BasePosition write from Draw into the Update path. Draw reads Position for rendering.

- [ ] **Step 8: QuestRewardModalDisplaySystem — move button creation to Update**

Move proceed button entity creation from Draw to Update. Remove BasePosition references.

- [ ] **Step 9: QuestTribulationDisplaySystem — move Position write to Update**

Move the chalice Position write from Draw to Update.

- [ ] **Step 10: SkipPledgeDisplaySystem — consolidate writes to Update**

Remove Position/BasePosition writes from Draw path. Ensure all position writes happen in Update only.

---

### Task 8: Delete AmbushDisplaySystem Parallax Reset

**Files:**
- Modify: `ECS/Scenes/BattleScene/AmbushDisplaySystem.cs:120,212,363-385`

- [ ] **Step 1: Delete ResetAnchorParallax method**

Delete the entire `ResetAnchorParallax()` method (lines 363-385).

- [ ] **Step 2: Remove calls to ResetAnchorParallax**

Remove the call at line 120 and line 212. The parallax system will automatically detect position changes when `UpdateAnchorTransforms()` writes to Position.

- [ ] **Step 3: In UpdateAnchorTransforms, ensure it writes Position**

Verify that `UpdateAnchorTransforms()` (line 387+) writes `Transform.Position` for the anchor entities. If it currently writes `BasePosition`, change to `Position`. The parallax system detects the external write and re-anchors automatically.

---

### Task 9: Build, Verify, Update CLAUDE.md, Commit

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Verify no remaining references to removed fields**

Search the entire codebase for any remaining references to removed fields:

Run: `grep -rE "BasePosition|AffectsUIBounds|CaptureBaseOnFirstUpdate|UpdateBaseFromCurrentEachFrame|LastAppliedOffset|LastAppliedPosition" --include="*.cs" .`
Expected: Zero results.

- [ ] **Step 2: Build the project**

Run: `dotnet build`
Expected: Build succeeds with no errors.

- [ ] **Step 3: Update CLAUDE.md parallax section**

Replace the current "Parallax System" section in CLAUDE.md with:

```markdown
### Parallax System

The `ParallaxLayerSystem` is fully agnostic — external systems cooperate with it only through `Transform.Position`, never by reading or writing `ParallaxLayer` fields directly. Layout systems freely write `Position` every frame to assert the entity's base; the parallax system detects external writes, derives the anchor, and applies its offset. No system should reference `ParallaxLayer.Anchor`, `AnchorInitialized`, `LastWrittenPosition`, or any other internal parallax state.
```

- [ ] **Step 4: Run the game and visually verify parallax behavior**

Run: `dotnet run`

Verify:
- World map location tiles have subtle parallax movement when moving cursor
- Battle scene cards in hand have parallax + smooth fly-in animation
- Equipment icons, buttons (End Turn, Skip), and enemy meters all have parallax
- Shop tiles have parallax
- No visual jitter or snapping on scene transitions

- [ ] **Step 5: Commit all changes**

```bash
git add -A
git commit -m "refactor: make ParallaxLayerSystem fully plug-and-play

Remove Transform.BasePosition and all ParallaxLayer cooperation flags.
Parallax system now tracks anchors internally via private dictionary.
New PositionTween component/system for smooth position animations.
24+ systems migrated from BasePosition protocol to direct Position writes.
10 systems migrated from Draw-path to Update-path position writes."
```

- [ ] **Step 6: Delete PARALLAX_COUPLING_REPORT.md (now obsolete)**

```bash
git rm PARALLAX_COUPLING_REPORT.md
git commit -m "chore: remove obsolete parallax coupling report"
```
