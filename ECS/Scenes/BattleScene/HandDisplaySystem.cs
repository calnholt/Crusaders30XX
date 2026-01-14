using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for managing the display of cards in the player's hand
    /// </summary>
    [DebugTab("Hand Display")] 
    public class HandDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
		
		// Auto scale for smaller screens
		[DebugEditable(DisplayName = "Auto Scale Threshold Height", Step = 10f, Min = 200f, Max = 4000f)]
		public int AutoScaleThresholdHeight { get; set; } = 1080;
		[DebugEditable(DisplayName = "Min Card Scale", Step = 0.05f, Min = 0.3f, Max = 1.0f)]
		public float MinCardScale { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Scale Curve Power", Step = 0.1f, Min = 0.1f, Max = 4f)]
		public float CardScalePower { get; set; } = 1.0f;
        
        // Hand layout settings (moved from CardConfig)
        [DebugEditable(DisplayName = "Bottom Margin", Step = 2f, Min = 0f, Max = 1000f)]
        public float HandBottomMargin { get; set; } = 168f;

        [DebugEditable(DisplayName = "Max Angle (deg)", Step = 0.5f, Min = 0f, Max = 45f)]
        public float HandFanMaxAngleDeg { get; set; } = 5f;

        [DebugEditable(DisplayName = "Arc Radius", Step = 2f, Min = 0f, Max = 2000f)]
        public float HandFanRadius { get; set; } = 0f;

        // Height difference between center and ends of the fan. If > 0, this
        // overrides radius-based curvature to make tuning intuitive. The ends
        // will be lowered by at most this many pixels relative to the center.
        [DebugEditable(DisplayName = "Curve Height", Step = 1f, Min = 0f, Max = 500f)]
        public float HandFanCurveHeight { get; set; } = 24f;

        [DebugEditable(DisplayName = "Curve Offset Y", Step = 2f, Min = -1000f, Max = 1000f)]
        public float HandFanCurveOffset { get; set; } = 0f;

        [DebugEditable(DisplayName = "Hover Lift", Step = 1f, Min = 0f, Max = 200f)]
        public float HandHoverLift { get; set; } = 4f;

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
        
		// Root entity removed; each card owns its transform base and parallax

		public HandDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
        }

		// Baseline snapshot of CardVisualSettings captured once to compute scaled values from
		private bool _baselineCaptured;
		private int _baseCardWidth, _baseCardHeight, _baseCardGap, _baseCardBorderThickness, _baseCardCornerRadius, _baseHighlightBorderThickness;
		private int _baseTextMarginX, _baseTextMarginY, _baseBlockNumberMarginX, _baseBlockNumberMarginY, _baseCardOffsetYExtra;
		private float _baseUIScale, _baseNameScale, _baseCostScale, _baseDescriptionScale, _baseBlockScale, _baseBlockNumberScale;
		private float _lastAppliedScale = -1f;

		private void CaptureBaselineIfNeeded(CardVisualSettings s)
		{
			if (_baselineCaptured || s == null) return;
			_baseUIScale = s.UIScale;
			_baseCardWidth = s.CardWidth;
			_baseCardHeight = s.CardHeight;
			_baseCardOffsetYExtra = s.CardOffsetYExtra;
			_baseCardGap = s.CardGap;
			_baseCardBorderThickness = s.CardBorderThickness;
			_baseCardCornerRadius = s.CardCornerRadius;
			_baseHighlightBorderThickness = s.HighlightBorderThickness;
			_baseTextMarginX = s.TextMarginX;
			_baseTextMarginY = s.TextMarginY;
			_baseNameScale = s.NameScale;
			_baseCostScale = s.CostScale;
			_baseDescriptionScale = s.DescriptionScale;
			_baseBlockScale = s.BlockScale;
			_baseBlockNumberScale = s.BlockNumberScale;
			_baseBlockNumberMarginX = s.BlockNumberMarginX;
			_baseBlockNumberMarginY = s.BlockNumberMarginY;
			_baselineCaptured = true;
		}

		private void ApplyViewportScalingIfNeeded(CardVisualSettings s)
		{
			if (s == null) return;
			CaptureBaselineIfNeeded(s);
			if (!_baselineCaptured) return;

			int vh = Game1.VirtualHeight;
			float baseH = Math.Max(1, AutoScaleThresholdHeight);
			float raw = vh >= baseH ? 1f : (vh / baseH);
			float scaled = MathF.Pow(MathF.Max(MinCardScale, MathF.Min(1f, raw)), MathF.Max(0.1f, CardScalePower));
			if (MathF.Abs(scaled - _lastAppliedScale) < 0.001f) return; // no change
			_lastAppliedScale = scaled;

			// Scale integer dimensions and margins
			s.CardWidth = Math.Max(10, (int)MathF.Round(_baseCardWidth * scaled));
			s.CardHeight = Math.Max(10, (int)MathF.Round(_baseCardHeight * scaled));
			s.CardOffsetYExtra = (int)MathF.Round(_baseCardOffsetYExtra * scaled);
			s.CardGap = (int)MathF.Round(_baseCardGap * scaled);
			s.CardBorderThickness = Math.Max(0, (int)MathF.Round(_baseCardBorderThickness * scaled));
			s.CardCornerRadius = Math.Max(0, (int)MathF.Round(_baseCardCornerRadius * scaled));
			s.HighlightBorderThickness = Math.Max(0, (int)MathF.Round(_baseHighlightBorderThickness * scaled));
			s.TextMarginX = Math.Max(0, (int)MathF.Round(_baseTextMarginX * scaled));
			s.TextMarginY = Math.Max(0, (int)MathF.Round(_baseTextMarginY * scaled));
			s.BlockNumberMarginX = Math.Max(0, (int)MathF.Round(_baseBlockNumberMarginX * scaled));
			s.BlockNumberMarginY = Math.Max(0, (int)MathF.Round(_baseBlockNumberMarginY * scaled));

			// Scale float text scales and UI scale coherently
			s.UIScale = _baseUIScale * scaled;
			s.NameScale = _baseNameScale * scaled;
			s.CostScale = _baseCostScale * scaled;
			s.DescriptionScale = _baseDescriptionScale * scaled;
			s.BlockScale = _baseBlockScale * scaled;
			s.BlockNumberScale = _baseBlockNumberScale * scaled;
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
                if (deck != null && deck.Hand.Contains(entity) && entity.GetComponent<AnimatingHandToDiscard>() == null && entity.GetComponent<AnimatingHandToZone>() == null && entity.GetComponent<AnimatingHandToDrawPile>() == null)
                {
                    // Get the index of this card in the hand
                    var cardIndex = deck.Hand.IndexOf(entity);
                    
                    if (cardIndex >= 0)
                    {
                        // Compute fan layout positions around bottom-center pivot
                        int count = deck.Hand.Count;
                        float screenWidth = Game1.VirtualWidth;
                        float screenHeight = Game1.VirtualHeight;

						// Scale bottom margin for small screens using the same scaling power
						int vh = Game1.VirtualHeight;
						float baseH = Math.Max(1, AutoScaleThresholdHeight);
						float rawScale = vh >= baseH ? 1f : (vh / baseH);
						float marginScale = MathF.Pow(MathF.Max(MinCardScale, MathF.Min(1f, rawScale)), MathF.Max(0.1f, CardScalePower));
						float bottomMarginScaled = HandBottomMargin * marginScale;
					// Pivot anchored to screen bottom-center; ParallaxLayer on each card will offset current Position
					var pivot = new Vector2(screenWidth / 2f, screenHeight - bottomMarginScaled);

                        // normalized index t in [-1, 1]
                        float mid = (count - 1) * 0.5f;
                        float t = (count == 1) ? 0f : (cardIndex - mid) / Math.Max(1f, mid);

                        // angle (in radians) based on normalized index
                        float maxAngleRad = (float)(Math.PI / 180.0) * HandFanMaxAngleDeg;
                        float angleRad = t * maxAngleRad;

                        // Horizontal spread scales with hand size
                        // Use unnormalized index delta so width grows with number of cards
                        float indexDelta = cardIndex - mid; // [-mid, +mid]
                        // Use shared settings for spacing
                        var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
						var cvs = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
						// Apply viewport-aware scaling to card visuals (1080p baseline -> scale down below)
						ApplyViewportScalingIfNeeded(cvs);
                        float cardSpacing = (cvs != null) ? (cvs.CardWidth + cvs.CardGap) : 0f;
                        if (cardSpacing <= 0f) { cardSpacing = 250 + (-20); }
                        float x = pivot.X + indexDelta * cardSpacing;

                        // Vertical arc
                        // If curve height is specified, scale curvature so the ends are lowered by
                        // exactly HandFanCurveHeight regardless of angle settings. Otherwise fall back
                        // to the legacy radius-based formula.
                        float yArc;
                        if (HandFanCurveHeight > 0f && maxAngleRad > 0f)
                        {
                            float denom = 1f - (float)Math.Cos(Math.Max(0.0001f, maxAngleRad));
                            float numer = 1f - (float)Math.Cos(Math.Abs(angleRad));
                            float factor = numer / denom; // 0 at center, 1 at ends
                            yArc = HandFanCurveHeight * factor;
                        }
                        else
                        {
                            float cos = (float)Math.Cos(angleRad);
                            yArc = HandFanRadius * (1f - cos);
                        }
                        float y = pivot.Y + HandFanCurveOffset + yArc;

						// If this card just appeared, spawn its base offscreen to the right so it flies in
						if (transform.BasePosition == Vector2.Zero)
						{
							float spawnX = Game1.VirtualWidth + ((cvs?.CardWidth ?? 250) * 1.5f);
							float spawnY = pivot.Y + HandFanCurveOffset;
							var spawn = new Vector2(spawnX, spawnY);
							transform.BasePosition = spawn;
							transform.Position = spawn;
						}

                        // Hover lift
                        var ui = entity.GetComponent<UIElement>();
                        bool hovered = ui?.IsHovered == true;
                        if (hovered) { y -= HandHoverLift; }

						// Smooth tween the BasePosition toward the layout target; ParallaxLayer will set current Position
						var baseCurrent = transform.BasePosition;
						var baseTarget = new Vector2(x, y);
						float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
						float alpha = 1f - (float)Math.Exp(-HandTweenSpeed * dt);
						transform.BasePosition = Vector2.Lerp(baseCurrent, baseTarget, MathHelper.Clamp(alpha, 0f, 1f));
                        transform.Rotation = angleRad; // reserved for future visual rotation support
                        transform.Scale = new Vector2(HandHoverScale, HandHoverScale);

                        // Z-order (ensures proper overlapping)
                        // Special case: if this is the equipped weapon at index 0, never allow it to pop above index 1
                        bool isWeapon = false;
                        try
                        {
                            string id = cardData.Card.CardId ?? string.Empty;
                            var cardObj = CardFactory.Create(id);
                            if (!string.IsNullOrEmpty(id) && cardObj != null)
                            {
                                isWeapon = cardObj.IsWeapon;
                            }
                        }
                        catch { }

                        if (isWeapon && cardIndex == 0)
                        {
                            // Keep weapon behind the next card at all times (no hover boost)
                            transform.ZOrder = HandZBase - 1;
                        }
                        else
                        {
                            transform.ZOrder = HandZBase + (cardIndex * HandZStep) + (hovered ? HandZHoverBoost : 0);
                        }

                        // Update UI bounds to original unrotated dimensions
                        if (ui != null)
                        {
                            int w = cvs?.CardWidth ?? 250;
                            int h = cvs?.CardHeight ?? 350;

                            ui.Bounds = new Rectangle(
                                (int)transform.Position.X - w / 2,
                                (int)transform.Position.Y - h / 2,
                                w,
                                h
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
                    // Only draw cards that are actually in the hand and not currently animating to discard or draw pile
                    var cardsInHand = deck.Hand
                        .Where(e => e.GetComponent<AnimatingHandToDiscard>() == null && e.GetComponent<AnimatingHandToZone>() == null && e.GetComponent<AnimatingHandToDrawPile>() == null)
                        .OrderBy(e => 
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
                        if (e == payState.CardToPlay) return false;
                        if (payState.SelectedCards.Contains(e)) return false;
                        var cd = e.GetComponent<CardData>();
                        if (cd == null) return false;
						// Never allow the weapon to be used to pay costs; hide it
						try
						{
							string id = cd.Card.CardId ?? string.Empty;
							var cardObj = CardFactory.Create(id);
							if (!string.IsNullOrEmpty(id) && cardObj != null)
							{
								if (cardObj.IsWeapon) return false;
							}
						}
						catch { }
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