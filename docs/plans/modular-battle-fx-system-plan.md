# Modular Battle FX System Implementation Plan

## Document Status

- **Status:** Approved design, ready for implementation.
- **Repository:** `Crusaders30XX`
- **Runtime:** .NET 8.0, MonoGame DesktopGL, ECS architecture.
- **Primary scene:** `SceneId.Battle`.
- **Mockup:** `mockups/card-fx-modular-animation-system-v1.html`
- **Required final verification after implementation:** `dotnet build` from the repository root.
- **Save compatibility:** None required. Recipes live on runtime object definitions and do not require save migration.

This document is intentionally comprehensive. It should be enough for an implementer to build the feature without making product or architecture decisions locally.

---

## 1. Objective

Create an additive v1 composable battle visual effects system for:

- Cards
- Equipment
- Medals
- Enemy attacks

Each object defines a code-owned visual effect recipe. A recipe chooses reusable visual modules, a timing profile, intensity, particle multiplier, and target intent. Runtime systems translate those recipes into cosmetic animation requests.

Display systems own animation state and rendering. Gameplay systems only publish events or enqueue queue wrappers. Visual effects must never mutate game state directly.

The system also adds debug menu playback lists:

- `Card Modular Effects`
- `Equipment Modular Effects`
- `Medal Modular Effects`
- `Enemy Attack Modular Effects`

Each debug tab lists object names and provides a button to play that object's configured modular effect as a preview. Preview playback is purely cosmetic and must not update gameplay state, queues, counters, save data, uses, HP, passives, tracking, or object callbacks.

---

## 2. Canonical Product Decisions

These decisions are closed.

### 2.1 Additive rollout

- Existing animation behavior remains as fallback.
- Cards with no modular recipe continue to use existing `CardBase.Animation` values such as `"Attack"` and `"Buff"`.
- Existing `StartPlayerAttackAnimation`, `StartBuffAnimation`, `StartEnemyAttackAnimation`, `SplashEffectAnimationDisplaySystem`, and `PlayerAnimationSystem` behavior remains valid.
- Objects opt into modular FX by setting a recipe property.
- v1 wires representative recipes only. Do not migrate every object in the first implementation.

### 2.2 Object-owned recipes

- Recipes are defined in code on object classes, not JSON.
- Add recipe properties to the base object classes.
- Reusable presets live in a central static preset class.
- Object constructors choose a preset and may adjust intensity, particle multiplier, timing profile, target intent, or modules.
- Recipes must be immutable or defensively cloned when requested so one object cannot mutate a shared preset for another object.

### 2.3 Presentation-only ownership

- Display systems own animation state.
- Services remain read-only helpers/calculators.
- Gameplay systems do not hold direct references to display systems.
- Cross-system behavior goes through `EventManager`, `EventQueue`, or `EventQueueBridge`.
- Draw functions do not mutate gameplay state.

### 2.4 Deterministic queue behavior

- Real gameplay use must preserve existing sequencing.
- Player attack cards must still wait until visual impact before `OnPlay` resolves.
- Buff/support cards must still wait for visual completion before `OnPlay` resolves.
- Enemy attacks must still wait for impact before damage/effects resolve.
- Debug playback must not enqueue gameplay rule events.

### 2.5 Mirroring

- Recipes are authored in canonical player-to-enemy orientation.
- Runtime mirroring flips horizontal direction automatically when the target is on the opposite side.
- Objects must not define separate left-facing and right-facing recipes.

### 2.6 Debug playback

- Debug playback uses the same rendering path as real modular effects.
- Debug playback marks requests with `IsPreview = true`.
- Debug playback may publish visual events only.
- Debug playback must not call:
  - `CardBase.OnPlay`
  - `EquipmentBase.OnActivate`
  - `MedalBase.Activate`
  - `EnemyAttackBase.OnAttackHit`
  - `EnemyAttackBase.OnAttackReveal`
  - `EnemyAttackBase.OnBlocksConfirmed`
  - HP/passive/resource mutation events
  - tracking events
  - save writes
  - event queue rule resolution for gameplay

---

## 3. Existing Context

### 3.1 Current animation paths

- `CardPlaySystem` checks `CardBase.Animation`.
  - `"Attack"` enqueues `QueuedStartPlayerAttackAnimation` then `QueuedWaitPlayerImpactEvent`.
  - `"Buff"` enqueues `QueuedStartBuffAnimation(true)` then `QueuedWaitBuffComplete(true)`.
- `PlayerAnimationSystem` listens for:
  - `StartPlayerAttackAnimation`
  - `StartBuffAnimation`
  - `StartDebuffAnimation`
  - `ModifyHpEvent`
- `EnemyDisplaySystem` listens for `StartEnemyAttackAnimation` and publishes `EnemyAttackImpactNow`.
- `EnemyAttackDisplaySystem` enqueues enemy attack animation and wait events during block confirmation.
- `SplashEffectAnimationDisplaySystem` reacts to `ModifyHpEvent` and `ApplyPassiveEvent`.
- `ShockwaveDisplaySystem` and `RectangularShockwaveDisplaySystem` already support shader-backed shockwaves through events.

### 3.2 Existing object surfaces

- `CardBase` has `Animation`, callbacks, text, and gameplay metadata.
- `EquipmentBase` has `OnActivate`, `CanActivate`, uses, slot, and color.
- `MedalBase` has `Activate`, `OnAcquire`, count state, and text.
- `EnemyAttackBase` has damage, condition metadata, callbacks, and progress overrides.

### 3.3 Existing debug menu

- `DebugMenuSystem` reflects registered systems with `[DebugTab]`.
- It displays `[DebugEditable]` members and `[DebugAction]` / `[DebugActionInt]` buttons.
- It currently has no annotation for dynamic action lists.
- The debug menu is already scrollable and cache-backed.

