using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Single authority for moving cards between zones (hand, discard, draw, exhaust, assigned block).
    /// Other systems should publish CardMoveRequested events instead of mutating Deck lists directly.
    /// </summary>
    public class CardZoneSystem : Core.System
    {
        public CardZoneSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<CardMoveRequested>(OnCardMoveRequested);
            EventManager.Subscribe<CardMoved>(OnCardMoved);
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<CardMoveFinalizeRequested>(OnCardMoveFinalizeRequested);
            Console.WriteLine("[CardZoneSystem] Subscribed to CardMoveRequested");
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnCardMoved(CardMoved evt)
        {
            if (evt.From == CardZoneType.AssignedBlock && (evt.To == CardZoneType.DiscardPile || evt.To == CardZoneType.ExhaustPile))
            {
                CardBlockService.Resolve(evt.Card, EntityManager);
            }
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current != SubPhase.PlayerEnd) return;
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            foreach (var card in deck.Hand)
            {
                var cardObj = CardFactory.Create(card.GetComponent<CardData>().Card.CardId);
                if (cardObj?.ExhaustsOnEndTurn ?? false)
                {
                    EventQueueBridge.EnqueueTriggerAction("CardZoneSystem.OnChangeBattlePhase.EndTurnExhaust", () =>
                    {
                        EventManager.Publish(new CardMoveRequested { Card = card, Deck = deckEntity, Destination = CardZoneType.ExhaustPile, Reason = "EndTurnExhaust" });
                    }, .05f);
                }
            }
        }

        private void OnCardMoveRequested(CardMoveRequested evt)
        {
            Console.WriteLine($"[CardZoneSystem] CardMoveRequested card={evt.Card?.Id} to={evt.Destination} reason={evt.Reason}");
            if (evt == null || evt.Card == null) return;

            var deckEntity = evt.Deck ?? EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            var from = GetZoneOf(deck, evt.Card);

            // Intercept Hand/HandStaged/CostSelected -> Discard on PlayCard/PayCost to run animation first; finalize will mutate zones and publish CardMoved
            if (evt.Destination == CardZoneType.DiscardPile)
            {
                bool isFromHand = deck.Hand.Contains(evt.Card);
                bool isFromCostSelected = evt.Card.GetComponent<SelectedForPayment>() != null;
                bool isFromHandStaged = false;
                {
                    var payEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                    var payState = payEntity?.GetComponent<PayCostOverlayState>();
                    if (payState != null && (payState.IsOpen || payState.IsReturning) && payState.CardToPlay == evt.Card)
                    {
                        isFromHandStaged = true;
                    }
                }

                if (isFromHand || isFromCostSelected || isFromHandStaged)
                {
                    if (evt.Card.GetComponent<AnimatingHandToDiscard>() == null)
                    {
                        EntityManager.AddComponent(evt.Card, new AnimatingHandToDiscard());
                        var uiAnim = evt.Card.GetComponent<UIElement>();
                        if (uiAnim != null)
                        {
                            uiAnim.IsInteractable = false;
                            uiAnim.IsHovered = false;
                            uiAnim.IsClicked = false;
                        }
                        EventManager.Publish(new PlayCardToDiscardAnimationRequested
                        {
                            Card = evt.Card,
                            Deck = deckEntity,
                            ContextId = evt.ContextId
                        });
                    }
                    return;
                }
            }

            // Remove from all known deck lists first to avoid duplicates
            deck.DrawPile.Remove(evt.Card);
            deck.Hand.Remove(evt.Card);
            deck.DiscardPile.Remove(evt.Card);
            deck.ExhaustPile.Remove(evt.Card);

            switch (evt.Destination)
            {
                case CardZoneType.Hand:
                {
                    // When returning to hand, remove assignment component and insert at index if provided
                    var abc = evt.Card.GetComponent<AssignedBlockCard>();
                    if (abc != null)
                    {
                        EntityManager.RemoveComponent<AssignedBlockCard>(evt.Card);
                    }
                    var sfp = evt.Card.GetComponent<SelectedForPayment>();
                    if (sfp != null)
                    {
                        EntityManager.RemoveComponent<SelectedForPayment>(evt.Card);
                    }
                    if (evt.InsertIndex.HasValue)
                    {
                        int idx = evt.InsertIndex.Value;
                        if (idx < 0) idx = 0;
                        if (idx > deck.Hand.Count) idx = deck.Hand.Count;
                        deck.Hand.Insert(idx, evt.Card);
                    }
                    else
                    {
                        if (!deck.Hand.Contains(evt.Card))
                        {
                            deck.Hand.Add(evt.Card);
                        }
                    }
                    // Make card interactable in hand
                    var uiH = evt.Card.GetComponent<UIElement>();
                    if (uiH != null)
                    {
                        uiH.IsInteractable = true;
                        uiH.IsHovered = false;
                        uiH.IsClicked = false;
                        uiH.EventType = UIElementEventType.None;
                    }
                    // Fallback restore of tooltip config if a backup still exists (e.g., if BlockAssignmentRemoved didn't run)
                    {
                        var backup = evt.Card.GetComponent<TooltipOverrideBackup>();
                        var ui = evt.Card.GetComponent<UIElement>();
                        if (backup != null && ui != null)
                        {
                            ui.TooltipType = backup.OriginalType;
                            ui.TooltipPosition = backup.OriginalPosition;
                            ui.TooltipOffsetPx = backup.OriginalOffsetPx;
                            var ct = evt.Card.GetComponent<CardTooltip>();
                            if (backup.HadCardTooltip)
                            {
                                if (ct == null) { EntityManager.AddComponent(evt.Card, new CardTooltip { CardId = backup.OriginalCardTooltipId ?? string.Empty }); }
                                else { ct.CardId = backup.OriginalCardTooltipId ?? string.Empty; }
                            }
                            else
                            {
                                if (ct != null) { EntityManager.RemoveComponent<CardTooltip>(evt.Card); }
                            }
                            EntityManager.RemoveComponent<TooltipOverrideBackup>(evt.Card);
                        }
                    }
                    var t = evt.Card.GetComponent<Transform>();
                    if (t != null && t.Position == Vector2.Zero)
                    {
                        t.Position = Vector2.Zero;
                        t.Rotation = 0f;
                        t.Scale = Vector2.One;
                    }
                    break;
                }
                case CardZoneType.HandStaged:
                {
                    // Card is staged outside of any deck list; keep it non-interactable
                    var uiS = evt.Card.GetComponent<UIElement>();
                    if (uiS != null)
                    {
                        uiS.IsInteractable = false;
                        uiS.IsHovered = false;
                        uiS.IsClicked = false;
                        uiS.EventType = UIElementEventType.None;
                    }
                    break;
                }
                case CardZoneType.CostSelected:
                {
                    // Temporarily selected to pay a cost: keep interactable so it can be unselected
                    var ui = evt.Card.GetComponent<UIElement>();
                    if (ui != null)
                    {
                        ui.IsInteractable = true;
                        ui.IsHovered = false;
                        ui.IsClicked = false;
                        ui.EventType = UIElementEventType.None;
                    }
                    var t = evt.Card.GetComponent<Transform>();
                    if (t != null) t.ZOrder = 29500;
                    if (evt.Card.GetComponent<SelectedForPayment>() == null)
                    {
                        EntityManager.AddComponent(evt.Card, new SelectedForPayment());
                    }
                    break;
                }
                case CardZoneType.AssignedBlock:
                {
                    var t = evt.Card.GetComponent<Transform>();
                    var abc = evt.Card.GetComponent<AssignedBlockCard>();
                    if (abc == null)
                    {
                        var cd = evt.Card.GetComponent<CardData>();
                        var bg = ResolveCardBgColor(cd?.Color ?? CardData.CardColor.White);
                        var fg = ResolveFgForBg(bg);
                        abc = new AssignedBlockCard
                        {
                            ContextId = evt.ContextId,
                            BlockAmount = BlockValueService.GetBlockValue(evt.Card),
                            StartPos = t?.Position ?? Vector2.Zero,
                            CurrentPos = t?.Position ?? Vector2.Zero,
                            TargetPos = t?.Position ?? Vector2.Zero,
                            StartScale = t?.Scale.X ?? 1f,
                            TargetScale = 0.35f,
                            Phase = AssignedBlockCard.PhaseState.Pullback,
                            Elapsed = 0f,
                            AssignedAtTicks = DateTime.UtcNow.Ticks,
                            IsEquipment = false,
                            ColorKey = NormalizeColorKey(cd?.Color.ToString() ?? "White"),
                            Tooltip = ResolveCardName(cd),
                            DisplayBgColor = bg,
                            DisplayFgColor = fg
                        };
                        EntityManager.AddComponent(evt.Card, abc);
                    }
                    // Do not assign HotKey here; it will be added when animation reaches Idle
                    // Ensure the card is not still in hand to prevent later clicks from finding it
                    deck.Hand.Remove(evt.Card);
                    // Make assigned block cards non-interactable as cards (interactions happen via assigned UI)
                    var uiA = evt.Card.GetComponent<UIElement>();
                    if (uiA != null)
                    {
                        uiA.IsInteractable = false;
                        uiA.IsHovered = false;
                        uiA.IsClicked = false;
                        uiA.EventType = UIElementEventType.UnassignCardAsBlock;
                    }
                    break;
                }
                case CardZoneType.DiscardPile:
                {
                    deck.DiscardPile.Add(evt.Card);
                    // Disable interactions for discarded cards
                    var uiD = evt.Card.GetComponent<UIElement>();
                    if (uiD != null)
                    {
                        uiD.IsInteractable = false;
                        uiD.IsHovered = false;
                        uiD.IsClicked = false;
                        uiD.EventType = UIElementEventType.None;
                    }
                    var sfpD = evt.Card.GetComponent<SelectedForPayment>();
                    if (sfpD != null)
                    {
                        EntityManager.RemoveComponent<SelectedForPayment>(evt.Card);
                    }
                    // Push discarded cards behind UI
                    var t = evt.Card.GetComponent<Transform>();
                    if (t != null) t.ZOrder = -10000;
                    break;
                }
                case CardZoneType.DrawPile:
                {
                    deck.DrawPile.Add(evt.Card);
                    // Not interactable in draw pile
                    var uiDP = evt.Card.GetComponent<UIElement>();
                    if (uiDP != null)
                    {
                        uiDP.IsInteractable = false;
                        uiDP.IsHovered = false;
                        uiDP.IsClicked = false;
                        uiDP.EventType = UIElementEventType.None;
                    }
                    // Reset transform so highlight hit-test uses proper defaults when re-drawn
                    var tdp = evt.Card.GetComponent<Transform>();
                    if (tdp != null)
                    {
                        tdp.Rotation = 0f;
                        if (tdp.Position == Vector2.Zero) { tdp.Position = Vector2.Zero; }
                        tdp.Scale = Vector2.One;
                    }
                    break;
                }
                case CardZoneType.ExhaustPile:
                {
                    EntityManager.DestroyEntity(evt.Card.Id);
                    break;
                }
            }

            EventManager.Publish(new CardMoved
            {
                Card = evt.Card,
                Deck = deckEntity,
                From = from,
                To = evt.Destination,
                ContextId = evt.ContextId
            });
            Console.WriteLine($"[CardZoneSystem] CardMoved from={from} to={evt.Destination}");
        }

        private void OnCardMoveFinalizeRequested(CardMoveFinalizeRequested evt)
        {
            if (evt == null || evt.Card == null) return;
            var deckEntity = evt.Deck ?? EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            var from = GetZoneOf(deck, evt.Card);
            // Normalize lists to avoid duplicates
            deck.DrawPile.Remove(evt.Card);
            deck.Hand.Remove(evt.Card);
            deck.DiscardPile.Remove(evt.Card);
            deck.ExhaustPile.Remove(evt.Card);

            switch (evt.Destination)
            {
                case CardZoneType.DiscardPile:
                {
                    deck.DiscardPile.Add(evt.Card);
                    var uiD = evt.Card.GetComponent<UIElement>();
                    if (uiD != null)
                    {
                        uiD.IsInteractable = false;
                        uiD.IsHovered = false;
                        uiD.IsClicked = false;
                        uiD.EventType = UIElementEventType.None;
                    }
                    var sfpD = evt.Card.GetComponent<SelectedForPayment>();
                    if (sfpD != null)
                    {
                        EntityManager.RemoveComponent<SelectedForPayment>(evt.Card);
                    }
                    var t = evt.Card.GetComponent<Transform>();
                    if (t != null) t.ZOrder = -10000;
                    break;
                }
                case CardZoneType.ExhaustPile:
                {
                    EntityManager.DestroyEntity(evt.Card.Id);
                    break;
                }
                default:
                {
                    // If finalize was called for an unsupported destination, no-op back to hand
                    if (!deck.Hand.Contains(evt.Card))
                    {
                        deck.Hand.Add(evt.Card);
                    }
                    break;
                }
            }

            // Clear animation marker if present
            var anim = evt.Card.GetComponent<AnimatingHandToDiscard>();
            if (anim != null) { EntityManager.RemoveComponent<AnimatingHandToDiscard>(evt.Card); }

            EventManager.Publish(new CardMoved
            {
                Card = evt.Card,
                Deck = deckEntity,
                From = from,
                To = evt.Destination,
                ContextId = evt.ContextId
            });
            Console.WriteLine($"[CardZoneSystem] Finalized CardMoved from={from} to={evt.Destination}");
        }

        private static string NormalizeColorKey(string c)
        {
            if (string.IsNullOrWhiteSpace(c)) return "White";
            switch (c.Trim().ToLowerInvariant())
            {
                case "r": case "red": return "Red";
                case "w": case "white": return "White";
                case "b": case "black": return "Black";
                default: return char.ToUpperInvariant(c[0]) + c.Substring(1);
            }
        }

        private static Microsoft.Xna.Framework.Color ResolveCardBgColor(CardData.CardColor color)
        {
            switch (color)
            {
                case CardData.CardColor.Red: return Color.DarkRed;
                case CardData.CardColor.Black: return Color.Black;
                case CardData.CardColor.White:
                default: return Color.White;
            }
        }

        private static Microsoft.Xna.Framework.Color ResolveFgForBg(Microsoft.Xna.Framework.Color bg)
        {
            return (bg == Color.White) ? Color.Black : Color.White;
        }

        private static string ResolveCardName(CardData cd)
        {
            if (cd == null) return string.Empty;
            try
            {
                var cardObj = CardFactory.Create(cd.Card.CardId);
                if (cardObj != null)
                {
                    return cardObj.Name ?? cardObj.CardId ?? string.Empty;
                }
            }
            catch { }
            return cd.Card.CardId ?? string.Empty;
        }

        private static CardZoneType GetZoneOf(Deck deck, Entity card)
        {
            if (deck.Hand.Contains(card)) return CardZoneType.Hand;
            if (deck.DrawPile.Contains(card)) return CardZoneType.DrawPile;
            if (deck.DiscardPile.Contains(card)) return CardZoneType.DiscardPile;
            if (card.HasComponent<AssignedBlockCard>()) return CardZoneType.AssignedBlock;
            if (deck.ExhaustPile.Contains(card)) return CardZoneType.ExhaustPile;
            return CardZoneType.ExhaustPile;
        }
    }
}


