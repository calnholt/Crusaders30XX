using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Handles the Plunder mechanic for enemies like Wyvern.
    /// During preblock, steals a random card from the player's deck.
    /// The player can rescue the card by dealing enough damage to the enemy.
    /// </summary>
    public class PlunderManagementSystem : Core.System
    {
        private static readonly Random _random = new Random();

        public PlunderManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
            EventManager.Subscribe<PlunderTriggerEvent>(OnPlunderTrigger);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current == SubPhase.PreBlock)
            {
                var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
                if (enemy == null) return;

                var ap = enemy.GetComponent<AppliedPassives>();
                if (ap == null || !ap.Passives.ContainsKey(AppliedPassiveType.Plunder)) return;

                // Trigger plunder
                EventManager.Publish(new PlunderTriggerEvent { Enemy = enemy });
            }
            else if (evt.Current == SubPhase.PlayerStart)
            {
                // Reset damage tracking at start of player turn
                ResetDamageTracking();
            }
        }

        private void OnPlunderTrigger(PlunderTriggerEvent evt)
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;

            var deck = deckEntity.GetComponent<Deck>();
            if (deck?.DrawPile == null || deck.DrawPile.Count == 0)
            {
                Console.WriteLine("[PlunderManagementSystem] No cards in deck to plunder.");
                return;
            }

            // If there's already a plundered card, discard it first
            var existingPlundered = EntityManager.GetEntitiesWithComponent<Plundered>().FirstOrDefault();
            if (existingPlundered != null)
            {
                DiscardPlunderedCard(existingPlundered, deckEntity);
            }

            // Get a random card from the draw pile (excluding weapon cards)
            var eligibleCards = deck.DrawPile
                .Where(c => c.GetComponent<Plundered>() == null)
                .Where(c => (c.GetComponent<CardData>()?.Card.IsWeapon ?? false) == false)
                .ToList();

            if (eligibleCards.Count == 0)
            {
                Console.WriteLine("[PlunderManagementSystem] No eligible cards to plunder.");
                return;
            }

            var cardToPlunder = eligibleCards[_random.Next(eligibleCards.Count)];
            var threshold = _random.Next(4, 9); // 4 to 8 inclusive

            // Remove from draw pile (but don't add to any zone - it's "held" by the wyvern)
            deck.DrawPile.Remove(cardToPlunder);

            // Add Plundered component
            EntityManager.AddComponent(cardToPlunder, new Plundered
            {
                Owner = cardToPlunder,
                DamageThreshold = threshold,
                DamageDealt = 0
            });

            var cardData = cardToPlunder.GetComponent<CardData>();
            Console.WriteLine($"[PlunderManagementSystem] Plundered card: {cardData?.Card.CardId ?? "unknown"}, threshold: {threshold}");

            EventManager.Publish(new PlunderCardEvent { Card = cardToPlunder, DamageThreshold = threshold });
        }

        private void OnModifyHp(ModifyHpEvent evt)
        {
            // Only track damage dealt to enemy (negative delta = damage)
            if (evt.Delta >= 0) return;

            var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            if (enemy == null || evt.Target != enemy) return;

            // Check if there's a plundered card
            var plunderedCard = EntityManager.GetEntitiesWithComponent<Plundered>().FirstOrDefault();
            if (plunderedCard == null) return;

            var plundered = plunderedCard.GetComponent<Plundered>();
            if (plundered == null) return;

            // Add damage dealt
            int damageDealt = Math.Abs(evt.Delta);
            plundered.DamageDealt += damageDealt;

            var cardData = plunderedCard.GetComponent<CardData>();
            Console.WriteLine($"[PlunderManagementSystem] Damage dealt to enemy: {damageDealt}, total: {plundered.DamageDealt}/{plundered.DamageThreshold}");

            // Check if threshold reached
            if (plundered.DamageDealt >= plundered.DamageThreshold)
            {
                RescueCard(plunderedCard);
            }
        }

        private void RescueCard(Entity card)
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;

            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            // Remove Plundered component
            EntityManager.RemoveComponent<Plundered>(card);

            // Add to player's hand
            deck.Hand.Add(card);

            var cardData = card.GetComponent<CardData>();
            Console.WriteLine($"[PlunderManagementSystem] Card rescued: {cardData?.Card.CardId ?? "unknown"}");

            EventManager.Publish(new PlunderRescueEvent { Card = card });
        }

        private void DiscardPlunderedCard(Entity card, Entity deckEntity)
        {
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            // Remove Plundered component
            EntityManager.RemoveComponent<Plundered>(card);

            // Add to discard pile
            deck.DiscardPile.Add(card);

            var cardData = card.GetComponent<CardData>();
            Console.WriteLine($"[PlunderManagementSystem] Previously plundered card discarded: {cardData?.Card.CardId ?? "unknown"}");
        }

        private void ResetDamageTracking()
        {
            var plunderedCard = EntityManager.GetEntitiesWithComponent<Plundered>().FirstOrDefault();
            if (plunderedCard == null) return;

            var plundered = plunderedCard.GetComponent<Plundered>();
            if (plundered != null)
            {
                plundered.DamageDealt = 0;
                Console.WriteLine("[PlunderManagementSystem] Damage tracking reset for new turn.");
            }
        }
    }
}
