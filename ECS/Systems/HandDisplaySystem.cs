using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for managing the display of cards in the player's hand
    /// </summary>
    public class HandDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Dictionary<string, Texture2D> _textureCache = new();
        private SpriteFont _font;
        private readonly DeckManagementSystem _deckSystem;
        
        public HandDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font, DeckManagementSystem deckSystem) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _deckSystem = deckSystem;
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Update card positions based on hand layout
            UpdateCardPosition(entity);
        }
        
        private void UpdateCardPosition(Entity entity)
        {
            var transform = entity.GetComponent<Transform>();
            var cardData = entity.GetComponent<CardData>();
            
            if (transform == null || cardData == null) return;
            
            // Find the deck entity and check if this card is in the hand
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null && deck.Hand.Contains(entity))
                {
                    // Get the index of this card in the hand
                    var cardIndex = deck.Hand.IndexOf(entity);
                    
                    if (cardIndex >= 0)
                    {
                        // Position cards at the bottom of the screen
                        float screenWidth = _graphicsDevice.Viewport.Width;
                        float cardSpacing = 120f;
                        float startX = (screenWidth - (deck.Hand.Count * cardSpacing)) / 2f;
                        float y = _graphicsDevice.Viewport.Height - 200f; // 200 pixels from bottom
                        
                        transform.Position = new Vector2(startX + (cardIndex * cardSpacing), y);
                        
                        // Update UI bounds for interaction
                        var uiElement = entity.GetComponent<UIElement>();
                        if (uiElement != null)
                        {
                            uiElement.Bounds = new Rectangle((int)transform.Position.X - 50, (int)transform.Position.Y - 75, 100, 150);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Triggers deck shuffling and drawing of cards
        /// </summary>
        public void TriggerDeckShuffleAndDraw(int drawCount = 4)
        {
            // Find the deck entity
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null && _deckSystem != null)
                {
                    _deckSystem.ShuffleAndDraw(deck, drawCount);
                }
            }
        }
        
        /// <summary>
        /// Draws all cards in hand with their colors and information
        /// </summary>
        public void DrawHand()
        {
            // Find the deck entity and get cards that are actually in the hand
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null)
                {
                    // Only draw cards that are actually in the hand
                    var cardsInHand = deck.Hand.OrderBy(e => 
                    {
                        var transform = e.GetComponent<Transform>();
                        return transform?.Position.X ?? 0f;
                    });
                    
                    foreach (var entity in cardsInHand)
                    {
                        DrawCard(entity);
                    }
                }
            }
        }
        
        private void DrawCard(Entity entity)
        {
            var cardData = entity.GetComponent<CardData>();
            var transform = entity.GetComponent<Transform>();
            var sprite = entity.GetComponent<Sprite>();
            
            if (cardData == null || transform == null) return;
            
            var position = transform.Position;
            var cardColor = GetCardColor(cardData.Color);
            var costColor = GetCostColor(cardData.CardCostType);
            
            // Draw card background
            DrawCardBackground(position, cardColor);
            
            // Draw card name (shorter text for better fit)
            string displayName = cardData.Name.Length > 12 ? cardData.Name.Substring(0, 12) : cardData.Name;
            DrawCardText(position, displayName, Color.White, 0.8f, new Vector2(0, -60));
            
            // Draw cost
            string costText = GetCostText(cardData.CardCostType);
            DrawCardText(position, costText, costColor, 0.6f, new Vector2(-35, -35));
            
            // Draw description (shortened for better fit)
            string shortDesc = cardData.Description.Length > 25 ? cardData.Description.Substring(0, 25) + "..." : cardData.Description;
            DrawCardText(position, shortDesc, Color.White, 0.4f, new Vector2(0, 10));
            
            // Draw block value if applicable
            if (cardData.BlockValue > 0)
            {
                DrawCardText(position, $"Block: {cardData.BlockValue}", Color.Cyan, 0.5f, new Vector2(0, 60));
            }
        }
        
        private Color GetCardColor(CardData.CardColor color)
        {
            return color switch
            {
                CardData.CardColor.Red => Color.Red,
                CardData.CardColor.White => Color.White,
                CardData.CardColor.Black => Color.DarkGray,
                _ => Color.Gray
            };
        }
        
        private Color GetCostColor(CardData.CostType costType)
        {
            return costType switch
            {
                CardData.CostType.Red => Color.Red,
                CardData.CostType.White => Color.White,
                CardData.CostType.Black => Color.DarkGray,
                _ => Color.Gray
            };
        }
        
        private string GetCostText(CardData.CostType costType)
        {
            return costType switch
            {
                CardData.CostType.Red => "Red",
                CardData.CostType.White => "White",
                CardData.CostType.Black => "Black",
                _ => "Free"
            };
        }
        
        private void DrawCardBackground(Vector2 position, Color color)
        {
            // Create a simple rectangle for the card background
            var rect = new Rectangle((int)position.X - 50, (int)position.Y - 75, 100, 150);
            
            // Draw filled rectangle
            var texture = new Texture2D(_graphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            
            _spriteBatch.Draw(texture, rect, color);
            
            // Draw border
            _spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Black); // Top
            _spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.Black); // Left
            _spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - 2, rect.Y, 2, rect.Height), Color.Black); // Right
            _spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), Color.Black); // Bottom
        }
        
        private void DrawCardText(Vector2 position, string text, Color color, float scale, Vector2 offset)
        {
            try
            {
                // Use proper font rendering
                Console.WriteLine(text);
                var textSize = _font.MeasureString(text);
                var textPosition = position + offset;
                var drawPosition = textPosition - (textSize * scale) / 2f;
                _spriteBatch.DrawString(_font, text, drawPosition, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }
    }
} 