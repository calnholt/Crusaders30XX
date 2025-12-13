using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using System;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
    internal static class CardBlockService
    {
        public static void Resolve(Entity card, EntityManager entityManager)
        {
            var cardObj = CardFactory.Create(card.GetComponent<CardData>().Card.CardId);
            var contextId = card.GetComponent<AssignedBlockCard>().ContextId;
            var planned = entityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault(e => e.GetComponent<AttackIntent>().Planned.Any(pa => pa.ContextId == contextId));
            if (planned == null) return;
            var attackId = planned.GetComponent<AttackIntent>().Planned.FirstOrDefault(pa => pa.ContextId == contextId).AttackId;
            AttackDefinitionCache.TryGet(attackId, out var attackDef);
            var specialEffects = attackDef.specialEffects;
            if (specialEffects.Any(se => se.type == "Corrode"))
            {
                Console.WriteLine($"[CardBlockService] Corrode effect detected - {attackId} - {cardObj.CardId}");
                BlockValueService.ApplyDelta(card, -1);
            }
        }
    }
}