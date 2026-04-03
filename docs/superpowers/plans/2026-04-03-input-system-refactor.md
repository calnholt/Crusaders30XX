# InputSystem Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor InputSystem into a thin hover+click layer by pushing interactability filtering to overlay systems and consolidating all click dispatch through UIElementEventType.

**Architecture:** InputSystem resolves pointer position, hit-tests for top entity, sets hover, detects click edges, and delegates to `UIElementEventDelegateService`. Overlay systems (modal, pay-cost, phase) manage `IsInteractable` via a `SuppressCount` mechanism on `UIElement`. All click routing goes through `UIElementEventType` enum values handled in the delegate service.

**Tech Stack:** C# / .NET 8.0 / MonoGame / Custom ECS

**Spec:** `docs/superpowers/specs/2026-04-03-input-system-refactor-design.md`

**Important discoveries during planning (deviations from spec):**
- `EndTurn`, `SkipPledge`, `ViewDeck`, `ViewDiscard` already exist in `UIElementEventType` — no need to add them. Only `CardClicked` is new.
- `EnemyAbsorbPulse` is dead code — subscribed in `EnemyDisplaySystem` but never published. Drop from plan, remove subscription.
- `SkipPledgeDisplaySystem` already sets `EventType = UIElementEventType.SkipPledge` on its button entity but ALSO adds `UIButton` — just remove the `UIButton`.
- `EnemyAttackDisplaySystem` already sets `EventType = UIElementEventType.ConfirmBlocks` on the confirm button — no `UIButton` component on it. The confirm entity is named `"UIButton_ConfirmEnemyAttack"` (just the entity name, not the component).
- `Courageous.cs` publishes `DebugCommandEvent { Command = "EndTurn" }` — needs updating to typed event.
- `AmbushDisplaySystem` publishes `DebugCommandEvent { Command = "ConfirmEnemyAttack" }` in 2 places — needs updating to typed event.
- Spec says make `UIElementEventDelegateService` non-static with constructor injection. Simpler approach: keep it static, add `EntityManager` parameter to `HandleEvent`. Both callers already have `EntityManager`.

---

### Task 1: Add SuppressCount to UIElement, CardClicked enum value, and typed events

**Files:**
- Modify: `ECS/Components/CardComponents.cs` (UIElement class ~line 335, UIElementEventType enum ~line 377)
- Modify: `ECS/Events/CardEvents.cs` (add new event classes)

This task is purely additive — nothing breaks.

- [ ] **Step 1: Add SuppressCount to UIElement**

In `ECS/Components/CardComponents.cs`, replace the `UIElement` class (lines 335-351) with a version that has `SuppressCount` and computed `IsInteractable`:

```csharp
public class UIElement : IComponent
{
    public Entity Owner { get; set; }
    
    public Rectangle Bounds { get; set; }
    public bool BaseInteractable { get; set; } = false;
    public int SuppressCount { get; set; } = 0;
    public bool IsInteractable
    {
        get => BaseInteractable && SuppressCount == 0;
        set => BaseInteractable = value;
    }
    public bool IsHovered { get; set; } = false;
    public bool IsClicked { get; set; } = false;
    public string Tooltip { get; set; } = "";
    public TooltipType TooltipType { get; set; } = TooltipType.Text;
    public TooltipPosition TooltipPosition { get; set; } = TooltipPosition.Above;
    public int TooltipOffsetPx { get; set; } = 6;
    public UIElementEventType EventType { get; set; } = UIElementEventType.None;
    public UILayerType LayerType { get; set; } = UILayerType.Default;
    public bool IsPreventDefaultClick { get; set; } = false;
    public bool IsHidden { get; set; } = false;

    public void Suppress() => SuppressCount++;
    public void Restore() => SuppressCount = Math.Max(0, SuppressCount - 1);
}
```

Note: `IsInteractable`'s setter writes to `BaseInteractable`, getter returns `BaseInteractable && SuppressCount == 0`. This maintains full backward compatibility — all existing code that sets `ui.IsInteractable = true/false` continues to work. Add `using System;` if not already present at the top of the file.

- [ ] **Step 2: Add CardClicked to UIElementEventType enum**

In `ECS/Components/CardComponents.cs`, add `CardClicked` to the `UIElementEventType` enum (after line ~416, before the closing brace):

```csharp
CardClicked,
```

- [ ] **Step 3: Add typed event classes**

In `ECS/Events/CardEvents.cs`, add these event classes at the end of the namespace (before the closing brace):

