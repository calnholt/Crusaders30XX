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
            EventManager.Subscribe<RequestDrawCardsEvent>(OnRequestDrawCards);
            EventManager.Subscribe<RedrawHandEvent>(OnRedrawHandEvent);
            EventManager.Subscribe<DeckShuffleEvent>(OnDeckShuffleEvent);
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }
        
        private void OnRequestDrawCards(RequestDrawCardsEvent evt)
        {
            // Find the first deck and draw count cards
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            DrawCards(deck, Math.Max(1, evt.Count));

        }

        private void OnRedrawHandEvent(RedrawHandEvent evt)
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            // Move current hand to discard, then reshuffle, then draw
            // Move current hand to discard and reset their transforms, so re-drawn cards animate from spawn
            foreach (var c in deck.Hand)
            {
                var t = c.GetComponent<Transform>();
                if (t != null)
                {
                    t.Position = Vector2.Zero;
                    t.Rotation = 0f;
                }
            }
            deck.DiscardPile.AddRange(deck.Hand);
            deck.Hand.Clear();
            ShuffleDrawPile(deck);
            DrawCards(deck, evt.DrawCount);

            EventManager.Publish(new CardsDrawnEvent
            {
                Deck = deckEntity,
                DrawnCards = deck.Hand.ToList()
            });
        }
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
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
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var maxHandSize = player.GetComponent<MaxHandSize>().Value;
            if (deck.DrawPile.Count > 0 && deck.Hand.Count < maxHandSize)
            {
                var card = deck.DrawPile[0];
                deck.DrawPile.RemoveAt(0);
                // Reset transform so the HandDisplaySystem spawns it from offscreen east
                var transform = card.GetComponent<Transform>();
                if (transform != null)
                {
                    transform.Position = Vector2.Zero;
                    transform.Rotation = 0f;
                }
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
            // Optionally publish CardsDrawnEvent reflecting current hand for UI updates
            EventManager.Publish(new CardsDrawnEvent
            {
                Deck = deck.Owner,
                DrawnCards = deck.Hand.ToList()
            });
            return drawnCount;
        }
        
        /// <summary>
        /// Event handler for deck shuffle event
        /// </summary>
        private void OnDeckShuffleEvent(DeckShuffleEvent evt)
        {
            // Support null Deck in event by defaulting to the first deck entity
            var deckEntity = evt.Deck ?? EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            ShuffleDrawPile(deck);
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