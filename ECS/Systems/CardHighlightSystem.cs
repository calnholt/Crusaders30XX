using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
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
    [DebugTab("Card Highlight")]
    public class CardHighlightSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture; // Reuse texture instead of creating new ones
        private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        private double _lastTotalSeconds = 0.0; // From Update(gameTime)
        private double _pulseStartSeconds = 0.0; // When current hovered started pulsing
        private Entity _currentHovered;
        // Equipment highlight tracking (separate pulse timer)
        private double _pulseStartSecondsEquipment = 0.0;
        private Entity _currentEquipmentHovered;
        
        // Highlight settings now come from EquipmentHighlightSettings via HighlightSettingsSystem
        
        public CardHighlightSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            
            // Create a single pixel texture that we can reuse
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            EventManager.Subscribe<HighlightRenderEvent>(evt =>
            {
                // Unified highlight render for cards and equipment
                var t = evt.Transform ?? evt.Entity.GetComponent<Transform>();
                var ui = evt.UI ?? evt.Entity.GetComponent<UIElement>();
                if (ui == null || !ui.IsHovered) return;
                if (!ReferenceEquals(evt.Entity, _currentHovered))
                {
                    _currentHovered = evt.Entity;
                    _pulseStartSeconds = _lastTotalSeconds;
                }
                var fakeGameTime = new GameTime(TimeSpan.FromSeconds(_lastTotalSeconds), TimeSpan.Zero);
                Rectangle targetRect;
                float rot = 0f;
                if (evt.Entity.GetComponent<CardData>() != null && t != null)
                {
                    targetRect = ComputeCardBounds(evt.Entity, t.Position);
                    rot = t.Rotation;
                }
                else
                {
                    targetRect = ui.Bounds;
                }
                DrawHighlight(targetRect, rot, fakeGameTime);
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
            var rect = ComputeCardBounds(cardEntity, t.Position);
            DrawHighlight(rect, t.Rotation, gameTime);
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
                            var rect = ComputeCardBounds(cardEntity, transform.Position);
                            DrawHighlight(rect, transform.Rotation, gameTime);
                        }
                    }
                }
            }
        }

        private Rectangle ComputeCardBounds(Entity cardEntity, Vector2 position)
        {
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var s = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int cw = s?.CardWidth ?? 250;
            int ch = s?.CardHeight ?? 350;
            int offsetYExtra = s?.CardOffsetYExtra ?? (int)Math.Round((s?.UIScale ?? 1f) * 25);
            return new Rectangle(
                (int)position.X - cw / 2,
                (int)position.Y - (ch / 2 + offsetYExtra),
                cw,
                ch
            );
        }

        private void DrawHighlight(Rectangle targetRect, float rotation, GameTime gameTime)
        {
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var s = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int th = s?.HighlightBorderThickness ?? 5;
            var highlightRect = new Rectangle(
                targetRect.X - th,
                targetRect.Y - th,
                targetRect.Width + th * 2,
                targetRect.Height + th * 2
            );

            var hoverDuration = gameTime.TotalGameTime.TotalSeconds - _pulseStartSeconds;
            var esEntity = EntityManager.GetEntitiesWithComponent<EquipmentHighlightSettings>().FirstOrDefault();
            var es = esEntity?.GetComponent<EquipmentHighlightSettings>() ?? new EquipmentHighlightSettings();
            float pulse01 = (float)(Math.Cos(hoverDuration * es.GlowPulseSpeed) * 0.5f + 0.5f);
            float eased = (float)Math.Pow(MathHelper.Clamp(pulse01, 0f, 1f), es.GlowEasingPower);
            float pulseAmount = MathHelper.Lerp(MathHelper.Clamp(es.GlowMinIntensity, 0f, 1f), MathHelper.Clamp(es.GlowMaxIntensity, 0f, 1f), eased);

            int radius = Math.Max(0, (s?.CardCornerRadius ?? 18) + th);
            var baseTex = GetRoundedRectTexture(highlightRect.Width, highlightRect.Height, radius);
            var center = new Vector2(highlightRect.X + highlightRect.Width / 2f, highlightRect.Y + highlightRect.Height / 2f);

            int layers = es.GlowLayers;
            float spread = es.GlowSpread;
            Color glowColor = new Color((byte)es.GlowColorR, (byte)es.GlowColorG, (byte)es.GlowColorB);
            for (int i = layers; i >= 1; i--)
            {
                float spreadAnim = 1f + es.GlowSpreadAmplitude * (float)Math.Sin(hoverDuration * es.GlowSpreadSpeed);
                float scale = 1f + i * spread * spreadAnim;
                float layerAlpha = MathHelper.Clamp(pulseAmount * (0.22f / i), 0f, es.MaxAlpha);
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