```csharp
public class EndTurnRequested { }

public class SkipPledgeRequested { }

public class ConfirmBlocksRequested { }
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeds with no errors.

- [ ] **Step 5: Commit**

```bash
git add ECS/Components/CardComponents.cs ECS/Events/CardEvents.cs
git commit -m "add SuppressCount to UIElement, CardClicked enum value, and typed events"
```

---

### Task 2: Expand UIElementEventDelegateService

**Files:**
- Modify: `ECS/Scenes/UIElementEventDelegateSystem.cs`

Add `EntityManager` parameter and new handler cases. Old callers will be updated in later tasks.

- [ ] **Step 1: Add EntityManager parameter and new cases**

Replace the entire content of `ECS/Scenes/UIElementEventDelegateSystem.cs` with:

```csharp
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    internal static class UIElementEventDelegateService
    {
        public static void HandleEvent(UIElementEventType type, Entity entity, EntityManager entityManager)
        {
            switch(type)
            {
                case UIElementEventType.ConfirmBlocks:
                {
                    EventManager.Publish(new ConfirmBlocksRequested());
                    break;
                }
                case UIElementEventType.UnassignCardAsBlock:
                {
                    EventManager.Publish(new UnassignCardAsBlockRequested { CardEntity = entity });
                    break;
                }
                case UIElementEventType.ActivateEquipment:
                {
                    EventManager.Publish(new ActivateEquipmentRequested { EquipmentEntity = entity });
                    break;
                }
                case UIElementEventType.CardListModalClose:
                {
                    EventManager.Publish(new CloseCardListModalEvent { });
                    break;
                }
                case UIElementEventType.QuestSelect:
                {
                    EventManager.Publish(new QuestSelectRequested { Entity = entity });
                    break;
                }
                case UIElementEventType.PayCostCancel:
                {
                    EventManager.Publish(new PayCostCancelRequested());
                    break;
                }
                case UIElementEventType.AbandonQuest:
                {
                    EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
                    break;
                }
                case UIElementEventType.GoToCustomize:
                {
                    EventManager.Publish(new ShowTransition { Scene = SceneId.CustomizationV2, SkipHold = true });
                    break;
                }
                case UIElementEventType.LeaveShop:
                {
                    EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
                    break;
                }
                case UIElementEventType.CardClicked:
                {
                    var payStateEntity = entityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                    var payState = payStateEntity?.GetComponent<PayCostOverlayState>();
                    if (payState != null && payState.IsOpen)
                    {
                        EventManager.Publish(new PayCostCandidateClicked { Card = entity });
                    }
                    else
                    {
                        EventManager.Publish(new PlayCardRequested { Card = entity });
                    }
                    break;
                }
                case UIElementEventType.EndTurn:
                {
                    EventManager.Publish(new EndTurnRequested());
                    break;
                }
                case UIElementEventType.SkipPledge:
                {
                    EventManager.Publish(new SkipPledgeRequested());
                    break;
                }
                case UIElementEventType.ViewDeck:
                {
                    var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var deck = deckEntity?.GetComponent<Deck>();
                    if (deck != null)
                    {
                        EventManager.Publish(new OpenCardListModalEvent { Title = "Draw Pile", Cards = deck.DrawPile.ToList() });
                    }
                    break;
                }
                case UIElementEventType.ViewDiscard:
                {
                    var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var deck = deckEntity?.GetComponent<Deck>();
                    if (deck != null)
                    {
                        EventManager.Publish(new OpenCardListModalEvent { Title = "Discard Pile", Cards = deck.DiscardPile.ToList() });
                    }
                    break;
                }
                default:
                {
                    if (type != UIElementEventType.None)
                    {
                        Console.WriteLine($"UIElementEventDelegateService: unhandled event type {type} on entity {entity.Id}");
                    }
                    break;
                }
            }
        }
    }
}
```

Key changes:
- `HandleEvent` now takes `EntityManager` as third parameter
- `ConfirmBlocks` now publishes `ConfirmBlocksRequested` instead of `DebugCommandEvent`
- New cases: `CardClicked`, `EndTurn`, `SkipPledge`, `ViewDeck`, `ViewDiscard`
- Default case only logs for non-None types (many entities have `EventType.None`)

- [ ] **Step 2: Update existing callers to pass EntityManager**

In `ECS/Scenes/HotKeySystem.cs`, line 75, change:
```csharp
UIElementEventDelegateService.HandleEvent(ui.EventType, entity);
```
to:
```csharp
UIElementEventDelegateService.HandleEvent(ui.EventType, entity, EntityManager);
```

In `ECS/Scenes/InputSystem.cs`, line 249, change:
```csharp
UIElementEventDelegateService.HandleEvent(uiElement.EventType, entity);
```
to:
```csharp
UIElementEventDelegateService.HandleEvent(uiElement.EventType, entity, EntityManager);
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. Existing behavior unchanged — old callers pass EntityManager but no new event types are being emitted yet.

- [ ] **Step 4: Commit**

