using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using System;
using Crusaders30XX.ECS.Data.Attacks;

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
            // card effects
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
            var contextId = card.GetComponent<AssignedBlockCard>().ContextId;
            var planned = entityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault(e => e.GetComponent<AttackIntent>().Planned.Any(pa => pa.ContextId == contextId));
            if (planned == null) return;
            var attackId = planned.GetComponent<AttackIntent>().Planned.FirstOrDefault(pa => pa.ContextId == contextId).AttackId;
            AttackDefinitionCache.TryGet(attackId, out var attackDef);
            var specialEffects = attackDef.specialEffects;
            if (specialEffects.Any(se => se.type == "Corrode"))
            {
                Console.WriteLine($"[CardBlockService] Corrode effect detected - {attackId} - {cardId}");
                BlockValueService.ApplyDelta(card, -1);
            }
        }
    }
}