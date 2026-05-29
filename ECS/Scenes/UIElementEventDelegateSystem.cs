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
            LoggingService.Append("UIElementEventDelegateService.HandleEvent", new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = type.ToString(),
                ["entityId"] = entity.Id,
                ["entityName"] = entity.Name,
                ["entityType"] = entity.GetType().Name,
            });
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
                        LoggingService.Append("UIElementEventDelegateService.CardClicked", new System.Text.Json.Nodes.JsonObject
                        {
                            ["branch"] = "PayCostCandidateClicked",
                            ["entityId"] = entity.Id,
                        });
                        EventManager.Publish(new PayCostCandidateClicked { Card = entity });
                    }
                    else
                    {
                        LoggingService.Append("UIElementEventDelegateService.CardClicked", new System.Text.Json.Nodes.JsonObject
                        {
                            ["branch"] = "PlayCardRequested",
                            ["entityId"] = entity.Id,
                            ["payStateExists"] = payStateEntity != null,
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
