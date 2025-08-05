using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
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
        public DeckManagementSystem(EntityManager entityManager) : base(entityManager) 
        {
            // Subscribe to deck management events
            EventManager.Subscribe<DeckShuffleDrawEvent>(OnDeckShuffleDrawEvent);
        }
        
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
        
        /// <summary>
        /// Draws multiple cards from the deck to the hand
        /// </summary>
        public int DrawCards(Deck deck, int count)
        {
            int drawnCount = 0;
            
            for (int i = 0; i < count; i++)
            {
                if (DrawCard(deck))
                {
                    drawnCount++;
                }
                else
                {
                    break; // No more cards to draw
                }
            }
            
            return drawnCount;
        }
        
        /// <summary>
        /// Event handler for deck shuffle and draw events
        /// </summary>
        private void OnDeckShuffleDrawEvent(DeckShuffleDrawEvent evt)
        {
            var deck = evt.Deck.GetComponent<Deck>();
            if (deck != null)
            {
                var drawnCards = ShuffleAndDraw(deck, evt.DrawCount);
                
                // Publish event for cards drawn
                EventManager.Publish(new CardsDrawnEvent
                {
                    Deck = evt.Deck,
                    DrawnCards = deck.Hand.ToList()
                });
            }
        }
        
        /// <summary>
        /// Shuffles the deck and draws the specified number of cards
        /// </summary>
        public int ShuffleAndDraw(Deck deck, int drawCount)
        {
            // First, move all cards from hand and discard pile back to draw pile
            deck.DrawPile.AddRange(deck.Hand);
            deck.DrawPile.AddRange(deck.DiscardPile);
            deck.Hand.Clear();
            deck.DiscardPile.Clear();
            
            // Shuffle the draw pile
            ShuffleDrawPile(deck);
            
            // Draw the specified number of cards
            return DrawCards(deck, drawCount);
        }
    }
} 