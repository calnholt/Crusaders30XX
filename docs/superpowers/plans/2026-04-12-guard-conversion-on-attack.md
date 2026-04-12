# Guard Conversion on Attack — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move guard conversion logic from `GuardManagementSystem` into `EnemyAttackBase` as configurable properties + a virtual method, and remove the `Sentinel` passive and enemy entirely.

**Architecture:** Three float properties (`GuardConversionChance`, `GuardConversionMinRatio`, `GuardConversionMaxRatio`) and a virtual `RollGuardConversion(int damage)` method are added to `EnemyAttackBase` with defaults that exactly replicate current behavior. `GuardManagementSystem.TryGuardConversion` delegates the roll to the attack definition. Sentinel passive and enemy are deleted across all referencing files.

**Tech Stack:** C# / .NET 8, MonoGame. No test framework — verification is by compiler (no build errors) and manual play.

**Spec:** `docs/superpowers/specs/2026-04-12-guard-conversion-on-attack-design.md`

---

## File Map

| File | Change |
|------|--------|
| `ECS/Objects/Enemies/EnemyAttackBase.cs` | Add 3 properties + virtual `RollGuardConversion` |
| `ECS/Scenes/BattleScene/GuardManagementSystem.cs` | Delegate roll to `attackDef`, remove private method + dead code |
| `ECS/Components/CardComponents.cs` | Remove `Sentinel` from `AppliedPassiveType` enum |
| `ECS/Services/TooltipTextService.cs` | Remove `AppliedPassiveType.Sentinel` case |
| `ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs` | Remove `AppliedPassiveType.Sentinel` from passive set |
| `ECS/Objects/Enemies/Sentinel.cs` | Delete entirely |
| `ECS/Factories/EnemyFactory.cs` | Remove two `"sentinel"` entries |
| `ECS/Factories/EnemyAttackFactory.cs` | Remove `sentinel_slam`, `twin_strike`, `rapid_jab` (two locations) |
| `Content/Data/Locations/desert.json` | Replace `"sentinel"` enemy node with `"sand_golem"` |

---

## Task 1: Add guard conversion to `EnemyAttackBase`

**Files:**
- Modify: `ECS/Objects/Enemies/EnemyAttackBase.cs`

- [ ] **Step 1: Add the three properties and virtual method**

In `ECS/Objects/Enemies/EnemyAttackBase.cs`, add `using System;` at the top if not already present, then insert these members into the `EnemyAttackBase` class body (after the existing public properties, before `Initialize`):

```csharp
// Probability (0.0–1.0) that guard conversion is attempted. Set to 0f to opt out.
public float GuardConversionChance { get; protected set; } = 0.75f;

// Min conversion as a ratio of damage (floor applied, clamped to minimum of 1)
public float GuardConversionMinRatio { get; protected set; } = 0f;

// Max conversion as a ratio of damage (exclusive upper bound, floor applied)
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

`System` is already imported via `System.ComponentModel` — add `using System;` at the top if it isn't there explicitly (it may be transitively available but make it explicit for `Math` and `Random`).

- [ ] **Step 2: Commit**

```bash
git add ECS/Objects/Enemies/EnemyAttackBase.cs
git commit -m "feat: add guard conversion properties and RollGuardConversion to EnemyAttackBase"
```

---

## Task 2: Update `GuardManagementSystem` to delegate the roll

**Files:**
- Modify: `ECS/Scenes/BattleScene/GuardManagementSystem.cs`

- [ ] **Step 1: Replace the roll call and remove the private method**

In `TryGuardConversion` (around line 129), change:

```csharp
int conversionAmount = RollGuardConversion(damage);
```

to:

```csharp
int conversionAmount = attackDef.RollGuardConversion(damage);
```

- [ ] **Step 2: Remove the private `RollGuardConversion` method**

Delete this entire method (lines 187–194):

```csharp
private int RollGuardConversion(int damage)
{
    if (damage <= 1) return 0;
    // 50% chance of 0; remaining 50% uniform across 1..(damage - 1)
    // (prevents returning the full `damage` amount).
    if (Random.Shared.Next(0, 4) == 0) return 0;
    return Random.Shared.Next(1, (int)Math.Floor(damage / 2.0)); // max is exclusive => [1..damage-1]
}
```

- [ ] **Step 3: Remove dead code and Sentinel passive references**

Remove the commented-out Sentinel check (line 118):

```csharp
// if (ap == null || !ap.Passives.ContainsKey(AppliedPassiveType.Sentinel)) return;
```

Remove the `PassiveTriggered` publish in the full conversion branch (inside the `EnqueueTriggerAction` lambda, around line 144):

```csharp
EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Sentinel });
```

Remove the `PassiveTriggered` publish in the partial conversion branch (around line 181):

```csharp
EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Sentinel });
```

- [ ] **Step 4: Commit**

```bash
git add ECS/Scenes/BattleScene/GuardManagementSystem.cs
git commit -m "refactor: delegate guard conversion roll to attack definition"
```

---

## Task 3: Remove Sentinel passive from enum and services

**Files:**
- Modify: `ECS/Components/CardComponents.cs`
- Modify: `ECS/Services/TooltipTextService.cs`
- Modify: `ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs`

- [ ] **Step 1: Remove from `AppliedPassiveType` enum**

In `ECS/Components/CardComponents.cs` around line 1062, delete:

```csharp
Sentinel,
```

- [ ] **Step 2: Remove tooltip case**

In `ECS/Services/TooltipTextService.cs` around line 143, delete the entire case:

```csharp
case AppliedPassiveType.Sentinel:
    return "Each attack may be partially or fully converted into a guard. Guards absorb incoming damage. Remaining guards at the start of this enemy's turn become Aggression.";
