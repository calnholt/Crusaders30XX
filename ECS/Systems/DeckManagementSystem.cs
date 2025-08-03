using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for managing deck operations like shuffling, drawing, and discarding
    /// </summary>
    public class DeckManagementSystem : Core.System
    {
        public DeckManagementSystem(EntityManager entityManager) : base(entityManager) { }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var deck = entity.GetComponent<Deck>();
            if (deck == null) return;
            
            // Ensure all cards are properly categorized
            CategorizeCards(deck);
        }
        
        private void CategorizeCards(Deck deck)
        {
            // Move cards to appropriate piles based on their state
            var allCards = deck.Cards.ToList();
            
            foreach (var card in allCards)
            {
                var cardInPlay = card.GetComponent<CardInPlay>();
                if (cardInPlay != null && cardInPlay.IsExhausted)
                {
                    if (!deck.ExhaustPile.Contains(card))
                    {
                        deck.DrawPile.Remove(card);
                        deck.DiscardPile.Remove(card);
                        deck.Hand.Remove(card);
                        deck.ExhaustPile.Add(card);
                    }
                }
            }
        }
        
        /// <summary>
        /// Shuffles the draw pile
        /// </summary>
        public void ShuffleDrawPile(Deck deck)
        {
            var random = new System.Random();
            var cards = deck.DrawPile.ToList();
            
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
            
            deck.DrawPile.Clear();
            deck.DrawPile.AddRange(cards);
        }
        
        /// <summary>
        /// Draws a card from the draw pile to the hand
        /// </summary>
        public bool DrawCard(Deck deck)
        {
            if (deck.DrawPile.Count == 0)
            {
                // Reshuffle discard pile into draw pile
                if (deck.DiscardPile.Count > 0)
                {
                    deck.DrawPile.AddRange(deck.DiscardPile);
                    deck.DiscardPile.Clear();
                    ShuffleDrawPile(deck);
                }
                else
                {
                    return false; // No cards to draw
                }
            }
            
            if (deck.DrawPile.Count > 0 && deck.Hand.Count < deck.MaxHandSize)
            {
                var card = deck.DrawPile[0];
                deck.DrawPile.RemoveAt(0);
                deck.Hand.Add(card);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Discards a card from hand to discard pile
        /// </summary>
        public void DiscardCard(Deck deck, Entity card)
        {
            if (deck.Hand.Contains(card))
            {
                deck.Hand.Remove(card);
                deck.DiscardPile.Add(card);
            }
        }
    }
} 