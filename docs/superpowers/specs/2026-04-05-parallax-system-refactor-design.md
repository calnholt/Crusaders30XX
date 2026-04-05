# Parallax System Refactor: Plug-and-Play Design

## Goal

Make `ParallaxLayerSystem` 100% plug-and-play. Attaching a `ParallaxLayer` component to any entity makes it respond to cursor-based parallax with zero knowledge required from any other system. No cooperation flags, no special position fields, no exposed internal state.

## Current Problems

1. **`Transform.BasePosition`** exists solely for parallax, forcing every layout system (23+) to write to it instead of `Position`
2. **Cooperation flags** (`UpdateBaseFromCurrentEachFrame`, `CaptureBaseOnFirstUpdate`, `AffectsUIBounds`) require layout systems to understand parallax internals
3. **Exposed internal state** (`LastAppliedOffset`, `LastAppliedPosition`) allows external systems to manipulate parallax tracking (AmbushDisplaySystem does this)
4. **Position writes in Draw** — 10 systems write layout positions in Draw functions, which conflicts with parallax applying offsets during Update
5. **Tween + parallax coupling** — HandDisplaySystem tweens `BasePosition` because it can't tween `Position` (parallax mutates it). This couples animation logic to parallax awareness.

## Design

### 1. PositionTween Component + PositionTweenSystem

A generic smooth-position system that composes transparently with parallax. Solves the tween+parallax coupling for any system that needs smooth movement.

**`PositionTween` component:**

| Field | Type | Default | Owner |
|-------|------|---------|-------|
| `Target` | Vector2 | Zero | Layout systems (write each frame) |
| `Current` | Vector2 | Zero | PositionTweenSystem only |
| `Speed` | float | 10f | Config (set once) |
| `Initialized` | bool | false | PositionTweenSystem only |

**`PositionTweenSystem`** — registered before ParallaxLayerSystem:
- Queries entities with `PositionTween` + `Transform`
- If not initialized: sets `Current = Transform.Position`, marks initialized
- Each frame: lerps `Current` toward `Target` via exponential smoothing (`1 - exp(-Speed * dt)`)
- Writes `Transform.Position = Current`

Layout systems set `Target`. For spawn animations, set `Current` to the spawn point before the first update. The system handles interpolation; parallax applies offset on top.

### 2. ParallaxLayer Component (Pure Config)

All internal state and cooperation flags removed. The component is purely declarative.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `MultiplierX` | float | 0.03f | Horizontal cursor sensitivity |
| `MultiplierY` | float | 0.03f | Vertical cursor sensitivity |
| `MaxOffset` | float | 48f | Maximum parallax offset in pixels |
| `SmoothTime` | float | 0.08f | Exponential smoothing time constant |

**Removed fields:**
- `CaptureBaseOnFirstUpdate` — replaced by auto-detection
- `UpdateBaseFromCurrentEachFrame` — replaced by auto-detection
- `AffectsUIBounds` — if entity has UIElement, bounds are always synced
- `LastAppliedOffset` — moved to system-internal tracking
- `LastAppliedPosition` — moved to system-internal tracking

**Factory methods** (simplified — just configure the four fields):
- `GetUIParallaxLayer()` — MultiplierX/Y: 0.025f, MaxOffset: 48f, SmoothTime: 0.08f
- `GetLocationParallaxLayer()` — MultiplierX/Y: 0.01f, MaxOffset: 12f, SmoothTime: 0.01f
- `GetCharacterParallaxLayer()` — MultiplierX/Y: 0.01f, MaxOffset: 48f, SmoothTime: 0.08f

### 3. ParallaxLayerSystem (Autonomous, Internal Tracking)

The system tracks anchor state in a private dictionary, invisible to all other systems.

**Internal state:**
```
Dictionary<int, ParallaxState> _states  // keyed by entity ID

struct ParallaxState
{
    Vector2 Anchor;          // layout position (derived from external writes)
    Vector2 LastWrittenPos;  // what this system last wrote to Position
    bool Initialized;        // false until first frame
}
```

**Per-entity logic each frame:**

1. **First frame** (`!Initialized`): Set `Anchor = Position`. Mark `Initialized = true`. Continue to step 3.
2. **Detect external writes**: If `Position != LastWrittenPos`, an external system moved the entity. Set `Anchor = Position`.
3. **Compute offset**: `delta = screenCenter - cursorPos`, `raw = delta * (MultiplierX, MultiplierY)`, clamp magnitude to `MaxOffset`.
4. **Apply with smoothing**: `target = Anchor + offset`. Lerp from current `Position` toward `target` using exponential smoothing (`SmoothTime`). Write result to `Position`. Store as `LastWrittenPos`.
5. **UIBounds sync**: If entity has `UIElement`, update `Bounds` to be centered on the new `Position` (preserving existing width/height).

