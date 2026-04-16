# Timeline System Design

**Date:** 2026-04-16
**Status:** Approved (post-grill)

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
| `RefreshNumber` | `int` | Monotonically increasing counter; increments each time a refresh marker is crossed |

### `TimelineEvent` (new plain class)

| Field | Type | Description |
|---|---|---|
| `Position` | `int` | Absolute position on the timeline |
| `Type` | `TimelineEventType` | `Attack` or `Refresh` |
| `Attack` | `PlannedAttack?` | Null if Refresh |
| `Resolved` | `bool` | Marked true after processing |
| `InsertionIndex` | `int` | Order of insertion at this position; used to resolve same-position attacks |

### `CardBase` additions

| Property | Type | Default | Description |
|---|---|---|---|
| `TimeAdvancement` | `int` | `Cost.Count + 1 + (IsFreeAction ? 0 : 1)` | Spaces advanced when played for text |
| `Timing` | `CardResolutionTiming` | `Fast` | `Fast` = resolve effect then advance; `Slow` = advance then resolve |

### `EnemyAttackBase` addition

| Property | Type | Default | Description |
|---|---|---|---|
| `RevealAtDistance` | `int` | `WindowSize (8)` | `OnAttackReveal` fires when `attack.Position - CurrentPosition <= RevealAtDistance` |

### `PledgeCredit` (new marker component on player entity)

Marks that the player has an unused pledge opportunity. Capped at 1 — multiple consecutive refresh intervals without pledging do not stack credits.

### `Pledge` component addition

| Field | Type | Description |
|---|---|---|
| `IsReady` | `bool` | Set to `false` on pledge; set to `true` at the next `TimelineRefreshEvent`. Pledged cards can only be played when `IsReady = true`. |

### `BattleStateInfo` addition

| Field | Type | Description |
|---|---|---|
| `IntervalTracking` | `Dictionary<string, int>` | Per-refresh-interval counters; cleared at each `TimelineRefreshEvent`. Analog of `TurnTracking` for the timeline model. |

### Removed

- `PhaseState`, `MainPhase`, `SubPhase` enums
- `ActionPoints` component
- `PhaseState.TurnNumber` and `BattleInfo.TurnNumber` (replaced by `TimelineState.RefreshNumber`)

---

## Card Resolution Timing

Cards played for their text effect have two timing modes:

**Fast** (default): Effect resolves immediately → timeline then advances by `TimeAdvancement` spaces, triggering any events crossed.

**Slow**: Timeline advances by `TimeAdvancement` spaces first (triggering any events crossed in order) → effect resolves after all crossed events complete.

**Play for block** is always Fast regardless of the `Timing` property: block value is added to the pool, then the timeline advances 1 space.

---

## Player Input

| Action | Mouse | Controller |
|---|---|---|
| Play card for text | Left click | A button |
| Play card for block | Right click | X button |

---

## Card Actions

The player may always take any of these actions — there is no phase gate:

### 1. Play for Text
- Pay discard costs from hand (cards discarded, no extra time cost for the payment itself)
- If card has `Frozen` component, player gains 1 frostbite (text plays only, not block plays)
- Execute based on `Timing`:
  - **Fast**: `OnPlay` fires → advance `TimeAdvancement` spaces
  - **Slow**: advance `TimeAdvancement` spaces → `OnPlay` fires
- Any events on crossed spaces resolve in position order during the advance

### 2. Play for Block
- Always Fast
- Add card's `Block` value to `BlockPool`
- Advance 1 space (triggering any event on that space)
- Gain Courage if red card; gain Temperance if white card
- `Shackle` rule: playing any shackled card for block auto-plays all other shackled cards in hand for block simultaneously; each advances 1 space and contributes its block value

### 3. Pledge
- Requires `PledgeCredit` on the player (granted at refresh intervals, capped at 1)
- Advance 1 space → mark selected card as pledged (`IsReady = false`), remove `PledgeCredit`
- Pledged card becomes playable (`IsReady = true`) at the next `TimelineRefreshEvent`

### 4. Pass
- Always available; a persistent button visible at all times (equivalent position to the old End Turn button)
- Advances player directly to the next refresh marker position
- All attack events at intermediate positions resolve in order before the refresh fires

