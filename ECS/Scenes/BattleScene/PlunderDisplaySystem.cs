using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the plundered card to the right of the enemy with a damage gauge below it.
    /// Shows progress toward rescuing the card.
    /// </summary>
    [DebugTab("Plunder Display")]
    public class PlunderDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        #region Debug-Editable Fields

        [DebugEditable(DisplayName = "Card X Offset", Step = 5f, Min = -300f, Max = 300f)]
        public float CardXOffset { get; set; } = 180f;

        [DebugEditable(DisplayName = "Card Y Offset", Step = 5f, Min = -200f, Max = 200f)]
        public float CardYOffset { get; set; } = -20f;

        [DebugEditable(DisplayName = "Card Scale", Step = 0.05f, Min = 0.2f, Max = 1.5f)]
        public float CardScale { get; set; } = 0.55f;

        [DebugEditable(DisplayName = "Gauge Width", Step = 5, Min = 50, Max = 300)]
        public int GaugeWidth { get; set; } = 140;

        [DebugEditable(DisplayName = "Gauge Height", Step = 2, Min = 10, Max = 50)]
        public int GaugeHeight { get; set; } = 24;

        [DebugEditable(DisplayName = "Gauge Y Offset", Step = 5f, Min = 0f, Max = 200f)]
        public float GaugeYOffset { get; set; } = 130f;

        [DebugEditable(DisplayName = "Gauge BG Color R", Step = 5, Min = 0, Max = 255)]
        public int GaugeBgColorR { get; set; } = 40;

        [DebugEditable(DisplayName = "Gauge BG Color G", Step = 5, Min = 0, Max = 255)]
        public int GaugeBgColorG { get; set; } = 40;

        [DebugEditable(DisplayName = "Gauge BG Color B", Step = 5, Min = 0, Max = 255)]
        public int GaugeBgColorB { get; set; } = 40;

        [DebugEditable(DisplayName = "Gauge Fill Color R", Step = 5, Min = 0, Max = 255)]
        public int GaugeFillColorR { get; set; } = 200;

        [DebugEditable(DisplayName = "Gauge Fill Color G", Step = 5, Min = 0, Max = 255)]
        public int GaugeFillColorG { get; set; } = 160;

        [DebugEditable(DisplayName = "Gauge Fill Color B", Step = 5, Min = 0, Max = 255)]
        public int GaugeFillColorB { get; set; } = 50;

        [DebugEditable(DisplayName = "Font Scale", Step = 0.02f, Min = 0.1f, Max = 0.5f)]
        public float FontScale { get; set; } = 0.18f;

        #endregion

        public PlunderDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = FontSingleton.ContentFont;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Plundered>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            // Find the plundered card
            var plunderedCard = EntityManager.GetEntitiesWithComponent<Plundered>().FirstOrDefault();
            if (plunderedCard == null) return;

            var plundered = plunderedCard.GetComponent<Plundered>();
            if (plundered == null) return;

            // Get enemy position
            var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            if (enemy == null) return;

            var enemyTransform = enemy.GetComponent<Transform>();
            if (enemyTransform == null) return;

            // Calculate card position (to the right of enemy)
            var cardPosition = new Vector2(
                enemyTransform.Position.X + CardXOffset,
                enemyTransform.Position.Y + CardYOffset
            );

            // Render the plundered card
            EventManager.Publish(new CardRenderScaledEvent
            {
                Card = plunderedCard,
                Position = cardPosition,
                Scale = CardScale
            });

            // Draw the damage gauge below the card
            DrawDamageGauge(cardPosition, plundered.DamageDealt, plundered.DamageThreshold);
        }

        private void DrawDamageGauge(Vector2 cardPosition, int damageDealt, int damageThreshold)
        {
            if (damageThreshold <= 0) return;

            // Position gauge below the card
            float gaugeX = cardPosition.X - GaugeWidth / 2f;
            float gaugeY = cardPosition.Y + GaugeYOffset;

            // Background
            var bgColor = new Color(GaugeBgColorR, GaugeBgColorG, GaugeBgColorB);
            var bgRect = new Rectangle((int)gaugeX, (int)gaugeY, GaugeWidth, GaugeHeight);
            _spriteBatch.Draw(_pixel, bgRect, bgColor);

            // Fill (progress)
            float progress = Math.Min(1f, (float)damageDealt / damageThreshold);
            int fillWidth = (int)(GaugeWidth * progress);
            if (fillWidth > 0)
            {
                var fillColor = new Color(GaugeFillColorR, GaugeFillColorG, GaugeFillColorB);
                var fillRect = new Rectangle((int)gaugeX, (int)gaugeY, fillWidth, GaugeHeight);
                _spriteBatch.Draw(_pixel, fillRect, fillColor);
            }

            // Border
            DrawRectangleBorder(bgRect, Color.White, 2);

            // Text (X / Y)
            if (_font != null)
            {
                string text = $"{damageDealt} / {damageThreshold}";
                var textSize = _font.MeasureString(text) * FontScale;
                var textPosition = new Vector2(
                    gaugeX + GaugeWidth / 2f - textSize.X / 2f,
                    gaugeY + GaugeHeight / 2f - textSize.Y / 2f
                );
                _spriteBatch.DrawString(_font, text, textPosition, Color.White, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawRectangleBorder(Rectangle rect, Color color, int thickness)
        {
            // Top
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