---

## 4. Non-Goals

- Do not remove legacy animation systems.
- Do not migrate all cards, equipment, medals, or enemy attacks in v1.
- Do not add JSON loaders for recipes.
- Do not persist recipes to saves.
- Do not add gameplay effects to visual systems.
- Do not make services mutate components, publish events, or change singleton state.
- Do not pass one system instance into another system constructor.
- Do not use `MouseState` or `GamePad`; debug menu continues through existing input services.
- Do not introduce a new scene-level UI framework for debug playback.
- Do not require shaders for the feature to work. Shader-backed modules should degrade by simply omitting shader distortion when shaders are disabled.

---

## 5. Target Data Contracts

Names are normative unless an existing namespace conflict requires a mechanical equivalent.

### 5.1 Enums

Create a visual effect model file such as:

```text
ECS/Data/VisualEffects/VisualEffectModels.cs
```

Recommended namespace:

```csharp
namespace Crusaders30XX.ECS.Data.VisualEffects;
```

Add:

```csharp
public enum VisualEffectModule
{
    WhiteWash,
    RedVignette,
    Shockwave,
    SlashBand,
    SmokeScreen,
    SwordArc,
    CrossSlash,
    HammerArc,
    CrossBloom,
    Ring,
    Halo,
    Beam,
    Rays,
    Shards,
    Debris,
    SmokeBlobs,
    Cracks,
    HitFlash,
    Shake,
    PunchZoom,
    HitStop
}
```

```csharp
public enum VisualEffectTimingProfile
{
    SnapImpact,
    HeavyImpact,
    HolyRise,
    RitualPulse,
    DefensiveLock,
    FlickerChaos
}
```

```csharp
public enum VisualEffectTargetRole
{
    Enemy,
    Player,
    Self,
    Opponent
}
```

```csharp
public enum VisualEffectSourceKind
{
    Card,
    Equipment,
    Medal,
    EnemyAttack,
    Debug
}
```

### 5.2 Recipe model

Add:

```csharp
public sealed class VisualEffectRecipe
{
    public string Id { get; init; } = string.Empty;
    public VisualEffectTimingProfile Timing { get; init; } = VisualEffectTimingProfile.SnapImpact;
    public VisualEffectTargetRole TargetRole { get; init; } = VisualEffectTargetRole.Enemy;
    public float Intensity { get; init; } = 1f;
    public float ParticleMultiplier { get; init; } = 1f;
    public IReadOnlyList<VisualEffectModule> Modules { get; init; } = Array.Empty<VisualEffectModule>();
}
```

Implement helper methods without mutating shared instances:

```csharp
public VisualEffectRecipe Clone();
public VisualEffectRecipe WithIntensity(float intensity);
public VisualEffectRecipe WithParticleMultiplier(float particleMultiplier);
public VisualEffectRecipe WithTarget(VisualEffectTargetRole targetRole);
public VisualEffectRecipe WithTiming(VisualEffectTimingProfile timing);
public VisualEffectRecipe WithModules(params VisualEffectModule[] modules);
```

Use immutable copies internally. If this is implemented with mutable setters instead of `init`, all request paths must clone before use.

### 5.3 Timing profile model

Create:

```csharp
public readonly struct VisualEffectTiming
{
    public float DurationSeconds { get; init; }
    public float ImpactTimeSeconds { get; init; }
    public float HitStopStartSeconds { get; init; }
    public float HitStopDurationSeconds { get; init; }
}
```

Create a resolver:

```csharp
public static class VisualEffectTimingProfileResolver
{
    public static VisualEffectTiming Resolve(VisualEffectTimingProfile profile);
}
```

Use mockup-derived defaults:

| Profile | Duration | Impact | Hit-stop start | Hit-stop duration |
| --- | ---: | ---: | ---: | ---: |
| `SnapImpact` | `0.56f` | `0.18f` | `0.13f` | `0.08f` |
| `HeavyImpact` | `0.84f` | `0.26f` | `0.20f` | `0.145f` |
| `HolyRise` | `1.10f` | `0.36f` | `0.0f` | `0.0f` |
| `RitualPulse` | `0.98f` | `0.30f` | `0.0f` | `0.0f` |
| `DefensiveLock` | `0.72f` | `0.22f` | `0.0f` | `0.0f` |
| `FlickerChaos` | `0.86f` | `0.18f` | `0.14f` | `0.08f` |

Clamp impact to `0..DurationSeconds`.

### 5.4 Presets

Create:

```text
ECS/Data/VisualEffects/VisualEffectPresets.cs
```

Add static factory methods:

```csharp
public static VisualEffectRecipe LightSlash();
public static VisualEffectRecipe HeavyHammer();
public static VisualEffectRecipe HolyStrike();
public static VisualEffectRecipe HolySupport();
public static VisualEffectRecipe DefensiveGuard();
public static VisualEffectRecipe BloodRitual();
public static VisualEffectRecipe EnemySlash();
public static VisualEffectRecipe EnemyHeavyImpact();
```

Each method returns a fresh recipe instance.

Preset defaults:

| Preset | Timing | Target | Modules |
| --- | --- | --- | --- |
| `LightSlash` | `SnapImpact` | `Enemy` | `SwordArc`, `HitFlash`, `Debris` |
| `HeavyHammer` | `HeavyImpact` | `Enemy` | `HammerArc`, `Ring`, `Debris`, `Cracks`, `HitFlash`, `Shockwave`, `Shake`, `PunchZoom`, `HitStop` |
| `HolyStrike` | `HolyRise` | `Enemy` | `CrossBloom`, `Beam`, `Rays`, `Ring`, `WhiteWash`, `HitFlash` |
| `HolySupport` | `HolyRise` | `Player` | `CrossBloom`, `Halo`, `Beam`, `Rays`, `WhiteWash` |
| `DefensiveGuard` | `DefensiveLock` | `Player` | `Ring`, `Halo`, `WhiteWash`, `PunchZoom` |
| `BloodRitual` | `RitualPulse` | `Self` | `RedVignette`, `Ring`, `SmokeBlobs`, `Rays` |
| `EnemySlash` | `SnapImpact` | `Player` | `CrossSlash`, `SlashBand`, `HitFlash`, `Shake` |
| `EnemyHeavyImpact` | `HeavyImpact` | `Player` | `Ring`, `Debris`, `Cracks`, `HitFlash`, `Shockwave`, `Shake`, `HitStop` |