```bash
git add ECS/Scenes/UIElementEventDelegateSystem.cs ECS/Scenes/HotKeySystem.cs ECS/Scenes/InputSystem.cs
git commit -m "expand UIElementEventDelegateService with EntityManager and new event type handlers"
```

---

### Task 3: Wire entity factories to use EventType

**Files:**
- Modify: `ECS/Factories/EntityFactory.cs:361`
- Modify: `ECS/Scenes/BattleScene/DrawPileDisplaySystem.cs:130-136`
- Modify: `ECS/Scenes/BattleScene/DiscardPileDisplaySystem.cs:131-146`
- Modify: `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs:207-210`

Set `UIElementEventType` at entity creation so entities are wired for the new dispatch path. Old dispatch paths in InputSystem still work as fallback.

- [ ] **Step 1: Set CardClicked on card entities**

In `ECS/Factories/EntityFactory.cs`, line 361, change:
```csharp
var uiElement = new UIElement { Bounds = new Rectangle(-1000, -1000, 250, 350), IsInteractable = true, TooltipPosition = TooltipPosition.Above, TooltipOffsetPx = 30 };
```
to:
```csharp
var uiElement = new UIElement { Bounds = new Rectangle(-1000, -1000, 250, 350), IsInteractable = true, TooltipPosition = TooltipPosition.Above, TooltipOffsetPx = 30, EventType = UIElementEventType.CardClicked };
```

- [ ] **Step 2: Set ViewDeck on draw pile entity**

In `ECS/Scenes/BattleScene/DrawPileDisplaySystem.cs`, line 134, change:
```csharp
EntityManager.AddComponent(root, new UIElement { Bounds = scaledRect, IsInteractable = true, Tooltip = "View Draw Pile" });
```
to:
```csharp
EntityManager.AddComponent(root, new UIElement { Bounds = scaledRect, IsInteractable = true, Tooltip = "View Draw Pile", EventType = UIElementEventType.ViewDeck });
```

Keep the `DrawPileClickable` component addition on line 135 for now — InputSystem still reads it. It will be removed in Task 10.

- [ ] **Step 3: Set ViewDiscard on discard pile entity**

In `ECS/Scenes/BattleScene/DiscardPileDisplaySystem.cs`, line 135, change:
```csharp
EntityManager.AddComponent(root, new UIElement { Bounds = scaledRect, IsInteractable = true, Tooltip = "View Discard Pile" });
```
to:
```csharp
EntityManager.AddComponent(root, new UIElement { Bounds = scaledRect, IsInteractable = true, Tooltip = "View Discard Pile", EventType = UIElementEventType.ViewDiscard });
```

Keep the `DiscardPileClickable` component addition on line 136 for now.

- [ ] **Step 4: Set EndTurn EventType on end turn button**

In `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs`, line 210, change:
```csharp
EntityManager.AddComponent(endBtn, new UIElement { Bounds = btnRect, IsInteractable = true, IsHidden = true });
```
to:
```csharp
EntityManager.AddComponent(endBtn, new UIElement { Bounds = btnRect, IsInteractable = true, IsHidden = true, EventType = UIElementEventType.EndTurn });
```

Keep the `UIButton` component addition on line 208 for now — HotKeySystem still reads it.

- [ ] **Step 5: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. Entities now have EventType set, but old dispatch paths still work.

- [ ] **Step 6: Commit**

```bash
git add ECS/Factories/EntityFactory.cs ECS/Scenes/BattleScene/DrawPileDisplaySystem.cs ECS/Scenes/BattleScene/DiscardPileDisplaySystem.cs ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs
git commit -m "set UIElementEventType on card, draw pile, discard pile, and end turn entities"
```

---

### Task 4: Replace DebugCommandEvent subscriptions with typed events

**Files:**
- Modify: `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs:51-58`
- Modify: `ECS/Scenes/BattleScene/SkipPledgeDisplaySystem.cs:66-73`
- Modify: `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs:201-208`
- Modify: `ECS/Scenes/BattleScene/EnemyDisplaySystem.cs:41-48`
- Modify: `ECS/Scenes/BattleScene/AmbushDisplaySystem.cs:209,320`
- Modify: `ECS/Objects/Cards/Courageous.cs:24`

Switch subscribers and publishers from `DebugCommandEvent` string matching to typed events.

- [ ] **Step 1: EndTurnDisplaySystem — subscribe to EndTurnRequested**

In `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs`, replace lines 50-58:
```csharp
// Hook debug command to support button click via input system
EventManager.Subscribe<DebugCommandEvent>(evt =>
{
    if (evt.Command == "EndTurn")
    {
        System.Console.WriteLine("[EndTurnDisplaySystem] DebugCommand EndTurn received");
        OnEndTurnPressed();
    }
});
```
with:
```csharp
EventManager.Subscribe<EndTurnRequested>(_ =>
{
    System.Console.WriteLine("[EndTurnDisplaySystem] EndTurnRequested received");
    OnEndTurnPressed();
});
```

