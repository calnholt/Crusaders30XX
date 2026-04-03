# InputSystem Refactor Design

## Problem

`InputSystem.cs` has poor boundaries. It tangles 5 distinct responsibilities: pointer coalescing, game-state-aware interactability filtering, hit testing, click routing/dispatch for many specific component types, and a profiler keyboard shortcut. Adding any new clickable UI type requires modifying InputSystem directly.

## Design Decisions

- **InputSystem becomes a thin hover + click layer.** It trusts `IsInteractable` and delegates all click handling to `UIElementEventDelegateService`.
- **Overlay systems own interactability.** Each system that creates a modal/overlay/phase restriction manages `IsInteractable` for the entities it gates. InputSystem never checks game state.
- **All click dispatch goes through `UIElementEventType`.** The `UIButton` component and `DebugCommandEvent` string-dispatch pattern are removed. `UIElementEventDelegateService` is the single source of truth for click behavior.
- **Profiler toggle moves to `Game1.cs`** alongside the existing debug menu toggle.

## Workstream 1: Slim InputSystem

After refactor, `InputSystem.Update()` does exactly this:

1. **Pointer coalescing** — mouse screen-to-virtual coords, or gamepad cursor position via `CursorStateEvent` (unchanged logic)
2. **Collect interactable entities** — `GetEntitiesWithComponent<UIElement>().Where(ui => ui.IsInteractable)` — no game-state checks
3. **Hit test** — find top-most entity under pointer. Gamepad path uses `CursorStateEvent.TopEntity`. Mouse path uses rotated-rect for cards, AABB for non-cards (unchanged logic)
4. **Set hover** — `IsHovered = true` on the top entity only
5. **Detect click edge** — mouse left-button pressed edge or controller A-press edge
6. **Dispatch** — `UIElementEventDelegateService.HandleEvent(topEntity.UIElement.EventType, topEntity)`

Deleted from InputSystem:
- `HandleUIClick()` — all hardcoded component checks (`CardData`, `DrawPileClickable`, `DiscardPileClickable`, `UIButton`)
- `HandleCardClick()` — card play / pay-cost branching
- All inline game-state filtering (modal check, pay-cost check, phase check)
- Profiler toggle (P key)
- `ToggleProfilerOverlay()` method

## Workstream 2: Overlay Systems Own Interactability

### SuppressCount on UIElement

Add `int SuppressCount` to `UIElement`. Systems call helper methods to suppress/restore:

- `Suppress()` increments `SuppressCount`, sets `IsInteractable = false`
- `Restore()` decrements `SuppressCount`, sets `IsInteractable = true` only when count reaches 0

