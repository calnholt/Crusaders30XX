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
using Crusaders30XX.ECS.Data.Cards;

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
        private SpriteFont _font;
        private Texture2D _pixelTexture; // Reuse texture for card backgrounds
        private CardVisualSettings _settings;

        // Adjustable overrides; when nonzero, override settings component
        [DebugEditable(DisplayName = "Card Corner Radius", Step = 1, Min = 0, Max = 64)]
        public int CornerRadiusOverride { get; set; } = 0;
        [DebugEditable(DisplayName = "Card Border Thickness", Step = 1, Min = 0, Max = 32)]
        public int BorderThicknessOverride { get; set; } = 0;

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
        
        public CardDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font, ContentManager content) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
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
            CardDefinition def = null;
            bool hasDef = !string.IsNullOrWhiteSpace(cardData.CardId) && CardDefinitionCache.TryGet(cardData.CardId, out def) && def != null;
            
            // Draw card background (rotated if transform has rotation)
            float rotation = transform?.Rotation ?? 0f;
            // If this is a weapon and we're not in Action phase, gray it out
			Color bgColor = cardColor;
			bool isWeaponDetected = false;
            try
            {
                if (hasDef)
                {
                    if (def.isWeapon)
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
            string displayName = hasDef ? (def.name ?? def.id ?? cardData.CardId) : (cardData.CardId ?? string.Empty);
            DrawCardTextWrappedRotatedScaled(cardCenter, rotation, new Vector2(_settings.TextMarginX * visualScale, _settings.TextMarginY * visualScale), displayName, textColor, _settings.NameScale * visualScale, visualScale);
            
            // Draw cost pips (colored circles with yellow outline) under the name
            var defCosts = hasDef ? (def.cost ?? Array.Empty<string>()) : Array.Empty<string>();
            DrawCostPipsScaled(cardCenter, rotation, (int)(_settings.TextMarginX * visualScale), (int)Math.Round((_settings.TextMarginY + 34 * _settings.UIScale) * visualScale), cardData.Color, defCosts, visualScale);

            string displayText = hasDef ? (def.text ?? string.Empty) : string.Empty;
            DrawCardTextWrappedRotatedScaled(cardCenter, rotation, new Vector2(_settings.TextMarginX * visualScale, (_settings.TextMarginY + (int)Math.Round(84 * _settings.UIScale)) * visualScale), displayText, textColor, _settings.DescriptionScale * visualScale, visualScale);
            
            // Draw block value and shield icon at bottom-left, but hide for weapons
            bool isWeapon = hasDef && def.isWeapon;
            int blockValueToShow = 0;
            if (hasDef) { blockValueToShow = BlockValueService.GetBlockValue(entity); }
            if (!isWeapon && blockValueToShow > 0)
            {
                string blockText = blockValueToShow.ToString();
                var textSize = _font.MeasureString(blockText) * (_settings.BlockNumberScale * visualScale);
                float marginX = _settings.BlockNumberMarginX * visualScale;
                float marginY = _settings.BlockNumberMarginY * visualScale;
                float baselineY = _settings.CardHeight * visualScale - marginY;

                // First draw the number
                float numberLocalX = marginX;
                float numberLocalY = baselineY - textSize.Y;
                DrawCardTextRotatedSingleScaled(cardCenter, rotation, new Vector2(numberLocalX, numberLocalY), blockText, textColor, _settings.BlockNumberScale * visualScale, visualScale);

                // Then draw the shield icon to the right
                var shield = GetOrLoadTexture("shield");
                if (shield != null)
                {
                    float iconHeight = Math.Max(8f, ShieldIconHeight * _settings.UIScale * visualScale);
                    float iconWidth = shield.Height > 0 ? iconHeight * (shield.Width / (float)shield.Height) : iconHeight;
                    float gap = Math.Max(0f, ShieldIconGap * _settings.UIScale * visualScale);
                    float iconLocalX = numberLocalX + textSize.X + gap + ShieldIconOffsetX * visualScale;
                    float iconLocalY = baselineY - iconHeight + ShieldIconOffsetY * visualScale;
                    DrawTextureRotatedLocalScaled(cardCenter, rotation, new Vector2(iconLocalX, iconLocalY), shield, new Vector2(iconWidth, iconHeight), Color.White, visualScale);
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
            bool isFree = GetIsFreeAction(entity);
            string apText = isFree ? "Free" : "1AP";
            var apSize = _font.MeasureString(apText) * (APTextScale * visualScale);
            float apLocalX = (_settings.CardWidth * visualScale - apSize.X) / 2f + APOffsetX * visualScale;
            float apLocalY = _settings.CardHeight * visualScale - (APBottomMarginY * visualScale) - apSize.Y;
            DrawCardTextRotatedSingleScaled(cardCenter, rotation, new Vector2(apLocalX, apLocalY), apText, textColor, APTextScale * visualScale, visualScale);
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

        private bool GetIsFreeAction(Entity card)
        {
            try
            {
                var data = card.GetComponent<CardData>();
                string id = data?.CardId ?? string.Empty;
                if (string.IsNullOrEmpty(id)) return false;
                if (!Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.TryGet(id, out var def) || def == null) return false;
                return def.isFreeAction;
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
        private void DrawCardTextWrappedRotatedScaled(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale, float overallScale)
        {
            try
            {
                float maxLineWidth = _settings.CardWidth * overallScale - (_settings.TextMarginX * overallScale * 2);
                float lineHeight = _font.LineSpacing * scale;

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

                    _spriteBatch.DrawString(_font, line, world, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
                _spriteBatch.DrawString(_font, text, world, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
                float lineWidth = _font.MeasureString(testLine).X * scale;

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
                            if (_font.MeasureString(attempt).X * scale <= maxLineWidth)
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