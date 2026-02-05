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

        [DebugEditable(DisplayName = "Mark Alpha", Step = 5, Min = 0, Max = 255)]
        public int MarkAlpha { get; set; } = 220;

        [DebugEditable(DisplayName = "Icon Scale", Step = 0.01f, Min = 0.01f, Max = 2.0f)]
        public float IconScale { get; set; } = 0.08f;

        [DebugEditable(DisplayName = "Icon Text Gap", Step = 1f, Min = 0f, Max = 50f)]
        public float IconTextGap { get; set; } = 4f;

        [DebugEditable(DisplayName = "Mark Offset X", Step = 1f, Min = -1000f, Max = 1000f)]
        public float MarkOffsetX { get; set; } = 0f;

        [DebugEditable(DisplayName = "Mark Offset Y", Step = 1f, Min = -1000f, Max = 1000f)]
        public float MarkOffsetY { get; set; } = -207f;

        [DebugEditable(DisplayName = "Combined Y Offset", Step = 1f, Min = -100f, Max = 100f)]
        public float CombinedYOffset { get; set; } = 0f;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float TextScale { get; set; } = 0.15f;

        [DebugEditable(DisplayName = "Text Background Padding X", Step = 1f, Min = 0f, Max = 20f)]
        public float TextBgPaddingX { get; set; } = 4f;

        [DebugEditable(DisplayName = "Text Background Padding Y", Step = 1f, Min = 0f, Max = 20f)]
        public float TextBgPaddingY { get; set; } = 2f;

        [DebugEditable(DisplayName = "Text Background Height Multiplier", Step = 0.05f, Min = 0.5f, Max = 2.0f)]
        public float TextBgHeightMultiplier { get; set; } = 0.55f;

        [DebugEditable(DisplayName = "Trap Left Angle", Step = 1f, Min = -45f, Max = 45f)]
        public float TrapLeftAngle { get; set; } = 11f;

        [DebugEditable(DisplayName = "Trap Right Angle", Step = 1f, Min = -45f, Max = 45f)]
        public float TrapRightAngle { get; set; } = -23f;

        [DebugEditable(DisplayName = "Trap Top Angle", Step = 1f, Min = -45f, Max = 45f)]
        public float TrapTopAngle { get; set; } = 2f;

        [DebugEditable(DisplayName = "Trap Bottom Angle", Step = 1f, Min = -45f, Max = 45f)]
        public float TrapBottomAngle { get; set; } = -2f;

        [DebugEditable(DisplayName = "Trap Left Offset", Step = 1f, Min = 0f, Max = 50f)]
        public float TrapLeftOffset { get; set; } = 0f;

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
            // No update needed - rendering is event-driven
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
            center.Y += MarkOffsetY + CombinedYOffset;

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
            center.Y += (MarkOffsetY + CombinedYOffset) * evt.Scale;

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
            float finalAlpha = MarkAlpha / 255f;
            finalAlpha = MathHelper.Clamp(finalAlpha, 0f, 1f);

            DrawChip(center, scale, rotation, effectType, finalAlpha);
        }

        private void DrawChip(Vector2 chipCenter, float scale, float rotation, MarkEffectType effectType, float alpha)
        {
            var font = FontSingleton.ContentFont;
            if (font == null || _markTexture == null) return;

            string text = MarkManagementSystem.GetEffectDescription(effectType);
            float textScaleFinal = TextScale * scale;
            var rawTextSize = font.MeasureString(text);
            var scaledTextSize = rawTextSize * textScaleFinal;

            // Icon size (independent scale)
            float iconScaleFinal = IconScale * scale;
            float iconW = _markTexture.Width * iconScaleFinal;
            float iconH = _markTexture.Height * iconScaleFinal;

            float gap = IconTextGap * scale;
            float contentW = iconW + gap + scaledTextSize.X;
            float contentH = Math.Max(iconH, scaledTextSize.Y);

            float bgHeight = (contentH + TextBgPaddingY * 2f * scale) * TextBgHeightMultiplier;

            // Compensate for trapezoid angles - angled sides reduce usable width at text level
            float leftAngleRad = MathHelper.ToRadians(Math.Abs(TrapLeftAngle));
            float rightAngleRad = MathHelper.ToRadians(Math.Abs(TrapRightAngle));
            float angleCompensation = bgHeight * ((float)Math.Tan(leftAngleRad) + (float)Math.Tan(rightAngleRad));
            float bgWidth = contentW + TextBgPaddingX * 2f * scale + angleCompensation;

            // Draw trapezoid background
            var trapezoidTexture = ECS.Rendering.PrimitiveTextureFactory.GetAntialiasedTrapezoidMask(
                _graphicsDevice, bgWidth, bgHeight, TrapLeftOffset, TrapTopAngle, TrapRightAngle, TrapBottomAngle, TrapLeftAngle);

            var bgOrigin = new Vector2(trapezoidTexture.Width / 2f, trapezoidTexture.Height / 2f);
            _spriteBatch.Draw(trapezoidTexture, chipCenter, null, Color.Black, rotation, bgOrigin, 0.5f, SpriteEffects.None, 0f);

            // Position icon and text within the chip (local coords relative to chip center)
            float contentStartX = -contentW / 2f;
            var iconLocal = new Vector2(contentStartX + iconW / 2f, 0f);
            var textLocal = new Vector2(contentStartX + iconW + gap + scaledTextSize.X / 2f, 0f);

            // Rotate local offsets for card rotation
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);

            var iconWorldOffset = new Vector2(
                iconLocal.X * cos - iconLocal.Y * sin,
                iconLocal.X * sin + iconLocal.Y * cos);
            var textWorldOffset = new Vector2(
                textLocal.X * cos - textLocal.Y * sin,
                textLocal.X * sin + textLocal.Y * cos);

            // Draw icon
            var markColor = new Color(255, 80, 80) * alpha;
            var iconOrigin = new Vector2(_markTexture.Width / 2f, _markTexture.Height / 2f);
            _spriteBatch.Draw(_markTexture, chipCenter + iconWorldOffset, null, markColor, rotation, iconOrigin, iconScaleFinal, SpriteEffects.None, 0f);

            // Draw text
            var textOrigin = new Vector2(rawTextSize.X / 2f, rawTextSize.Y / 2f);
            var textColor = Color.DarkRed;
            _spriteBatch.DrawString(font, text, chipCenter + textWorldOffset, textColor, rotation, textOrigin, textScaleFinal, SpriteEffects.None, 0f);
        }
    }
}
