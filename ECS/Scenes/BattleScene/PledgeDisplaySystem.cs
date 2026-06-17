using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the pledge icon on cards that have the Pledge component.
    /// The icon is rendered at the top of the card, correctly scaled and rotated.
    /// </summary>
    [DebugTab("Pledge Display")]
    public class PledgeDisplaySystem : Core.System
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Texture2D _pledgeTexture;
        private CardGeometrySettings _settings;

        [DebugEditable(DisplayName = "Icon Scale", Step = 0.01f, Min = 0.01f, Max = 1.0f)]
        public float IconScale { get; set; } = 0.09f;

        [DebugEditable(DisplayName = "Icon Offset Y", Step = 1f, Min = -300f, Max = 300f)]
        public float IconOffsetY { get; set; } = -190f;

        public PledgeDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _pledgeTexture = content.Load<Texture2D>("pledge");
            
            EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
            EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
            EventManager.Subscribe<CardRenderScaledRotatedEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderScaledRotatedEvent", () => OnCardRenderScaledRotatedEvent(evt)));
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Pledge>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw() { }

        private Rectangle ComputeCardBounds(Vector2 position)
        {
            _settings ??= CardGeometryService.GetSettings(EntityManager);
            return CardGeometryService.GetVisualRect(_settings, position);
        }

        private void OnCardRenderEvent(CardRenderEvent evt)
        {
            var card = evt.Card;
            if (card == null || card.GetComponent<Pledge>() == null) return;
            var transform = card.GetComponent<Transform>();
            if (transform == null) return;

            DrawPledgeIcon(card, transform.Position, 1f, transform.Rotation);
        }

        private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
        {
            var card = evt.Card;
            if (card == null || card.GetComponent<Pledge>() == null) return;

            DrawPledgeIcon(card, evt.Position, evt.Scale, 0f);
        }

        private void OnCardRenderScaledRotatedEvent(CardRenderScaledRotatedEvent evt)
        {
            var card = evt.Card;
            if (card == null || card.GetComponent<Pledge>() == null) return;
            var transform = card.GetComponent<Transform>();
            float rotation = transform?.Rotation ?? 0f;

            DrawPledgeIcon(card, evt.Position, evt.Scale, rotation);
        }

        private void DrawPledgeIcon(Entity card, Vector2 position, float cardScale, float cardRotation)
        {
            var bounds = CardGeometryService.GetVisualRect(CardGeometryService.GetSettings(EntityManager), position, cardScale);
            var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

            float offsetY = IconOffsetY * cardScale;
            
            float cos = (float)Math.Cos(cardRotation);
            float sin = (float)Math.Sin(cardRotation);
            Vector2 rotatedOffset = new Vector2(
                -sin * offsetY,
                cos * offsetY
            );
            Vector2 iconPos = center + rotatedOffset;

            var origin = new Vector2(_pledgeTexture.Width / 2f, _pledgeTexture.Height / 2f);
            float effectiveScale = IconScale * cardScale;
            
            _spriteBatch.Draw(
                _pledgeTexture,
                iconPos,
                null,
                Color.White,
                cardRotation,
                origin,
                effectiveScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
