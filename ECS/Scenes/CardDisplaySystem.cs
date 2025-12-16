using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for rendering individual cards with their visual elements
    /// </summary>
    [DebugTab("Card Display")]
    public class CardDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ContentManager _content;
        private readonly Dictionary<string, Texture2D> _textureCache = new();
        private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        private SpriteFont _nameFont = FontSingleton.TitleFont;
        private SpriteFont _contentFont = FontSingleton.ContentFont;
        private Texture2D _pixelTexture; // Reuse texture for card backgrounds
        private CardVisualSettings _settings;

        // Adjustable overrides; when nonzero, override settings component
        [DebugEditable(DisplayName = "Card Corner Radius", Step = 1, Min = 0, Max = 64)]
        public int CornerRadiusOverride { get; set; } = 0;
        [DebugEditable(DisplayName = "Card Border Thickness", Step = 1, Min = 0, Max = 32)]
        public int BorderThicknessOverride { get; set; } = 1;

        // Debug-adjustable shield icon layout
        [DebugEditable(DisplayName = "Shield Icon Height", Step = 1, Min = 8, Max = 128)]
        public int ShieldIconHeight { get; set; } = 36;
        [DebugEditable(DisplayName = "Shield Icon Gap", Step = 1, Min = 0, Max = 64)]
        public int ShieldIconGap { get; set; } = 4;
        [DebugEditable(DisplayName = "Shield Icon Offset X", Step = 1, Min = -200, Max = 200)]
        public int ShieldIconOffsetX { get; set; } = 0;
        [DebugEditable(DisplayName = "Shield Icon Offset Y", Step = 1, Min = -200, Max = 200)]
        public int ShieldIconOffsetY { get; set; } = -6;

        // Debug-adjustable cost pip visuals
        [DebugEditable(DisplayName = "Cost Pip Diameter", Step = 1, Min = 6, Max = 128)]
        public int CostPipDiameter { get; set; } = 21;
        [DebugEditable(DisplayName = "Cost Pip Gap", Step = 1, Min = 0, Max = 64)]
        public int CostPipGap { get; set; } = 6;
        [DebugEditable(DisplayName = "Cost Pip Outline Fraction", Step = 0.01f, Min = 0f, Max = 0.5f)]
        public float CostPipOutlineFrac { get; set; } = 0.13f;

        // Debug-adjustable AP text
        [DebugEditable(DisplayName = "AP Text Scale", Step = 0.05f, Min = 0.3f, Max = 2.0f)]
        public float APTextScale { get; set; } = 0.125f;
        [DebugEditable(DisplayName = "AP Bottom Margin Y", Step = 1, Min = 0, Max = 200)]
        public int APBottomMarginY { get; set; } = 14;
        [DebugEditable(DisplayName = "AP Offset X", Step = 1, Min = -200, Max = 200)]
        public int APOffsetX { get; set; } = 0;

        // Debug-adjustable block delta text
        [DebugEditable(DisplayName = "Block Delta Scale", Step = 0.01f, Min = 0.01f, Max = 2.0f)]
        public float BlockDeltaScale { get; set; } = 0.09f;
        [DebugEditable(DisplayName = "Block Delta Gap X", Step = 1, Min = -100, Max = 200)]
        public int BlockDeltaGapX { get; set; } = 1;
        [DebugEditable(DisplayName = "Block Delta Offset X", Step = 1, Min = -200, Max = 200)]
        public int BlockDeltaOffsetX { get; set; } = 0;
        [DebugEditable(DisplayName = "Block Delta Offset Y", Step = 1, Min = -200, Max = 200)]
        public int BlockDeltaOffsetY { get; set; } = -4;

        // Debug-adjustable damage trapezoid and text
        [DebugEditable(DisplayName = "Damage Trap Width", Step = 1, Min = 10, Max = 400)]
        public int DamageTrapWidth { get; set; } = 113;
        [DebugEditable(DisplayName = "Damage Trap Height", Step = 1, Min = 8, Max = 200)]
        public int DamageTrapHeight { get; set; } = 41;
        [DebugEditable(DisplayName = "Damage Trap Left Margin X", Step = 1, Min = -200, Max = 200)]
        public int DamageTrapLeftMarginX { get; set; } = 12;
        [DebugEditable(DisplayName = "Damage Trap Bottom Margin Y", Step = 1, Min = 0, Max = 200)]
        public int DamageTrapBottomMarginY { get; set; } = 65;
        [DebugEditable(DisplayName = "Damage Trap Left Side Offset", Step = 1, Min = -200, Max = 200)]
        public int DamageTrapLeftSideOffset { get; set; } = 0;
        [DebugEditable(DisplayName = "Damage Trap Top Angle", Step = 1, Min = -89, Max = 89)]
        public float DamageTrapTopAngleDeg { get; set; } = 0f;
        [DebugEditable(DisplayName = "Damage Trap Right Angle", Step = 1, Min = -89, Max = 89)]
        public float DamageTrapRightAngleDeg { get; set; } = -20f;
        [DebugEditable(DisplayName = "Damage Trap Bottom Angle", Step = 1, Min = -89, Max = 89)]
        public float DamageTrapBottomAngleDeg { get; set; } = -5f;
        [DebugEditable(DisplayName = "Damage Trap Left Angle", Step = 1, Min = -89, Max = 89)]
        public float DamageTrapLeftAngleDeg { get; set; } = 10f;
        [DebugEditable(DisplayName = "Damage Text Scale", Step = 0.01f, Min = 0.01f, Max = 2.0f)]
        public float DamageTextScale { get; set; } = 0.18f;
        [DebugEditable(DisplayName = "Damage Text Offset X", Step = 1, Min = -200, Max = 200)]
        public int DamageTextOffsetX { get; set; } = 0;
        [DebugEditable(DisplayName = "Damage Text Offset Y", Step = 1, Min = -200, Max = 200)]
        public int DamageTextOffsetY { get; set; } = 0;
        [DebugEditable(DisplayName = "Damage Delta Scale", Step = 0.01f, Min = 0.01f, Max = 2.0f)]
        public float DamageDeltaScale { get; set; } = 0.1f;
        
        public CardDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _content = content;
            
            // Create a single pixel texture that we can reuse
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
            // Subscribe to card render events
            EventManager.Subscribe<CardRenderEvent>(OnCardRenderEvent);
            EventManager.Subscribe<CardRenderScaledEvent>(OnCardRenderScaledEvent);
            EventManager.Subscribe<CardRenderScaledRotatedEvent>(OnCardRenderScaledRotatedEvent);
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Card display system doesn't need update logic for individual entities
            // Rendering is handled by the DrawCard method
        }
        
        /// <summary>
        /// Event handler for card render events
        /// </summary>
        private void OnCardRenderEvent(CardRenderEvent evt)
        {
            // Allow highlight to draw beneath this specific card
            var t = evt.Card.GetComponent<Transform>();
            var ui = evt.Card.GetComponent<UIElement>();
            EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
            DrawCard(evt.Card, evt.Position);
        }

        private void OnCardRenderScaledEvent(CardRenderScaledEvent evt)
        {
            var transform = evt.Card.GetComponent<Transform>();
            Vector2 originalScale = transform?.Scale ?? Vector2.One;
            if (transform != null)
            {
                transform.Scale = new Vector2(evt.Scale, evt.Scale);
                float originalRotation = transform.Rotation;
                Vector2 originalPosition = transform.Position;
                // Ensure no rotation for grid preview and sync position to render position
                transform.Rotation = 0f;
                transform.Position = evt.Position;
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
                DrawCard(evt.Card, evt.Position);
                // Restore original transform after drawing
                transform.Scale = originalScale;
                transform.Rotation = originalRotation;
                transform.Position = originalPosition;
                if (ui != null) ui.Bounds = GetCardVisualRectScaled(evt.Position, evt.Scale);
            }
            else
            {
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
                DrawCard(evt.Card, evt.Position);
            }
        }

        private void OnCardRenderScaledRotatedEvent(CardRenderScaledRotatedEvent evt)
        {
            var transform = evt.Card.GetComponent<Transform>();
            Vector2 originalScale = transform?.Scale ?? Vector2.One;
            if (transform != null)
            {
                transform.Scale = new Vector2(evt.Scale, evt.Scale);
                Vector2 originalPosition = transform.Position;
                // Preserve rotation but sync position for accurate highlight/bounds
                transform.Position = evt.Position;
                var t = evt.Card.GetComponent<Transform>();
                var ui = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t, UI = ui });
                DrawCard(evt.Card, evt.Position);
                // Restore transform and update bounds
                transform.Scale = originalScale;
                transform.Position = originalPosition;
                if (ui != null) ui.Bounds = GetCardVisualRectScaled(evt.Position, evt.Scale);
            }
            else
            {
                var t2 = evt.Card.GetComponent<Transform>();
                var ui2 = evt.Card.GetComponent<UIElement>();
                EventManager.Publish(new HighlightRenderEvent { Entity = evt.Card, Transform = t2, UI = ui2 });
                DrawCard(evt.Card, evt.Position);
            }
        }
        
        /// <summary>
        /// Draws a single card with all its visual elements
        /// </summary>
        public void DrawCard(Entity entity, Vector2 position)
        {
            var cardData = entity.GetComponent<CardData>();
            var transform = entity.GetComponent<Transform>();
            
            if (cardData == null) return;
            float visualScale = transform?.Scale.X ?? 1f;
            
            var cardColor = GetCardColor(cardData.Color);
            // Resolve definition from CardId
            CardBase card = cardData.Card;
            bool hasDef = card != null;
            
            // Draw card background (rotated if transform has rotation)
            float rotation = transform?.Rotation ?? 0f;
            // If this is a weapon and we're not in Action phase, gray it out
			Color bgColor = cardColor;
			bool isWeaponDetected = false;
            try
            {
                if (hasDef)
                {
                    if (card.IsWeapon)
                    {
						isWeaponDetected = true;
                        var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
                        var ui = entity.GetComponent<UIElement>();
                        // Detect if pay-cost overlay is active; when active, do not override interactability
                        var payStateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                        var payState = payStateEntity?.GetComponent<PayCostOverlayState>();
                        bool overlayActive = payState != null && (payState.IsOpen || payState.IsReturning);
						if (phase != null && phase.Sub != SubPhase.Action)
                        {
                            bgColor = Color.DimGray;
                            if (ui != null) ui.IsInteractable = false;
                        }
						else if (!overlayActive)
                        {
                            // Ensure weapon becomes interactable during Action only when no overlay is active
							if (ui != null) ui.IsInteractable = true;
							// Use weapon-specific visuals: light yellow background
							bgColor = new Color(215, 186, 147);
                        }
                    }
                }
            }
            catch { }
            DrawCardBackgroundRotatedScaled(position, rotation, bgColor, visualScale);

            // Compute actual visual center from rect so text aligns exactly with background
            var cardRectForCenter = GetCardVisualRectScaled(position, visualScale);
            var cardCenter = new Vector2(cardRectForCenter.X + cardRectForCenter.Width / 2f, cardRectForCenter.Y + cardRectForCenter.Height / 2f);
            
            // Name text (wrapped within card width), rotated with card
            var textColor = isWeaponDetected ? Color.Black : GetCardTextColor(cardData.Color);
            string displayName = hasDef ? (card.Name ?? string.Empty) : string.Empty;
            DrawCardTextWrappedRotatedScaled(cardCenter, rotation, new Vector2(_settings.TextMarginX * visualScale, _settings.TextMarginY * visualScale), displayName, textColor, _settings.NameScale * visualScale, visualScale, _nameFont);
            
            // Draw cost pips (colored circles with yellow outline) under the name
            var defCosts = hasDef ? card.Cost.ToArray() : [];
            DrawCostPipsScaled(cardCenter, rotation, (int)(_settings.TextMarginX * visualScale), (int)Math.Round((_settings.TextMarginY + 34 * _settings.UIScale) * visualScale), cardData.Color, defCosts, visualScale);

            string displayText = hasDef ? (card.Text ?? string.Empty) : string.Empty;
            DrawCardTextWrappedRotatedScaled(cardCenter, rotation, new Vector2(_settings.TextMarginX * visualScale, (_settings.TextMarginY + (int)Math.Round(84 * _settings.UIScale)) * visualScale), displayText, textColor, _settings.DescriptionScale * visualScale, visualScale, _contentFont);
            
            // Draw damage value in trapezoid above block section for attack cards
            int effectiveDamage = 0;
            int damageDelta = 0;
            if (hasDef && card.Type == CardType.Attack)
            {
                try
                {
                    // Base damage includes printed damage plus any conditional damage from the card definition
                    int baseDamage = card.Damage;
                    try
                    {
                        baseDamage = Math.Max(0, baseDamage + card.GetConditionalDamage(EntityManager, entity) + AttackDamageValueService.GetTotalDelta(entity));
                    }
                    catch
                    {
                        // If conditional resolution fails for any reason, fall back to printed damage
                        baseDamage = Math.Max(0, card.Damage);
                    }

                    int finalDamage = baseDamage;

                    // Preview passive effects in a read-only way, mirroring HpManagementSystem semantics
                    try
                    {
                        var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                        var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();

                        if (player != null && enemy != null && baseDamage > 0)
                        {
                            var previewEvent = new ModifyHpRequestEvent
                            {
                                Source = player,
                                Target = enemy,
                                Delta = -baseDamage,
                                DamageType = ModifyTypeEnum.Attack
                            };

                            int passiveDelta = AppliedPassivesService.GetPassiveDelta(previewEvent, ReadOnly: true);
                            int newDelta = previewEvent.Delta + passiveDelta;
                            finalDamage = Math.Max(0, -newDelta);
                        }
                    }
                    catch
                    {
                        // If anything goes wrong during passive preview, just use base damage
                        finalDamage = baseDamage;
                    }

                    effectiveDamage = Math.Max(0, finalDamage);
                    damageDelta = effectiveDamage - card.Damage;
                }
                catch
                {
                    effectiveDamage = Math.Max(0, card.Damage);
                    damageDelta = 0;
                }

                if (effectiveDamage > 0)
                {
                    DrawDamageTrapezoidAndValue(cardCenter, rotation, visualScale, cardData.Color, effectiveDamage, damageDelta);
                }
            }

            // Draw block value and shield icon at bottom-left, but hide for weapons
            bool isWeapon = hasDef && card.IsWeapon;
            int blockValueToShow = 0;
            int printedBlockValue = 0;
            int blockDeltaValue = 0;
            bool hasBlockDefinition = hasDef && card != null;
            if (hasBlockDefinition)
            {
                printedBlockValue = card.Block;
                blockValueToShow = BlockValueService.GetTotalBlockValue(entity);
                blockDeltaValue = blockValueToShow - printedBlockValue;
            }
            if (!isWeapon && blockValueToShow > 0)
            {
                string blockText = blockValueToShow.ToString();
                var textSize = _nameFont.MeasureString(blockText) * (_settings.BlockNumberScale * visualScale);
                float marginX = _settings.BlockNumberMarginX * visualScale;
                float marginY = _settings.BlockNumberMarginY * visualScale;
                float baselineY = _settings.CardHeight * visualScale - marginY;

                // First draw the number
                float numberLocalX = marginX;
                float numberLocalY = baselineY - textSize.Y;
                DrawCardTextRotatedSingleScaled(cardCenter, rotation, new Vector2(numberLocalX, numberLocalY), blockText, textColor, _settings.BlockNumberScale * visualScale, visualScale);

                // Draw the shield icon to the right of the main block number
                float shieldRightX = numberLocalX + textSize.X;
                var shield = GetOrLoadTexture("shield");
                if (shield != null)
                {
                    float iconHeight = Math.Max(8f, ShieldIconHeight * _settings.UIScale * visualScale);
                    float iconWidth = shield.Height > 0 ? iconHeight * (shield.Width / (float)shield.Height) : iconHeight;
                    float gap = Math.Max(0f, ShieldIconGap * _settings.UIScale * visualScale);
                    float iconBaseX = numberLocalX + textSize.X;
                    float iconLocalX = iconBaseX + gap + ShieldIconOffsetX * visualScale;
                    float iconLocalY = baselineY - iconHeight + ShieldIconOffsetY * visualScale;
                    DrawTextureRotatedLocalScaled(cardCenter, rotation, new Vector2(iconLocalX, iconLocalY), shield, new Vector2(iconWidth, iconHeight), Color.White, visualScale);
                    shieldRightX = iconLocalX + iconWidth;
                }

                // Finally draw the delta to the right of the shield (or block number if no shield)
                if (hasBlockDefinition && blockDeltaValue != 0)
                {
                    string deltaText = blockDeltaValue > 0 ? $"+{blockDeltaValue}" : blockDeltaValue.ToString();
                    float deltaScale = BlockDeltaScale * visualScale;
                    var deltaTextSize = _nameFont.MeasureString(deltaText) * deltaScale;
                    float deltaLocalX = shieldRightX + (BlockDeltaGapX * visualScale) + (BlockDeltaOffsetX * visualScale);
                    float deltaLocalY = baselineY - deltaTextSize.Y + (BlockDeltaOffsetY * visualScale);
                    DrawCardTextRotatedSingleScaled(cardCenter, rotation, new Vector2(deltaLocalX, deltaLocalY), deltaText, textColor, BlockDeltaScale * visualScale, visualScale);
                }
            }
            else if (isWeapon)
            {
                // Draw a sword icon at bottom-left for weapon cards (same size as shield icon)
                var sword = GetOrLoadTexture("sword");
                if (sword != null)
                {
                    float marginX = _settings.BlockNumberMarginX * visualScale;
                    float marginY = _settings.BlockNumberMarginY * visualScale;
                    float baselineY = _settings.CardHeight * visualScale - marginY;
                    float iconHeight = Math.Max(8f, ShieldIconHeight * _settings.UIScale * visualScale);
                    float iconWidth = sword.Height > 0 ? iconHeight * (sword.Width / (float)sword.Height) : iconHeight;
                    float iconLocalX = marginX + ShieldIconOffsetX * visualScale;
                    float iconLocalY = baselineY - iconHeight + ShieldIconOffsetY * visualScale;
                    DrawTextureRotatedLocalScaled(cardCenter, rotation, new Vector2(iconLocalX, iconLocalY), sword, new Vector2(iconWidth, iconHeight), Color.White, visualScale);
                }
            }

            // Draw AP cost text at bottom-center: 0AP if free action else 1AP
            string bottomText;
            if (hasDef && card.Type == CardType.Block)
            {
                bottomText = "Block";
            }
            else if (card.Type == CardType.Relic)
            {
                bottomText = "Relic";
            }
            else
            {
                bool isFree = GetIsFreeAction(entity) || hasDef && card.Type == CardType.Block;
                bottomText = isFree ? "Free" : "1AP";
            }
            var apSize = _nameFont.MeasureString(bottomText) * (APTextScale * visualScale);
            float apLocalX = (_settings.CardWidth * visualScale - apSize.X) / 2f + APOffsetX * visualScale;
            float apLocalY = _settings.CardHeight * visualScale - (APBottomMarginY * visualScale) - apSize.Y;
            DrawCardTextRotatedSingleScaled(cardCenter, rotation, new Vector2(apLocalX, apLocalY), bottomText, textColor, APTextScale * visualScale, visualScale);
        }
        
        private Color GetCardColor(CardData.CardColor color)
        {
            return color switch
            {
                CardData.CardColor.Red => Color.DarkRed,
                CardData.CardColor.White => Color.White,
                CardData.CardColor.Black => Color.Black,
                CardData.CardColor.Yellow => Color.LightYellow,
                _ => Color.Gray
            };
        }
        
        private Color GetCostColor(string costType)
        {
            if (string.IsNullOrWhiteSpace(costType)) return Color.Gray;
            switch (costType.Trim().ToLowerInvariant())
            {
                case "red": return Color.DarkRed;
                case "white": return Color.White;
                case "black": return Color.Black;
                case "any": return Color.Gray;
                default: return Color.Gray;
            }
        }

        private Color GetCardTextColor(CardData.CardColor color)
        {
            switch (color)
            {
                case CardData.CardColor.White:
                    return Color.Black;
                default:
                    return Color.White;
            }
        }

        private Color GetDamageTrapezoidColor(CardData.CardColor color)
        {
            return Color.Red;
            // return color switch
            // {
            //     CardData.CardColor.Red => Color.Black,
            //     CardData.CardColor.White => Color.DarkRed,
            //     CardData.CardColor.Black => Color.DarkRed,
            //     _ => Color.Black
            // };
        }
        
        private void DrawCostPipsScaled(Vector2 cardCenter, float rotation, int localOffsetX, int localOffsetY, CardData.CardColor cardColor, string[] costs, float overallScale)
        {
            if (costs == null || costs.Length == 0) return;

            // Circle sizing and spacing based on UI scale
            float diameter = Math.Max(6f, CostPipDiameter * _settings.UIScale * overallScale);
            float radius = diameter / 2f;
            float gap = Math.Max(0f, CostPipGap * _settings.UIScale * overallScale);
            float totalWidth = costs.Length * diameter + (costs.Length - 1) * gap;

            // Start X so pips are left-aligned from localOffsetX
            float startLocalX = localOffsetX;
            float y = localOffsetY + radius; // center of circles on this Y line

            for (int i = 0; i < costs.Length; i++)
            {
                float x = startLocalX + i * (diameter + gap) + radius;
                var costType = costs[i];
                var fill = GetCostColor(costType);
                var outline = GetConditionalOutlineColor(cardColor, costType);
                DrawCirclePipRotatedScaled(cardCenter, rotation, new Vector2(x, y), radius, fill, outline, overallScale);
            }
        }

        private Color? GetConditionalOutlineColor(CardData.CardColor cardColor, string costType)
        {
            // Only outline when card color matches the cost color, with specific outline color rules
            if (cardColor == CardData.CardColor.Red && EqualsIgnoreCase(costType, "red"))
                return Color.Black;
            if (cardColor == CardData.CardColor.White && EqualsIgnoreCase(costType, "white"))
                return Color.Black;
            if (cardColor == CardData.CardColor.Black && EqualsIgnoreCase(costType, "black"))
                return Color.White;
            return null; // no outline
        }

        private void DrawCirclePipRotatedScaled(Vector2 cardCenter, float rotation, Vector2 localCenterFromTopLeft, float radius, Color fillColor, Color? outlineColor, float overallScale)
        {
            // Use cached anti-aliased circle texture from PrimitiveTextureFactory
            int radiusTex = Math.Max(1, (int)Math.Ceiling(radius));
            var circleTex = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radiusTex);
            int textureSize = circleTex.Width; // equals radiusTex * 2

            // Compute world position with rotation
            float localX = -_settings.CardWidth * overallScale / 2f + localCenterFromTopLeft.X;
            float localY = -_settings.CardHeight * overallScale / 2f + localCenterFromTopLeft.Y;
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
            var worldCenter = cardCenter + rotated;

            // Optional outline
            if (outlineColor.HasValue)
            {
                float outlineScale = 1f;
                _spriteBatch.Draw(circleTex, worldCenter, null, outlineColor.Value, rotation, new Vector2(textureSize / 2f, textureSize / 2f), outlineScale, SpriteEffects.None, 0f);
                // Fill inside outline slightly smaller (centered)
                float fillScale = Math.Max(0f, 1f - CostPipOutlineFrac * 2f);
                _spriteBatch.Draw(circleTex, worldCenter, null, fillColor, rotation, new Vector2(textureSize / 2f, textureSize / 2f), fillScale, SpriteEffects.None, 0f);
            }
            else
            {
                // No outline: draw just the filled circle centered
                _spriteBatch.Draw(circleTex, worldCenter, null, fillColor, rotation, new Vector2(textureSize / 2f, textureSize / 2f), 1f, SpriteEffects.None, 0f);
            }
        }

        private void DrawDamageTrapezoidAndValue(Vector2 cardCenter, float rotation, float overallScale, CardData.CardColor cardColor, int damageValue, int damageDelta)
        {
            if (damageValue <= 0) return;
            if (_settings == null) _settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();

            float uiScale = _settings.UIScale;

            float baseWidth = DamageTrapWidth * uiScale;
            float baseHeight = DamageTrapHeight * uiScale;
            float baseLeftMarginX = DamageTrapLeftMarginX * uiScale;
            float baseBottomMarginY = DamageTrapBottomMarginY * uiScale;

            float trapWidth = baseWidth * overallScale;
            float trapHeight = baseHeight * overallScale;

            float localX = baseLeftMarginX * overallScale;
            float baselineY = _settings.CardHeight * overallScale - (baseBottomMarginY * overallScale);
            float localY = baselineY - trapHeight;

            var trapezoidTexture = PrimitiveTextureFactory.GetAntialiasedTrapezoidMask(
                _graphicsDevice,
                baseWidth,
                baseHeight,
                DamageTrapLeftSideOffset * uiScale,
                DamageTrapTopAngleDeg,
                DamageTrapRightAngleDeg,
                DamageTrapBottomAngleDeg,
                DamageTrapLeftAngleDeg
            );

            var trapezoidColor = GetDamageTrapezoidColor(cardColor);

            if (trapezoidTexture != null)
            {
                DrawTextureRotatedLocalScaled(
                    cardCenter,
                    rotation,
                    new Vector2(localX, localY),
                    trapezoidTexture,
                    new Vector2(trapWidth, trapHeight),
                    trapezoidColor,
                    overallScale
                );
            }

            // If the trapezoid is red/dark red, force white text for contrast.
            var textColor = (trapezoidColor == Color.DarkRed || trapezoidColor == Color.Red)
                ? Color.White
                : GetCardTextColor(cardColor);

            // Main effective damage text
            string damageText = damageValue.ToString();
            float mainScale = DamageTextScale * overallScale;
            var mainSize = _nameFont.MeasureString(damageText) * mainScale;

            // Optional damage delta text inside the trapezoid (to the right of the main value)
            string deltaText = null;
            float deltaScale = DamageDeltaScale * overallScale;
            Vector2 deltaSize = Vector2.Zero;
            float gapX = 4f * overallScale;

            if (damageDelta != 0)
            {
                deltaText = damageDelta > 0 ? $"+{damageDelta}" : damageDelta.ToString();
                deltaSize = _nameFont.MeasureString(deltaText) * deltaScale;
            }

            float totalWidth = (damageDelta != 0)
                ? mainSize.X + gapX + deltaSize.X
                : mainSize.X;

            float centerY = localY + (trapHeight - mainSize.Y) / 2f + DamageTextOffsetY * overallScale;
            float startX = localX + (trapWidth - totalWidth) / 2f + DamageTextOffsetX * overallScale;

            float mainLocalX = startX;
            float mainLocalY = centerY;

            DrawCardTextRotatedSingleScaled(
                cardCenter,
                rotation,
                new Vector2(mainLocalX, mainLocalY),
                damageText,
                textColor,
                mainScale,
                overallScale
            );

            if (damageDelta != 0 && deltaText != null)
            {
                float deltaLocalX = mainLocalX + mainSize.X + gapX;
                float deltaLocalY = centerY + (mainSize.Y - deltaSize.Y) / 2f;

                DrawCardTextRotatedSingleScaled(
                    cardCenter,
                    rotation,
                    new Vector2(deltaLocalX, deltaLocalY),
                    deltaText,
                    textColor,
                    deltaScale,
                    overallScale
                );
            }
        }

        private bool GetIsFreeAction(Entity card)
        {
            try
            {
                var data = card.GetComponent<CardData>();
                string id = data?.Card.CardId ?? string.Empty;
                if (string.IsNullOrEmpty(id)) return false;
                return data.Card.IsFreeAction;
            }
            catch { return false; }
        }

        private static bool EqualsIgnoreCase(string a, string b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        
        private void DrawCardBackgroundRotatedScaled(Vector2 position, float rotation, Color color, float scale)
        {
            // Compute rect centered on position
            var rect = GetCardVisualRectScaled(position, scale);
            var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            int radius = (int)Math.Round((CornerRadiusOverride > 0 ? CornerRadiusOverride : _settings.CardCornerRadius) * scale);
            int bt = (int)Math.Round((BorderThicknessOverride > 0 ? BorderThicknessOverride : _settings.CardBorderThickness) * scale);

            // Draw rounded border as outer rounded rect in border color, then inset fill
            var outer = GetRoundedRectTexture(rect.Width, rect.Height, Math.Max(0, radius));
            var inner = GetRoundedRectTexture(
                Math.Max(1, rect.Width - bt * 2),
                Math.Max(1, rect.Height - bt * 2),
                Math.Max(0, radius - bt)
            );

            // Outer (border)
            _spriteBatch.Draw(
                outer,
                position: center,
                sourceRectangle: null,
                color: Color.Black,
                rotation: rotation,
                origin: new Vector2(outer.Width / 2f, outer.Height / 2f),
                scale: Vector2.One,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );

            // Inner (fill), inset by border thickness in local space
            // We achieve inset by using a smaller texture and drawing with same center
            _spriteBatch.Draw(
                inner,
                position: center,
                sourceRectangle: null,
                color: color,
                rotation: rotation,
                origin: new Vector2(inner.Width / 2f, inner.Height / 2f),
                scale: Vector2.One,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }

        private Texture2D GetRoundedRectTexture(int width, int height, int radius)
        {
            var key = (width, height, radius);
            if (_roundedRectCache.TryGetValue(key, out var tex)) return tex;
            var texture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
            _roundedRectCache[key] = texture;
            return texture;
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

        private Rectangle GetCardVisualRectScaled(Vector2 position, float scale)
        {
            if (_settings == null) _settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
            int w = (int)Math.Round(_settings.CardWidth * scale);
            int h = (int)Math.Round(_settings.CardHeight * scale);
            int offsetY = (int)Math.Round((_settings.CardOffsetYExtra) * scale);
            return new Rectangle(
                (int)position.X - w / 2,
                (int)position.Y - (h / 2 + offsetY),
                w,
                h
            );
        }

        private Texture2D GetOrLoadTexture(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;
            if (_textureCache.TryGetValue(assetName, out var tex) && tex != null) return tex;
            try
            {
                var loaded = _content.Load<Texture2D>(assetName);
                _textureCache[assetName] = loaded;
                return loaded;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Draws a texture at a card-local top-left offset with the same rotation and visual scale as the card
        private void DrawTextureRotatedLocalScaled(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, Texture2D texture, Vector2 targetSize, Color color, float overallScale)
        {
            if (texture == null) return;
            float localX = -_settings.CardWidth * overallScale / 2f + localOffsetFromTopLeft.X;
            float localY = -_settings.CardHeight * overallScale / 2f + localOffsetFromTopLeft.Y;
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
            var world = cardCenterPosition + rotated;
            var scale = new Vector2(targetSize.X / texture.Width, targetSize.Y / texture.Height);
            _spriteBatch.Draw(texture, world, sourceRectangle: null, color: color, rotation: rotation, origin: Vector2.Zero, scale: scale, effects: SpriteEffects.None, layerDepth: 0f);
        }
        
        // New: draw wrapped text in card-local space rotated with the card
        private void DrawCardTextWrappedRotatedScaled(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale, float overallScale, SpriteFont font)
        {
            try
            {
                float maxLineWidth = _settings.CardWidth * overallScale - (_settings.TextMarginX * overallScale * 2);
                float lineHeight = font.LineSpacing * scale;

                // Convert card-local from top-left to local centered coordinates
                float startLocalX = -_settings.CardWidth * overallScale / 2f + localOffsetFromTopLeft.X;
                float startLocalY = -_settings.CardHeight * overallScale / 2f + localOffsetFromTopLeft.Y;

                float currentY = startLocalY;
                foreach (var line in WrapText(text, maxLineWidth, scale))
                {
                    // Position of this line's top-left in card-local coords
                    var local = new Vector2(startLocalX, currentY);
                    // Rotate to world
                    float cos = (float)Math.Cos(rotation);
                    float sin = (float)Math.Sin(rotation);
                    var rotated = new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
                    var world = cardCenterPosition + rotated;

                    _spriteBatch.DrawString(font, line, world, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    currentY += lineHeight;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }

        // New: draw a single unwrapped line rotated; localOffsetFromTopLeft is in card-local (unrotated) pixels
        private void DrawCardTextRotatedSingleScaled(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale, float overallScale)
        {
            try
            {
                float localX = -_settings.CardWidth * overallScale / 2f + localOffsetFromTopLeft.X;
                float localY = -_settings.CardHeight * overallScale / 2f + localOffsetFromTopLeft.Y;
                float cos = (float)Math.Cos(rotation);
                float sin = (float)Math.Sin(rotation);
                var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
                var world = cardCenterPosition + rotated;
                _spriteBatch.DrawString(_nameFont, text, world, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }

        private IEnumerable<string> WrapText(string text, float maxLineWidth, float scale)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            string[] words = text.Split(' ');
            string currentLine = string.Empty;

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float lineWidth = _nameFont.MeasureString(testLine).X * scale;

                if (lineWidth <= maxLineWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        yield return currentLine;
                        currentLine = word; // start new line with the current word
                    }
                    else
                    {
                        // Single word longer than line: hard-break by characters
                        string longWord = word;
                        string partial = string.Empty;
                        foreach (char c in longWord)
                        {
                            string attempt = partial + c;
                            if (_nameFont.MeasureString(attempt).X * scale <= maxLineWidth)
                            {
                                partial = attempt;
                            }
                            else
                            {
                                if (partial.Length > 0)
                                {
                                    yield return partial;
                                }
                                partial = c.ToString();
                            }
                        }
                        currentLine = partial;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                yield return currentLine;
            }
        }
    }
}