using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for rendering individual cards with their visual elements
    /// </summary>
    public class CardDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Dictionary<string, Texture2D> _textureCache = new();
        private SpriteFont _font;
        
        public CardDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            
            // Subscribe to card render events
            EventManager.Subscribe<CardRenderEvent>(OnCardRenderEvent);
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Card display system doesn't need update logic for individual entities
            // Rendering is handled by the DrawCard method
        }
        
        /// <summary>
        /// Event handler for card render events
        /// </summary>
        private void OnCardRenderEvent(CardRenderEvent evt)
        {
            DrawCard(evt.Card, evt.Position);
        }
        
        /// <summary>
        /// Draws a single card with all its visual elements
        /// </summary>
        public void DrawCard(Entity entity, Vector2 position)
        {
            var cardData = entity.GetComponent<CardData>();
            var sprite = entity.GetComponent<Sprite>();
            
            if (cardData == null) return;
            
            var cardColor = GetCardColor(cardData.Color);
            var costColor = GetCostColor(cardData.CardCostType);
            
            // Draw card background
            DrawCardBackground(position, cardColor);
            
            DrawCardText(position, cardData.Name, Color.Black, 0.8f, new Vector2(-35, -70));
            
            // Draw cost
            string costText = GetCostText(cardData.CardCostType);
            DrawCardText(position, costText, costColor, 0.6f, new Vector2(-35, -35));
            
            DrawCardText(position, cardData.Description, Color.Black, 0.4f, new Vector2(-35, 10));
            
            // Draw block value if applicable
            if (cardData.BlockValue > 0)
            {
                DrawCardText(position, $"Block: {cardData.BlockValue}", Color.Cyan, 0.5f, new Vector2(0, 60));
            }
        }
        
        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public void DrawCard(Entity entity)
        {
            var transform = entity.GetComponent<Transform>();
            if (transform != null)
            {
                DrawCard(entity, transform.Position);
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
            var rect = new Rectangle((int)position.X - 50, (int)position.Y - 75, 200, 300);
            
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
                var textSize = _font.MeasureString(text);
                var textPosition = position + offset;
                // Position text relative to card's left edge instead of centering
                var drawPosition = textPosition;
                _spriteBatch.DrawString(_font, text, drawPosition, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }
    }
} 