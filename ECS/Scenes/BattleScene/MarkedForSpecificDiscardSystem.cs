using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

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
            // When the phase leaves Block/EnemyAttack, clear marks
            EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
            {
                if (evt.Current != SubPhase.Block && evt.Current != SubPhase.EnemyAttack)
                {
                    ClearExistingSpecificDiscardMarks();
                }
            });
            EventManager.Subscribe<OnEnemyAttackHitEvent>(OnOnEnemyAttackHitEvent);
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
            if (evt.Amount <= 0) { ClearExistingSpecificDiscardMarks(); return; }

            // Select random cards from hand (excluding weapon card)
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            // Identify weapon to exclude
            Entity weapon = null;
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            weapon = player?.GetComponent<EquippedWeapon>()?.SpawnedEntity;
            var candidates = deck.Hand.Where(c => !ReferenceEquals(c, weapon)).ToList();
            int pick = System.Math.Min(evt.Amount, candidates.Count);
            if (pick <= 0) return;
            // Shuffle candidates and take the first N
            var selected = candidates.OrderBy(_ => _random.Next()).Take(pick).ToList();
            foreach (var card in selected)
            {
                EntityManager.AddComponent(card, new MarkedForSpecificDiscard { Owner = card });
            }
        }

        private void ClearExistingSpecificDiscardMarks()
        {
            foreach (var e in EntityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>())
            {
                EntityManager.RemoveComponent<MarkedForSpecificDiscard>(e);
            }
        }

        private void OnOnEnemyAttackHitEvent(OnEnemyAttackHitEvent evt)
        {
            var entities = EntityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>();
            if (entities == null || entities.Count() == 0) return;
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            foreach (var e in entities)
            {
                EntityManager.RemoveComponent<MarkedForSpecificDiscard>(e);
                EventManager.Publish(new CardMoveRequested { Card = e, Deck = deckEntity, Destination = CardZoneType.DiscardPile, ContextId = GetComponentHelper.GetContextId(EntityManager), Reason = "DiscardSpecificCard" });
            }
        }
    }
}


