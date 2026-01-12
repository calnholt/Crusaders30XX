using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
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
        private CardVisualSettings _settings;
        private bool _isPledgePhase = false;
        private readonly HashSet<Entity> _previewPledges = new HashSet<Entity>();

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
            
            // Draw pledge icon right after each card is drawn so higher-Z cards can occlude it
            EventManager.Subscribe<CardRenderEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderEvent", () => OnCardRenderEvent(evt)));
            EventManager.Subscribe<CardRenderScaledEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderScaledEvent", () => OnCardRenderScaledEvent(evt)));
            EventManager.Subscribe<CardRenderScaledRotatedEvent>(evt => FrameProfiler.Measure("PledgeDisplaySystem.OnCardRenderScaledRotatedEvent", () => OnCardRenderScaledRotatedEvent(evt)));

            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EventManager.Subscribe<SkipPledgeRequested>(OnSkipPledgeRequested);
        }

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            bool wasPledgePhase = _isPledgePhase;
            _isPledgePhase = evt.Current == SubPhase.Pledge;

            if (wasPledgePhase && !_isPledgePhase)
            {
                // Leaving pledge phase - remove any lingering previews
                var previews = EntityManager.GetEntitiesWithComponent<PledgePreview>();
                foreach (var entity in previews)
                {
                    EntityManager.RemoveComponent<PledgePreview>(entity);
                }
                _previewPledges.Clear();
            }
        }

        private void OnSkipPledgeRequested(SkipPledgeRequested evt)
        {
            // Remove temporary pledges when skipping
            var previews = EntityManager.GetEntitiesWithComponent<PledgePreview>();
            foreach (var entity in previews)
            {
                EntityManager.RemoveComponent<PledgePreview>(entity);
            }
            _previewPledges.Clear();
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Pledge>().Concat(EntityManager.GetEntitiesWithComponent<PledgePreview>()).Distinct();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_isPledgePhase) return;

            // Find all cards in hand to check hover status
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            foreach (var card in deck.Hand)
            {
                var ui = card.GetComponent<UIElement>();
                bool isHovered = ui?.IsHovered == true;
                bool hasPledge = card.GetComponent<Pledge>() != null;
                bool hasPreview = card.GetComponent<PledgePreview>() != null;
                bool isTrackedAsPreview = _previewPledges.Contains(card);

                // Only show preview for eligible cards
                bool isEligible = PledgeManagementSystem.IsEligibleForPledge(card);

                if (isHovered && isEligible && !hasPledge && !hasPreview)
                {
                    // Add preview pledge
                    EntityManager.AddComponent(card, new PledgePreview { Owner = card });
                    _previewPledges.Add(card);
                    Console.WriteLine($"[PledgeDisplaySystem] Hovered card: adding preview pledge to {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");
                }
                else if (!isHovered && isTrackedAsPreview)
                {
                    // Remove preview pledge
                    EntityManager.RemoveComponent<PledgePreview>(card);
                    _previewPledges.Remove(card);
                    Console.WriteLine($"[PledgeDisplaySystem] Unhovered card: removing preview pledge from {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");
                }
                else if (hasPledge && isTrackedAsPreview)
                {
                    // If it was a preview but now has a real pledge (clicked), stop tracking as preview
                    EntityManager.RemoveComponent<PledgePreview>(card);
                    _previewPledges.Remove(card);
                }
            }
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
            // Only draw overlay for pledged or preview cards
            var card = evt.Card;
            if (card == null || (card.GetComponent<Pledge>() == null && card.GetComponent<PledgePreview>() == null)) return;
            var transform = card.GetComponent<Transform>();
            if (transform == null) return;

            DrawPledgeIcon(card, transform.Position, 1f, transform.Rotation);
        }

        private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
        {
            var card = evt.Card;
            if (card == null || (card.GetComponent<Pledge>() == null && card.GetComponent<PledgePreview>() == null)) return;

            DrawPledgeIcon(card, evt.Position, evt.Scale, 0f);
        }

        private void OnCardRenderScaledRotatedEvent(CardRenderScaledRotatedEvent evt)
        {
            var card = evt.Card;
            if (card == null || (card.GetComponent<Pledge>() == null && card.GetComponent<PledgePreview>() == null)) return;
            var transform = card.GetComponent<Transform>();
            float rotation = transform?.Rotation ?? 0f;

            DrawPledgeIcon(card, evt.Position, evt.Scale, rotation);
        }

        private void DrawPledgeIcon(Entity card, Vector2 position, float cardScale, float cardRotation)
        {
            // Compute card bounds at the given position
            var bounds = ComputeCardBounds(position);
            var center = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

            // Calculate icon position (near top of card)
            float offsetY = IconOffsetY * cardScale;
            
            // Apply card rotation to the offset
            float cos = (float)Math.Cos(cardRotation);
            float sin = (float)Math.Sin(cardRotation);
            Vector2 rotatedOffset = new Vector2(
                -sin * offsetY,
                cos * offsetY
            );
            Vector2 iconPos = center + rotatedOffset;

            // Draw icon centered
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
