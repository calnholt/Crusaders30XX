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
- Card gameplay remains immediate after validation and cost payment, matching current `CardPlaySystem` behavior. Card visual queue events serialize presentation only; they must not delay `CardBase.OnPlay`, HP/resource/passive mutation, AP spend, tracking, or card movement.
- Recipe-enabled cards run legacy card animation and modular FX in parallel when a legacy animation exists, then the card presentation queue waits for all required visual signals before moving to the next queued visual.
- Enemy attacks still resolve damage/effects at the authoritative impact signal. For modular enemy attacks, the modular coordinator publishes that impact signal.
- Recipe-enabled equipment and medal activations use trigger-queued activation wrappers. When the queued activation resolves, gameplay activation and modular visual start happen in the same queue step, then the trigger queue waits for modular visual completion.
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
- Current card gameplay is immediate after validation and cost payment. The queued card animation events do not suspend the `OnPlay` method; `OnPlay`, HP/passive/resource changes, AP spend, tracking, and card movement happen immediately after the visual queue events are enqueued.
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
    ClawSlash,
    Bite,
    RockBlast,
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

`ClawSlash`, `Bite`, and `RockBlast` come from the mockup additions. They are general visual primitives and must not force target selection by themselves. Recipes decide target intent through `VisualEffectTargetRole`.

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