**Cleanup:** Subscribe to entity destruction events to remove stale dictionary entries.

**System registration:** After PositionTweenSystem, after all scene systems. Current position in Game1.cs (near end of registration list) is correct.

### 4. Transform Changes

Remove `BasePosition` from `Transform`:

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

`BasePosition` existed solely for parallax. No other system has an independent use for it.

### 5. Migration

Every affected system falls into one of three categories. Migration is mechanical per category.

#### Category A: Write Position in Update (14 systems)

Change `t.BasePosition = pos` to `t.Position = pos`. Delete `BasePosition` initialization.

| System | Notes |
|--------|-------|
| HandDisplaySystem | Also add PositionTween: replace BasePosition tween with Tween.Target |
| DrawPileDisplaySystem | Direct replacement |
| DiscardPileDisplaySystem | Direct replacement |
| PlayerDisplaySystem | Direct replacement |
| GuardianAngelDisplaySystem | Also add PositionTween: replace BasePosition write with Tween.Target |
| QueuedEventsDisplaySystem | Direct replacement |
| EquippedWeaponDisplaySystem | Direct replacement |
| LocationSelectDisplaySystem | Direct replacement |
| PointOfInterestDisplaySystem | Direct replacement |
| CustomizeButtonDisplaySystem | Direct replacement (entity creation + updates) |
| AchievementButtonDisplaySystem | Direct replacement (entity creation + updates) |
| AssignedBlockCardsDisplaySystem | Already writes Position; remove any BasePosition refs |
| AmbushDisplaySystem | Delete ResetAnchorParallax() entirely; just write Position |
| DeckManagementSystem | Replace BasePosition resets with Position-only resets |

#### Category B: Move Layout from Draw to Update (10 systems)

Extract layout position computation from Draw into Update. Draw reads `Position` for rendering but never writes it.

| System | Complexity | Notes |
|--------|-----------|-------|
| EquipmentDisplaySystem | Low | Move per-item BasePosition write to Update |
| EndTurnDisplaySystem | Low | Move button position write to Update |
| MedalDisplaySystem | Low | Move per-medal BasePosition write to Update |
| EnemyDamageMeterDisplaySystem | Medium | Entity creation with BasePosition in Draw |
| DialogDisplaySystem | Medium | EndButton creation in Draw |
| EnemyAttackDisplaySystem.Draw.cs | Medium | Banner anchor BasePosition in Draw |
| QuestRewardModalDisplaySystem | Medium | Proceed button creation in Draw |
| QuestTribulationDisplaySystem | Low | Move Position write to Update |
| ForSaleDisplaySystem | High | Complex: extracts parallax offset manually. In new design, just write Position in Update; all manual offset logic disappears |
| SkipPledgeDisplaySystem | Low | Consolidate writes to Update only |

#### Category C: Entity Creation (EntityFactory + inline)

Replace `new Transform { Position = pos, BasePosition = pos }` with `new Transform { Position = pos }`. Covers EntityFactory.cs and ~10 inline creation sites.

### 6. Data Flow Summary

**System without tween:**
```
Layout (Update)          ParallaxLayerSystem (Update)       Draw
writes Position    ->    detects write, anchors,       ->   reads Position
                         applies offset to Position         renders there
```

**System with tween:**
```
Layout (Update/Draw)     TweenSystem (Update)    ParallaxLayerSystem (Update)    Draw
sets Tween.Target  ->    lerps Current,      ->  detects write, anchors,    ->   reads Position
                         writes Position         applies offset to Position      renders there
```

**The contract:**
- Layout systems write `Position` (or `Tween.Target` for animation) — that's it
- `ParallaxLayerSystem` is invisible to all other systems
- `PositionTweenSystem` is independent of parallax — they compose transparently

## CLAUDE.md Update

Update the Parallax System section to reflect the new contract:

> The `ParallaxLayerSystem` is fully agnostic — external systems cooperate with it only through `Transform.Position`, never by reading or writing `ParallaxLayer` fields directly. Layout systems freely write `Position` every frame to assert the entity's base; the parallax system detects external writes, derives the anchor, and applies its offset. No system should reference `ParallaxLayer.Anchor`, `AnchorInitialized`, `LastWrittenPosition`, or any other internal parallax state.
