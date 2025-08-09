using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Events;
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
        private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        private double _lastTotalSeconds = 0.0; // From Update(gameTime)
        private double _pulseStartSeconds = 0.0; // When current hovered started pulsing
        private Entity _currentHovered;
        
        public CardHighlightSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            
            // Create a single pixel texture that we can reuse
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            EventManager.Subscribe<CardHighlightRenderEvent>(evt =>
            {
                // We don't get GameTime here; render on-demand by using last known hover timing
                // This event is invoked immediately before the card draws each frame
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                if (t == null || ui == null || !ui.IsHovered) return;
                // Use last gameTime.TotalGameTime captured during Update for consistent pulse timing
                var fakeGameTime = new GameTime(TimeSpan.FromSeconds(_lastTotalSeconds), TimeSpan.Zero);
                DrawCardHighlight(evt.Card, t.Position, t.Rotation, fakeGameTime);
            });
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        public override void Update(GameTime gameTime)
        {
            _lastTotalSeconds = gameTime.TotalGameTime.TotalSeconds;
            base.Update(gameTime);
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Determine top-most hovered card (InputSystem ensures only one IsHovered)
            var uiElement = entity.GetComponent<UIElement>();
            if (uiElement != null && uiElement.IsHovered)
            {
                if (!ReferenceEquals(entity, _currentHovered))
                {
                    _currentHovered = entity;
                    _pulseStartSeconds = gameTime.TotalGameTime.TotalSeconds; // reset pulse to max
                }
            }
            else if (ReferenceEquals(entity, _currentHovered) && (uiElement == null || !uiElement.IsHovered))
            {
                _currentHovered = null;
            }
        }

        public void DrawBeforeCard(Entity cardEntity, GameTime gameTime)
        {
            var t = cardEntity.GetComponent<Transform>();
            var ui = cardEntity.GetComponent<UIElement>();
            if (t == null || ui == null || !ui.IsHovered) return;
            DrawCardHighlight(cardEntity, t.Position, t.Rotation, gameTime);
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
                            DrawCardHighlight(cardEntity, transform.Position, transform.Rotation, gameTime);
                        }
                    }
                }
            }
        }

        private void DrawCardHighlight(Entity cardEntity, Vector2 position, float rotation, GameTime gameTime)
        {
            // Create highlight rectangle using centralized config
            var highlightRect = CardConfig.GetCardHighlightRect(position);

            // Add pulsing effect based on individual card hover time
            var pulseSpeed = 3.0f; // Increased speed for better responsiveness
            var hoverDuration = gameTime.TotalGameTime.TotalSeconds - _pulseStartSeconds;
            // Start at max on new hover using cosine (1 -> -1 range mapped to 0.2..1.0)
            var pulseAmount = (float)(Math.Cos(hoverDuration * pulseSpeed) * 0.4 + 0.6);
            
            // Soft glow: draw multiple expanded rounded rects with decreasing alpha
            int radius = Math.Max(0, CardConfig.CARD_CORNER_RADIUS + CardConfig.HIGHLIGHT_BORDER_THICKNESS);
            var baseTex = GetRoundedRectTexture(highlightRect.Width, highlightRect.Height, radius);
            var center = new Vector2(highlightRect.X + highlightRect.Width / 2f, highlightRect.Y + highlightRect.Height / 2f);

            // Layered glow
            int layers = CardConfig.HIGHLIGHT_GLOW_LAYERS;
            float spread = CardConfig.HIGHLIGHT_GLOW_SPREAD; // how much each layer expands
            Color glowColor = CardConfig.HIGHLIGHT_GLOW_COLOR;
            for (int i = layers; i >= 1; i--)
            {
                float scale = 1f + i * spread;
                // Fade out quickly per layer; start bright on pulse reset
                float layerAlpha = MathHelper.Clamp(pulseAmount * (0.22f / i), 0f, CardConfig.HIGHLIGHT_MAX_ALPHA);
                _spriteBatch.Draw(
                    baseTex,
                    position: center,
                    sourceRectangle: null,
                    color: glowColor * layerAlpha,
                    rotation: rotation,
                    origin: new Vector2(baseTex.Width / 2f, baseTex.Height / 2f),
                    scale: new Vector2(scale, scale),
                    effects: SpriteEffects.None,
                    layerDepth: 0f
                );
            }
        }
        
        /// <summary>
        /// Dispose of resources when the system is destroyed
        /// </summary>
        public void Dispose()
        {
            _pixelTexture?.Dispose();
            foreach (var tex in _roundedRectCache.Values)
            {
                tex?.Dispose();
            }
            _roundedRectCache.Clear();
        }

        private Texture2D GetRoundedRectTexture(int width, int height, int radius)
        {
            var key = (width, height, radius);
            if (_roundedRectCache.TryGetValue(key, out var tex)) return tex;
            var texture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
            _roundedRectCache[key] = texture;
            return texture;
        }
    }
} 