Use immutable copies internally. Prefer a private array or `ImmutableArray<VisualEffectModule>` exposed as `IReadOnlyList<VisualEffectModule>`. Normalize duplicate modules away while preserving first occurrence order; use `Intensity` and `ParticleMultiplier` for amplification instead of repeated enum entries. If this is implemented with mutable setters instead of `init`, all request paths must clone before use.

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
public static VisualEffectRecipe EnemyClawSlash();
public static VisualEffectRecipe EnemyBite();
public static VisualEffectRecipe EnemyRockBlast();
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
| `EnemyClawSlash` | `SnapImpact` | `Player` | `ClawSlash`, `HitFlash`, `Debris`, `SlashBand`, `Shake` |
| `EnemyBite` | `HeavyImpact` | `Player` | `Bite`, `HitFlash`, `RedVignette`, `Shake`, `HitStop` |
| `EnemyRockBlast` | `HeavyImpact` | `Player` | `RockBlast`, `Ring`, `Debris`, `SmokeBlobs`, `HitFlash`, `Shockwave`, `Shake`, `PunchZoom`, `HitStop` |

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
    public Entity? Source { get; init; }
    public Entity? Target { get; init; }
    public VisualEffectSourceKind SourceKind { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public string ContextId { get; init; } = string.Empty;
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
- Factory methods clone recipes before assigning them to requests. The coordinator clones again when creating active effects so it can bake global debug multipliers into the active recipe.
- `VisualEffectRequested` is immutable after construction.
- `Source` and `Target` may be null at the event boundary, especially for debug preview requests. Accepted `ActiveVisualEffect` instances require non-null concrete source and target anchors. No fallback screen anchors are allowed.
- `ContextId` is used by real enemy attack requests only. The coordinator publishes `EnemyAttackImpactNow { ContextId }` at modular impact for accepted non-preview enemy attack requests.
- Completion events are visual timing events only. They do not imply gameplay success.
- Preview completion events must not be consumed by gameplay queue waits unless the wait was created for the same request ID.
- If the coordinator rejects a preview request, it logs quietly and publishes no lifecycle events.
- If the coordinator rejects a non-preview request that has already been published, it logs loudly and immediately publishes `VisualEffectImpactReached` and `VisualEffectCompleted` for that request ID. If the rejected request is an enemy attack with a non-empty `ContextId`, it also publishes `EnemyAttackImpactNow` for that context. This is an emergency anti-deadlock guard; normal callers should avoid publishing invalid real requests.

---

## 7. Queue Contracts

Create:

```text
ECS/Scenes/BattleScene/QueuedStartVisualEffect.cs
ECS/Scenes/BattleScene/QueuedWaitVisualEffectImpact.cs
ECS/Scenes/BattleScene/QueuedWaitVisualEffectComplete.cs
ECS/Scenes/BattleScene/QueuedStartCardVisuals.cs
ECS/Scenes/BattleScene/QueuedWaitCardVisuals.cs
ECS/Scenes/BattleScene/QueuedStartEnemyAttackVisuals.cs
ECS/Scenes/BattleScene/QueuedWaitEnemyAttackVisuals.cs
ECS/Scenes/BattleScene/QueuedActivateEquipmentWithVisual.cs
ECS/Scenes/BattleScene/QueuedActivateMedalWithVisual.cs
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

No modular visual wait has timeout behavior. Waits ignore non-matching request IDs and remain waiting until the matching event is published. This matches current queue style. Existing `EventQueue.Clear()` has no cancellation/dispose hook, so waits unsubscribe on normal completion only; document this as an existing queue limitation rather than extending queue lifecycle in this plan.

### 7.4 Card visual start and wait wrappers

Create a small enum for card compatibility visuals:

```csharp
public enum CardLegacyVisualKind
{
    None,
    Attack,
    Buff
}
```

`CardPlaySystem` maps `CardBase.Animation` to this enum. Keep string interpretation localized there.

`QueuedStartCardVisuals`:

- Constructor accepts `CardLegacyVisualKind legacyKind`, optional `VisualEffectRequested modularRequest`, and the buff target flag.
- Publishes the legacy start event and modular request in the same `StartResolving` so they start in parallel.
- For `Attack`, publishes `StartPlayerAttackAnimation`.
- For `Buff`, publishes `StartBuffAnimation { TargetIsPlayer = true }`, preserving current behavior.
- Publishes the modular request if non-null.
- Completes immediately.

`QueuedWaitCardVisuals`:

- Constructor accepts the same `CardLegacyVisualKind`, optional modular request ID, and buff target flag.
- If `legacyKind == Attack`, waits for `PlayerAttackImpactNow`.
- If `legacyKind == Buff`, waits for `BuffAnimationComplete { TargetIsPlayer = true }`.
- If a modular request ID exists, waits for `VisualEffectCompleted` with that request ID.
- Completes only after all required signals have occurred, in any order.
- Cards with a modular recipe but no legacy animation wait only for modular completion.

### 7.5 Enemy visual start and wait wrappers

`QueuedStartEnemyAttackVisuals`:

- Constructor accepts `contextId` and a fully built `VisualEffectRequested`.
- Publishes `StartEnemyAttackAnimation { ContextId = contextId, SuppressImpactEvent = true, SuppressSfx = true }`.
- Publishes the modular request in the same `StartResolving`.
- Completes immediately.

`QueuedWaitEnemyAttackVisuals`:

- Constructor accepts `contextId` and modular request ID.
- Waits for `VisualEffectImpactReached` matching the modular request ID.
- Waits for `EnemyAttackVisualComplete` matching the context ID.
- Completes only after both signals have occurred, in any order.

Extend `StartEnemyAttackAnimation` and add:

```csharp
public bool SuppressImpactEvent { get; init; }
public bool SuppressSfx { get; init; }
```

Add:

```csharp
public sealed class EnemyAttackVisualComplete
{
    public string ContextId { get; init; } = string.Empty;
}
```

When `SuppressImpactEvent` is true, `EnemyDisplaySystem` still moves the enemy portrait but does not publish `EnemyAttackImpactNow`. When `SuppressSfx` is true, it also skips the legacy enemy attack SFX. It publishes `EnemyAttackVisualComplete` when the lunge timer finishes.

### 7.6 Equipment and medal activation wrappers

Recipe-enabled equipment and medal activations enqueue trigger events instead of running directly.

`QueuedActivateEquipmentWithVisual`:

- Runs existing equipment activation gameplay inside `StartResolving`.
- Exact order: `OnActivate`, publish `EquipmentAbilityTriggered`, build/publish `VisualEffectRequested`, then wait for modular completion.
- If visual request creation fails, log and complete immediately after gameplay activation.
- Validation happens before enqueue only; the queued action assumes activation remains valid.

`QueuedActivateMedalWithVisual`:

- Runs existing medal activation gameplay inside `StartResolving`.
- Exact order: publish `MedalTriggered`, call `medal.Activate()`, build/publish `VisualEffectRequested`, then wait for modular completion.
- If visual request creation fails, log and complete immediately after gameplay activation.
- Do not add generic medal validation beyond the existing event path that emits `MedalActivateEvent`.

---

## 8. Runtime Request Resolution

Create a helper:

```text
ECS/Services/VisualEffectRequestFactory.cs
```

This service must be read-only. It may inspect entities and object data but must not publish events, enqueue events, mutate components, or change singleton state.

Recommended signatures:

```csharp
public static VisualEffectRequested? ForCard(
    EntityManager entityManager,
    Entity cardEntity,
    VisualEffectRecipe recipe,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested? ForEquipment(
    EntityManager entityManager,
    Entity equipmentEntity,
    VisualEffectRecipe recipe,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested? ForMedal(
    EntityManager entityManager,
    Entity medalEntity,
    VisualEffectRecipe recipe,
    bool isPreview = false);
```

```csharp
public static VisualEffectRequested? ForEnemyAttack(
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

- `VisualEffectTargetRole.Enemy`: prefer the named `"Enemy"` entity when active and it has an `Enemy` component, then fall back to first active `Enemy`.
- `VisualEffectTargetRole.Player`: first `Player`.
- `VisualEffectTargetRole.Self`: player actor for card/equipment/medal requests; enemy actor for enemy attack requests.
- `VisualEffectTargetRole.Opponent`: infer ownership from `VisualEffectSourceKind`. `EnemyAttack` means opponent is player; `Card`, `Equipment`, and `Medal` mean opponent is enemy.

Source resolution:

- Cards use the card entity as source when it has a valid `Transform`; otherwise use the player actor.
- Equipment and medals use the player actor as source, not the UI item entity.
- Enemy attacks use the enemy actor as source.
- Debug card/equipment/medal previews use player source and recipe target. Debug enemy attack previews use enemy source and player target.

Real gameplay factory methods return `null` if required source or target resolution fails. Callers must not publish a null request or enqueue waits for it.

`ForDebugPreview` attempts the same real battle source/target resolution, but still returns a preview request if entities are missing. The coordinator is the final accept/reject gate. If battle actors are missing, preview playback is a no-op with quiet logging. Do not use fallback screen anchors.

---

## 9. Gameplay Integration

### 9.1 Card play

Modify `CardPlaySystem` only at the existing animation enqueue point.

Current behavior:

- If `card.Animation == "Attack"`, enqueue player attack animation and wait impact.
- Else if `card.Animation == "Buff"`, enqueue buff animation and wait completion.
- Then call `card.OnPlay` immediately in the same method. Existing queued visual events serialize presentation, not gameplay mutation.

New behavior:

1. If `card.VisualEffectRecipe != null`:
   - Build a `VisualEffectRequested` through `VisualEffectRequestFactory.ForCard`.
   - If request creation succeeds, map `card.Animation` to `CardLegacyVisualKind`.
   - Enqueue `QueuedStartCardVisuals` so the legacy card visual and modular request start in parallel.
   - Enqueue `QueuedWaitCardVisuals`.
   - Continue immediately to `OnPlay`; do not wait inside `CardPlaySystem`.
   - If request creation fails, skip all queued card visuals for that recipe-enabled card, including legacy attack/buff animation, log, and continue immediately to `OnPlay`.
2. Else run existing string animation fallback unchanged.

Classification rule for v1:

- If `card.Animation` is `"Attack"` and a modular request exists, `QueuedWaitCardVisuals` waits for both `PlayerAttackImpactNow` and modular `VisualEffectCompleted`.
- If `card.Animation` is `"Buff"` and a modular request exists, `QueuedWaitCardVisuals` waits for both `BuffAnimationComplete { TargetIsPlayer = true }` and modular `VisualEffectCompleted`.
- If `card.Animation` is empty but a modular request exists, `QueuedWaitCardVisuals` waits for modular completion only.

Do not remove or reinterpret `CardBase.Animation` in v1.
Do not suppress `ModifyHpEvent`-driven legacy splash effects for recipe-enabled attack cards. Modular FX and existing splash effects may both play in v1.

### 9.2 Equipment activation

Modify `EquipmentManagerSystem.OnEquipmentActivate` after all existing validation succeeds:

- If `ActivationEffectRecipe == null`, keep current direct behavior unchanged.
- If `ActivationEffectRecipe != null`, enqueue `QueuedActivateEquipmentWithVisual` on the trigger queue and return.
- The queued wrapper runs `equipment.Equipment.OnActivate`, publishes `EquipmentAbilityTriggered`, publishes modular visual request, and waits for modular completion.
- Validation happens only before enqueue.
- Do not decrement uses from visual code.
- Existing equipment use accounting remains wherever the equipment object or manager currently performs it.
- If visual request creation fails inside the queued wrapper, gameplay activation still runs and the wrapper completes immediately.

### 9.3 Medal activation

Modify `MedalManagerSystem.OnMedalActivate`:

1. Resolve the equipped medal.
2. If `ActivationEffectRecipe == null`, keep the current `EventQueueBridge.EnqueueTriggerAction` behavior unchanged.
3. If `ActivationEffectRecipe != null`, enqueue `QueuedActivateMedalWithVisual` on the trigger queue.
4. The queued wrapper publishes `MedalTriggered`, calls `medal.Activate()`, publishes modular visual request, and waits for modular completion.

Do not add generic medal validation beyond the existing `MedalActivateEvent` path. If visual request creation fails inside the queued wrapper, gameplay activation still runs and the wrapper completes immediately.

### 9.4 Enemy attack

Modify the enemy attack animation start path where `QueuedStartEnemyAttackAnimation(ctx)` is currently enqueued.

New behavior:

1. Resolve current planned attack definition for `ctx`.
2. If `AttackEffectRecipe != null`:
   - Build request with source enemy, target player, source kind `EnemyAttack`, source ID attack ID.
   - Include `ContextId = ctx` in the request.
   - Enqueue `QueuedStartEnemyAttackVisuals` so visual-only legacy lunge and modular FX start in parallel.
   - Enqueue `QueuedWaitEnemyAttackVisuals`, which waits for modular impact and `EnemyAttackVisualComplete`.
   - The modular coordinator publishes the single authoritative `EnemyAttackImpactNow { ContextId = ctx }` at modular impact.
3. Else use existing `QueuedStartEnemyAttackAnimation(ctx)` and `QueuedWaitImpactEvent(ctx)`.

If modular request creation fails before publishing, fall back to existing legacy-only enemy animation and wait so enemy damage still resolves through `EnemyAttackImpactNow`.

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
- Reject requests whose concrete source/target anchors cannot be resolved. Preview rejection logs quietly. Non-preview rejection logs loudly and publishes emergency lifecycle events as described in section 6.
- Create `ActiveVisualEffect` instances.
- Tick elapsed time.
- Publish `VisualEffectImpactReached` once when elapsed crosses impact time.
- Publish `VisualEffectCompleted` once when elapsed reaches duration.
- For accepted non-preview enemy attack requests with non-empty `ContextId`, publish `EnemyAttackImpactNow` once at modular impact.
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
    public string ContextId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
```

Anchor resolution:

- Prefer `PortraitInfo.LastDrawCenter` when present and non-zero.
- Fall back to `Transform.Position`.
- For card source, use card `Transform.Position`.
- Do not use fallback screen-space anchors for preview or gameplay. If anchors cannot be resolved, reject the request.
- `PortraitInfo.LastDrawCenter` may include the current battle presentation transform. This is acceptable; effects spawned during shake/punch may capture the currently drawn portrait position.
- Active effect anchors are captured once at creation time and are not updated every frame.
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
| `MaxConcurrentEffects` | `16` | Integer |

The coordinator bakes global intensity and particle multipliers into the cloned active recipe when creating `ActiveVisualEffect`. Changing debug multipliers affects new effects only.

Capacity behavior:

- Preview requests may be rejected when at capacity.
- If a real request arrives at capacity, evict the oldest preview if one exists.
- Do not drop real gameplay requests because queues may be waiting on their lifecycle events.

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
- Shake and punch write a battle presentation transform that affects player and enemy portrait drawing only. They do not transform modular primitive/particle FX, HUD, cards, equipment, medals, or gameplay transforms.
- Do not mutate actor `Transform.Position`.
- Hit-stop freezes only modular effect visual sampling for its duration; it must not pause game simulation.

Create component:

```csharp
public sealed class BattlePresentationTransform : IComponent
{
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public Vector2 Scale { get; set; } = Vector2.One;
}
```

`ModularEffectScreenDisplaySystem` owns this component in v1:

- Ensure a dedicated named entity `BattlePresentation` exists.
- Rely on `EntityManager.CreateEntity` scene auto-tagging.
- Reset `BattlePresentationTransform` to identity at the start of its update, then accumulate shake/punch from active effects.
- Register the system in `SystemUpdatePhase.Presentation` before `PlayerDisplaySystem` and `EnemyDisplaySystem` update/draw consumers.
- `PlayerDisplaySystem` and `EnemyDisplaySystem` read the component during `Draw()` and apply it to portrait pixels and `PortraitInfo.LastDraw*`.
- `UIElement.Bounds` for portraits remains based on stable untransformed portrait placement so hover/click bounds do not jitter.

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
- `ClawSlash`
- `Bite`
- `RockBlast`
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
- Primitive FX are not affected by `BattlePresentationTransform`; they draw from captured stable anchors.

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
- Detect newly created `ActiveVisualEffect` components by `RequestId` and spawn particles once. Do not subscribe directly to `VisualEffectRequested`.
- Use deterministic per-request random seed from `RequestId.GetHashCode()` plus module offset.
- Particle count = base count * active recipe particle multiplier. The coordinator already baked in the global particle multiplier.
- Expire particles independently when lifetime ends.
- Particle FX are not affected by `BattlePresentationTransform`; they draw from captured stable anchors.

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
2. Register coordinator, screen, primitive, and particle systems in `SystemUpdatePhase.Presentation`, before existing display systems that read their output.
3. Coordinator advances elapsed and publishes impact/completion before display systems read active effects for the current frame.
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
- `ModularEffectScreenDisplaySystem.Update` must happen before portrait draw so `BattlePresentationTransform` is ready, but its `Draw` still happens after actor-space FX.

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
- Debug actions may publish preview requests even when battle actors are missing. The coordinator rejects those preview requests quietly. Do not use fallback anchors.

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

If required battle entities do not exist, preview playback is a no-op after coordinator rejection.

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

Wire representative enemy attack recipes:

| Enemy attack | Recipe |
| --- | --- |
| `BoneStrike` or `TrainingStrike` | `VisualEffectPresets.EnemySlash()` |
| A claw-themed attack such as `ScorchingClaw` or `FrozenClaw` | `VisualEffectPresets.EnemyClawSlash()` |
| A bite/fang/maw attack such as `VelvetFangs` or `RazorMaw` | `VisualEffectPresets.EnemyBite()` |
| A rock/sand impact attack such as `SandBlast` or `SandPound` | `VisualEffectPresets.EnemyRockBlast()` |

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
- Add combined card/enemy start and wait wrappers.
- Add trigger-queued equipment/medal activation wrappers.
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
- Publish `EnemyAttackImpactNow` at modular impact for accepted real enemy attack requests.
- Emergency-complete rejected real requests.
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
- Include `ClawSlash`, `Bite`, and `RockBlast`.
- Add `BattlePresentationTransform` ownership in the screen display system.
- Apply intensity, particle multiplier, and direction sign.
- Use existing factories or add cached primitive helpers only when needed.
- Add debug editables and debug replay actions where helpful.

### Step 7 - Battle registration and draw order

File:

- `ECS/Scenes/BattleScene/BattleSceneSystem.cs`

Tasks:

- Instantiate systems.
- Add modular systems to the world in `SystemUpdatePhase.Presentation` before existing presentation consumers.
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
- Ensure card gameplay remains immediate.
- Ensure recipe-enabled equipment and medal activations use trigger queue wrappers and wait for modular completion after gameplay activation.

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
- Keep existing `Animation` values in place.
- For cards, legacy animation and modular FX intentionally run in parallel for recipe-enabled objects.

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
| `Shake` | Portrait-only draw offset sampled from keyframes; visual only. |
| `PunchZoom` | Portrait-only visual scale pulse around screen center; visual only. |
| `HitStop` | Hold modular FX sample time briefly; do not pause game update. |

### 16.2 Primitive modules

| Module | Behavior |
| --- | --- |
| `SwordArc` | Fast tapered slash from source side through impact. |
| `CrossSlash` | Two delayed slash bars crossing at impact. |
| `ClawSlash` | Three fast parallel red-white slash streaks at impact. |
| `Bite` | Closing top/bottom fang impression with red impact ring. |
| `RockBlast` | Circular stone burst with chunk fragments and dusty impact center. |
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
- Duplicate recipe modules are normalized away.
- Timing profiles resolve expected durations and impact times.
- Request factory resolves player/enemy targets correctly.
- Debug preview request sets `IsPreview = true`.
- Queue waits ignore non-matching request IDs.
- Queue waits complete on matching impact/completion.
- Queue waits do not have timeout behavior.
- `QueuedWaitCardVisuals` completes only after every required legacy/modular signal arrives.
- `QueuedWaitEnemyAttackVisuals` completes only after modular impact and legacy visual completion arrive.
- Coordinator publishes impact exactly once.
- Coordinator publishes completion exactly once.
- Coordinator publishes `EnemyAttackImpactNow` at modular impact for accepted real enemy attack requests.
- Coordinator emergency-completes rejected non-preview requests.
- Coordinator quietly rejects invalid preview requests without lifecycle events.
- Coordinator removes completed active effect entities.
- Direction sign is `1` for player-to-enemy and `-1` for enemy-to-player.
- `BattlePresentationTransform` affects portrait draw data but not portrait `UIElement.Bounds`.
- Debug action list reflection ignores invalid signatures.
- Debug action list invokes enabled actions only.

### 17.2 Gameplay path tests

Add or extend tests:

- Card with modular attack recipe runs `OnPlay` immediately while queuing legacy attack and modular FX in parallel.
- Card with modular buff recipe runs `OnPlay` immediately while queuing legacy buff and modular FX in parallel.
- Recipe-enabled card with failed modular request creation skips queued card visuals but still runs gameplay immediately.
- Card without modular recipe still uses legacy animation branch.
- Equipment activation with recipe enqueues a trigger activation, calls `OnActivate` exactly once, publishes `EquipmentAbilityTriggered`, starts modular FX, and waits for completion.
- Medal activation with recipe enqueues a trigger activation, publishes `MedalTriggered`, calls `Activate` exactly once, starts modular FX, and waits for completion.
- Enemy attack with recipe starts visual-only legacy lunge and modular FX in parallel, publishes damage impact from modular impact only, and waits for modular impact plus legacy visual completion before advancing.
- Enemy attack with recipe falls back to legacy-only animation if request creation fails before publish.
- Debug preview of card/equipment/medal/enemy attack does not call gameplay callbacks.

### 17.3 Manual verification

Run:

```bash
dotnet run -- test-fight hammer skeleton hard
```

Verify:

- `Forge Strike` effect plays if available in hand.
- `Sword` or `Strike` slash effect mirrors toward enemy.
- Enemy attack modular slash/claw/bite/rock effects mirror toward player.
- Recipe-enabled card gameplay state changes immediately when played, even while visuals continue.
- Recipe-enabled equipment/medal activations stall later trigger queue work until modular visual completion.
- Debug menu has:
  - `Card Modular Effects`
  - `Equipment Modular Effects`
  - `Medal Modular Effects`
  - `Enemy Attack Modular Effects`
- Debug buttons play effects without changing HP, AP, cards, equipment uses, medal counters, passives, or battle phase when battle actors exist.
- Debug buttons quietly no-op when required battle actors are missing.
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
- Recipe-enabled card gameplay remains immediate, while card presentation queues legacy and modular visuals in parallel and waits for required visual signals.
- Recipe-enabled equipment and medal activations are trigger-queued and wait for modular completion after gameplay activation.
- Recipe-enabled enemy attack sequencing starts visual-only legacy lunge and modular FX in parallel, resolves damage at modular impact, and waits for modular impact plus legacy visual completion before advancing.
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
| `ECS/Scenes/BattleScene/QueuedStartCardVisuals.cs` | Start legacy card visual and modular request in parallel. |
| `ECS/Scenes/BattleScene/QueuedWaitCardVisuals.cs` | Wait for required legacy card signal plus modular completion. |
| `ECS/Scenes/BattleScene/QueuedStartEnemyAttackVisuals.cs` | Start visual-only enemy lunge and modular request in parallel. |
| `ECS/Scenes/BattleScene/QueuedWaitEnemyAttackVisuals.cs` | Wait for modular impact plus enemy visual completion. |
| `ECS/Scenes/BattleScene/QueuedActivateEquipmentWithVisual.cs` | Trigger-queued recipe-enabled equipment activation. |
| `ECS/Scenes/BattleScene/QueuedActivateMedalWithVisual.cs` | Trigger-queued recipe-enabled medal activation. |
| `ECS/Diagnostics/DebugAttributes.cs` | Add list-backed debug action annotation. |
| `ECS/Scenes/BattleScene/DebugMenuSystem.cs` | Render list-backed debug actions. |
| `ECS/Scenes/BattleScene/Debug/*ModularEffectsDebugSystem.cs` | Add four debug provider systems. |
| `ECS/Objects/Cards/CardBase.cs` | Add recipe property. |
| `ECS/Objects/Equipment/EquipmentBase.cs` | Add activation recipe property. |
| `ECS/Objects/Medals/MedalBase.cs` | Add activation recipe property. |
| `ECS/Objects/Enemies/EnemyAttackBase.cs` | Add attack recipe property. |
| `ECS/Scenes/BattleScene/CardPlaySystem.cs` | Add modular branch before legacy animation fallback. |
| `ECS/Scenes/BattleScene/EquipmentManagerSystem.cs` | Enqueue recipe-enabled activation wrapper after validation. |
| `ECS/Scenes/BattleScene/MedalManagerSystem.cs` | Enqueue recipe-enabled activation wrapper. |
| `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs` | Use modular queue branch for recipe-enabled attacks. |
| `ECS/Scenes/BattleScene/EnemyDisplaySystem.cs` | Support visual-only enemy lunge and `EnemyAttackVisualComplete`. |
| `ECS/Scenes/BattleScene/PlayerDisplaySystem.cs` | Apply battle presentation transform to portrait draw only. |
| `ECS/Scenes/BattleScene/BattleSceneSystem.cs` | Register and draw modular systems. |
| `ECS/Factories/EnemyAttackFactory.cs` | Add attack enumeration if missing. |

---

## 21. Open Technical Notes

These are implementation details, not product decisions.

- If primitive drawing requires cached polygons, add methods to `PrimitiveTextureFactory` and clear caches on `DeleteCachesEvent`.
- If debug list rendering becomes too large for `DebugMenuSystem`, extract internal helper methods inside the same system rather than creating a separate UI framework.
- If `HitStop` conflicts with queue timing, keep queue timing based on unpaused elapsed seconds and freeze only visual sampling.
- `EventQueue.Clear()` can drop an active wait without an unsubscribe hook. Modular waits follow existing queue wrapper style and unsubscribe on normal completion only. A future queue cancellation hook could clean this up, but do not add it in this plan.
- Card recipe visuals are additive to legacy card animation and existing `ModifyHpEvent`/`ApplyPassiveEvent`-driven displays in v1. Do not suppress legacy card splash/buff/lunge effects for migrated cards.
