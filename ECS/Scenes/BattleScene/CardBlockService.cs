using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using System;

namespace Crusaders30XX.ECS.Systems
{
    internal static class CardBlockService
    {
        public static void Resolve(Entity card, EntityManager entityManager)
        {
            var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var battleStateInfo = player.GetComponent<BattleStateInfo>();
            var cardId = card.GetComponent<CardData>().CardId;
            Console.WriteLine($"[CardBlockService] resolving {cardId}");
            switch (cardId)
            {
                case "bulwark":
                {
                    BlockValueService.SetDelta(card, 0);
                    break;
                }
                default:
                    break;
            }
        }
    }
}