```

- [ ] **Step 3: Remove from passive set**

In `ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs` around line 388, delete:

```csharp
AppliedPassiveType.Sentinel
```

Note: this is the last item in the set — also remove the trailing comma from the line above it (now `AppliedPassiveType.Anathema` or whichever precedes it).

- [ ] **Step 4: Commit**

```bash
git add ECS/Components/CardComponents.cs ECS/Services/TooltipTextService.cs ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs
git commit -m "refactor: remove Sentinel passive type and references"
```

---

## Task 4: Remove Sentinel enemy, attacks, and map node

**Files:**
- Delete: `ECS/Objects/Enemies/Sentinel.cs`
- Modify: `ECS/Factories/EnemyFactory.cs`
- Modify: `ECS/Factories/EnemyAttackFactory.cs`
- Modify: `Content/Data/Locations/desert.json`

- [ ] **Step 1: Delete `Sentinel.cs`**

```bash
git rm ECS/Objects/Enemies/Sentinel.cs
```

- [ ] **Step 2: Remove Sentinel from `EnemyFactory.cs`**

Remove this line from the switch expression (around line 46):

```csharp
"sentinel" => new Sentinel(difficulty),
```

Remove this entry from the `GetAllEnemies` dictionary (around line 84):

```csharp
{ "sentinel", new Sentinel(difficulty) },
```

- [ ] **Step 3: Remove Sentinel attacks from `EnemyAttackFactory.cs`**

Remove these three lines from the switch expression (around lines 122–124):

```csharp
// Sentinel attacks
"sentinel_slam" => new SentinelSlam(),
"twin_strike" => new TwinStrike(),
"rapid_jab" => new RapidJab(),
```

Remove these three entries from the `GetAllAttacks` dictionary (around lines 237–240):

```csharp
// Sentinel attacks
{ "sentinel_slam", new SentinelSlam() },
{ "twin_strike", new TwinStrike() },
{ "rapid_jab", new RapidJab() },
```

- [ ] **Step 4: Replace sentinel with sand_golem in `desert.json`**

In `Content/Data/Locations/desert.json` around line 66, change:

```json
{
  "id": "sentinel",
  "type": "Enemy"
}
```

to:

```json
{
  "id": "sand_golem",
  "type": "Enemy"
}
```

- [ ] **Step 5: Commit**

```bash
git add ECS/Factories/EnemyFactory.cs ECS/Factories/EnemyAttackFactory.cs Content/Data/Locations/desert.json
git commit -m "feat: remove Sentinel enemy and replace desert map node with SandGolem"
```

---

## Self-Review Checklist

- [x] **Spec coverage:** All 9 file changes from the spec are assigned to a task
- [x] **No placeholders:** All steps contain exact code or commands
- [x] **Type consistency:** `RollGuardConversion(int damage)` defined in Task 1, called as `attackDef.RollGuardConversion(damage)` in Task 2 — matches
- [x] **`AppliedPassiveType.Sentinel` removal:** Covered in Task 3 (enum, tooltip, passive set) and Task 2 (`PassiveTriggered` calls)
- [x] **Task ordering:** Task 1 (define method) before Task 2 (call it); Task 3 (remove enum value) before Task 4 (remove class that used it — note: `Sentinel.cs` itself doesn't reference the passive type so ordering is flexible, but this ordering is clean)
