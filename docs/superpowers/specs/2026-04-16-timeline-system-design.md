# Timeline System Design

**Date:** 2026-04-16
**Status:** Approved

## Overview

Replace the discrete phase machine (EnemyTurn → Block → Action → Pledge) with a continuous timeline. The player advances through an infinite timeline by taking actions. Enemy attacks are placed at specific positions on the timeline by each enemy's own scheduling logic. Every 4 spaces the player crosses a refresh marker that resets block, draws cards, ticks passives, and grants a pledge credit.

AP (Action Points) are removed entirely. Time is the new currency.

---

## Data Model

### `TimelineState` (new singleton component on a dedicated "Timeline" entity)

| Field | Type | Description |
|---|---|---|
| `CurrentPosition` | `int` | The player's absolute position on the infinite timeline |
| `Events` | `List<TimelineEvent>` | All known upcoming events (attacks + refresh markers), sorted by position |
| `BlockPool` | `int` | Accumulated block value from cards played for block this interval |
| `WindowSize` | `const int = 8` | Number of spaces visible to the player |

### `TimelineEvent` (new plain class)

| Field | Type | Description |
|---|---|---|
| `Position` | `int` | Absolute position on the timeline |
| `Type` | `TimelineEventType` | `Attack` or `Refresh` |
| `Attack` | `PlannedAttack?` | Null if Refresh |
| `Resolved` | `bool` | Marked true after processing |

### `CardBase` additions

| Property | Type | Default | Description |
|---|---|---|---|
| `TimeAdvancement` | `int` | `Cost.Count + 1 + (IsFreeAction ? 0 : 1)` | Spaces advanced when played for text |
| `Timing` | `CardResolutionTiming` | `Fast` | `Fast` = resolve effect then advance; `Slow` = advance then resolve |

### `PledgeCredit` (new marker component on player entity)

Marks that the player has an unused pledge opportunity. Capped at 1 — multiple consecutive refresh intervals without pledging do not stack credits.

### Removed

- `PhaseState`, `MainPhase`, `SubPhase` enums
- `ActionPoints` component
- `PhaseState.TurnNumber` and `BattleInfo.TurnNumber` (position on timeline replaces turn tracking)

---

## Card Resolution Timing

Cards played for their text effect have two timing modes:

**Fast** (default): Effect resolves immediately → timeline then advances by `TimeAdvancement` spaces, triggering any events crossed.

**Slow**: Timeline advances by `TimeAdvancement` spaces first (triggering any events crossed in order) → effect resolves after all crossed events complete.

**Play for block** is always Fast regardless of the `Timing` property: block value is added to the pool, then the timeline advances 1 space.

---

## Card Actions

The player may always take any of these actions — there is no phase gate:

### 1. Play for Text
- Pay discard costs from hand (cards discarded, no extra time cost for the payment itself)
- Execute based on `Timing`:
  - **Fast**: `OnPlay` fires → advance `TimeAdvancement` spaces
  - **Slow**: advance `TimeAdvancement` spaces → `OnPlay` fires
- Any events on crossed spaces resolve in position order during the advance

### 2. Play for Block
- Always Fast
- Add card's `Block` value to `BlockPool`
- Advance 1 space (triggering any event on that space)
- Gain Courage if red card; gain Temperance if white card

### 3. Pledge
- Requires `PledgeCredit` on the player (granted at refresh intervals, capped at 1)
- Advance 1 space → mark selected card as pledged, remove `PledgeCredit`

---

## Enemy Scheduling

`EnemyBase` gains a new abstract method:

```csharp
void PopulateTimeline(TimelineState state, int upToPosition)
```

Each enemy implements this to place `TimelineEvent` entries on the timeline up to `upToPosition`. `EnemyTimelineSchedulerSystem` calls this whenever the lookahead window (`CurrentPosition + WindowSize`) reaches positions not yet populated.

The enemy has full design freedom:
- **Pattern enemies**: fixed intervals (e.g., attack every 3 spaces)
- **Adaptive enemies**: inspect `TimelineState` (block pool, current position) and adjust
- **Burst enemies**: cluster attacks before a gap
- **Ambush enemies**: short-warning placements

