using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for highlighting cards when hovered over
    /// </summary>
    [DebugTab("Card Highlight")]
    public class UIElementHighlightSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture; // Reuse texture instead of creating new ones
        private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        private double _lastTotalSeconds = 0.0; // From Update(gameTime)
        private double _pulseStartSeconds = 0.0; // When current hovered started pulsing
        private Entity _currentHovered;
        // Equipment highlight tracking (separate pulse timer)
        
        // Highlight settings now come from EquipmentHighlightSettings via HighlightSettingsSystem
        
        public UIElementHighlightSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) 
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
        
        public override void Update(GameTime gameTime)
        {
            _lastTotalSeconds = gameTime.TotalGameTime.TotalSeconds;
            base.Update(gameTime);
        }

        public void Draw()
        {
            // Draw highlight around the currently hovered UI element (card or equipment)
            var hoveredEntities = EntityManager
                .GetEntitiesWithComponent<UIElement>()
                .Where(e =>
                {
                    var ui = e.GetComponent<UIElement>();
                    return ui != null && ui.IsHovered && ui.IsInteractable;
                })
                .ToList();

            if (hoveredEntities.Count == 0)
            {
                return;
            }

            // InputSystem typically ensures only one hovered at a time; draw all just in case
            foreach (var e in hoveredEntities)
            {
                if (e.GetComponent<Intimidated>() != null) continue;

                if (!ReferenceEquals(e, _currentHovered))
                {
                    _currentHovered = e;
                    _pulseStartSeconds = _lastTotalSeconds;
                }

                var fakeGameTime = new GameTime(TimeSpan.FromSeconds(_lastTotalSeconds), TimeSpan.Zero);
                var t = e.GetComponent<Transform>();
                var ui = e.GetComponent<UIElement>();
                Rectangle targetRect;
                float rot = 0f;

                if (e.GetComponent<CardData>() != null && t != null && e.GetComponent<AssignedBlockCard>() == null)
                {
                    targetRect = ComputeCardBounds(e, t.Position);
                }
                else
                {
                    targetRect = ui.Bounds;
                }
                rot = t.Rotation;

                DrawHighlight(targetRect, rot, fakeGameTime);
            }
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
