using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using System;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages MarkedForSpecificDiscard components based on the current enemy attack context.
    /// Extracted from EnemyAttackDisplaySystem.
    /// </summary>
    public class MarkedForSpecificDiscardSystem : Core.System
    {
        private readonly System.Random _random = new System.Random();
        public MarkedForSpecificDiscardSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<MarkedForSpecificDiscardEvent>(OnMarkedForSpecificDiscard);
            EventManager.Subscribe<DiscardMarkedForSpecificDiscardEvent>(OnDiscardMarkedForSpecificDiscard);
            EventManager.Subscribe<AttackResolved>(OnAttackResolved);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<AttackIntent>();
        }

        protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
        {
        }

        private void OnMarkedForSpecificDiscard(MarkedForSpecificDiscardEvent evt)
        {
            TryPreselectSpecificDiscards(evt);
        }

        private void TryPreselectSpecificDiscards(MarkedForSpecificDiscardEvent evt)
        {
            var attackDef = GetComponentHelper.GetPlannedAttack(EntityManager);
            if (attackDef == null) return;
            if (evt.Amount <= 0) return;
            string contextId = ResolveContextId(evt.ContextId);
            var candidates = GetComponentHelper.GetHandOfCards(EntityManager);
            int pick = System.Math.Min(evt.Amount, candidates.Count);
            LoggingService.Append("MarkedForSpecificDiscardSystem.TryPreselectSpecificDiscards", new System.Text.Json.Nodes.JsonObject { ["pickCount"] = pick, ["candidateCount"] = candidates.Count });
            if (pick <= 0) return;
            // Shuffle candidates and take the first N
            var selected = candidates.OrderBy(_ => _random.Next()).Take(pick).ToList();
            foreach (var card in selected)
            {
                EntityManager.AddComponent(card, new MarkedForSpecificDiscard
                {
                    Owner = card,
                    ContextId = contextId,
                });
            }
        }

        private void OnDiscardMarkedForSpecificDiscard(DiscardMarkedForSpecificDiscardEvent evt)
        {
            string contextId = ResolveContextId(evt.ContextId);
            var entities = GetMarkedCardsForContext(contextId).ToList();
            if (entities.Count == 0) return;

            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity?.GetComponent<Deck>() == null) return;

            foreach (var entity in entities)
            {
                EntityManager.RemoveComponent<MarkedForSpecificDiscard>(entity);
                EventManager.Publish(new CardMoveRequested
                {
                    Card = entity,
                    Deck = deckEntity,
                    Destination = CardZoneType.DiscardPile,
                    ContextId = contextId,
                    Reason = "DiscardSpecificCard",
                });
            }
        }

        private void OnAttackResolved(AttackResolved evt)
        {
            var entities = GetMarkedCardsForContext(evt.ContextId).ToList();
            if (entities.Count == 0) return;
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            foreach (var e in entities)
            {
                EntityManager.RemoveComponent<MarkedForSpecificDiscard>(e);
                if (evt.WasConditionMet)
                {
                    continue;
                }
                EventManager.Publish(new CardMoveRequested { Card = e, Deck = deckEntity, Destination = CardZoneType.DiscardPile, ContextId = evt.ContextId, Reason = "DiscardSpecificCard" });
            }
        }

        private IEnumerable<Entity> GetMarkedCardsForContext(string contextId)
        {
            return EntityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>()
                .Where(entity =>
                {
                    string markedContextId = entity.GetComponent<MarkedForSpecificDiscard>()?.ContextId;
                    return string.IsNullOrEmpty(markedContextId)
                        || string.Equals(markedContextId, contextId, StringComparison.Ordinal);
                });
        }

        private string ResolveContextId(string contextId)
        {
            return !string.IsNullOrWhiteSpace(contextId)
                ? contextId
                : GetComponentHelper.GetContextId(EntityManager) ?? string.Empty;
        }
    }
}
