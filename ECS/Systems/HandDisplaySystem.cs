using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for managing the display of cards in the player's hand
    /// </summary>
    public class HandDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        
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

                        var pivot = new Vector2(screenWidth / 2f, screenHeight - CardConfig.HAND_BOTTOM_MARGIN);

                        // normalized index t in [-1, 1]
                        float mid = (count - 1) * 0.5f;
                        float t = (count == 1) ? 0f : (cardIndex - mid) / Math.Max(1f, mid);

                        // angle and vertical arc offset
                        float angleDeg = t * CardConfig.HAND_FAN_MAX_ANGLE_DEG;
                        float angleRad = CardConfig.DegToRad(angleDeg);

                        // Horizontal spread based on spacing
                        float x = pivot.X + t * CardConfig.CARD_SPACING;

                        // Vertical arc using a circular approximation
                        float cos = (float)Math.Cos(angleRad);
                        float yArc = -CardConfig.HAND_FAN_RADIUS * (1f - cos);
                        float y = pivot.Y + CardConfig.HAND_FAN_CURVE_OFFSET + yArc;

                        // Hover lift
                        var ui = entity.GetComponent<UIElement>();
                        bool hovered = ui?.IsHovered == true;
                        if (hovered)
                        {
                            y -= CardConfig.HAND_HOVER_LIFT;
                        }

                        // Apply transform with smooth tween toward target (frame-rate independent)
                        var current = transform.Position;
                        var target = new Vector2(x, y);
                        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                        float alpha = 1f - (float)Math.Exp(-CardConfig.HAND_TWEEN_SPEED * dt);
                        transform.Position = Vector2.Lerp(current, target, MathHelper.Clamp(alpha, 0f, 1f));
                        transform.Rotation = angleRad; // reserved for future visual rotation support
                        transform.Scale = new Vector2(CardConfig.HAND_HOVER_SCALE, CardConfig.HAND_HOVER_SCALE);

                        // Z-order (ensures proper overlapping)
                        transform.ZOrder = CardConfig.HAND_Z_BASE + (cardIndex * CardConfig.HAND_Z_STEP) + (hovered ? CardConfig.HAND_Z_HOVER_BOOST : 0);

                        // Update AABB bounds (axis-aligned) that contain potential rotated card
                        int w = CardConfig.CARD_WIDTH;
                        int h = CardConfig.CARD_HEIGHT;
                        float absCos = Math.Abs(cos);
                        float absSin = Math.Abs((float)Math.Sin(angleRad));
                        int aabbW = (int)(w * absCos + h * absSin);
                        int aabbH = (int)(h * absCos + w * absSin);

                        if (ui != null)
                        {
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
                    
                    foreach (var entity in cardsInHand)
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