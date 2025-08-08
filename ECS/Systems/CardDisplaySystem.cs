using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Config;
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
        private Texture2D _pixelTexture; // Reuse texture for card backgrounds
        
        public CardDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            
            // Create a single pixel texture that we can reuse
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
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
            
            if (cardData == null) return;
            
            var cardColor = GetCardColor(cardData.Color);
            var costColor = GetCostColor(cardData.CardCostType);
            
            // Draw card background
            DrawCardBackground(position, cardColor);
            
            // Name text (wrapped within card bounds)
            DrawCardTextWrapped(position, CardConfig.GetNameTextPosition(position), cardData.Name, Color.Black, CardConfig.NAME_SCALE);
            
            // Draw cost
            string costText = GetCostText(cardData.CardCostType);
            DrawCardTextWrapped(position, CardConfig.GetCostTextPosition(position), costText, costColor, CardConfig.COST_SCALE);
            
            DrawCardTextWrapped(position, CardConfig.GetDescriptionTextPosition(position), cardData.Description, Color.Black, CardConfig.DESCRIPTION_SCALE);
            
            // Draw block value as blue number bottom-right
            if (cardData.BlockValue > 0)
            {
                string blockText = cardData.BlockValue.ToString();
                var measured = _font.MeasureString(blockText) * CardConfig.BLOCK_NUMBER_SCALE;
                var drawPos = CardConfig.GetBlockNumberPosition(position, measured);
                _spriteBatch.DrawString(_font, blockText, drawPos, Color.CornflowerBlue, 0f, Vector2.Zero, CardConfig.BLOCK_NUMBER_SCALE, SpriteEffects.None, 0f);
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
            // Create card background rectangle using centralized config
            var rect = CardConfig.GetCardVisualRect(position);
            
            // Draw filled rectangle using the reusable pixel texture
            _spriteBatch.Draw(_pixelTexture, rect, color);
            
            // Draw border using config thickness
            var borderThickness = CardConfig.CARD_BORDER_THICKNESS;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), Color.Black); // Top
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), Color.Black); // Left
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X + rect.Width - borderThickness, rect.Y, borderThickness, rect.Height), Color.Black); // Right
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - borderThickness, rect.Width, borderThickness), Color.Black); // Bottom
        }
        
        /// <summary>
        /// Dispose of resources when the system is destroyed
        /// </summary>
        public void Dispose()
        {
            _pixelTexture?.Dispose();
        }
        
        private void DrawCardTextWrapped(Vector2 cardPosition, Vector2 desiredAbsolutePosition, string text, Color color, float scale)
        {
            try
            {
                var cardRect = CardConfig.GetCardVisualRect(cardPosition);
                float minX = cardRect.Left + CardConfig.TEXT_MARGIN_X;
                float maxX = cardRect.Right - CardConfig.TEXT_MARGIN_X;
                float availableWidth = Math.Max(0f, maxX - desiredAbsolutePosition.X);

                // Fallback if desired position is outside, start at minX
                float startX = MathHelper.Clamp(desiredAbsolutePosition.X, minX, maxX);
                if (availableWidth <= 0f)
                {
                    startX = minX;
                    availableWidth = Math.Max(0f, maxX - minX);
                }

                float lineHeight = _font.LineSpacing * scale;
                float y = desiredAbsolutePosition.Y;
                float maxY = cardRect.Bottom - CardConfig.TEXT_MARGIN_Y - lineHeight;

                foreach (var line in WrapText(text, availableWidth, scale))
                {
                    if (y > maxY) break; // stop if out of vertical space

                    // Clamp line horizontally (handles very long tokens case)
                    var measured = _font.MeasureString(line) * scale;
                    float clampedX = MathHelper.Clamp(startX, minX, Math.Max(minX, maxX - measured.X));
                    _spriteBatch.DrawString(_font, line, new Vector2(clampedX, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                    y += lineHeight;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }

        private IEnumerable<string> WrapText(string text, float maxLineWidth, float scale)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            string[] words = text.Split(' ');
            string currentLine = string.Empty;

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float lineWidth = _font.MeasureString(testLine).X * scale;

                if (lineWidth <= maxLineWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        yield return currentLine;
                        currentLine = word; // start new line with the current word
                    }
                    else
                    {
                        // Single word longer than line: hard-break by characters
                        string longWord = word;
                        string partial = string.Empty;
                        foreach (char c in longWord)
                        {
                            string attempt = partial + c;
                            if (_font.MeasureString(attempt).X * scale <= maxLineWidth)
                            {
                                partial = attempt;
                            }
                            else
                            {
                                if (partial.Length > 0)
                                {
                                    yield return partial;
                                }
                                partial = c.ToString();
                            }
                        }
                        currentLine = partial;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                yield return currentLine;
            }
        }
    }
} 