This prevents multi-system conflicts (e.g., modal and pay-cost both suppressing the same entity — restoring one doesn't prematurely re-enable it).

### CardListModalSystem

On modal open:
- Iterate all UIElement entities
- Suppress those that are NOT `CardListModalClose` entities and NOT cards within the modal

On modal close:
- Restore all previously suppressed entities

### PayCostOverlaySystem

On overlay open:
- Suppress all UIElement entities except: `PayCostCancelButton` entities, hand cards, and `SelectedForPayment` cards

On overlay close:
- Restore all previously suppressed entities

### Phase Transition (Action Phase)

During Action sub-phase:
- Non-hand card entities with `CardData` have interactability suppressed
- Hand cards remain interactable

On phase exit:
- Restore suppressed card entities

### Pattern

Each overlay/mode system tracks which entities it suppressed (e.g., a `HashSet<Entity>`) so it can restore exactly the right set on close/exit.

## Workstream 3: UIElementEventType Consolidation

### New Enum Values

Add to `UIElementEventType`:
- `CardClicked`
- `OpenDrawPile`
- `OpenDiscardPile`
- `EndTurn`
- `SkipPledge`
- `EnemyAbsorbPulse`

### UIElementEventDelegateService Expansion

New cases in the switch:

| Event Type | Action |
|---|---|
| `CardClicked` | Check `PayCostOverlayState.IsOpen` → publish `PayCostCandidateClicked` or `PlayCardRequested` |
| `OpenDrawPile` | Look up `Deck`, publish `OpenCardListModalEvent` with draw pile cards |
| `OpenDiscardPile` | Look up `Deck`, publish `OpenCardListModalEvent` with discard pile cards |
| `EndTurn` | Publish new `EndTurnRequested` event |
| `SkipPledge` | Publish new `SkipPledgeRequested` event |
| `EnemyAbsorbPulse` | Publish new `EnemyAbsorbPulseRequested` event |

These three handlers currently go through `DebugCommandEvent` string dispatch. This refactor replaces them with dedicated typed events. The consuming systems (`EndTurnDisplaySystem`, `SkipPledgeDisplaySystem`, `EnemyDisplaySystem`) subscribe to the new typed events instead of filtering `DebugCommandEvent.Command` strings. The `ConfirmBlocks` event type already exists and handles the `"ConfirmEnemyAttack"` command.

### UIElementEventDelegateService Becomes Non-Static

`UIElementEventDelegateService` is currently `internal static` with no entity access. The new `CardClicked` case needs to look up `PayCostOverlayState`, and `OpenDrawPile`/`OpenDiscardPile` need to look up `Deck`. Convert the service to a non-static class that receives `EntityManager` in its constructor. InputSystem and HotKeySystem instantiate or receive a shared instance.

### Entity Factory Updates

Entities that currently rely on InputSystem's hardcoded component checks need `UIElementEventType` set at creation:

- **Card entities**: Set `UIElementEventType.CardClicked` when creating cards with `CardData`
- **Draw pile entity**: Set `UIElementEventType.OpenDrawPile` (currently uses `DrawPileClickable` marker component)
- **Discard pile entity**: Set `UIElementEventType.OpenDiscardPile` (currently uses `DiscardPileClickable` marker component)
- **End turn button**: Set `UIElementEventType.EndTurn` (currently uses `UIButton { Command = "EndTurn" }`)
- **Skip pledge button**: Set `UIElementEventType.SkipPledge` (currently uses `UIButton { Command = "SkipPledge" }`)
- **Enemy absorb pulse**: Set `UIElementEventType.EnemyAbsorbPulse` (currently uses `UIButton`)

### Removals

- **`UIButton` component** — delete from `CardComponents.cs`
- **`DrawPileClickable` component** — delete (replaced by `UIElementEventType.OpenDrawPile`)
- **`DiscardPileClickable` component** — delete (replaced by `UIElementEventType.OpenDiscardPile`)
- **`DebugCommandEvent` subscriptions** in `EndTurnDisplaySystem`, `SkipPledgeDisplaySystem`, `EnemyAttackDisplaySystem`, `EnemyDisplaySystem` — replace with subscriptions to `EndTurnRequested`, `SkipPledgeRequested`, `EnemyAbsorbPulseRequested` typed events
- **`UIButton.Command` fallback path** in `HotKeySystem.ProcessHotKeyClick()` — delete the `btn.Command` branch

### HotKeySystem Update

`ProcessHotKeyClick` currently has three dispatch paths: `UIButton.Command`, `UIElement.EventType`, and bare `IsClicked`. After refactor, only the `EventType` and `IsClicked` paths remain.

## Workstream 4: Profiler Toggle → Game1

Move the P-key toggle and `ToggleProfilerOverlay()` method from `InputSystem` to `Game1.cs`, next to the existing debug menu toggle. `Game1` already reads keyboard state each frame.

## What Doesn't Change

- **Hit testing logic** — rotated-rect for cards, AABB for non-cards (stays in InputSystem)
- **Pointer coalescing** — mouse-to-virtual-coords and CursorStateEvent consumption (stays in InputSystem)
- **CursorSystem** — gamepad cursor management is unaffected
- **HotKeySystem** — unchanged except removing the `UIButton.Command` fallback
- **DebugCommandSystem** — its `[DebugAction]` methods are for the debug menu, not UI click dispatch; unaffected
- **`IsHovered` / `IsClicked` flags on UIElement** — display systems that read these are unaffected
