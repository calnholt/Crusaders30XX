using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for highlighting cards when hovered over
    /// </summary>
    public class CardHighlightSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture; // Reuse texture instead of creating new ones
        private readonly Dictionary<Entity, double> _hoverStartTimes = new(); // Track when each card started being hovered
        
        public CardHighlightSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            
            // Create a single pixel texture that we can reuse
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Track hover state changes to reset pulse timing
            var uiElement = entity.GetComponent<UIElement>();
            if (uiElement != null)
            {
                if (uiElement.IsHovered)
                {
                    // If this card just started being hovered, record the start time
                    if (!_hoverStartTimes.ContainsKey(entity))
                    {
                        _hoverStartTimes[entity] = gameTime.TotalGameTime.TotalSeconds;
                    }
                }
                else
                {
                    // If card is no longer hovered, remove its timing
                    _hoverStartTimes.Remove(entity);
                }
            }
        }
        
        /// <summary>
        /// Draws highlights for all hovered cards
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            // Find all cards in hand that are hovered
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null)
                {
                    foreach (var cardEntity in deck.Hand)
                    {
                        var uiElement = cardEntity.GetComponent<UIElement>();
                        var transform = cardEntity.GetComponent<Transform>();
                        
                        if (uiElement != null && transform != null && uiElement.IsHovered)
                        {
                            DrawCardHighlight(cardEntity, transform.Position, gameTime);
                        }
                    }
                }
            }
        }
        
        private void DrawCardHighlight(Entity cardEntity, Vector2 position, GameTime gameTime)
        {
            // Create highlight rectangle using centralized config
            var highlightRect = CardConfig.GetCardHighlightRect(position);
            
            // Add pulsing effect based on individual card hover time
            var pulseSpeed = 3.0f; // Increased speed for better responsiveness
            var hoverStartTime = _hoverStartTimes.GetValueOrDefault(cardEntity, gameTime.TotalGameTime.TotalSeconds);
            var hoverDuration = gameTime.TotalGameTime.TotalSeconds - hoverStartTime;
            var pulseAmount = (float)Math.Sin(hoverDuration * pulseSpeed) * 0.4f + 0.6f; // Increased pulse range
            
            // Draw highlight border with pulsing effect
            var borderColor = Color.Yellow * pulseAmount;
            var borderThickness = CardConfig.HIGHLIGHT_BORDER_THICKNESS;
            
            // Draw highlight border (no filled background). Trim only sides to avoid overlapping at corners.
            // Top border (full width)
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    highlightRect.X,
                    highlightRect.Y,
                    highlightRect.Width,
                    borderThickness
                ),
                borderColor
            );
            // Bottom border (full width)
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    highlightRect.X,
                    highlightRect.Y + highlightRect.Height - borderThickness,
                    highlightRect.Width,
                    borderThickness
                ),
                borderColor
            );
            // Left border (trim top/bottom by thickness)
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    highlightRect.X,
                    highlightRect.Y + borderThickness,
                    borderThickness,
                    highlightRect.Height - (borderThickness * 2)
                ),
                borderColor
            );
            // Right border
            _spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    highlightRect.X + highlightRect.Width - borderThickness,
                    highlightRect.Y + borderThickness,
                    borderThickness,
                    highlightRect.Height - (borderThickness * 2)
                ),
                borderColor
            );
        }
        
        /// <summary>
        /// Dispose of resources when the system is destroyed
        /// </summary>
        public void Dispose()
        {
            _pixelTexture?.Dispose();
        }
    }
} 
