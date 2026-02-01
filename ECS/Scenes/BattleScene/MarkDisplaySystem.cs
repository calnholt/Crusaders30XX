using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Draws the mark.png texture over cards that have the Marked component.
    /// Also displays the penalty effect text below the icon.
    /// </summary>
    [DebugTab("Mark Display")]
    public class MarkDisplaySystem : Core.System
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly GraphicsDevice _graphicsDevice;
        private Texture2D _markTexture;
        private CardVisualSettings _settings;
        private float _elapsedTime = 0f;

        [DebugEditable(DisplayName = "Mark Alpha", Step = 5, Min = 0, Max = 255)]
        public int MarkAlpha { get; set; } = 220;

        [DebugEditable(DisplayName = "Mark Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
        public float MarkScale { get; set; } = 0.5f;

        [DebugEditable(DisplayName = "Mark Offset X", Step = 1f, Min = -100f, Max = 100f)]
        public float MarkOffsetX { get; set; } = 0f;

        [DebugEditable(DisplayName = "Mark Offset Y", Step = 1f, Min = -100f, Max = 100f)]
        public float MarkOffsetY { get; set; } = -20f;

        [DebugEditable(DisplayName = "Pulse Speed", Step = 0.1f, Min = 0f, Max = 10f)]
        public float PulseSpeed { get; set; } = 1.5f;

        [DebugEditable(DisplayName = "Min Alpha", Step = 5, Min = 0, Max = 255)]
        public int MinAlpha { get; set; } = 140;

        [DebugEditable(DisplayName = "Max Alpha", Step = 5, Min = 0, Max = 255)]
        public int MaxAlpha { get; set; } = 255;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float TextScale { get; set; } = 0.12f;

        [DebugEditable(DisplayName = "Text Offset Y", Step = 1f, Min = -100f, Max = 100f)]
        public float TextOffsetY { get; set; } = 30f;

        public MarkDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D markTexture)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _markTexture = markTexture;
            EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("MarkDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
            EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("MarkDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Marked>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Draw()
        {
            // Drawing is handled via event subscriptions
        }

        private Rectangle ComputeCardBounds(Vector2 position)
        {
            _settings ??= EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
            int cw = _settings?.CardWidth ?? 250;
            int ch = _settings?.CardHeight ?? 350;
            int offsetYExtra = _settings?.CardOffsetYExtra ?? (int)Math.Round((_settings?.UIScale ?? 1f) * 25);
            return new Rectangle(
                (int)position.X - cw / 2,
                (int)position.Y - (ch / 2 + offsetYExtra),
                cw,
                ch
            );
        }

        private void OnCardRenderEvent(CardRenderEvent evt)
        {
            if (!ShouldRenderMark(evt.Card)) return;

            var transform = evt.Card.GetComponent<Transform>();
            var ui = evt.Card.GetComponent<UIElement>();
            if (transform == null || ui == null) return;

            var bounds = ComputeCardBounds(transform.Position);
            var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

            center.X += MarkOffsetX;
            center.Y += MarkOffsetY;

            var marked = evt.Card.GetComponent<Marked>();
            DrawMarkOverlay(center, bounds.Width, bounds.Height, 1f, transform.Rotation, marked?.EffectType ?? MarkEffectType.Lose1HP);
        }

        private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
        {
            if (!ShouldRenderMark(evt.Card)) return;

            var transform = evt.Card.GetComponent<Transform>();
            if (transform == null) return;

            _settings ??= EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
            int cardWidth = _settings?.CardWidth ?? 250;
            int cardHeight = _settings?.CardHeight ?? 350;
            int cardOffsetYExtra = _settings?.CardOffsetYExtra ?? 0;

            var center = evt.Position;
            int offsetY = (int)Math.Round(cardOffsetYExtra * evt.Scale);
            center.Y -= offsetY;

            center.X += MarkOffsetX * evt.Scale;
            center.Y += MarkOffsetY * evt.Scale;

            var marked = evt.Card.GetComponent<Marked>();
            DrawMarkOverlay(center, cardWidth, cardHeight, evt.Scale, transform.Rotation, marked?.EffectType ?? MarkEffectType.Lose1HP);
        }

        private bool ShouldRenderMark(Entity card)
        {
            return card != null
                && card.GetComponent<Marked>() != null
                && _markTexture != null;
        }

        private void DrawMarkOverlay(Vector2 center, float cardWidth, float cardHeight, float scale, float rotation, MarkEffectType effectType)
        {
            // Calculate pulsing alpha using sine wave
            float alphaPulse = (float)Math.Sin(_elapsedTime * PulseSpeed * Math.PI) * 0.5f + 0.5f;
            int currentAlpha = (int)MathHelper.Lerp(MinAlpha, MaxAlpha, alphaPulse);
            float finalAlpha = (currentAlpha / 255f) * (MarkAlpha / 255f);
            finalAlpha = MathHelper.Clamp(finalAlpha, 0f, 1f);

            // Use uniform scale - fit to card width
            float uniformScale = (cardWidth / (float)_markTexture.Width) * MarkScale * scale;

            // Draw the mark texture centered on the card with red tint for danger
            var markColor = new Color(255, 80, 80) * finalAlpha;
            _spriteBatch.Draw(
                _markTexture,
                center,
                null,
                markColor,
                rotation,
                new Vector2(_markTexture.Width / 2f, _markTexture.Height / 2f),
                uniformScale,
                SpriteEffects.None,
                0f
            );

            // Draw effect text below the mark icon
            DrawEffectText(center, cardWidth, cardHeight, scale, rotation, effectType, finalAlpha);
        }

        private void DrawEffectText(Vector2 center, float cardWidth, float cardHeight, float scale, float rotation, MarkEffectType effectType, float alpha)
        {
            var font = FontSingleton.ContentFont;
            if (font == null) return;

            string text = MarkManagementSystem.GetEffectDescription(effectType);
            var textSize = font.MeasureString(text);
            float textScaleFinal = TextScale * scale;

            // Calculate text position below the mark icon
            var localOffset = new Vector2(0f, TextOffsetY * scale);

            // Rotate the offset around the center
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotatedOffset = new Vector2(
                localOffset.X * cos - localOffset.Y * sin,
                localOffset.X * sin + localOffset.Y * cos
            );

            var textPos = center + rotatedOffset;

            // Center the text
            var textOrigin = new Vector2(textSize.X / 2f, textSize.Y / 2f);

            // Color based on effect type
            Color textColor = effectType switch
            {
                MarkEffectType.Lose1HP or MarkEffectType.Lose2HP => new Color(255, 100, 100),
                MarkEffectType.Gain1Penance => new Color(180, 100, 255),
                MarkEffectType.Gain2Bleed => new Color(200, 50, 50),
                MarkEffectType.Gain1Burn => new Color(255, 150, 50),
                _ => Color.White
            };

            // Draw text with shadow for readability
            var shadowOffset = new Vector2(2 * scale, 2 * scale);
            var rotatedShadow = new Vector2(
                shadowOffset.X * cos - shadowOffset.Y * sin,
                shadowOffset.X * sin + shadowOffset.Y * cos
            );
            _spriteBatch.DrawString(font, text, textPos + rotatedShadow, Color.Black * (alpha * 0.8f), rotation, textOrigin, textScaleFinal, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(font, text, textPos, textColor * alpha, rotation, textOrigin, textScaleFinal, SpriteEffects.None, 0f);
        }
    }
}