`EnemyTimelineSchedulerSystem` guards against placement at positions ≤ `CurrentPosition`.

Refresh markers (positions 4, 8, 12, 16, ...) are pre-populated at battle start and are not the enemy's responsibility.

---

## Attack Resolution

When `TimelineAdvancementSystem` crosses an `Attack` space:

1. Fire `TimelineAttackTriggeredEvent` (carries `PlannedAttack`)
2. `TimelineAttackResolutionSystem` resolves:
   - `effectiveDamage = max(0, attack.Damage - BlockPool)`
   - `BlockPool = max(0, BlockPool - attack.Damage)`
   - Apply effective damage via `ModifyHpRequestEvent`
   - Run `attack.AttackDefinition.OnResolve()` for special attack effects
   - Mark `TimelineEvent.Resolved = true`

Excess block carries forward to the next attack but is cleared completely at the next refresh marker.

---

## Refresh Interval

Refresh markers are pre-placed at positions 4, 8, 12, 16, ... at battle start.

When `TimelineAdvancementSystem` crosses a `Refresh` space:

1. Fire `TimelineRefreshEvent`
2. In order:
   - Clear `BlockPool` to 0
   - Draw cards up to max hand size
   - Tick all passives (Burn, Poison, Slow, etc.) — passive systems subscribe to `TimelineRefreshEvent`
   - If no pledged card and `PledgeCredit` count < 1 → add `PledgeCredit` to player entity

---

## Event Ordering

When a single action crosses multiple spaces:
- All events process in strict ascending position order
- If a Refresh and Attack share the same position: Refresh resolves first (block clears and cards are drawn before damage applies)
- Fast card: `OnPlay` has already resolved before any events fire
- Slow card: all crossed events fire in order, then `OnPlay` fires

---

## New Events

Replacing `ChangeBattlePhaseEvent`:

| Event | Carries | Purpose |
|---|---|---|
| `TimelineAdvancedEvent` | `PreviousPosition`, `NewPosition` | Fired after each position change |
| `TimelineRefreshEvent` | `Position` | Fired when a refresh marker is crossed |
| `TimelineAttackTriggeredEvent` | `PlannedAttack`, `Position` | Fired when an attack space is reached |

---

## New Systems

| System | Responsibility |
|---|---|
| `TimelineAdvancementSystem` | Core loop: listens for player actions, executes card timing, advances position, fires events in order |
| `TimelineDisplaySystem` | Renders the 8-space scrolling window with attack and refresh markers |
| `BlockPoolManagementSystem` | Owns block pool state; applies pool against incoming attacks; clears at refresh |
| `EnemyTimelineSchedulerSystem` | Calls `enemy.PopulateTimeline()` as the lookahead window scrolls forward |
| `TimelineAttackResolutionSystem` | Resolves attack damage and special effects on `TimelineAttackTriggeredEvent` |
| `TimelinePledgeSystem` | Manages pledge credit grant at refresh; handles pledge action (1 time step) |
| `PledgeAvailableDisplaySystem` | Renders indicator when player holds an unused pledge credit |

---

## Deregistered Systems (not deleted)

The following systems remain in the codebase as inert reference implementations but are removed from `world.AddSystem()` in `Game1.cs`:

- `PhaseCoordinatorSystem`
- `PhaseChangeEventSystem`
- `ActionPointManagementSystem`
- `ActionPointDisplaySystem`
- `BattlePhaseDisplaySystem`
- `EndTurnDisplaySystem`
- `HandBlockInteractionSystem`
- `AssignedBlocksToDiscardSystem`
- `GuardQueueDisplaySystem`
- `EnemyAttackProgressManagementSystem`
- `EquipmentBlockInteractionSystem`
- `EnemyIntentPlanningSystem`
- `PledgeManagementSystem`
- `AttackResolutionSystem`

---

## Out of Scope

- Mechanics that interact with the old phase model (MustBeBlocked, Ambush, Intimidate on block) are deferred — their behavior in the timeline model will be defined per-feature as needed.
- Equipment block contribution to the pool is deferred pending equipment system review.
- The exact visual design of the timeline display (colors, sizing, animation) is left to implementation.
