using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Services;
namespace Crusaders30XX.ECS.Systems
{
    internal static class UIElementEventDelegateService
    {
        public static void HandleEvent(UIElementEventType type, Entity entity, EntityManager entityManager)
        {
            var activeDeck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            var handleLog = new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = type.ToString(),
                ["entityId"] = entity.Id,
                ["entityName"] = entity.Name,
                ["entityType"] = entity.GetType().Name,
            };
            if (entity.HasComponent<CardData>())
            {
                handleLog["cardId"] = entity.GetComponent<CardData>()?.Card?.CardId ?? "unknown";
                handleLog["inDeckHand"] = activeDeck?.Hand.Contains(entity) ?? false;
                handleLog["countsForLayout"] = HandStateLoggingService.CountsForHandLayout(entity);
                handleLog["countsForDraw"] = HandStateLoggingService.CountsForDraw(entity);
                handleLog["card"] = HandStateLoggingService.BuildCardSnapshot(entity);
            }
            LoggingService.Append("UIElementEventDelegateService.HandleEvent", handleLog);
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
                    EventManager.Publish(new RunEndSequenceRequested());
                    break;
                }
                case UIElementEventType.LeaveShop:
                {
                    EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
                    break;
                }
                case UIElementEventType.OpenLoadout:
                {
                    var deckEntity = RunDeckService.EnsureRunDeck(entityManager);
                    var deck = deckEntity?.GetComponent<Deck>();
                    if (deck?.Cards != null)
                    {
                        EventManager.Publish(new OpenCardListModalEvent { Title = "Loadout", Cards = deck.Cards.ToList() });
                    }
                    break;
                }
                case UIElementEventType.CardClicked:
                {
                    var payStateEntity = entityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                    var payState = payStateEntity?.GetComponent<PayCostOverlayState>();
                    if (payState != null && payState.IsOpen)
                    {
                        LoggingService.Append("UIElementEventDelegateService.CardClicked", new System.Text.Json.Nodes.JsonObject
                        {
                            ["branch"] = "PayCostCandidateClicked",
                            ["entityId"] = entity.Id,
                            ["cardId"] = entity.GetComponent<CardData>()?.Card?.CardId ?? "unknown",
                            ["inDeckHand"] = activeDeck?.Hand.Contains(entity) ?? false,
                            ["card"] = HandStateLoggingService.BuildCardSnapshot(entity)
                        });
                        EventManager.Publish(new PayCostCandidateClicked { Card = entity });
                    }
                    else
                    {
                        LoggingService.Append("UIElementEventDelegateService.CardClicked", new System.Text.Json.Nodes.JsonObject
                        {
                            ["branch"] = "PlayCardRequested",
                            ["entityId"] = entity.Id,
                            ["cardId"] = entity.GetComponent<CardData>()?.Card?.CardId ?? "unknown",
                            ["payStateExists"] = payStateEntity != null,
                            ["inDeckHand"] = activeDeck?.Hand.Contains(entity) ?? false,
                            ["card"] = HandStateLoggingService.BuildCardSnapshot(entity)
                        });
                        EventManager.Publish(new PlayCardRequested { Card = entity });
                    }
                    break;
                }
                case UIElementEventType.EndTurn:
                {
                    EventManager.Publish(new EndTurnRequested());
                    break;
                }
                case UIElementEventType.PledgeCard:
                {
                    EventManager.Publish(new PledgeCardRequested { Card = entity });
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
