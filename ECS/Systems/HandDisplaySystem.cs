using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for managing the display of cards in the player's hand
    /// </summary>
    [DebugTab("Hand Display")] 
    public class HandDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        
        // Hand layout settings (moved from CardConfig)
        [DebugEditable(DisplayName = "Bottom Margin", Step = 2f, Min = 0f, Max = 1000f)]
        public float HandBottomMargin { get; set; } = 150f;

        [DebugEditable(DisplayName = "Max Angle (deg)", Step = 0.5f, Min = 0f, Max = 45f)]
        public float HandFanMaxAngleDeg { get; set; } = 5f;

        [DebugEditable(DisplayName = "Arc Radius", Step = 2f, Min = 0f, Max = 2000f)]
        public float HandFanRadius { get; set; } = 0f;

        [DebugEditable(DisplayName = "Curve Offset Y", Step = 2f, Min = -1000f, Max = 1000f)]
        public float HandFanCurveOffset { get; set; } = 0f;

        [DebugEditable(DisplayName = "Hover Lift", Step = 1f, Min = 0f, Max = 200f)]
        public float HandHoverLift { get; set; } = 10f;

        [DebugEditable(DisplayName = "Hover Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
        public float HandHoverScale { get; set; } = 1.0f;

        [DebugEditable(DisplayName = "Z Base", Step = 1, Min = -10000, Max = 10000)]
        public int HandZBase { get; set; } = 100;

        [DebugEditable(DisplayName = "Z Step", Step = 1, Min = -1000, Max = 1000)]
        public int HandZStep { get; set; } = 1;

        [DebugEditable(DisplayName = "Z Hover Boost", Step = 10, Min = 0, Max = 10000)]
        public int HandZHoverBoost { get; set; } = 1000;

        [DebugEditable(DisplayName = "Tween Speed", Step = 0.5f, Min = 0.1f, Max = 60f)]
        public float HandTweenSpeed { get; set; } = 12f;
        
        public HandDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Update card positions based on hand layout
            UpdateCardPosition(entity, gameTime);
        }
        
        private void UpdateCardPosition(Entity entity, GameTime gameTime)
        {
            var transform = entity.GetComponent<Transform>();
            var cardData = entity.GetComponent<CardData>();
            
            if (transform == null || cardData == null) return;
            
            // Find the deck entity and check if this card is in the hand
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null && deck.Hand.Contains(entity))
                {
                    // Get the index of this card in the hand
                    var cardIndex = deck.Hand.IndexOf(entity);
                    
                    if (cardIndex >= 0)
                    {
                        // Compute fan layout positions around bottom-center pivot
                        int count = deck.Hand.Count;
                        float screenWidth = _graphicsDevice.Viewport.Width;
                        float screenHeight = _graphicsDevice.Viewport.Height;

                        var pivot = new Vector2(screenWidth / 2f, screenHeight - HandBottomMargin);

                        // normalized index t in [-1, 1]
                        float mid = (count - 1) * 0.5f;
                        float t = (count == 1) ? 0f : (cardIndex - mid) / Math.Max(1f, mid);

                        // angle and vertical arc offset
                        float angleDeg = t * HandFanMaxAngleDeg;
                        float angleRad = (float)(Math.PI / 180.0) * angleDeg;

                        // Horizontal spread scales with hand size
                        // Use unnormalized index delta so width grows with number of cards
                        float indexDelta = cardIndex - mid; // [-mid, +mid]
                        // Use shared settings for spacing
                        var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
                        var cvs = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
                        float cardSpacing = (cvs != null) ? (cvs.CardWidth + cvs.CardGap) : 0f;
                        if (cardSpacing <= 0f) { cardSpacing = 250 + (-20); }
                        float x = pivot.X + indexDelta * cardSpacing;

                        // Vertical arc using a circular approximation
                        float cos = (float)Math.Cos(angleRad);
                        // Ends lower (greater Y), center higher (smaller Y)
                        float yArc = HandFanRadius * (1f - cos);
                        float y = pivot.Y + HandFanCurveOffset + yArc;

                        // If this card just appeared (default position), spawn it offscreen to the right (due east)
                        if (transform.Position == Vector2.Zero)
                        {
                            float spawnX = _graphicsDevice.Viewport.Width + ((cvs?.CardWidth ?? 250) * 1.5f);
                            float spawnY = pivot.Y + HandFanCurveOffset;
                            transform.Position = new Vector2(spawnX, spawnY);
                        }

                        // Hover lift
                        var ui = entity.GetComponent<UIElement>();
                        bool hovered = ui?.IsHovered == true;
                        if (hovered) { y -= HandHoverLift; }

                        // Apply transform with smooth tween toward target (frame-rate independent)
                        var current = transform.Position;
                        var target = new Vector2(x, y);
                        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                        float alpha = 1f - (float)Math.Exp(-HandTweenSpeed * dt);
                        transform.Position = Vector2.Lerp(current, target, MathHelper.Clamp(alpha, 0f, 1f));
                        transform.Rotation = angleRad; // reserved for future visual rotation support
                        transform.Scale = new Vector2(HandHoverScale, HandHoverScale);

                        // Z-order (ensures proper overlapping)
                        transform.ZOrder = HandZBase + (cardIndex * HandZStep) + (hovered ? HandZHoverBoost : 0);

                        // Update UI bounds to axis-aligned bounding box of the rotated card
                        if (ui != null)
                        {
                            int w = cvs?.CardWidth ?? 250;
                            int h = cvs?.CardHeight ?? 350;
                            float absCos = Math.Abs(cos);
                            float absSin = Math.Abs((float)Math.Sin(angleRad));
                            int aabbW = (int)(w * absCos + h * absSin);
                            int aabbH = (int)(h * absCos + w * absSin);

                            ui.Bounds = new Rectangle(
                                (int)transform.Position.X - aabbW / 2,
                                (int)transform.Position.Y - aabbH / 2,
                                aabbW,
                                aabbH
                            );
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Triggers deck shuffling and drawing of cards by publishing events
        /// </summary>
        public void TriggerDeckShuffleAndDraw(int drawCount = 4)
        {
            // Find the deck entity
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null)
                {
                    // Publish event for deck shuffling and drawing
                    EventManager.Publish(new DeckShuffleDrawEvent
                    {
                        Deck = deckEntity,
                        DrawCount = drawCount
                    });
                }
            }
        }
        
        /// <summary>
        /// Draws all cards in hand by publishing render events
        /// </summary>
        public void DrawHand()
        {
            // Find the deck entity and get cards that are actually in the hand
            var deckEntities = EntityManager.GetEntitiesWithComponent<Deck>();
            var deckEntity = deckEntities.FirstOrDefault();
            
            if (deckEntity != null)
            {
                var deck = deckEntity.GetComponent<Deck>();
                if (deck != null)
                {
                    // Only draw cards that are actually in the hand
                    var cardsInHand = deck.Hand.OrderBy(e => 
                    {
                        var transform = e.GetComponent<Transform>();
                        return transform?.ZOrder ?? 0;
                    });
                    
                    // If pay-cost overlay is open, hide cards that are not viable to pay remaining costs,
                    // and hide any cards already selected for payment.
                    var payStateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                    var payState = payStateEntity?.GetComponent<PayCostOverlayState>();
                    bool isPayOpen = payState != null && payState.IsOpen;

                    bool IsViable(Entity e)
                    {
                        if (!isPayOpen) return true;
                        if (payState.SelectedCards.Contains(e)) return false;
                        var cd = e.GetComponent<CardData>();
                        if (cd == null) return false;
                        foreach (var req in payState.RequiredCosts)
                        {
                            if (req == "Any") return true;
                            if (req == "Red" && cd.Color == CardData.CardColor.Red) return true;
                            if (req == "White" && cd.Color == CardData.CardColor.White) return true;
                            if (req == "Black" && cd.Color == CardData.CardColor.Black) return true;
                        }
                        return false;
                    }

                    var toDraw = isPayOpen ? cardsInHand.Where(IsViable) : cardsInHand;

                    foreach (var entity in toDraw)
                    {
                        var transform = entity.GetComponent<Transform>();
                        if (transform != null)
                        {
                            // Publish render event for each card
                            EventManager.Publish(new CardRenderEvent
                            {
                                Card = entity,
                                Position = transform.Position,
                                IsInHand = true
                            });
                        }
                    }
                }
            }
        }
    }
} 