---

## Weapons

Weapons follow the same timeline actions as normal cards with one restriction: **once per refresh interval**. The usage flag resets at each `TimelineRefreshEvent`. All other weapon rules (can't play for block, can't discard for costs, don't count toward hand size) are unchanged.

---

## Card State Translations

| State | New Behavior |
|---|---|
| `Frozen` | Playing for text gives player 1 frostbite. Playing for block: no penalty. |
| `Intimidated` | Cannot play for block. Can play for text. Cleared at refresh. |
| `Sealed` | Cannot play for text. Cannot pledge. Can play for block. Seals count down: `-1` when the sealed card is played for block; `-1` when any card is played for text. At 0 seals, card is freed. |
| `Shackle` | Playing any shackled card for block auto-plays all shackled cards in hand for block. |
| `Recoil` | **Redesigned entirely.** Deregistered. Enemies that applied Recoil get new abilities. |
| `Marked` | Persists until consumed (playing the marked card). Not cleared at refresh. |
| `Plundered` | `DamageDealt` resets at each refresh. Rescue threshold and mechanic otherwise unchanged. |
| `ExhaustsOnEndTurn` | New system subscribes to `TimelineRefreshEvent` to exhaust matching cards. Flag and old system untouched. |
| `MarkedForEndOfTurnDiscard` | New system subscribes to `TimelineRefreshEvent` to discard matching cards. Component and old system untouched. |

---

## Enemy Scheduling

`EnemyArsenal` is replaced entirely. `EnemyBase` gains a new abstract method:

```csharp
void PopulateTimeline(TimelineState state, int upToPosition)
```

Each enemy implements this to place `TimelineEvent` entries on the timeline up to `upToPosition`. `EnemyTimelineSchedulerSystem` calls this on every `TimelineAdvancedEvent` when `Events.Max(e => e.Position) < CurrentPosition + WindowSize`.

The enemy has full design freedom:
- **Pattern enemies**: fixed intervals (e.g., attack every 3 spaces)
- **Adaptive enemies**: inspect `TimelineState` (block pool, position, refresh number) and adjust
- **Burst enemies**: cluster attacks before a gap
- **Ambush enemies**: short `RevealAtDistance` placements

`EnemyTimelineSchedulerSystem` guards against placement at positions ≤ `CurrentPosition`.

Refresh markers (positions 4, 8, 12, 16, ...) are pre-populated at battle start and are not the enemy's responsibility. Enemies may place attacks at the same position as refresh markers — the attack resolves first, the refresh resolves second.

Multiple attacks at the same position are allowed and resolve in insertion order.

---

## Attack Resolution

When `TimelineAdvancementSystem` crosses an `Attack` space:

1. Suppress all card interactability (`UIElement.Suppress()` on all hand cards)
2. Fire `TimelineAttackTriggeredEvent` (carries `PlannedAttack`, `Position`)
3. `TimelineAttackResolutionSystem` resolves:
   - `effectiveDamage = max(0, attack.Damage - BlockPool)`
   - `BlockPool = max(0, BlockPool - attack.Damage)`
   - Apply effective damage via `ModifyHpRequestEvent`
   - Run `attack.AttackDefinition.OnResolve()` for special attack effects
   - Mark `TimelineEvent.Resolved = true`
4. Restore card interactability (`UIElement.Restore()`)

Excess block carries forward to the next attack but is cleared completely at the next refresh marker.

---

## Attack Reveal

`EnemyTimelineSchedulerSystem` checks on every `TimelineAdvancedEvent` whether any unresolved, unrevealed attacks satisfy:

```
attack.Position - CurrentPosition <= attack.RevealAtDistance
```

When satisfied, `OnAttackReveal(EntityManager)` is called and the attack is marked as revealed.

### Timeline Display for Unrevealed Attacks

Attack spaces where `RevealAtDistance` has not been crossed display as `???` — name and damage are hidden. Once revealed, the space shows the attack name and damage value. Hovering a revealed attack space shows full attack details.

---

## Refresh Interval

Refresh markers are pre-placed at positions 4, 8, 12, 16, ... at battle start.

When `TimelineAdvancementSystem` crosses a `Refresh` space, the following resolve in order:

1. Attack at this position (if any) resolves first
2. Fire `TimelineRefreshEvent`
3. Clear `BlockPool` to 0
4. Draw cards up to max hand size — **if draw pile is empty and player cannot draw to full hand, player loses the battle**
5. Clear `Intimidated` from all cards in hand
6. Exhaust cards marked `ExhaustsOnEndTurn`
7. Discard cards marked `MarkedForEndOfTurnDiscard`
8. Reset `Plundered.DamageDealt` to 0 on all plundered cards
9. Reset `IntervalTracking` in `BattleStateInfo`
10. Tick all passives subscribing to `TimelineRefreshEvent` (Burn, Slow, etc.)
11. Set `IsReady = true` on any pledged cards with `IsReady = false`
12. Reset weapon usage flags
13. If no pledged card and `PledgeCredit` count < 1 → add `PledgeCredit` to player entity
14. Increment `RefreshNumber` on `TimelineState`

---

## Poison

Poison ticks on **real time**, not timeline position. The new `PoisonSystem` tracks elapsed `GameTime` seconds and applies poison damage every 1 second, independent of player actions or timeline position. This creates constant pressure during deliberation.

---

## Battle Start Sequence

Position 0 is a special setup event (not a regular `TimelineEvent`). Before the player takes their first action:

1. Enemy's `PopulateTimeline` is called to fill positions 1 through `WindowSize`
2. Player draws a full starting hand
3. Any `OnBattleStart` passives fire
4. Control passes to the player

---

## Event Ordering

When a single action crosses multiple spaces:
- All events process in strict ascending position order
- Multiple attacks at the same position resolve in insertion order
- At a shared Refresh + Attack position: **Attack resolves first, then Refresh**
- Fast card: `OnPlay` has already resolved before any events fire
- Slow card: all crossed events fire in order, then `OnPlay` fires

---

## Card Interactability

Cards in hand are interactable based on their component state:
- `Sealed` cards: interactable for block only
- `Intimidated` cards: interactable for text only
- All cards: suppressed (non-interactable) while an event is resolving (attack animation, refresh processing)

The existing `UIElement.Suppress()`/`Restore()` infrastructure handles both cases.

---

## New Events

Replacing `ChangeBattlePhaseEvent`:

| Event | Carries | Purpose |
|---|---|---|
| `TimelineAdvancedEvent` | `PreviousPosition`, `NewPosition` | Fired after each position change |
| `TimelineRefreshEvent` | `Position`, `RefreshNumber` | Fired when a refresh marker is crossed |
| `TimelineAttackTriggeredEvent` | `PlannedAttack`, `Position` | Fired when an attack space is reached |

---

## New Systems

| System | Responsibility |
|---|---|
| `TimelineAdvancementSystem` | Core loop: listens for player actions, executes card timing, advances position, fires events in order |
| `TimelineDisplaySystem` | Renders the 8-space scrolling window; shows attack name + damage when revealed, `???` when not; full details on hover |
| `BlockPoolManagementSystem` | Owns block pool state; clears at refresh |
| `EnemyTimelineSchedulerSystem` | Calls `enemy.PopulateTimeline()` on `TimelineAdvancedEvent`; fires `OnAttackReveal` at `RevealAtDistance` threshold |
| `TimelineAttackResolutionSystem` | Resolves attack damage and special effects on `TimelineAttackTriggeredEvent` |
| `TimelinePledgeSystem` | Manages pledge credit grant at refresh; handles pledge action (1 time step); manages `IsReady` flag |
| `PledgeAvailableDisplaySystem` | Renders indicator when player holds an unused pledge credit |
| `BlockPoolDisplaySystem` | Renders block pool value as a persistent indicator next to the Courage display |

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
- `EnemyArsenal` (component; entity no longer created)
- `NextTurnAttackIntent` (component; entity no longer created)

---

## Out of Scope

- Guard system (`GuardComponents`, guard conversion) — deferred; behavior in the timeline model defined separately after core timeline is stable
- Mechanics that interact with the old phase model (MustBeBlocked, Ambush) — deferred
- Equipment block contribution to the pool — deferred pending equipment system review
- The exact visual design of the timeline display (colors, sizing, animation) — left to implementation