### 5.5 Base object properties

Add imports rather than fully qualified names.

In `CardBase`:

```csharp
public VisualEffectRecipe VisualEffectRecipe { get; protected set; }
```

In `EquipmentBase`:

```csharp
public VisualEffectRecipe ActivationEffectRecipe { get; protected set; }
```

In `MedalBase`:

```csharp
public VisualEffectRecipe ActivationEffectRecipe { get; protected set; }
```

In `EnemyAttackBase`:

```csharp
public VisualEffectRecipe AttackEffectRecipe { get; protected set; }
```

The properties are nullable by convention. A null recipe means "use legacy fallback or no modular effect."

---

## 6. Event Contracts

Create or extend:

```text
ECS/Events/VisualEffectEvents.cs
```

Keep existing defeat pixel burst events intact.

Add:

```csharp
public sealed class VisualEffectRequested
{
    public Guid RequestId { get; init; } = Guid.NewGuid();
    public VisualEffectRecipe Recipe { get; init; }
    public Entity Source { get; init; }
    public Entity Target { get; init; }
    public VisualEffectSourceKind SourceKind { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsPreview { get; init; }
}
```

```csharp
public sealed class VisualEffectImpactReached
{
    public Guid RequestId { get; init; }
    public bool IsPreview { get; init; }
}
```

```csharp
public sealed class VisualEffectCompleted
{
    public Guid RequestId { get; init; }
    public bool IsPreview { get; init; }
}
```

Rules:

- `Recipe` must be cloned before being stored by any display/coordinator system.
- `Source` and `Target` may be null only for debug preview fallback. The coordinator must resolve battle defaults in that case.
- Completion events are visual timing events only. They do not imply gameplay success.
- Preview completion events must not be consumed by gameplay queue waits unless the wait was created for the same request ID.

---

## 7. Queue Contracts

Create:

```text
ECS/Scenes/BattleScene/QueuedStartVisualEffect.cs
ECS/Scenes/BattleScene/QueuedWaitVisualEffectImpact.cs
ECS/Scenes/BattleScene/QueuedWaitVisualEffectComplete.cs
```

### 7.1 Start wrapper

`QueuedStartVisualEffect`:

- Constructor accepts a fully built `VisualEffectRequested`.
- Publishes the request in `StartResolving`.
- Marks itself complete immediately.
- Stores the `RequestId` in `Payload`.

### 7.2 Impact wait wrapper

`QueuedWaitVisualEffectImpact`:

- Constructor accepts `Guid requestId`.
- Subscribes to `VisualEffectImpactReached`.
- Completes only when `RequestId` matches.
- Unsubscribes on completion.

### 7.3 Completion wait wrapper

`QueuedWaitVisualEffectComplete`:

- Constructor accepts `Guid requestId`.
- Subscribes to `VisualEffectCompleted`.
- Completes only when `RequestId` matches.
- Unsubscribes on completion.

Do not complete based only on `IsPreview` or source kind.

---

## 8. Runtime Request Resolution

Create a helper:

```text
ECS/Services/VisualEffectRequestFactory.cs
```

This service must be read-only. It may inspect entities and object data but must not publish events, enqueue events, mutate components, or change singleton state.

Recommended signatures:

```csharp
public static VisualEffectRequested ForCard(
    EntityManager entityManager,
    Entity cardEntity,
    VisualEffectRecipe recipe,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested ForEquipment(
    EntityManager entityManager,
    Entity equipmentEntity,
    VisualEffectRecipe recipe,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested ForMedal(
    EntityManager entityManager,
    Entity medalEntity,
    VisualEffectRecipe recipe,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested ForEnemyAttack(
    EntityManager entityManager,
    Entity enemyEntity,
    EnemyAttackBase attack,
    VisualEffectRecipe recipe,
    string contextId,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested ForDebugPreview(
    EntityManager entityManager,
    VisualEffectSourceKind sourceKind,
    string sourceId,
    string displayName,
    VisualEffectRecipe recipe);
```

Target resolution:

- `VisualEffectTargetRole.Enemy`: first active `Enemy`.
- `VisualEffectTargetRole.Player`: first `Player`.
- `VisualEffectTargetRole.Self`: source entity if available; otherwise player for card/equipment/medal previews and enemy for enemy attack previews.
- `VisualEffectTargetRole.Opponent`: enemy when source is player-owned; player when source is enemy-owned.

If resolution fails, return a request with null source/target and allow the coordinator to use screen-space defaults for debug preview only. Real gameplay requests with no target should be skipped by caller.

---

## 9. Gameplay Integration

### 9.1 Card play

Modify `CardPlaySystem` only at the existing animation enqueue point.

Current behavior:

- If `card.Animation == "Attack"`, enqueue player attack animation and wait impact.
- Else if `card.Animation == "Buff"`, enqueue buff animation and wait completion.
- Then call `card.OnPlay`.

New behavior:

1. If `card.VisualEffectRecipe != null`:
   - Build a `VisualEffectRequested` through `VisualEffectRequestFactory.ForCard`.
   - Enqueue `QueuedStartVisualEffect`.
   - If the card is attack-like, enqueue `QueuedWaitVisualEffectImpact`.
   - If the card is support/buff-like, enqueue `QueuedWaitVisualEffectComplete`.
   - Continue to `OnPlay` after the wait.