- [ ] **Step 2: SkipPledgeDisplaySystem — subscribe to SkipPledgeRequested**

In `ECS/Scenes/BattleScene/SkipPledgeDisplaySystem.cs`, replace lines 65-73:
```csharp
// Handle button click via debug command
EventManager.Subscribe<DebugCommandEvent>(evt =>
{
    if (evt.Command == "SkipPledge")
    {
        Console.WriteLine("[SkipPledgeDisplaySystem] SkipPledge command received");
        OnSkipPledgePressed();
    }
});
```
with:
```csharp
EventManager.Subscribe<SkipPledgeRequested>(_ =>
{
    Console.WriteLine("[SkipPledgeDisplaySystem] SkipPledgeRequested received");
    OnSkipPledgePressed();
});
```

- [ ] **Step 3: EnemyAttackDisplaySystem — subscribe to ConfirmBlocksRequested**

In `ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs`, replace lines 201-208:
```csharp
EventManager.Subscribe<DebugCommandEvent>(evt =>
{
    if (evt.Command == "ConfirmEnemyAttack")
    {
        Console.WriteLine("[EnemyAttackDisplaySystem] DebugCommand ConfirmEnemyAttack received");
        OnConfirmPressed();
    }
});
```
with:
```csharp
EventManager.Subscribe<ConfirmBlocksRequested>(_ =>
{
    Console.WriteLine("[EnemyAttackDisplaySystem] ConfirmBlocksRequested received");
    OnConfirmPressed();
});
```

- [ ] **Step 4: EnemyDisplaySystem — remove dead EnemyAbsorbPulse subscription**

In `ECS/Scenes/BattleScene/EnemyDisplaySystem.cs`, delete lines 41-48:
```csharp
EventManager.Subscribe<DebugCommandEvent>(evt =>
{
    if (evt.Command == "EnemyAbsorbPulse")
    {
        _pulseTimerSeconds = _pulseDurationSeconds;
        System.Console.WriteLine("[EnemyDisplaySystem] DebugCommand EnemyAbsorbPulse received");
    }
});
```

This subscription was never triggered — `"EnemyAbsorbPulse"` is never published anywhere.

- [ ] **Step 5: AmbushDisplaySystem — publish ConfirmBlocksRequested**

In `ECS/Scenes/BattleScene/AmbushDisplaySystem.cs`, replace line 209:
```csharp
EventManager.Publish(new DebugCommandEvent { Command = "ConfirmEnemyAttack" });
```
with:
```csharp
EventManager.Publish(new ConfirmBlocksRequested());
```

And replace line 320 (debug action):
```csharp
EventManager.Publish(new DebugCommandEvent { Command = "ConfirmEnemyAttack" });
```
with:
```csharp
EventManager.Publish(new ConfirmBlocksRequested());
```

- [ ] **Step 6: Courageous.cs — publish EndTurnRequested**

In `ECS/Objects/Cards/Courageous.cs`, replace line 24:
```csharp
EventManager.Publish(new DebugCommandEvent { Command = "EndTurn" });
```
with:
```csharp
EventManager.Publish(new EndTurnRequested());
```

- [ ] **Step 7: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. Both old (DebugCommandEvent via UIButton) and new (typed events via EventType) paths coexist — EndTurn/SkipPledge buttons still have UIButton components, but the systems now listen for the typed events instead.

- [ ] **Step 8: Commit**

```bash
git add ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs ECS/Scenes/BattleScene/SkipPledgeDisplaySystem.cs ECS/Scenes/BattleScene/EnemyAttackDisplaySystem.cs ECS/Scenes/BattleScene/EnemyDisplaySystem.cs ECS/Scenes/BattleScene/AmbushDisplaySystem.cs ECS/Objects/Cards/Courageous.cs
git commit -m "replace DebugCommandEvent string dispatch with typed events"
```

---

### Task 5: Add interactability suppression to CardListModalSystem and PayCostOverlaySystem

**Files:**
- Modify: `ECS/Scenes/CardListModalSystem.cs`
- Modify: `ECS/Scenes/BattleScene/PayCostOverlaySystem.cs`

Each overlay system suppresses entities it wants to gate on open, restores on close.

- [ ] **Step 1: CardListModalSystem — suppress on open, restore on close**

In `ECS/Scenes/CardListModalSystem.cs`, add a field to track suppressed entities:

```csharp
private readonly HashSet<Entity> _suppressedByModal = new HashSet<Entity>();
```

In the `OpenModal` method (around line 279), after `cmp.IsOpen = true`, add suppression logic:

