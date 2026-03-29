# Guard Mechanic Design Spec

## Context

Enemies need a defensive mechanic that creates strategic tension: guards absorb player damage but convert to aggression if left alive. This prototype tests the concept on a single enemy (Sentinel) with a system designed to generalize to all enemies later.

## Mechanic Overview

**Guard** is a queue of value-based shields on an enemy. Each guard has a numeric value (1..X).

### Guard Gain (Sentinel-specific)

At **PreBlock** phase, before the player sees each attack:

1. Roll conversion: 50% chance of 0 (no conversion)
2. Remaining 50% is uniform across 1..D (D = attack damage)
3. **Partial conversion** (amount < D): reduce attack damage by that amount, add guard(value) to queue
4. **Full conversion** (amount == D): skip the attack entirely, add guard(value) to queue, do not show the attack in `EnemyAttackDisplaySystem`, proceed to next attack

### Guard Consumption (Player Attacks Enemy)

When the player deals attack damage to an enemy with guards:

1. First guard in queue absorbs up to its value
2. Guard is **fully consumed** regardless of leftover (a 2-damage attack into a guard(5) destroys the guard, 0 damage through)
3. If attack damage exceeds guard value, excess carries to next guard in queue
4. Repeat until attack is fully absorbed or all guards consumed
5. Remaining damage (if any) continues through normal pipeline (Armor, Aegis, Shield, HP)

### Guard → Aggression (Enemy Turn Start)

At **EnemyStart** phase:

1. Count guards in queue
2. Publish `ApplyPassiveEvent { Type=Aggression, Delta=count }` (1 aggression per guard, regardless of guard value)
3. Clear the entire guard queue

## Architecture

### Data Layer

**New component** — `GuardQueue` in `ECS/Components/GuardComponents.cs`:

```csharp
public class GuardQueue : IComponent
{
    public Entity Owner { get; set; }
    public List<int> Queue { get; set; } = new();  // front = index 0
}
```

### Events

**New file** — `ECS/Events/GuardEvents.cs`:

- `AddGuardEvent { Entity Enemy, int Value }` — request to add a guard to the queue
- `GuardConsumedEvent { Entity Enemy, int GuardValue, int RemainingCount }` — fired when a guard is destroyed by player damage (for display animation)
- `GuardGainedEvent { Entity Enemy, int GuardValue }` — fired when a guard is added to the queue (for display animation)

### Damage Pipeline Integration

In `HpManagementSystem.OnModifyHpRequest`, **before** `GetPassiveDelta`:

```
if (target has GuardQueue with entries AND DamageType == Attack AND Delta < 0):
    Walk queue front-to-back:
        guard absorbs min(guardValue, remainingDamage)
        remove guard from queue (fully consumed)
        publish GuardConsumedEvent
        remainingDamage -= guardValue
        if remainingDamage <= 0: break
    e.Delta = -remainingDamage (or 0 if fully absorbed)
    if remainingDamage <= 0: return early
```

Guard absorbs **raw** incoming damage before Armor, Aegis, or Shield. This ensures guards are worth stripping.

### GuardManagementSystem

**New file** — `ECS/Scenes/BattleScene/GuardManagementSystem.cs`

Responsibilities:
- Subscribe to `AddGuardEvent`: push value onto `GuardQueue`, publish `GuardGainedEvent`
- Subscribe to `ChangeBattlePhaseEvent` at `EnemyStart`: count guards → publish aggression → clear queue
- Cleanup on `LoadSceneEvent` or `DeleteCachesEvent`

### GuardQueueDisplaySystem

**New file** — `ECS/Scenes/BattleScene/GuardQueueDisplaySystem.cs`

Renders guard pips above the enemy, **above** the intent pips (which sit at OffsetY=-210). All positioning via `DebugEditable` properties.

Display per guard: small pip/icon showing numeric value.

**Break animation** (on `GuardConsumedEvent`): consumed guard pip scales up 1.3x then fades to 0 over ~0.3s.

**Gain animation** (on `GuardGainedEvent`): new guard pip scales from 0 → 1 with brief overshoot over ~0.25s.

System attributes: `[DebugTab("Guard Queue Display")]` with `[DebugEditable]` on all positioning/sizing values.

## Sentinel Enemy

**New file** — `ECS/Objects/Enemies/Sentinel.cs`

### Stats

- Health: 16 + 2*(int)difficulty (Easy=16, Medium=18, Hard=20)
- No starting passives beyond the Guard mechanic

### OnStartOfBattle

Adds `GuardQueue` component to the enemy entity.

### Attack Patterns

Three patterns, chosen randomly each turn (equal weight):

1. **Sentinel Slam** — 1 hit, 9 damage
2. **Twin Strike** — 2 hits, 5 damage each
3. **Rapid Jabs** — 3 hits, 3 damage each

No additional effects (no conditions, no blocking restrictions).

Each attack's `OnAttackReveal` callback performs the guard conversion roll:
- 50% → 0 (no conversion)
- 50% → uniform 1..D
- Partial: reduce `AttackDefinition.Damage`, publish `AddGuardEvent`
- Full: remove planned attack from `AttackIntent.Planned`, publish `AddGuardEvent`. If no attacks remain, short-circuit to `EnemyEnd` (same pattern as Stun)

### Registration

- `EnemyFactory.cs`: add `"sentinel" => new Sentinel(difficulty)`
- `EnemyAttackFactory.cs`: register `sentinel_slam`, `twin_strike`, `rapid_jabs`
- `Game1.cs`: register `GuardManagementSystem` and `GuardQueueDisplaySystem`

## Files Summary

### New Files

| File | Purpose |
|------|---------|
| `ECS/Components/GuardComponents.cs` | `GuardQueue` component |
| `ECS/Events/GuardEvents.cs` | `AddGuardEvent`, `GuardConsumedEvent`, `GuardGainedEvent` |
| `ECS/Scenes/BattleScene/GuardManagementSystem.cs` | Guard lifecycle (add, consume→aggression, cleanup) |
| `ECS/Scenes/BattleScene/GuardQueueDisplaySystem.cs` | Guard display + break/gain animations |
| `ECS/Objects/Enemies/Sentinel.cs` | Enemy definition + 3 attack classes |

### Modified Files

| File | Change |
|------|--------|
| `ECS/Scenes/BattleScene/HpManagementSystem.cs` | Add `TryConsumeGuard` before `GetPassiveDelta` (~15 lines) |
| `ECS/Factories/EnemyFactory.cs` | Register Sentinel |
| `ECS/Factories/EnemyAttackFactory.cs` | Register 3 Sentinel attacks |
| `Game1.cs` | Register `GuardManagementSystem`, `GuardQueueDisplaySystem` |

## Verification

1. **Build**: `dotnet build` compiles without errors
2. **Run**: `dotnet run`, start a battle against the Sentinel
3. **Guard gain**: Observe guards appearing above intent pips during PreBlock phase — some attacks reduced, some fully converted
4. **Guard consumption**: Play attack cards targeting enemy — guards break with animation, damage absorbed correctly
5. **Guard → aggression**: Let enemy turn start with guards remaining — guards clear, aggression appears, next attack hits harder
6. **Full conversion skip**: When an attack fully converts, verify it's skipped (no attack banner shown) and next attack proceeds normally
7. **Edge case**: All attacks fully convert in a turn — verify enemy turn ends gracefully and transitions to player turn
