using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays "PLEDGE" text on cards that have the Pledge component.
    /// The text is rendered at the top of the card, correctly scaled and rotated.
    /// </summary>
    [DebugTab("Pledge Display")]
    public class PledgeDisplaySystem : Core.System
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font = FontSingleton.ContentFont;
        private readonly GraphicsDevice _graphicsDevice;
        private Texture2D _pixel;
        private CardVisualSettings _settings;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
        public float TextScale { get; set; } = 0.18f;

        [DebugEditable(DisplayName = "Text Offset Y", Step = 1f, Min = -200f, Max = 200f)]
        public float TextOffsetY { get; set; } = -100f;

        [DebugEditable(DisplayName = "Background Padding X", Step = 1f, Min = 0f, Max = 50f)]
        public float BackgroundPaddingX { get; set; } = 12f;

        [DebugEditable(DisplayName = "Background Padding Y", Step = 1f, Min = 0f, Max = 50f)]
        public float BackgroundPaddingY { get; set; } = 4f;

        [DebugEditable(DisplayName = "Background Alpha", Step = 5, Min = 0, Max = 255)]
        public int BackgroundAlpha { get; set; } = 200;

        [DebugEditable(DisplayName = "Text Color R", Step = 5, Min = 0, Max = 255)]
        public int TextColorR { get; set; } = 255;

        [DebugEditable(DisplayName = "Text Color G", Step = 5, Min = 0, Max = 255)]
        public int TextColorG { get; set; } = 215;

        [DebugEditable(DisplayName = "Text Color B", Step = 5, Min = 0, Max = 255)]
        public int TextColorB { get; set; } = 0;

        [DebugEditable(DisplayName = "Background Color R", Step = 5, Min = 0, Max = 255)]
        public int BackgroundColorR { get; set; } = 80;

        [DebugEditable(DisplayName = "Background Color G", Step = 5, Min = 0, Max = 255)]
        public int BackgroundColorG { get; set; } = 60;

        [DebugEditable(DisplayName = "Background Color B", Step = 5, Min = 0, Max = 255)]
        public int BackgroundColorB { get; set; } = 20;

        public string PledgeText { get; set; } = "PLEDGE";

        public PledgeDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            
            // Create a single pixel texture for backgrounds
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Draw pledge text right after each card is drawn so higher-Z cards can occlude it
            EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
            EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
            EventManager.Subscribe<CardRenderScaledRotatedEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderScaledRotatedEvent", () => OnCardRenderScaledRotatedEvent(evt)));
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Pledge>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No animation state needed for simple pledge display
        }

        public void Draw()
        {
            // Main draw is triggered via CardRenderEvent subscription
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
            // Only draw overlay for pledged cards
            var card = evt.Card;
            if (card == null || card.GetComponent<Pledge>() == null) return;
            var transform = card.GetComponent<Transform>();
            if (transform == null) return;

            DrawPledgeText(card, transform.Position, 1f, transform.Rotation);
        }

        private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
        {
            var card = evt.Card;
            if (card == null || card.GetComponent<Pledge>() == null) return;

            DrawPledgeText(card, evt.Position, evt.Scale, 0f);
        }

        private void OnCardRenderScaledRotatedEvent(CardRenderScaledRotatedEvent evt)
        {
            var card = evt.Card;
            if (card == null || card.GetComponent<Pledge>() == null) return;
            var transform = card.GetComponent<Transform>();
            float rotation = transform?.Rotation ?? 0f;

            DrawPledgeText(card, evt.Position, evt.Scale, rotation);
        }

        private void DrawPledgeText(Entity card, Vector2 position, float cardScale, float cardRotation)
        {
            // Compute card bounds at the given position
            var bounds = ComputeCardBounds(position);
            var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

            // Calculate text position (near top of card)
            float offsetY = TextOffsetY * cardScale;
            
            // Apply card rotation to the offset
            float cos = (float)Math.Cos(cardRotation);
            float sin = (float)Math.Sin(cardRotation);
            Vector2 rotatedOffset = new Vector2(
                -sin * offsetY,
                cos * offsetY
            );
            Vector2 textPos = center + rotatedOffset;

            // Measure text size
            var textSizeUnscaled = _font.MeasureString(PledgeText);
            float effectiveScale = TextScale * cardScale;
            var textSize = textSizeUnscaled * effectiveScale;

            // Draw background rectangle
            var bgColor = new Color(BackgroundColorR, BackgroundColorG, BackgroundColorB, BackgroundAlpha);
            float bgWidth = textSize.X + BackgroundPaddingX * 2 * cardScale;
            float bgHeight = textSize.Y + BackgroundPaddingY * 2 * cardScale;

            // Create background rect centered on text position
            var bgRect = new Rectangle(
                (int)(textPos.X - bgWidth / 2f),
                (int)(textPos.Y - bgHeight / 2f),
                (int)bgWidth,
                (int)bgHeight
            );

            // Draw rotated background
            _spriteBatch.Draw(
                _pixel,
                textPos,
                null,
                bgColor,
                cardRotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(bgWidth, bgHeight),
                SpriteEffects.None,
                0f
            );

            // Draw text centered
            var textOrigin = textSizeUnscaled / 2f;
            var textColor = new Color(TextColorR, TextColorG, TextColorB);
            
            _spriteBatch.DrawString(
                _font,
                PledgeText,
                textPos,
                textColor,
                cardRotation,
                textOrigin,
                effectiveScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