```csharp
// Suppress all UI entities except modal close buttons and cards
RestoreModalSuppressed();
foreach (var e in EntityManager.GetEntitiesWithComponent<UIElement>())
{
    if (e.GetComponent<CardListModalClose>() != null) continue;
    if (e.GetComponent<CardData>() != null) continue;
    var ui = e.GetComponent<UIElement>();
    if (ui != null && ui.BaseInteractable)
    {
        ui.Suppress();
        _suppressedByModal.Add(e);
    }
}
```

In the `CloseModal` method (around line 300), before `cmp.IsOpen = false`, add restore logic:

```csharp
RestoreModalSuppressed();
```

Add a helper method:

```csharp
private void RestoreModalSuppressed()
{
    foreach (var e in _suppressedByModal)
    {
        var ui = e.GetComponent<UIElement>();
        ui?.Restore();
    }
    _suppressedByModal.Clear();
}
```

- [ ] **Step 2: PayCostOverlaySystem — suppress on open, restore on close**

In `ECS/Scenes/BattleScene/PayCostOverlaySystem.cs`, add a field:

```csharp
private readonly HashSet<Entity> _suppressedByPayCost = new HashSet<Entity>();
```

In the `OnOpen` method (around line 271), after `state.IsOpen = true`, add:

```csharp
// Suppress all UI entities except cancel button, hand cards, and selected-for-payment
RestorePayCostSuppressed();
var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
var deck = deckEntity?.GetComponent<Deck>();
foreach (var e in EntityManager.GetEntitiesWithComponent<UIElement>())
{
    if (e.GetComponent<PayCostCancelButton>() != null) continue;
    var isCard = e.GetComponent<CardData>() != null;
    if (isCard && ((deck != null && deck.Hand.Contains(e)) || e.GetComponent<SelectedForPayment>() != null)) continue;
    var ui = e.GetComponent<UIElement>();
    if (ui != null && ui.BaseInteractable)
    {
        ui.Suppress();
        _suppressedByPayCost.Add(e);
    }
}
```

In the `Close` method (around line 320), before `state.IsOpen = false`, add:

```csharp
RestorePayCostSuppressed();
```

Add a helper method:

```csharp
private void RestorePayCostSuppressed()
{
    foreach (var e in _suppressedByPayCost)
    {
        var ui = e.GetComponent<UIElement>();
        ui?.Restore();
    }
    _suppressedByPayCost.Clear();
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. Both InputSystem's inline filtering AND overlay suppression now coexist — double-filtering is safe because the overlay suppression is a subset of what InputSystem would filter.

- [ ] **Step 4: Commit**

```bash
git add ECS/Scenes/CardListModalSystem.cs ECS/Scenes/BattleScene/PayCostOverlaySystem.cs
git commit -m "add interactability suppression to CardListModalSystem and PayCostOverlaySystem"
```

---

### Task 6: Add phase-based card suppression

**Files:**
- Modify: `ECS/Scenes/BattleScene/PhaseCoordinatorSystem.cs`

During Action phase, non-hand CardData entities should not be clickable.

- [ ] **Step 1: Add phase-based suppression to PhaseCoordinatorSystem**

In `ECS/Scenes/BattleScene/PhaseCoordinatorSystem.cs`, add a field:

```csharp
private readonly HashSet<Entity> _actionPhaseSuppressed = new HashSet<Entity>();
```

Subscribe to `ChangeBattlePhaseEvent` (add a second subscription after the existing one, or integrate into the existing handler). After the phase is set, add:

```csharp
private void OnPhaseChangedForInteractability(ChangeBattlePhaseEvent evt)
{
    // Restore any previously suppressed cards
    foreach (var e in _actionPhaseSuppressed)
    {
        var ui = e.GetComponent<UIElement>();
        ui?.Restore();
    }
    _actionPhaseSuppressed.Clear();

    // During Action phase, suppress non-hand cards
    if (evt.Current == SubPhase.Action)
    {
        var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
        var deck = deckEntity?.GetComponent<Deck>();
        if (deck != null)
        {
            foreach (var e in EntityManager.GetEntitiesWithComponent<CardData>())
            {
                if (deck.Hand.Contains(e)) continue;
                var ui = e.GetComponent<UIElement>();
                if (ui != null && ui.BaseInteractable)
                {
                    ui.Suppress();
                    _actionPhaseSuppressed.Add(e);
                }
            }
        }
    }
}
```

In the constructor, subscribe:
```csharp
EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChangedForInteractability);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. Phase-based suppression now coexists with InputSystem's existing phase filtering.

- [ ] **Step 3: Commit**

```bash
git add ECS/Scenes/BattleScene/PhaseCoordinatorSystem.cs
git commit -m "add phase-based card interactability suppression to PhaseCoordinatorSystem"
```