2. Else run existing string animation fallback unchanged.

Classification rule for v1:

- If `card.Animation` is `"Attack"`, wait for modular impact.
- If `card.Animation` is `"Buff"`, wait for modular completion.
- If `card.Animation` is empty but recipe exists, wait for completion.

Do not remove or reinterpret `CardBase.Animation` in v1.

### 9.2 Equipment activation

Modify `EquipmentManagerSystem.OnEquipmentActivate` after all validation succeeds and before or after `OnActivate` based on recipe target:

- For v1, publish modular effect immediately before `equipment.Equipment.OnActivate`.
- Do not wait unless a future equipment effect explicitly requires queue sequencing.
- Do not decrement uses from visual code.
- Existing `EquipmentAbilityTriggered` remains after gameplay activation.

The real path may publish:

```csharp
EventManager.Publish(request);
```

No queue wrapper is required for v1 equipment activation unless a specific equipment already uses event queue sequencing. Equipment object callbacks that currently enqueue `QueuedStartBuffAnimation` may remain until migrated one by one.

### 9.3 Medal activation

Modify `MedalManagerSystem.OnMedalActivate` inside the existing `EventQueueBridge.EnqueueTriggerAction` callback:

1. Resolve the equipped medal.
2. If it has `ActivationEffectRecipe`, publish `VisualEffectRequested`.
3. Continue existing `MedalTriggered` publish and `medal.Activate()`.

Do not wait for visual completion in v1. Medal gameplay behavior remains authoritative.

### 9.4 Enemy attack

Modify the enemy attack animation start path where `QueuedStartEnemyAttackAnimation(ctx)` is currently enqueued.

New behavior:

1. Resolve current planned attack definition for `ctx`.
2. If `AttackEffectRecipe != null`:
   - Build request with source enemy, target player, source kind `EnemyAttack`, source ID attack ID.
   - Enqueue `QueuedStartVisualEffect`.
   - Enqueue `QueuedWaitVisualEffectImpact`.
3. Else use existing `QueuedStartEnemyAttackAnimation(ctx)` and `QueuedWaitImpactEvent(ctx)`.

Preserve `QueuedResolveAttackEvent(ctx)` and advancement logic exactly after the wait.

---

## 10. Coordinator System

Create:

```text
ECS/Scenes/BattleScene/ModularEffectCoordinatorSystem.cs
```

Attributes:

```csharp
[DebugTab("Modular Effect Coordinator")]
```

System responsibilities:

- Subscribe to `VisualEffectRequested`.
- Clone and validate recipes.
- Resolve source and target anchors.
- Create `ActiveVisualEffect` instances.
- Tick elapsed time.
- Publish `VisualEffectImpactReached` once when elapsed crosses impact time.
- Publish `VisualEffectCompleted` once when elapsed reaches duration.
- Remove completed effects.
- Publish shockwave events at impact for requests with `VisualEffectModule.Shockwave`.
- Expose active effect snapshots to display systems without direct system references.

Because systems must not hold direct references to other systems, use one of these patterns:

1. Store active effect state on ECS entities with components, and display systems query those components.
2. Have display systems independently subscribe to `VisualEffectRequested` and duplicate only presentation timing state they own.

Preferred v1 pattern: ECS entities with components.

Create component:

```text
ECS/Components/VisualEffectComponents.cs
```

```csharp
public sealed class ActiveVisualEffect : IComponent
{
    public Entity Owner { get; set; }
    public Guid RequestId { get; set; }
    public VisualEffectRecipe Recipe { get; set; }
    public VisualEffectTiming Timing { get; set; }
    public Entity Source { get; set; }
    public Entity Target { get; set; }
    public Vector2 SourceAnchor { get; set; }
    public Vector2 TargetAnchor { get; set; }
    public Vector2 ImpactAnchor { get; set; }
    public int DirectionSign { get; set; } = 1;
    public float ElapsedSeconds { get; set; }
    public bool ImpactPublished { get; set; }
    public bool IsPreview { get; set; }
    public VisualEffectSourceKind SourceKind { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
```

Anchor resolution:

- Prefer `PortraitInfo.LastDrawCenter` when present and non-zero.
- Fall back to `Transform.Position`.
- For card source, use card `Transform.Position`.
- For debug preview source fallback:
  - player side: `(Game1.VirtualWidth * 0.30f, Game1.VirtualHeight * 0.42f)`
  - enemy side: `(Game1.VirtualWidth * 0.72f, Game1.VirtualHeight * 0.42f)`
- Impact anchor defaults to target anchor.

Mirroring:

```csharp
DirectionSign = TargetAnchor.X >= SourceAnchor.X ? 1 : -1;
```

For enemy-to-player, direction sign should normally be `-1`.

Cleanup:

- Remove all `ActiveVisualEffect` entities on `LoadSceneEvent`.
- Remove all active preview effects when leaving battle scene.
- Subscribe to `DeleteCachesEvent` only if the coordinator owns caches. Display systems should clear their own caches.

Debug editables:

| Property | Default | Notes |
| --- | ---: | --- |
| `GlobalIntensityMultiplier` | `1.0f` | `Step = 0.01f` |
| `GlobalParticleMultiplier` | `1.0f` | `Step = 0.01f` |
| `PreviewSourceXPercent` | `0.30f` | `Step = 0.01f` |
| `PreviewTargetXPercent` | `0.72f` | `Step = 0.01f` |
| `PreviewYPercent` | `0.42f` | `Step = 0.01f` |
| `MaxConcurrentEffects` | `16` | Integer |

---

## 11. Display Systems

Use multiple display systems so each owns its own animation/rendering logic.

