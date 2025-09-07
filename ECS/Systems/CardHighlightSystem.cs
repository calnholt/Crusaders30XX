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

            // Equipment highlight pre-render event (emitted by EquipmentDisplaySystem before drawing tiles)
            EventManager.Subscribe<EquipmentHighlightRenderEvent>(evt =>
            {
                var ui = evt.Equipment.GetComponent<UIElement>();
                if (ui == null || !ui.IsHovered) return;
                if (!ReferenceEquals(evt.Equipment, _currentEquipmentHovered))
                {
                    _currentEquipmentHovered = evt.Equipment;
                    _pulseStartSecondsEquipment = _lastTotalSeconds; // reset pulse for equipment
                }
                var fake = new GameTime(TimeSpan.FromSeconds(_lastTotalSeconds), TimeSpan.Zero);
                DrawEquipmentHighlight(ui.Bounds, fake);
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
            // Create highlight rectangle based on shared CardVisualSettings
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var s = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int cw = s?.CardWidth ?? 250;
            int ch = s?.CardHeight ?? 350;
            int offsetYExtra = s?.CardOffsetYExtra ?? (int)Math.Round((s?.UIScale ?? 1f) * 25);
            int th = s?.HighlightBorderThickness ?? 5;
            var cardRect = new Rectangle(
                (int)position.X - cw / 2,
                (int)position.Y - (ch / 2 + offsetYExtra),
                cw,
                ch
            );
            var highlightRect = new Rectangle(
                cardRect.X - th,
                cardRect.Y - th,
                cardRect.Width + th * 2,
                cardRect.Height + th * 2
            );

            // Add pulsing effect based on individual card hover time
            var hoverDuration = gameTime.TotalGameTime.TotalSeconds - _pulseStartSeconds;
            // Cosine pulse mapped to 0..1, then eased and remapped to [GlowMinIntensity..GlowMaxIntensity]
            var esEntity = EntityManager.GetEntitiesWithComponent<EquipmentHighlightSettings>().FirstOrDefault();
            var es = esEntity?.GetComponent<EquipmentHighlightSettings>() ?? new EquipmentHighlightSettings();
            float pulse01 = (float)(Math.Cos(hoverDuration * es.GlowPulseSpeed) * 0.5f + 0.5f);
            float eased = (float)Math.Pow(MathHelper.Clamp(pulse01, 0f, 1f), es.GlowEasingPower);
            float pulseAmount = MathHelper.Lerp(MathHelper.Clamp(es.GlowMinIntensity, 0f, 1f), MathHelper.Clamp(es.GlowMaxIntensity, 0f, 1f), eased);
            
            // Soft glow: draw multiple expanded rounded rects with decreasing alpha
            int radius = Math.Max(0, (s?.CardCornerRadius ?? 18) + th);
            var baseTex = GetRoundedRectTexture(highlightRect.Width, highlightRect.Height, radius);
            var center = new Vector2(highlightRect.X + highlightRect.Width / 2f, highlightRect.Y + highlightRect.Height / 2f);

            // Layered glow
            int layers = es.GlowLayers;
            float spread = es.GlowSpread; // how much each layer expands
            Color glowColor = new Color((byte)es.GlowColorR, (byte)es.GlowColorG, (byte)es.GlowColorB);
            for (int i = layers; i >= 1; i--)
            {
                // Temporal spread animation (gently expand/contract the glow)
                float spreadAnim = 1f + es.GlowSpreadAmplitude * (float)Math.Sin(hoverDuration * es.GlowSpreadSpeed);
                float scale = 1f + i * spread * spreadAnim;
                // Fade out quickly per layer; start bright on pulse reset
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

        private void DrawEquipmentHighlight(Rectangle bounds, GameTime gameTime)
        {
            var settings = EntityManager.GetEntitiesWithComponent<EquipmentHighlightSettings>().FirstOrDefault()?.GetComponent<EquipmentHighlightSettings>() ?? new EquipmentHighlightSettings();
            int th = Math.Max(0, settings.HighlightBorderThickness);
            int radius = Math.Max(0, settings.CornerRadius + th);
            var highlightRect = new Rectangle(
                bounds.X - th,
                bounds.Y - th,
                bounds.Width + th * 2,
                bounds.Height + th * 2
            );

            var baseTex = GetRoundedRectTexture(highlightRect.Width, highlightRect.Height, radius);
            var center = new Vector2(highlightRect.X + highlightRect.Width / 2f, highlightRect.Y + highlightRect.Height / 2f);

            var hoverDuration = gameTime.TotalGameTime.TotalSeconds - _pulseStartSecondsEquipment;
            float pulse01 = (float)(Math.Cos(hoverDuration * settings.GlowPulseSpeed) * 0.5f + 0.5f);
            float eased = (float)Math.Pow(MathHelper.Clamp(pulse01, 0f, 1f), settings.GlowEasingPower);
            float pulseAmount = MathHelper.Lerp(MathHelper.Clamp(settings.GlowMinIntensity, 0f, 1f), MathHelper.Clamp(settings.GlowMaxIntensity, 0f, 1f), eased);

            Color glowColor = new Color((byte)settings.GlowColorR, (byte)settings.GlowColorG, (byte)settings.GlowColorB);
            for (int i = settings.GlowLayers; i >= 1; i--)
            {
                float spreadAnim = 1f + settings.GlowSpreadAmplitude * (float)Math.Sin(hoverDuration * settings.GlowSpreadSpeed);
                float scale = 1f + i * settings.GlowSpread * spreadAnim;
                float layerAlpha = MathHelper.Clamp(pulseAmount * (0.22f / i), 0f, settings.MaxAlpha);
                _spriteBatch.Draw(
                    baseTex,
                    position: center,
                    sourceRectangle: null,
                    color: glowColor * layerAlpha,
                    rotation: 0f,
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
