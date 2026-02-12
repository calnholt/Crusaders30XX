# Cards

## Creating a New Card

1. Create a new class in this directory inheriting from `CardBase`
2. Register the card in `CardFactory.cs` in **both** `Create()` (switch) and `GetAllCards()` (dictionary), alphabetically

## CardBase Properties

| Property | Type | Default | Notes |
|---|---|---|---|
| `CardId` | string | — | Snake_case unique ID, must match factory key |
| `Name` | string | "" | Display name |
| `Damage` | int | 0 | Base damage value |
| `Block` | int | 0 | Base block value |
| `Cost` | List\<string\> | [] | Discard costs: `"Red"`, `"White"`, `"Black"`, `"Any"` |
| `Text` | string | "" | Card description text |
| `Animation` | string | "" | `"Attack"`, `"Buff"`, etc. |
| `Type` | CardType | Attack | `Attack`, `Prayer`, `Block`, `Relic` |
| `Target` | string | "" | `"Enemy"` or `"Player"` |
| `IsFreeAction` | bool | false | If true, doesn't consume an action point |
| `IsWeapon` | bool | false | Weapon rules: play once per action phase, can't block, can't discard for costs |
| `ExhaustsOnEndTurn` | bool | false | — |
| `IsToken` | bool | false | — |
| `CanAddToLoadout` | bool | true | — |
| `Tooltip` | string | "" | Auto-processed for keywords via `KeywordTooltipTextService` |
| `CardTooltip` | string | "" | — |

## Callbacks

All callbacks are nullable and optional.

| Callback | Signature | Purpose |
|---|---|---|
| `OnPlay` | `Action<EntityManager, Entity>` | Main card effect when played |
| `OnBlock` | `Action<EntityManager, Entity>` | Effect when used to block |
| `OnDraw` | `Action<EntityManager, Entity>` | Effect when drawn |
| `OnCreate` | `Action<EntityManager, Entity>` | Runs when entity is created in `EntityFactory` |
| `OnDiscardedForCost` | `Action<EntityManager, Entity>` | Runs when discarded to pay another card's cost |
| `CanPlay` | `Func<EntityManager, Entity, bool>` | Pure validation; return false to block play (no side effects) |
| `OnCantPlay` | `Action<EntityManager, Entity>` | Publish `CantPlayCardMessage` when play is rejected; called by systems after `CanPlay` returns false |
| `GetConditionalDamage` | `Func<EntityManager, Entity, int>` | Bonus damage shown on card and added via `GetDerivedDamage()` |

## Dealing Damage

Always use `GetDerivedDamage()` which combines `GetConditionalDamage` + `AttackDamageValueService.GetTotalDamageValue()` (accounts for power, modifications, etc.):

```csharp
EventManager.Publish(new ModifyHpRequestEvent {
    Source = player,
    Target = enemy,
    Delta = -GetDerivedDamage(entityManager, card),
    DamageType = ModifyTypeEnum.Attack
});
```

## Common Events

```csharp
// Gain/lose AP
EventManager.Publish(new ModifyActionPointsEvent { Delta = +1 });

// Gain/spend/lose courage
EventManager.Publish(new ModifyCourageRequestEvent { Delta = +2, Type = ModifyCourageType.Gain });
EventManager.Publish(new ModifyCourageRequestEvent { Delta = -3, Type = ModifyCourageType.Spent });

// Apply passives (Power, Frostbite, etc.)
EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Power, Delta = +1 });

// Heal
EventManager.Publish(new ModifyHpRequestEvent { Source = player, Target = player, Delta = +3, DamageType = ModifyTypeEnum.Heal });

// Draw cards
EventManager.Publish(new RequestDrawCardsEvent { Count = 1 });

// Freeze cards
EventManager.Publish(new FreezeCardsEvent { ... });
```

## Checking Payment Cards

Access `LastPaymentCache` to inspect what cards were discarded to pay costs:

```csharp
var cacheEntity = entityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
var paymentCards = cacheEntity?.GetComponent<LastPaymentCache>()?.PaymentCards;
```

## Checking Battle State / Phase Tracking

```csharp
var battleStateInfo = entityManager.GetEntitiesWithComponent<BattleStateInfo>()
    .FirstOrDefault().GetComponent<BattleStateInfo>();
battleStateInfo.PhaseTracking.TryGetValue(TrackingTypeEnum.CourageLost.ToString(), out var value);
```

## Key Components

- `Frozen` — Card cannot be played during action phase; removed when used to block
- `Sealed` — Cannot play or pledge, can still block
- `Intimidated` — Cannot block
- `MarkedForExhaust` — Exhausts after play
- `ModifiedDamage` / `ModifiedBlock` — Tracked via `AttackDamageValueService`
- `CardData` — Holds `CardBase` reference and `CardColor` (White, Red, Black, Yellow)

## Card Colors

- **White**: Blocking grants 1 temperance
- **Red**: Blocking grants 1 courage
- **Black**: Cards get +1 block value automatically (applied in `EntityFactory`)

## Common Card Archetypes

### Vanilla Attack (no text, just damage)
Target `"Enemy"`, Animation `"Attack"`, Block typically 2–3. `Text` left empty. OnPlay only publishes `ModifyHpRequestEvent`. See `Smite`, `Fervor`, `Reckoning`, `Absolution`.

### Courage-Gated Attack (additional cost: lose N courage)
Same as vanilla attack but adds `CanPlay` + `OnCantPlay` for courage validation, and spends courage in `OnPlay` before dealing damage. Text: `"As an additional cost, lose {N} courage."` See `Stab`, `Impale`, `Exaltation`.

### Buff Prayer
Target `"Player"`, Animation `"Buff"`, `Type = CardType.Prayer`, typically `IsFreeAction = true`, Block typically 2–3. OnPlay publishes `ApplyPassiveEvent`. See `IncreaseFaith`, `DowseWithHolyWater`, `LitanyOfWrath`.

## Conventions

- Attacks: Target `"Enemy"`, Animation `"Attack"`
- Prayers/Buffs: Target `"Player"`, Animation `"Buff"`
- All attacks must call `GetDerivedDamage()` for damage (never use raw `Damage`)
- Use private fields for numeric constants and interpolate into `Text`
- Keep `OnPlay` focused — publish events, don't manage state directly
- `CanPlay` must be a pure bool check (no side effects); `OnCantPlay` publishes `CantPlayCardMessage`