### 11.1 Screen treatment display

Create:

```text
ECS/Scenes/BattleScene/ModularEffectScreenDisplaySystem.cs
```

Attributes:

```csharp
[DebugTab("Modular FX Screen")]
```

Draws modules:

- `WhiteWash`
- `RedVignette`
- `SlashBand`
- `SmokeScreen`
- `Shake`
- `PunchZoom`
- `HitStop`

Implementation notes:

- Fullscreen wash/vignette/smoke use `_pixel` strips, cached radial textures, or simple transparent overlays.
- Shake and punch affect only visual overlay placement or a screen-space draw offset owned by this display path.
- Do not mutate actor `Transform.Position`.
- Hit-stop freezes only modular effect visual sampling for its duration; it must not pause game simulation.

### 11.2 Primitive impact display

Create:

```text
ECS/Scenes/BattleScene/ModularEffectPrimitiveDisplaySystem.cs
```

Attributes:

```csharp
[DebugTab("Modular FX Primitives")]
```

Draws modules:

- `SwordArc`
- `CrossSlash`
- `HammerArc`
- `CrossBloom`
- `Ring`
- `Halo`
- `Beam`
- `Rays`
- `Cracks`
- `HitFlash`

Implementation notes:

- Use `_pixel`, existing texture factories, or new cached primitive helpers.
- Add primitive helper methods only when repeated or non-trivial.
- Hammer silhouette can be drawn from rectangles/polygons matching the mockup shape.
- Beam and slash gradients may use multi-strip alpha/color lerp.
- Rays can be approximated with triangles or radial strips if conic gradients are too expensive.
- All positions are derived from `ActiveVisualEffect` anchors and `DirectionSign`.

### 11.3 Particle display

Create:

```text
ECS/Scenes/BattleScene/ModularEffectParticleDisplaySystem.cs
```

Attributes:

```csharp
[DebugTab("Modular FX Particles")]
```

Draws modules:

- `Shards`
- `Debris`
- `SmokeBlobs`

Implementation notes:

- Particle state is owned by this system.
- Subscribe to `VisualEffectRequested` or detect newly created `ActiveVisualEffect` components and spawn particles once.
- Use deterministic per-request random seed from `RequestId.GetHashCode()` plus module offset.
- Particle count = base count * recipe particle multiplier * global particle multiplier.
- Expire particles independently when lifetime ends.

Base counts:

| Module | Base count |
| --- | ---: |
| `Shards` | `11` |
| `Debris` | `12` |
| `SmokeBlobs` | `7` |

### 11.4 Shockwave dispatch

Do not create a duplicate shockwave renderer.

The coordinator publishes existing:

- `ShockwaveEvent` for circular impact shockwaves.
- `RectangularShockwaveEvent` only if a future recipe explicitly requires rectangular bounds.

Defaults for `Shockwave` module:

```csharp
DurationSec = 0.5f;
MaxRadiusPx = 260f * intensity;
RippleWidthPx = 6f;
Strength = 0.02f * intensity;
ChromaticAberrationAmp = 0.003f * intensity;
ChromaticAberrationFreq = 2f;
ShadingIntensity = 0.15f * intensity;
```

If shaders are disabled, existing shockwave systems no-op. Other modules still render.

---

## 12. Draw Order

In `BattleSceneSystem`:

1. Instantiate and register modular FX systems near existing battle presentation systems.
2. Update coordinator before display systems in normal system order.
3. Draw order should be:
   - battle background
   - player and enemy portraits
   - pixel burst / actor defeat effects
   - modular primitive and particle actor-space FX
   - modular screen treatment overlays
   - active character indicator / enemy intent / battle HUD
   - hand/cards/equipment/medals
   - modal foregrounds and debug menu

Practical placement in current draw method:

- Draw primitive/particle systems after `EnemyDisplaySystem.Draw` and `PixelBurstDisplaySystem.Draw`.
- Draw screen overlays after actor-space FX and before HUD resource displays.
- Do not draw modular FX over pay-cost modal foregrounds or debug menu.

---

## 13. Debug Annotation And Menu Extension

### 13.1 New annotation

Extend `ECS/Diagnostics/DebugAttributes.cs`.

Add:

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class DebugActionListAttribute : Attribute
{
    public string DisplayName { get; }
    public int Order { get; set; }

    public DebugActionListAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}
