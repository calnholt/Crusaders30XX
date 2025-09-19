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
            EventManager.Subscribe<ResetDeckEvent>(OnResetDeckEvent);
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

            Console.WriteLine($"[DeckManagementSystem] OnRequestDrawCards count={evt.Count} hand={deck.Hand.Count} drawPile={deck.DrawPile.Count} discard={deck.DiscardPile.Count}");
            DrawCards(deck, Math.Max(1, evt.Count));

        }

        private void OnRedrawHandEvent(RedrawHandEvent evt)
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            System.Console.WriteLine($"[DeckManagementSystem] OnRedrawHand drawCount={evt.DrawCount} before hand={deck.Hand.Count} drawPile={deck.DrawPile.Count} discard={deck.DiscardPile.Count}");
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
            System.Console.WriteLine($"[DeckManagementSystem] OnRedrawHand after hand={deck.Hand.Count} drawPile={deck.DrawPile.Count} discard={deck.DiscardPile.Count}");
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
                return false; // No cards to draw
            }
            if (deck.DrawPile.Count > 0)
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
                // Ensure UI becomes interactable again when a card enters the hand
                var ui = card.GetComponent<UIElement>();
                if (ui != null)
                {
                    ui.IsInteractable = true;
                    ui.IsHovered = false;
                    ui.IsClicked = false;
                }
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
            System.Console.WriteLine($"[DeckManagementSystem] OnDeckShuffle drawPile(before)={deck.DrawPile.Count} discard={deck.DiscardPile.Count}");
            ShuffleDrawPile(deck);
            System.Console.WriteLine($"[DeckManagementSystem] OnDeckShuffle drawPile(after)={deck.DrawPile.Count}");
        }

        /// <summary>
        /// Event handler for deck shuffle and draw events
        /// </summary>
        private void OnDeckShuffleDrawEvent(DeckShuffleDrawEvent evt)
        {
            var deck = evt.Deck.GetComponent<Deck>();
            if (deck != null)
            {
                System.Console.WriteLine($"[DeckManagementSystem] OnDeckShuffleDraw drawCount={evt.DrawCount} hand(before)={deck.Hand.Count}");
                var drawnCards = ShuffleAndDraw(deck, evt.DrawCount);
                
                // Publish event for cards drawn
                EventManager.Publish(new CardsDrawnEvent
                {
                    Deck = evt.Deck,
                    DrawnCards = deck.Hand.ToList()
                });
                System.Console.WriteLine($"[DeckManagementSystem] OnDeckShuffleDraw drawn={drawnCards} hand(after)={deck.Hand.Count}");
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

        /// <summary>
        /// Moves all non-weapon cards from Hand and Discard back into the Draw pile and shuffles.
        /// Weapon card (from EquippedWeapon) is excluded from movement.
        /// </summary>
        private void ResetDeckExcludingWeapon(Deck deck)
        {
            if (deck == null) return;

            // Identify the equipped weapon entity if any
            Entity weapon = null;
            try
            {
                var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var equipped = player?.GetComponent<EquippedWeapon>();
                weapon = equipped?.SpawnedEntity;
            }
            catch { }

            // Collect non-weapon cards from hand and discard
            var toReturn = new List<Entity>();
            foreach (var c in deck.Hand)
            {
                if (weapon != null && ReferenceEquals(c, weapon)) continue;
                toReturn.Add(c);
            }
            foreach (var c in deck.DiscardPile)
            {
                if (weapon != null && ReferenceEquals(c, weapon)) continue;
                toReturn.Add(c);
            }

            // Clear from original zones (preserve weapon in hand if present)
            if (toReturn.Count > 0)
            {
                // Remove each from both lists to be safe
                foreach (var c in toReturn)
                {
                    deck.Hand.Remove(c);
                    deck.DiscardPile.Remove(c);
                }
                deck.DrawPile.AddRange(toReturn);
            }

            // Shuffle draw pile after returning
            ShuffleDrawPile(deck);
        }

        /// <summary>
        /// Event handler for ResetDeckEvent
        /// </summary>
        private void OnResetDeckEvent(ResetDeckEvent evt)
        {
            var deckEntity = evt.Deck ?? EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            System.Console.WriteLine($"[DeckManagementSystem] OnResetDeck before hand={deck.Hand.Count} draw={deck.DrawPile.Count} discard={deck.DiscardPile.Count}");
            ResetDeckExcludingWeapon(deck);
            System.Console.WriteLine($"[DeckManagementSystem] OnResetDeck after hand={deck.Hand.Count} draw={deck.DrawPile.Count} discard={deck.DiscardPile.Count}");
        }
    }
} 