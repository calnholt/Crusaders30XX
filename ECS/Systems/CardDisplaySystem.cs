using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        
        public CardDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font) 
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            
            // Create a single pixel texture that we can reuse
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            
            // Subscribe to card render events
            EventManager.Subscribe<CardRenderEvent>(OnCardRenderEvent);
            EventManager.Subscribe<CardRenderScaledEvent>(OnCardRenderScaledEvent);
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
            EnsureSettings();
            var cardRectForCenter = GetCardVisualRect(position);
            var cardCenter = new Vector2(cardRectForCenter.X + cardRectForCenter.Width / 2f, cardRectForCenter.Y + cardRectForCenter.Height / 2f);
            
            // Name text (wrapped within card width), rotated with card
            DrawCardTextWrappedRotated(cardCenter, rotation, new Vector2(_settings.TextMarginX, _settings.TextMarginY), cardData.Name, Color.Black, _settings.NameScale);
            
            // Draw cost
            string costText = GetCostText(cardData.CardCostType);
            DrawCardTextWrappedRotated(cardCenter, rotation, new Vector2(_settings.TextMarginX, _settings.TextMarginY + (int)Math.Round(34 * CardConfig.UIScale)), costText, costColor, _settings.CostScale);
            
            DrawCardTextWrappedRotated(cardCenter, rotation, new Vector2(_settings.TextMarginX, _settings.TextMarginY + (int)Math.Round(84 * CardConfig.UIScale)), cardData.Description, Color.Black, _settings.DescriptionScale);
            
            // Draw block value as blue number bottom-left
            if (cardData.BlockValue > 0)
            {
                string blockText = cardData.BlockValue.ToString();
                var measured = _font.MeasureString(blockText) * _settings.BlockNumberScale;
                float localX = _settings.BlockNumberMarginX;
                float localY = _settings.CardHeight - _settings.BlockNumberMarginY - measured.Y;
                DrawCardTextRotatedSingle(cardCenter, rotation, new Vector2(localX, localY), blockText, Color.CornflowerBlue, _settings.BlockNumberScale);
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
                CardData.CardColor.Red => Color.Red,
                CardData.CardColor.White => Color.White,
                CardData.CardColor.Black => Color.DarkGray,
                _ => Color.Gray
            };
        }
        
        private Color GetCostColor(CardData.CostType costType)
        {
            return costType switch
            {
                CardData.CostType.Red => Color.Red,
                CardData.CostType.White => Color.White,
                CardData.CostType.Black => Color.DarkGray,
                _ => Color.Gray
            };
        }
        
        private string GetCostText(CardData.CostType costType)
        {
            return costType switch
            {
                CardData.CostType.Red => "Red",
                CardData.CostType.White => "White",
                CardData.CostType.Black => "Black",
                _ => "Free"
            };
        }
        
        private void DrawCardBackgroundRotated(Vector2 position, float rotation, Color color)
        {
            // Compute rect centered on position
            EnsureSettings();
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

        private void EnsureSettings()
        {
            if (_settings != null) return;
            // Find or create settings singleton on a shared entity
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            if (settingsEntity == null)
            {
                settingsEntity = EntityManager.CreateEntity("CardVisualSettings");
                var settings = new CardVisualSettings
                {
                    CardWidth = CardConfig.CARD_WIDTH,
                    CardHeight = CardConfig.CARD_HEIGHT,
                    CardGap = CardConfig.CARD_GAP,
                    CardBorderThickness = CardConfig.CARD_BORDER_THICKNESS,
                    CardCornerRadius = CardConfig.CARD_CORNER_RADIUS,
                    HighlightBorderThickness = CardConfig.HIGHLIGHT_BORDER_THICKNESS,
                    TextMarginX = CardConfig.TEXT_MARGIN_X,
                    TextMarginY = CardConfig.TEXT_MARGIN_Y,
                    NameScale = CardConfig.NAME_SCALE,
                    CostScale = CardConfig.COST_SCALE,
                    DescriptionScale = CardConfig.DESCRIPTION_SCALE,
                    BlockScale = CardConfig.BLOCK_SCALE,
                    BlockNumberScale = CardConfig.BLOCK_NUMBER_SCALE,
                    BlockNumberMarginX = CardConfig.BLOCK_NUMBER_MARGIN_X,
                    BlockNumberMarginY = CardConfig.BLOCK_NUMBER_MARGIN_Y
                };
                EntityManager.AddComponent(settingsEntity, settings);
                _settings = settings;
            }
            else
            {
                _settings = settingsEntity.GetComponent<CardVisualSettings>();
            }
        }

        private Rectangle GetCardVisualRect(Vector2 position)
        {
            EnsureSettings();
            return new Rectangle(
                (int)position.X - _settings.CardWidth / 2,
                (int)position.Y - (_settings.CardHeight / 2 + (int)Math.Round(25 * CardConfig.UIScale)),
                _settings.CardWidth,
                _settings.CardHeight
            );
        }
        
        // New: draw wrapped text in card-local space rotated with the card
        private void DrawCardTextWrappedRotated(Vector2 cardCenterPosition, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale)
        {
            try
            {
                EnsureSettings();
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
                EnsureSettings();
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