---

### Task 7: Slim InputSystem

**Files:**
- Modify: `ECS/Scenes/InputSystem.cs`

Remove all filtering logic, `HandleUIClick`, `HandleCardClick`, and profiler toggle. Dispatch all clicks through the delegate service.

- [ ] **Step 1: Replace InputSystem.Update with the slim version**

Replace the `Update` method (lines 38-209) with:

```csharp
public override void Update(GameTime gameTime)
{
    if (!IsActive) return;

    if (!Game1.WindowIsActive)
    {
        _previousMouseState = Mouse.GetState();
        _previousKeyboardState = Keyboard.GetState();
        return;
    }

    var mouseState = Mouse.GetState();
    var mousePosition = mouseState.Position;
    bool hasCursor = _cursorEvent != null;
    Vector2 pointerVec;
    if (hasCursor)
    {
        pointerVec = _cursorEvent.Position;
    }
    else
    {
        var dest = Game1.RenderDestination;
        float scaleX = (float)dest.Width / Game1.VirtualWidth;
        float scaleY = (float)dest.Height / Game1.VirtualHeight;
        if (scaleX <= 0.001f) scaleX = 1f;
        if (scaleY <= 0.001f) scaleY = 1f;
        float virtX = (mousePosition.X - dest.X) / scaleX;
        float virtY = (mousePosition.Y - dest.Y) / scaleY;
        pointerVec = new Vector2(virtX, virtY);
    }
    var pointerPoint = new Point((int)Math.Round(pointerVec.X), (int)Math.Round(pointerVec.Y));
    var keyboardState = Keyboard.GetState();

    // Collect all interactable UI elements — trust IsInteractable (overlay systems manage suppression)
    var uiEntities = GetRelevantEntities()
        .Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), IsCard = e.GetComponent<CardData>() != null })
        .Where(x => x.UI != null && x.UI.IsInteractable)
        .ToList();

    // Reset hover and click flags
    foreach (var x in uiEntities)
    {
        x.UI.IsHovered = false;
        x.UI.IsClicked = false;
    }

    // Determine the top entity under cursor
    dynamic top = null;
    if (_cursorEvent != null && _cursorEvent.TopEntity != null)
    {
        var topEntity = _cursorEvent.TopEntity;
        var topUI = topEntity.GetComponent<UIElement>();
        var topT = topEntity.GetComponent<Transform>();
        var topIsCard = topEntity.GetComponent<CardData>() != null;
        if (uiEntities.Any(x => x.E == topEntity))
        {
            top = new { E = topEntity, UI = topUI, T = topT, IsCard = topIsCard };
        }
    }
    else
    {
        var underMouse = uiEntities
            .Where(x =>
            {
                if (x.UI.Bounds.Width < 2 || x.UI.Bounds.Height < 2) return false;
                return IsUnderMouse(x, pointerPoint);
            })
            .OrderByDescending(x => x.T?.ZOrder ?? 0)
            .ToList();
        top = underMouse.FirstOrDefault();
    }

    if (top != null && !StateSingleton.PreventClicking && !StateSingleton.IsTutorialActive)
    {
        top.UI.IsHovered = true;

        bool mouseEdge = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
        bool controllerEdge = _cursorEvent != null && _cursorEvent.IsAPressedEdge && _cursorEvent.Source != InputMethod.Mouse;
        bool isClickEdge = mouseEdge || controllerEdge;

        if (isClickEdge)
        {
            top.UI.IsClicked = true;
            var uiElement = ((Entity)top.E).GetComponent<UIElement>();
            if (uiElement != null && uiElement.EventType != UIElementEventType.None)
            {
                UIElementEventDelegateService.HandleEvent(uiElement.EventType, top.E, EntityManager);
            }
        }
    }

    _previousMouseState = mouseState;
    _previousKeyboardState = keyboardState;
    _cursorEvent = null;
}
```

- [ ] **Step 2: Delete HandleUIClick and HandleCardClick methods**

Remove the `HandleUIClick` method (lines 243-311 in the original) and `HandleCardClick` method (lines 313-335 in the original). They are no longer called.

- [ ] **Step 3: Delete ToggleProfilerOverlay method**

Remove the `ToggleProfilerOverlay` method (lines 345-358 in the original). This moves to Game1 in Task 8.

- [ ] **Step 4: Clean up unused imports**

Remove any imports that are no longer needed. The following may now be unused — verify before removing:
- `System.Collections.Generic` (still needed for `IEnumerable`)
- `System.Linq` (still needed for `Select`, `Where`, etc.)

- [ ] **Step 5: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. InputSystem now delegates all click handling to the delegate service. No more inline filtering — overlay systems and phase coordinator handle interactability.

