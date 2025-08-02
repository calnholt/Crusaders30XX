using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Example system that demonstrates turn management
    /// </summary>
    public class TurnManagementSystem : Core.System
    {
        private float _turnTimer = 0f;
        private const float TURN_DURATION = 30f; // 30 seconds per turn
        
        public TurnManagementSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<GameState>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var gameState = entity.GetComponent<GameState>();
            if (gameState == null) return;
            
            if (gameState.CurrentPhase == GameState.GamePhase.Combat)
            {
                _turnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                if (_turnTimer >= TURN_DURATION)
                {
                    EndTurn(gameState);
                    _turnTimer = 0f;
                }
            }
        }
        
        private void EndTurn(GameState gameState)
        {
            gameState.IsPlayerTurn = !gameState.IsPlayerTurn;
            
            if (gameState.IsPlayerTurn)
            {
                gameState.TurnNumber++;
                StartPlayerTurn();
            }
            else
            {
                StartEnemyTurn();
            }
        }
        
        private void StartPlayerTurn()
        {
            // Reset player energy and draw cards
            var players = EntityManager.GetEntitiesWithComponent<Player>();
            foreach (var player in players)
            {
                var playerComponent = player.GetComponent<Player>();
                if (playerComponent != null)
                {
                    playerComponent.CurrentEnergy = playerComponent.MaxEnergy;
                    playerComponent.Block = 0;
                }
            }
            
            // Draw cards for player
            var decks = EntityManager.GetEntitiesWithComponent<Deck>();
            foreach (var deck in decks)
            {
                var deckComponent = deck.GetComponent<Deck>();
                if (deckComponent != null)
                {
                    for (int i = 0; i < deckComponent.DrawPerTurn; i++)
                    {
                        // This would call the deck management system
                        // For now, we'll just simulate drawing
                    }
                }
            }
        }
        
        private void StartEnemyTurn()
        {
            // Process enemy actions
            var enemies = EntityManager.GetEntitiesWithComponent<Enemy>();
            foreach (var enemy in enemies)
            {
                var enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    // Reset enemy block
                    enemyComponent.Block = 0;
                    
                    // Process enemy intentions
                    if (enemyComponent.Intentions.Count > 0)
                    {
                        var currentIntent = enemyComponent.Intentions[enemyComponent.IntentIndex];
                        // Process the intent (damage, block, etc.)
                        
                        // Move to next intent
                        enemyComponent.IntentIndex = (enemyComponent.IntentIndex + 1) % enemyComponent.Intentions.Count;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Example system that demonstrates card effects
    /// </summary>
    public class CardEffectSystem : Core.System
    {
        public CardEffectSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var cardData = entity.GetComponent<CardData>();
            var cardInPlay = entity.GetComponent<CardInPlay>();
            
            if (cardData == null || cardInPlay == null) return;
            
            // Example: Process card effects based on card type
            switch (cardData.Type)
            {
                case CardData.CardType.Attack:
                    ProcessAttackCard(entity, cardData);
                    break;
                case CardData.CardType.Skill:
                    ProcessSkillCard(entity, cardData);
                    break;
                case CardData.CardType.Power:
                    ProcessPowerCard(entity, cardData);
                    break;
            }
        }
        
        private void ProcessAttackCard(Entity card, CardData cardData)
        {
            // Attack cards typically deal damage
            // This would be triggered when the card is played
        }
        
        private void ProcessSkillCard(Entity card, CardData cardData)
        {
            // Skill cards provide utility effects
            // This would be triggered when the card is played
        }
        
        private void ProcessPowerCard(Entity card, CardData cardData)
        {
            // Power cards provide ongoing effects
            // This would be triggered when the card is played
        }
        
        /// <summary>
        /// Example method to play a card
        /// </summary>
        public void PlayCard(Entity card, Entity target)
        {
            var cardData = card.GetComponent<CardData>();
            var cardInPlay = card.GetComponent<CardInPlay>();
            
            if (cardData == null || cardInPlay == null) return;
            
            // Check if card is playable
            if (!cardInPlay.IsPlayable) return;
            
            // Check energy cost
            var players = EntityManager.GetEntitiesWithComponent<Player>();
            foreach (var player in players)
            {
                var playerComponent = player.GetComponent<Player>();
                if (playerComponent != null && playerComponent.CurrentEnergy >= cardInPlay.EnergyCost)
                {
                    // Spend energy
                    playerComponent.CurrentEnergy -= cardInPlay.EnergyCost;
                    
                    // Apply card effect
                    ApplyCardEffect(card, target);
                    
                    // Move card to discard pile
                    MoveCardToDiscard(card);
                    
                    break;
                }
            }
        }
        
        private void ApplyCardEffect(Entity card, Entity target)
        {
            var cardData = card.GetComponent<CardData>();
            if (cardData == null) return;
            
            // Example: Simple damage effect for attack cards
            if (cardData.Type == CardData.CardType.Attack)
            {
                // This would call the combat system to apply damage
                // For now, we'll just simulate it
            }
        }
        
        private void MoveCardToDiscard(Entity card)
        {
            // Find the deck and move card to discard pile
            var decks = EntityManager.GetEntitiesWithComponent<Deck>();
            foreach (var deck in decks)
            {
                var deckComponent = deck.GetComponent<Deck>();
                if (deckComponent != null)
                {
                    // Remove from hand
                    deckComponent.Hand.Remove(card);
                    
                    // Add to discard pile
                    deckComponent.DiscardPile.Add(card);
                    
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Example system that demonstrates status effects
    /// </summary>
    public class StatusEffectSystem : Core.System
    {
        public StatusEffectSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Player>()
                .Concat(EntityManager.GetEntitiesWithComponent<Enemy>());
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // This system would handle status effects like poison, strength, etc.
            // For now, it's a placeholder for future status effect implementation
        }
    }
} 