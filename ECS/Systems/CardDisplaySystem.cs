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
            EventManager.Publish(new CardHighlightRenderEvent { Card = evt.Card });
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
                // Ensure no rotation for grid preview
                transform.Rotation = 0f;
                EventManager.Publish(new CardHighlightRenderEvent { Card = evt.Card });
                DrawCard(evt.Card, evt.Position);
                transform.Scale = originalScale;
                transform.Rotation = originalRotation;
            }
            else
            {
                EventManager.Publish(new CardHighlightRenderEvent { Card = evt.Card });
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
                EventManager.Publish(new CardHighlightRenderEvent { Card = evt.Card });
                DrawCard(evt.Card, evt.Position);
                transform.Scale = originalScale;
            }
            else
            {
                EventManager.Publish(new CardHighlightRenderEvent { Card = evt.Card });
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
            
            var cardColor = GetCardColor(cardData.Color);
            var costColor = GetCostColor(cardData.CardCostType);
            
            // Draw card background (rotated if transform has rotation)
            float rotation = transform?.Rotation ?? 0f;
            DrawCardBackgroundRotated(position, rotation, cardColor);

            // Compute actual visual center from rect so text aligns exactly with background
            var cardRectForCenter = GetCardVisualRect(position);
            var cardCenter = new Vector2(cardRectForCenter.X + cardRectForCenter.Width / 2f, cardRectForCenter.Y + cardRectForCenter.Height / 2f);
            
            // Name text (wrapped within card width), rotated with card
            var textColor = GetCardTextColor(cardData.Color);
            DrawCardTextWrappedRotated(cardCenter, rotation, new Vector2(_settings.TextMarginX, _settings.TextMarginY), cardData.Name, textColor, _settings.NameScale);
            
            // Draw cost pips (colored circles with yellow outline) under the name
            DrawCostPips(cardCenter, rotation, _settings.TextMarginX, _settings.TextMarginY + (int)Math.Round(34 * _settings.UIScale), cardData);
            
            DrawCardTextWrappedRotated(cardCenter, rotation, new Vector2(_settings.TextMarginX, _settings.TextMarginY + (int)Math.Round(84 * _settings.UIScale)), cardData.Description, textColor, _settings.DescriptionScale);
            
            // Draw block value and shield icon at bottom-left
            if (cardData.BlockValue > 0)
            {
                string blockText = cardData.BlockValue.ToString();
                var textSize = _font.MeasureString(blockText) * _settings.BlockNumberScale;
                float marginX = _settings.BlockNumberMarginX;
                float marginY = _settings.BlockNumberMarginY;
                float baselineY = _settings.CardHeight - marginY;

                // First draw the number
                float numberLocalX = marginX;
                float numberLocalY = baselineY - textSize.Y;
                DrawCardTextRotatedSingle(cardCenter, rotation, new Vector2(numberLocalX, numberLocalY), blockText, textColor, _settings.BlockNumberScale);

                // Then draw the shield icon to the right
                var shield = GetOrLoadTexture("shield");
                if (shield != null)
                {
                    float iconHeight = Math.Max(8f, ShieldIconHeight * _settings.UIScale);
                    float iconWidth = shield.Height > 0 ? iconHeight * (shield.Width / (float)shield.Height) : iconHeight;
                    float gap = Math.Max(0f, ShieldIconGap * _settings.UIScale);
                    float iconLocalX = numberLocalX + textSize.X + gap + ShieldIconOffsetX;
                    float iconLocalY = baselineY - iconHeight + ShieldIconOffsetY;
                    DrawTextureRotatedLocal(cardCenter, rotation, new Vector2(iconLocalX, iconLocalY), shield, new Vector2(iconWidth, iconHeight), Color.White);
                }
            }
        }
        
        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        public void DrawCard(Entity entity)
        {
            var transform = entity.GetComponent<Transform>();
            if (transform != null)
            {
                DrawCard(entity, transform.Position);
            }
        }
        
        private Color GetCardColor(CardData.CardColor color)
        {
            return color switch
            {
                CardData.CardColor.Red => Color.DarkRed,
                CardData.CardColor.White => Color.White,
                CardData.CardColor.Black => Color.DarkGray,
                _ => Color.Gray
            };
        }
        
        private Color GetCostColor(CardData.CostType costType)
        {
            return costType switch
            {
                CardData.CostType.Red => Color.DarkRed,
                CardData.CostType.White => Color.White,
                CardData.CostType.Black => Color.Black,
                CardData.CostType.Any => Color.Gray,
                _ => Color.Gray
            };
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
        
        private void DrawCostPips(Vector2 cardCenter, float rotation, int localOffsetX, int localOffsetY, CardData data)
        {
            var costs = (data.CostArray != null && data.CostArray.Count > 0)
                ? data.CostArray
                : (data.CardCostType != CardData.CostType.NoCost ? new List<CardData.CostType> { data.CardCostType } : new List<CardData.CostType>());
            if (costs.Count == 0) return;

            // Circle sizing and spacing based on UI scale
            float diameter = Math.Max(6f, CostPipDiameter * _settings.UIScale);
            float radius = diameter / 2f;
            float gap = Math.Max(0f, CostPipGap * _settings.UIScale);
            float totalWidth = costs.Count * diameter + (costs.Count - 1) * gap;

            // Start X so pips are left-aligned from localOffsetX
            float startLocalX = localOffsetX;
            float y = localOffsetY + radius; // center of circles on this Y line

            for (int i = 0; i < costs.Count; i++)
            {
                float x = startLocalX + i * (diameter + gap) + radius;
                var costType = costs[i];
                var fill = GetCostColor(costType);
                var outline = GetConditionalOutlineColor(data.Color, costType);
                DrawCirclePipRotated(cardCenter, rotation, new Vector2(x, y), radius, fill, outline);
            }
        }

        private Color? GetConditionalOutlineColor(CardData.CardColor cardColor, CardData.CostType costType)
        {
            // Only outline when card color matches the cost color, with specific outline color rules
            if (cardColor == CardData.CardColor.Red && costType == CardData.CostType.Red)
                return Color.Black;
            if (cardColor == CardData.CardColor.White && costType == CardData.CostType.White)
                return Color.Black;
            if (cardColor == CardData.CardColor.Black && costType == CardData.CostType.Black)
                return Color.White;
            return null; // no outline
        }

        private void DrawCirclePipRotated(Vector2 cardCenter, float rotation, Vector2 localCenterFromTopLeft, float radius, Color fillColor, Color? outlineColor)
        {
            // Create/reuse a circle texture for fill and outline
            int textureSize = (int)Math.Ceiling(radius * 2);
            if (textureSize < 2) textureSize = 2;
            var key = ($"circle_{textureSize}");
            if (!_textureCache.TryGetValue(key, out var circleTex) || circleTex == null)
            {
                circleTex = CreateCircleTexture(textureSize);
                _textureCache[key] = circleTex;
            }

            // Compute world position with rotation
            float localX = -_settings.CardWidth / 2f + localCenterFromTopLeft.X;
            float localY = -_settings.CardHeight / 2f + localCenterFromTopLeft.Y;
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

        private Texture2D CreateCircleTexture(int diameter)
        {
            var tex = new Texture2D(_graphicsDevice, diameter, diameter);
            var data = new Color[diameter * diameter];
            float r = diameter / 2f;
            float r2 = r * r;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist2 = dx * dx + dy * dy;
                    data[y * diameter + x] = dist2 <= r2 ? Color.White : Color.Transparent;
                }
            }
            tex.SetData(data);
            return tex;
        }
        
        private void DrawCardBackgroundRotated(Vector2 position, float rotation, Color color)
        {
            // Compute rect centered on position
            var rect = GetCardVisualRect(position);
            var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            int radius = CornerRadiusOverride > 0 ? CornerRadiusOverride : _settings.CardCornerRadius;
            int bt = BorderThicknessOverride > 0 ? BorderThicknessOverride : _settings.CardBorderThickness;

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

        private Rectangle GetCardVisualRect(Vector2 position)
        {
            if (_settings == null) _settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
            return new Rectangle(
                (int)position.X - _settings.CardWidth / 2,
                (int)position.Y - (_settings.CardHeight / 2 + _settings.CardOffsetYExtra),
                _settings.CardWidth,
                _settings.CardHeight
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

        // Draws a texture at a card-local top-left offset with the same rotation as the card
        private void DrawTextureRotatedLocal(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, Texture2D texture, Vector2 targetSize, Color color)
        {
            if (texture == null) return;
            float localX = -_settings.CardWidth / 2f + localOffsetFromTopLeft.X;
            float localY = -_settings.CardHeight / 2f + localOffsetFromTopLeft.Y;
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
            var world = cardCenterPosition + rotated;
            var scale = new Vector2(targetSize.X / texture.Width, targetSize.Y / texture.Height);
            _spriteBatch.Draw(texture, world, sourceRectangle: null, color: color, rotation: rotation, origin: Vector2.Zero, scale: scale, effects: SpriteEffects.None, layerDepth: 0f);
        }
        
        // New: draw wrapped text in card-local space rotated with the card
        private void DrawCardTextWrappedRotated(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale)
        {
            try
            {
                float maxLineWidth = _settings.CardWidth - (_settings.TextMarginX * 2);
                float lineHeight = _font.LineSpacing * scale;

                // Convert card-local from top-left to local centered coordinates
                float startLocalX = -_settings.CardWidth / 2f + localOffsetFromTopLeft.X;
                float startLocalY = -_settings.CardHeight / 2f + localOffsetFromTopLeft.Y;

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
        private void DrawCardTextRotatedSingle(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale)
        {
            try
            {
                float localX = -_settings.CardWidth / 2f + localOffsetFromTopLeft.X;
                float localY = -_settings.CardHeight / 2f + localOffsetFromTopLeft.Y;
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