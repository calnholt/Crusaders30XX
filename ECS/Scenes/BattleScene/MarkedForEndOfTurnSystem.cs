using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages MarkedForEndOfTurnDiscard components. When the player turn ends (SubPhase.PlayerEnd),
    /// all entities with this component are automatically discarded.
    /// </summary>
    public class MarkedForEndOfTurnSystem : Core.System
    {
        public MarkedForEndOfTurnSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
        {
            // No per-frame update needed
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current != SubPhase.PlayerEnd)
            {
                return;
            }

            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null)
            {
                return;
            }

            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null)
            {
                return;
            }

            // Only discard cards that are marked AND in the player's hand
            var markedCards = EntityManager.GetEntitiesWithComponent<MarkedForEndOfTurnDiscard>()
                .Where(card => deck.Hand.Contains(card))
                .ToList();

            if (markedCards.Count == 0)
            {
                return;
            }

            foreach (var card in markedCards)
            {
                EventManager.Publish(new CardMoveRequested 
                { 
                    Card = card, 
                    Deck = deckEntity, 
                    Destination = CardZoneType.DiscardPile, 
                    Reason = "EndOfTurnDiscard" 
                });
                EntityManager.RemoveComponent<MarkedForEndOfTurnDiscard>(card);
                Console.WriteLine($"[MarkedForEndOfTurnSystem] Requested move to DiscardPile for card {card.Id}");
            }
        }
    }
}