- [ ] **Step 6: Commit**

```bash
git add ECS/Scenes/InputSystem.cs
git commit -m "slim InputSystem to thin hover + click layer with delegated dispatch"
```

---

### Task 8: Move profiler toggle to Game1

**Files:**
- Modify: `Game1.cs`

- [ ] **Step 1: Add profiler toggle to Game1.Update**

In `Game1.cs`, after the entity list overlay toggle block (after line 272), add:

```csharp
// Toggle profiler overlay on P key press
if (kb.IsKeyDown(Keys.P) && !_prevKeyboard.IsKeyDown(Keys.P))
{
    var e = _world.EntityManager.GetEntitiesWithComponent<ProfilerOverlay>().FirstOrDefault();
    if (e == null)
    {
        e = _world.EntityManager.CreateEntity("ProfilerOverlay");
        _world.EntityManager.AddComponent(e, new ProfilerOverlay { IsOpen = true });
    }
    else
    {
        var p = e.GetComponent<ProfilerOverlay>();
        p.IsOpen = !p.IsOpen;
    }
}
```

Ensure `ProfilerOverlay` import is present. If Game1.cs doesn't already import the components namespace, add:
```csharp
using Crusaders30XX.ECS.Components;
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. Profiler toggle now handled in Game1 alongside debug menu toggle.

- [ ] **Step 3: Commit**

```bash
git add Game1.cs
git commit -m "move profiler toggle from InputSystem to Game1"
```

---

### Task 9: Remove UIButton dispatch from HotKeySystem and HotKeyProgressRingSystem

**Files:**
- Modify: `ECS/Scenes/HotKeySystem.cs:54-91`
- Modify: `ECS/Scenes/HotKeyProgressRingSystem.cs:105,201`

- [ ] **Step 1: Simplify HotKeySystem.ProcessHotKeyClick**

In `ECS/Scenes/HotKeySystem.cs`, replace the `ProcessHotKeyClick` method (lines 54-91) with:

```csharp
private void ProcessHotKeyClick(Entity entity)
{
    var ui = entity.GetComponent<UIElement>();
    var hotKey = entity.GetComponent<HotKey>();

    if (hotKey != null && hotKey.ParentEntity != null)
    {
        Console.WriteLine($"Processing hotkey click for parent entity: {hotKey.ParentEntity.Id}");
        ProcessHotKeyClick(hotKey.ParentEntity);
        return;
    }

    Console.WriteLine($"Processing hotkey click for entity: {entity.Id} {entity.Name} ui={ui?.EventType} uiClicked={ui?.IsClicked}");
    
    if (ui != null && ui.EventType != UIElementEventType.None)
    {
        UIElementEventDelegateService.HandleEvent(ui.EventType, entity, EntityManager);
        if (ui.IsInteractable)
        {
            EventManager.Publish(new HotKeySelectEvent { Entity = entity });
        }
    }
    else if (ui != null)
    {
        ui.IsClicked = true;
        if (ui.IsInteractable)
        {
            EventManager.Publish(new HotKeySelectEvent { Entity = entity });
        }
    }
}
```

This removes the `var btn = entity.GetComponent<UIButton>()` lookup and the `btn.Command` dispatch branch.

- [ ] **Step 2: Remove UIButton from HotKeySystem LINQ queries**

In `ECS/Scenes/HotKeySystem.cs`, line 130, change:
```csharp
.Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), Btn = e.GetComponent<UIButton>() })
```
to:
```csharp
.Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>() })
```

Do the same at line 182 (same pattern in the gamepad input block).

- [ ] **Step 3: Remove UIButton from HotKeyProgressRingSystem LINQ queries**

In `ECS/Scenes/HotKeyProgressRingSystem.cs`, line 105, change:
```csharp
.Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), Btn = e.GetComponent<UIButton>() })
```
to:
```csharp
.Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>() })
```

Do the same at line 201 (same pattern).

- [ ] **Step 4: Remove UIButton import if present**

Check if `HotKeySystem.cs` or `HotKeyProgressRingSystem.cs` have unused UIButton-related imports after these changes. The component is in `Crusaders30XX.ECS.Components` which is likely still needed for other component types.

- [ ] **Step 5: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. HotKey systems no longer reference UIButton.

- [ ] **Step 6: Commit**

```bash
git add ECS/Scenes/HotKeySystem.cs ECS/Scenes/HotKeyProgressRingSystem.cs
git commit -m "remove UIButton dispatch from HotKeySystem and HotKeyProgressRingSystem"
```

---

### Task 10: Delete dead components and remove UIButton from entity creation

**Files:**
- Modify: `ECS/Components/CardComponents.cs` — delete `UIButton`, `DrawPileClickable`, `DiscardPileClickable`
- Modify: `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs:203,208` — remove UIButton creation and lookup
- Modify: `ECS/Scenes/BattleScene/SkipPledgeDisplaySystem.cs:201` — remove UIButton creation
- Modify: `ECS/Scenes/BattleScene/DrawPileDisplaySystem.cs:135` — remove DrawPileClickable addition
- Modify: `ECS/Scenes/BattleScene/DiscardPileDisplaySystem.cs:136,143-146` — remove DiscardPileClickable additions
- Modify: `ECS/Scenes/BattleScene/MustBeBlockedSystem.cs` — entity name `"UIButton_ConfirmEnemyAttack"` is fine to keep (it's just a name, not a component reference)

- [ ] **Step 1: Remove UIButton creation from EndTurnDisplaySystem**

In `ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs`, delete line 208:
```csharp
EntityManager.AddComponent(endBtn, new UIButton { Label = "End Turn", Command = "EndTurn" });
```

Also update line 203, which looks up the entity by UIButton component:
```csharp
var endBtn = EntityManager.GetEntitiesWithComponent<UIButton>().FirstOrDefault(e => e.GetComponent<UIButton>().Command == "EndTurn");
```
Change to:
```csharp
var endBtn = EntityManager.GetEntity("UIButton_EndTurn");
```

- [ ] **Step 2: Remove UIButton creation from SkipPledgeDisplaySystem**

In `ECS/Scenes/BattleScene/SkipPledgeDisplaySystem.cs`, delete line 201:
```csharp
EntityManager.AddComponent(skipBtn, new UIButton { Label = "Skip Pledge", Command = "SkipPledge" });
```

- [ ] **Step 3: Remove DrawPileClickable from DrawPileDisplaySystem**

In `ECS/Scenes/BattleScene/DrawPileDisplaySystem.cs`, delete line 135:
```csharp
EntityManager.AddComponent(root, new DrawPileClickable());
```

- [ ] **Step 4: Remove DiscardPileClickable from DiscardPileDisplaySystem**

In `ECS/Scenes/BattleScene/DiscardPileDisplaySystem.cs`, delete line 136:
```csharp
EntityManager.AddComponent(root, new DiscardPileClickable());
```

Also delete lines 143-146:
```csharp
if (root.GetComponent<DiscardPileClickable>() == null)
{
    EntityManager.AddComponent(root, new DiscardPileClickable());
}
```

- [ ] **Step 5: Delete component classes from CardComponents.cs**

In `ECS/Components/CardComponents.cs`, delete the `UIButton` class (lines 701-706):
```csharp
public class UIButton : IComponent
{
    public Entity Owner { get; set; }
    public string Label { get; set; } = "";
    public string Command { get; set; } = "";
}
```

Delete the `DrawPileClickable` class (lines 651-654):
```csharp
public class DrawPileClickable : IComponent
{
    public Entity Owner { get; set; }
}
```

Delete the `DiscardPileClickable` class (lines 659-662):
```csharp
public class DiscardPileClickable : IComponent
{
    public Entity Owner { get; set; }
}
```

- [ ] **Step 6: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. If there are compile errors, they indicate missed references to the deleted components — find and remove them.

- [ ] **Step 7: Commit**

```bash
git add ECS/Components/CardComponents.cs ECS/Scenes/BattleScene/EndTurnDisplaySystem.cs ECS/Scenes/BattleScene/SkipPledgeDisplaySystem.cs ECS/Scenes/BattleScene/DrawPileDisplaySystem.cs ECS/Scenes/BattleScene/DiscardPileDisplaySystem.cs
git commit -m "remove UIButton, DrawPileClickable, and DiscardPileClickable components"
```

---

### Task 11: Final verification

- [ ] **Step 1: Full build**

Run: `dotnet build`
Expected: Clean build, zero errors, zero warnings related to these changes.

- [ ] **Step 2: Search for any remaining references to deleted components**

Search the codebase for:
- `UIButton` (should only appear in entity NAME strings like `"UIButton_EndTurn"`, not as component references)
- `DrawPileClickable`
- `DiscardPileClickable`
- `HandleUIClick`
- `HandleCardClick`

Fix any remaining references found.

- [ ] **Step 3: Search for any remaining DebugCommandEvent usage for migrated commands**

Search for:
- `"EndTurn"` in DebugCommandEvent contexts
- `"SkipPledge"` in DebugCommandEvent contexts
- `"ConfirmEnemyAttack"` in DebugCommandEvent contexts

Note: `DebugCommandEvent` is still used for `"AnimateAssignedBlocksToDiscard"` and `"EventQueue.Published"` — these are out of scope and should remain.

- [ ] **Step 4: Commit any fixes**

```bash
git add -A
git commit -m "fix remaining references from InputSystem refactor"
```