```

Add a public model for returned actions:

```csharp
public sealed class DebugNamedAction
{
    public string Label { get; init; } = string.Empty;
    public Action Invoke { get; init; }
    public bool IsEnabled { get; init; } = true;
}
```

Allowed method signatures:

```csharp
IEnumerable<DebugNamedAction> MethodName()
IReadOnlyList<DebugNamedAction> MethodName()
List<DebugNamedAction> MethodName()
```

Ignore methods with parameters or incompatible return types.

### 13.2 Debug menu rendering

Modify `DebugMenuSystem`:

- Add a cache:

```csharp
private readonly Dictionary<Type, List<(string label, MethodInfo method, DebugActionListAttribute meta)>> _debugActionListsCache = new();
```

- Clear this cache in `TryInvalidateCachesOnSceneOrSystemsChange`.
- Discover methods with `DebugActionListAttribute`.
- Include list sections in panel height measurement.
- Render each list as:
  - Section header: attribute display name.
  - One row per returned `DebugNamedAction`.
  - Disabled rows drawn dim and not clickable.
- Invoke `DebugNamedAction.Invoke` only for enabled clicked rows.
- Catch and log invocation exceptions using the existing debug action error pattern.
- Keep labels ASCII-safe through existing `DrawStringClippedScaled`.
- Re-evaluate list methods each frame or at least when tab is active so newly assigned recipes appear without restarting. The method list reflection can be cached; returned actions should not be cached permanently.

Layout:

- Use existing `ButtonHeight`, `Spacing`, scroll, and culling behavior.
- Each action row uses the full panel width minus padding.
- Long labels are clipped with ellipsis.
- The menu should remain usable when a factory returns many objects.

### 13.3 Debug provider systems

Create four systems. Each is presentation/debug only and has no `GetRelevantEntities` workload.

```text
ECS/Scenes/BattleScene/Debug/CardModularEffectsDebugSystem.cs
ECS/Scenes/BattleScene/Debug/EquipmentModularEffectsDebugSystem.cs
ECS/Scenes/BattleScene/Debug/MedalModularEffectsDebugSystem.cs
ECS/Scenes/BattleScene/Debug/EnemyAttackModularEffectsDebugSystem.cs
```

Each system:

- Has a `[DebugTab]` with the exact tab name.
- Has one `[DebugActionList("Play Effects")]` method.
- Uses the corresponding factory to enumerate object definitions.
- Includes only objects with non-null recipes.
- Creates actions that call `EventManager.Publish(VisualEffectRequestFactory.ForDebugPreview(...))`.
- Does not enqueue gameplay events.
- Does not call object gameplay callbacks.

Factory enumeration:

- Cards: `CardFactory.GetAllCards()`
- Equipment: `EquipmentFactory.GetAllEquipment()`
- Medals: `MedalFactory.GetAllMedals()`
- Enemy attacks: add `EnemyAttackFactory.GetAllAttacks()` if missing, or implement an equivalent read-only enumeration in the factory.

Debug label format:

```text
Name [id]
```

Examples:

```text
Forge Strike [forge_strike]
Saint Luke [st_luke]
Bone Strike [bone_strike]
```

Debug preview target behavior:

- Card previews: source player, target per recipe.
- Equipment previews: source player, target per recipe.
- Medal previews: source player, target per recipe.
- Enemy attack previews: source enemy, target player.

If battle entities do not exist, still publish preview with fallback screen anchors. The coordinator handles fallback coordinates for `IsPreview = true`.

---

## 14. Initial Recipe Wiring

Wire only representative recipes in v1.

### 14.1 Cards

| Card | Recipe |
| --- | --- |
| `ForgeStrike` | `VisualEffectPresets.HeavyHammer()` |
| `Sword` | `VisualEffectPresets.LightSlash()` |
| `Strike` | `VisualEffectPresets.LightSlash()` |
| `Smite` | `VisualEffectPresets.HolyStrike()` |

Optional if already visually useful and low risk:

| Card | Recipe |
| --- | --- |
| `Hammer` | `VisualEffectPresets.HeavyHammer().WithIntensity(0.9f)` |
| `DivineProtection` | `VisualEffectPresets.DefensiveGuard()` |
| `DowseWithHolyWater` | `VisualEffectPresets.HolySupport()` |

Do not wire every card in v1.

### 14.2 Equipment

Wire one equipment activation recipe:

| Equipment | Recipe |
| --- | --- |
| `HelmOfSeeing` or `PurgingBracers` | `VisualEffectPresets.DefensiveGuard()` |

Use the object that is easiest to trigger in current test fights. Do not alter its gameplay effect.

### 14.3 Medals

Wire one medal activation recipe:

| Medal | Recipe |
| --- | --- |
| `StLuke` or `StMichael` | `VisualEffectPresets.HolySupport()` |

Use the object that is easiest to trigger in current test fights. Do not alter its gameplay effect.

### 14.4 Enemy attacks

Wire one enemy attack recipe:

| Enemy attack | Recipe |
| --- | --- |
| `BoneStrike` or `TrainingStrike` | `VisualEffectPresets.EnemySlash()` |

Prefer an attack reachable with:

```bash
dotnet run -- test-fight hammer skeleton hard
```

---

## 15. Implementation Sequence

Follow this order to reduce compile churn and keep behavior testable.

### Step 1 - Data models and presets

Files:

- `ECS/Data/VisualEffects/VisualEffectModels.cs`
- `ECS/Data/VisualEffects/VisualEffectPresets.cs`

Tasks:

- Add enums, recipe model, timing model, timing resolver, and presets.
- Add clone/with helpers.
- Add unit tests for clone and presets.

### Step 2 - Base object recipe properties

Files:

- `ECS/Objects/Cards/CardBase.cs`
- `ECS/Objects/Equipment/EquipmentBase.cs`
- `ECS/Objects/Medals/MedalBase.cs`
- `ECS/Objects/Enemies/EnemyAttackBase.cs`

Tasks:

- Add nullable recipe properties.
- Add imports.
- Do not change callbacks or existing animation string behavior.

### Step 3 - Events and queue wrappers

Files:

- `ECS/Events/VisualEffectEvents.cs`
- `ECS/Scenes/BattleScene/QueuedStartVisualEffect.cs`
- `ECS/Scenes/BattleScene/QueuedWaitVisualEffectImpact.cs`
- `ECS/Scenes/BattleScene/QueuedWaitVisualEffectComplete.cs`

Tasks:

- Add event contracts.
- Add queue wrappers.
- Test waits by publishing matching and non-matching request IDs.

### Step 4 - Request factory

File:

- `ECS/Services/VisualEffectRequestFactory.cs`

Tasks:

- Implement read-only request creation.
- Resolve player/enemy/source/target.
- Clone recipes.
- Add tests for target role resolution.

### Step 5 - Coordinator and active component

Files:

- `ECS/Components/VisualEffectComponents.cs`
- `ECS/Scenes/BattleScene/ModularEffectCoordinatorSystem.cs`

Tasks:

- Subscribe to requests.
- Spawn active effect entities.
- Tick elapsed time.
- Publish impact and completion once.
- Dispatch shockwave at impact.
- Cleanup on scene load.
- Add debug editables.

### Step 6 - Display systems

Files:

- `ECS/Scenes/BattleScene/ModularEffectScreenDisplaySystem.cs`
- `ECS/Scenes/BattleScene/ModularEffectPrimitiveDisplaySystem.cs`
- `ECS/Scenes/BattleScene/ModularEffectParticleDisplaySystem.cs`

Tasks:

- Draw mockup modules at approximate fidelity.
- Apply intensity, particle multiplier, and direction sign.
- Use existing factories or add cached primitive helpers only when needed.
- Add debug editables and debug replay actions where helpful.

### Step 7 - Battle registration and draw order

File:

- `ECS/Scenes/BattleScene/BattleSceneSystem.cs`

Tasks:

- Instantiate systems.
- Add systems to world.
- Add draw calls in the required order.
- Ensure no direct system references are passed into other systems.

### Step 8 - Gameplay integration

Files:

- `ECS/Scenes/BattleScene/CardPlaySystem.cs`
- `ECS/Scenes/BattleScene/EquipmentManagerSystem.cs`
- `ECS/Scenes/BattleScene/MedalManagerSystem.cs`
- `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs`

Tasks:

- Add modular recipe branches.
- Keep legacy fallback unchanged.
- Ensure real gameplay waits use matching request IDs.
- Ensure equipment and medal visual requests do not mutate gameplay.

### Step 9 - Debug annotation and debug providers

Files:

- `ECS/Diagnostics/DebugAttributes.cs`
- `ECS/Scenes/BattleScene/DebugMenuSystem.cs`
- `ECS/Scenes/BattleScene/Debug/CardModularEffectsDebugSystem.cs`
- `ECS/Scenes/BattleScene/Debug/EquipmentModularEffectsDebugSystem.cs`
- `ECS/Scenes/BattleScene/Debug/MedalModularEffectsDebugSystem.cs`
- `ECS/Scenes/BattleScene/Debug/EnemyAttackModularEffectsDebugSystem.cs`
- `ECS/Factories/EnemyAttackFactory.cs` if attack enumeration is missing

Tasks:

- Add `DebugActionListAttribute` and `DebugNamedAction`.
- Extend debug menu reflection and rendering.
- Add four provider systems and register them in battle.
- Ensure debug preview publishes only `VisualEffectRequested` with `IsPreview = true`.

### Step 10 - Initial object recipes

Files:

- Representative card/equipment/medal/enemy attack object files.

Tasks:

- Add recipe assignments in constructors.
- Keep existing `Animation` values in place unless a specific duplicate visual is unacceptable.
- If duplicate legacy and modular visuals occur for a recipe-enabled object, the gameplay integration branch should prevent the legacy animation path for that object.

---

## 16. Rendering Details By Module

All module values are starting defaults. Add `[DebugEditable]` for hardcoded tuning values on display systems.

### 16.1 Screen modules

| Module | Behavior |
| --- | --- |
| `WhiteWash` | Radial bright wash centered on impact; fades in early and expands/fades out. |
| `RedVignette` | Fullscreen dark red vignette pulse; multiply-like approximation with alpha overlay. |
| `SlashBand` | Wide diagonal band crossing impact direction; mirrored with direction sign. |
| `SmokeScreen` | Several soft dark radial patches around impact; fade and drift upward. |
| `Shake` | Screen-space draw offset sampled from keyframes; visual only. |
| `PunchZoom` | Subtle visual scale pulse around screen center; visual only. |
| `HitStop` | Hold modular FX sample time briefly; do not pause game update. |

### 16.2 Primitive modules

| Module | Behavior |
| --- | --- |
| `SwordArc` | Fast tapered slash from source side through impact. |
| `CrossSlash` | Two delayed slash bars crossing at impact. |
| `HammerArc` | Dark hammer silhouette rotating into target. |
| `CrossBloom` | Expanding glowing cross at source or target based on recipe target. |
| `Ring` | Expanding circular ring at impact. |
| `Halo` | Ellipse above friendly target, rising and fading. |
| `Beam` | Vertical light beam centered on target. |
| `Rays` | Radial rays rotating and fading. |
| `Cracks` | Jagged red line segments radiating from impact. |
| `HitFlash` | Short white radial flash at impact. |

### 16.3 Particle modules

| Module | Behavior |
| --- | --- |
| `Shards` | Bright jagged particles scattering away from impact. |
| `Debris` | Dark rectangular/polygon particles scattering away from impact. |
| `SmokeBlobs` | Soft circles drifting upward from impact. |

---

## 17. Testing Plan

### 17.1 Unit tests

Add tests under:

```text
tests/Crusaders30XX.Tests/
```

Recommended test classes:

- `VisualEffectRecipeTests`
- `VisualEffectRequestFactoryTests`
- `ModularEffectCoordinatorSystemTests`
- `VisualEffectQueueTests`
- `DebugActionListTests`

Required scenarios:

- Presets return fresh instances.
- `WithIntensity` does not mutate original recipe.
- `WithParticleMultiplier` does not mutate original recipe.
- Timing profiles resolve expected durations and impact times.
- Request factory resolves player/enemy targets correctly.
- Debug preview request sets `IsPreview = true`.
- Queue waits ignore non-matching request IDs.
- Queue waits complete on matching impact/completion.
- Coordinator publishes impact exactly once.
- Coordinator publishes completion exactly once.
- Coordinator removes completed active effect entities.
- Direction sign is `1` for player-to-enemy and `-1` for enemy-to-player.
- Debug action list reflection ignores invalid signatures.
- Debug action list invokes enabled actions only.

### 17.2 Gameplay path tests

Add or extend tests:

- Card with modular attack recipe waits for visual impact before `OnPlay`.
- Card with modular buff recipe waits for visual completion before `OnPlay`.
- Card without modular recipe still uses legacy animation branch.
- Equipment activation with recipe still calls `OnActivate` exactly once.
- Medal activation with recipe still calls `Activate` exactly once.
- Enemy attack with recipe waits for visual impact before resolution.
- Debug preview of card/equipment/medal/enemy attack does not call gameplay callbacks.

### 17.3 Manual verification

Run:

```bash
dotnet run -- test-fight hammer skeleton hard
```

Verify:

- `Forge Strike` effect plays if available in hand.
- `Sword` or `Strike` slash effect mirrors toward enemy.
- Enemy attack modular effect mirrors toward player.
- Debug menu has:
  - `Card Modular Effects`
  - `Equipment Modular Effects`
  - `Medal Modular Effects`
  - `Enemy Attack Modular Effects`
- Debug buttons play effects without changing HP, AP, cards, equipment uses, medal counters, passives, or battle phase.
- Debug menu scrolls when lists exceed panel height.
- Shader disabled mode still shows non-shader modules:

```bash
dotnet run -- no-shaders
```

### 17.4 Final verification

After implementation:

```bash
dotnet build
```

Fix compile errors before handoff.

---

## 18. Acceptance Criteria

The feature is complete when:

- Representative objects can define modular effect recipes in code.
- Real gameplay requests modular effects for recipe-enabled objects.
- Legacy animation fallback still works for objects without recipes.
- Player attack and enemy attack sequencing still waits for impact.
- Buff/support sequencing still waits for completion where applicable.
- Effects mirror correctly between player-to-enemy and enemy-to-player.
- Debug menu exposes object-type effect tabs with play buttons.
- Debug playback is purely cosmetic and does not mutate game state.
- All new systems follow ECS event/component ownership rules.
- `dotnet build` succeeds.

---

## 19. Implementation Guardrails

- Use imports, not fully qualified names.
- Add `[DebugTab]` to systems with draw/debug controls.
- Add as many useful `[DebugEditable]` values as practical for animation tuning.
- Float text/display scales use `Step = 0.01f`.
- Draw functions must not update gameplay state.
- Do not read or write `ParallaxLayer` internals.
- Do not use `MouseState` or `GamePad`.
- Do not pass another system as a constructor parameter.
- Services must remain read-only.
- Preview effects must stay visually faithful to the real effect path.
- Keep all debug and display strings ASCII-only.

---

## 20. Likely File Map

| File | Action |
| --- | --- |
| `ECS/Data/VisualEffects/VisualEffectModels.cs` | Create models, enums, timing resolver. |
| `ECS/Data/VisualEffects/VisualEffectPresets.cs` | Create reusable recipe presets. |
| `ECS/Components/VisualEffectComponents.cs` | Create `ActiveVisualEffect`. |
| `ECS/Events/VisualEffectEvents.cs` | Add modular effect events without removing existing pixel burst events. |
| `ECS/Services/VisualEffectRequestFactory.cs` | Create read-only request builder. |
| `ECS/Scenes/BattleScene/ModularEffectCoordinatorSystem.cs` | Create request coordinator and lifecycle owner. |
| `ECS/Scenes/BattleScene/ModularEffectScreenDisplaySystem.cs` | Create screen overlay renderer. |
| `ECS/Scenes/BattleScene/ModularEffectPrimitiveDisplaySystem.cs` | Create primitive renderer. |
| `ECS/Scenes/BattleScene/ModularEffectParticleDisplaySystem.cs` | Create particle renderer. |
| `ECS/Scenes/BattleScene/QueuedStartVisualEffect.cs` | Create queue start wrapper. |
| `ECS/Scenes/BattleScene/QueuedWaitVisualEffectImpact.cs` | Create impact wait wrapper. |
| `ECS/Scenes/BattleScene/QueuedWaitVisualEffectComplete.cs` | Create completion wait wrapper. |
| `ECS/Diagnostics/DebugAttributes.cs` | Add list-backed debug action annotation. |
| `ECS/Scenes/BattleScene/DebugMenuSystem.cs` | Render list-backed debug actions. |
| `ECS/Scenes/BattleScene/Debug/*ModularEffectsDebugSystem.cs` | Add four debug provider systems. |
| `ECS/Objects/Cards/CardBase.cs` | Add recipe property. |
| `ECS/Objects/Equipment/EquipmentBase.cs` | Add activation recipe property. |
| `ECS/Objects/Medals/MedalBase.cs` | Add activation recipe property. |
| `ECS/Objects/Enemies/EnemyAttackBase.cs` | Add attack recipe property. |
| `ECS/Scenes/BattleScene/CardPlaySystem.cs` | Add modular branch before legacy animation fallback. |
| `ECS/Scenes/BattleScene/EquipmentManagerSystem.cs` | Publish activation recipe request. |
| `ECS/Scenes/BattleScene/MedalManagerSystem.cs` | Publish activation recipe request. |
| `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs` | Use modular queue branch for recipe-enabled attacks. |
| `ECS/Scenes/BattleScene/BattleSceneSystem.cs` | Register and draw modular systems. |
| `ECS/Factories/EnemyAttackFactory.cs` | Add attack enumeration if missing. |

---

## 21. Open Technical Notes

These are implementation details, not product decisions.

- If primitive drawing requires cached polygons, add methods to `PrimitiveTextureFactory` and clear caches on `DeleteCachesEvent`.
- If debug list rendering becomes too large for `DebugMenuSystem`, extract internal helper methods inside the same system rather than creating a separate UI framework.
- If preview fallback anchors make effects appear too high or low, tune only coordinator debug editables, not individual recipes.
- If `HitStop` conflicts with queue timing, keep queue timing based on unpaused elapsed seconds and freeze only visual sampling.
- If a recipe-enabled object still emits an old visual from inside its own callback, leave it unless it creates unacceptable duplication for the representative v1 objects. Remove duplicate object-local animation calls only for migrated objects.
