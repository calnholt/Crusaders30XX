using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using System;
using Crusaders30XX.ECS.Data.Cards;

namespace Crusaders30XX.ECS.Systems
{
    internal static class EvaluateAdditionalCostService
    {
        public static bool CanPay(EntityManager entityManager, string cardId)
        {
            CardDefinitionCache.TryGet(cardId, out CardDefinition def);
            switch (cardId)
            {
                case "impale":
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    var courageCmp = player?.GetComponent<Courage>();
                    int courage = courageCmp?.Amount ?? 0;
                    if (courage < def.valuesParse[0])
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {def.valuesParse[0]} courage!" });
                        return false;
                    }
                    return true;
                }
                case "shroud_of_turin":
                {
                    var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var deck = deckEntity?.GetComponent<Deck>();
                    if (deck == null) return false;
                    var cardsInHand = deck.Hand.FindAll(c => {
                        var cd = c.GetComponent<CardData>();
                        if (cd == null) return false;
                        CardDefinitionCache.TryGet(cd.CardId, out CardDefinition cdDef);
                        return !cdDef.isWeapon && cd.CardId != cardId;
                    });
                    if (cardsInHand.Count == 0)
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = $"Requires at least one card in hand!" });
                        return false;
                    }
                    return true;  
                }
                case "stab":
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    int courage = player?.GetComponent<Courage>()?.Amount ?? 0;
                    if (courage < def.valuesParse[0])
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {def.valuesParse[0]} courage!" });
                        return false;
                    }
                    return true;
                }
                case "sword":
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    var courageCmp = player?.GetComponent<Courage>();
                    int courage = courageCmp?.Amount ?? 0;
                    if (courage < def.valuesParse[0])
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {def.valuesParse[0]} courage!" });
                        return false;
                    }
                    return true;
                }
                default:
                    return true;
            }
        }
    }
}


