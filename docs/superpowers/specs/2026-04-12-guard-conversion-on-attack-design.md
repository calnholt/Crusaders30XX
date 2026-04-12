# Guard Conversion on Attack — Design Spec

**Date:** 2026-04-12

## Overview

Move guard conversion logic from `GuardManagementSystem` into `EnemyAttackBase` so each attack definition owns its conversion behavior. Simultaneously remove the `Sentinel` passive and the `Sentinel` enemy entirely, as the passive is antiquated and the enemy relied exclusively on it.

## Goals

- Guard conversion chance and range are defined per-attack, not globally in the system
- All attacks participate in guard conversion by default (opt-out via `GuardConversionChance = 0f`)
- No behavioral change for the default case — values replicate current system behavior exactly
- `Sentinel` passive and enemy fully removed

## Changes

### 1. `ECS/Objects/Enemies/EnemyAttackBase.cs`

Add three properties and a virtual method:

```csharp
// Probability (0.0–1.0) that guard conversion is attempted. 0f = no conversion.
public float GuardConversionChance { get; protected set; } = 0.75f;

// Min conversion amount as a ratio of damage (clamped to a minimum of 1)
public float GuardConversionMinRatio { get; protected set; } = 0f;

// Max conversion amount as a ratio of damage (exclusive upper bound)
public float GuardConversionMaxRatio { get; protected set; } = 0.5f;

public virtual int RollGuardConversion(int damage)
{
    if (damage <= 1) return 0;
    if (Random.Shared.NextDouble() >= GuardConversionChance) return 0;
    int min = Math.Max(1, (int)Math.Floor(damage * GuardConversionMinRatio));
    int max = (int)Math.Floor(damage * GuardConversionMaxRatio);
    if (max <= min) return min;
    return Random.Shared.Next(min, max);
}
```

Attacks opt out by setting `GuardConversionChance = 0f` in their constructor. Attacks with unusual logic override `RollGuardConversion` entirely.

### 2. `ECS/Scenes/BattleScene/GuardManagementSystem.cs`

- Replace `RollGuardConversion(damage)` call in `TryGuardConversion` with `attackDef.RollGuardConversion(damage)`
- Remove the private `RollGuardConversion(int damage)` method
- Remove the commented-out Sentinel passive check
- Remove two `PassiveTriggered` event publishes using `AppliedPassiveType.Sentinel`

### 3. Sentinel Passive Removal

| File | Change |
|------|--------|
| `ECS/Components/CardComponents.cs` | Remove `Sentinel` from `AppliedPassiveType` enum |
| `ECS/Services/TooltipTextService.cs` | Remove `AppliedPassiveType.Sentinel` case |
| `ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs` | Remove `AppliedPassiveType.Sentinel` from passive set |

### 4. Sentinel Enemy Removal

| File | Change |
|------|--------|
| `ECS/Objects/Enemies/Sentinel.cs` | Delete file entirely (removes `Sentinel`, `SentinelSlam`, `TwinStrike`, `RapidJab`) |
| `ECS/Factories/EnemyFactory.cs` | Remove both `"sentinel"` entries |
| `ECS/Factories/EnemyAttackFactory.cs` | Remove `sentinel_slam`, `twin_strike`, `rapid_jab` entries (two locations each) |
| `Content/Data/Locations/desert.json` | Replace `"sentinel"` enemy node with `"sand_golem"` |

## What Does NOT Change

- `GuardQueue` component — remains, used by guard conversion mechanism
- `GuardManagementSystem` — stays, handles `AddGuardEvent`, `RemoveGuardEvent`, conversion logic
- `GuardQueueDisplaySystem` — stays, renders guard pips
- `HpManagementSystem` — guard queue check stays

## Default Behavior Equivalence

Current system behavior maps exactly to the new defaults:
- `GuardConversionChance = 0.75f` — was `Random.Shared.Next(0, 4) != 0` (75% chance)
- `GuardConversionMinRatio = 0f` — was hardcoded `1` (clamped to `Math.Max(1, ...)`)
- `GuardConversionMaxRatio = 0.5f` — was `(int)Math.Floor(damage / 2.0)` (exclusive)
