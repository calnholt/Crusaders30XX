using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
    public abstract class CardDisplayBase : Core.System
    {
        protected readonly GraphicsDevice _graphicsDevice;
        protected readonly SpriteBatch _spriteBatch;
        protected readonly ContentManager _content;
        protected readonly Dictionary<string, Texture2D> _textureCache = new();
        protected readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        protected readonly Dictionary<(int w, int h, int rTL, int rTR, int rBR, int rBL), Texture2D> _perCornerRoundedRectCache = new();
        protected SpriteFont _nameFont = FontSingleton.TitleFont;
        protected SpriteFont _contentFont = FontSingleton.ContentFont;
        protected Texture2D _pixelTexture;
        protected CardVisualSettings _settings;

        protected CardDisplayBase(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _content = content;

            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardData>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        protected CardVisualSettings GetSettings()
        {
            if (_settings == null)
                _settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
            return _settings;
        }

        protected void DrawRectangleRotatedLocalScaled(Vector2 cardCenter, float rotation, Vector2 localOffsetFromTopLeft, float width, float height, Color color, float visualScale, float cardW, float cardH)
        {
            float localX = -cardW * visualScale / 2f + localOffsetFromTopLeft.X;
            float localY = -cardH * visualScale / 2f + localOffsetFromTopLeft.Y;
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
            var world = cardCenter + rotated;
            _spriteBatch.Draw(_pixelTexture, world, null, color, rotation, Vector2.Zero, new Vector2(width, height), SpriteEffects.None, 0f);
        }

        protected void DrawTextureRotatedLocalScaled(Vector2 cardCenter, float rotation, Vector2 localOffsetFromTopLeft, Texture2D texture, Vector2 targetSize, Color color, float visualScale, float cardW, float cardH)
        {
            if (texture == null) return;
            float localX = -cardW * visualScale / 2f + localOffsetFromTopLeft.X;
            float localY = -cardH * visualScale / 2f + localOffsetFromTopLeft.Y;
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
            var world = cardCenter + rotated;
            var scale = new Vector2(targetSize.X / texture.Width, targetSize.Y / texture.Height);
            _spriteBatch.Draw(texture, world, sourceRectangle: null, color: color, rotation: rotation, origin: Vector2.Zero, scale: scale, effects: SpriteEffects.None, layerDepth: 0f);
        }

        protected void DrawCardTextRotatedSingleScaled(Vector2 cardCenter, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale, float overallScale, float cardW, float cardH, SpriteFont font = null)
        {
            try
            {
                font ??= _nameFont;
                float localX = -cardW * overallScale / 2f + localOffsetFromTopLeft.X;
                float localY = -cardH * overallScale / 2f + localOffsetFromTopLeft.Y;
                float cos = (float)Math.Cos(rotation);
                float sin = (float)Math.Sin(rotation);
                var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
                var world = cardCenter + rotated;
                _spriteBatch.DrawString(font, text, world, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }

        protected void DrawCardTextWrappedRotatedScaled(Vector2 cardCenter, float rotation, Vector2 localOffsetFromTopLeft, string text, Color color, float scale, float overallScale, SpriteFont font, float maxWidth, float cardW, float cardH)
        {
            try
            {
                float lineHeight = font.LineSpacing * scale;
                float startLocalX = -cardW * overallScale / 2f + localOffsetFromTopLeft.X;
                float startLocalY = -cardH * overallScale / 2f + localOffsetFromTopLeft.Y;

                float currentY = startLocalY;
                foreach (var line in TextUtils.WrapText(font, text, scale, (int)maxWidth))
                {
                    var local = new Vector2(startLocalX, currentY);
                    float cos = (float)Math.Cos(rotation);
                    float sin = (float)Math.Sin(rotation);
                    var rotated = new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
                    var world = cardCenter + rotated;
                    _spriteBatch.DrawString(font, line, world, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    currentY += lineHeight;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font rendering error: {ex.Message}");
            }
        }

        protected void DrawCirclePipRotatedScaled(Vector2 cardCenter, float rotation, Vector2 localCenterFromTopLeft, float radius, Color fillColor, Color? outlineColor, float overallScale, float cardW, float cardH, float outlineFrac = 0.13f)
        {
            int radiusTex = Math.Max(1, (int)Math.Ceiling(radius));
            var circleTex = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radiusTex);
            int textureSize = circleTex.Width;

            float localX = -cardW * overallScale / 2f + localCenterFromTopLeft.X;
            float localY = -cardH * overallScale / 2f + localCenterFromTopLeft.Y;
            float cos = (float)Math.Cos(rotation);
            float sin = (float)Math.Sin(rotation);
            var rotated = new Vector2(localX * cos - localY * sin, localX * sin + localY * cos);
            var worldCenter = cardCenter + rotated;

            if (outlineColor.HasValue)
            {
                _spriteBatch.Draw(circleTex, worldCenter, null, outlineColor.Value, rotation, new Vector2(textureSize / 2f, textureSize / 2f), 1f, SpriteEffects.None, 0f);
                float fillScale = Math.Max(0f, 1f - outlineFrac * 2f);
                _spriteBatch.Draw(circleTex, worldCenter, null, fillColor, rotation, new Vector2(textureSize / 2f, textureSize / 2f), fillScale, SpriteEffects.None, 0f);
            }
            else
            {
                _spriteBatch.Draw(circleTex, worldCenter, null, fillColor, rotation, new Vector2(textureSize / 2f, textureSize / 2f), 1f, SpriteEffects.None, 0f);
            }
        }

        protected Texture2D GetRoundedRectTexture(int width, int height, int radius)
        {
            var key = (width, height, radius);
            if (_roundedRectCache.TryGetValue(key, out var tex)) return tex;
            var texture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
            _roundedRectCache[key] = texture;
            return texture;
        }

        protected Texture2D GetPerCornerRoundedRectTexture(int width, int height, int rTL, int rTR, int rBR, int rBL)
        {
            var key = (width, height, rTL, rTR, rBR, rBL);
            if (_perCornerRoundedRectCache.TryGetValue(key, out var tex)) return tex;
            var texture = RoundedRectTextureFactory.CreateRoundedRectPerCorner(_graphicsDevice, width, height, rTL, rTR, rBR, rBL);
            _perCornerRoundedRectCache[key] = texture;
            return texture;
        }

        protected Texture2D GetOrLoadTexture(string assetName)
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

        protected Rectangle GetCardVisualRect(Vector2 position, float scale, float cardW, float cardH, float offsetYExtra = 0f)
        {
            int w = (int)Math.Round(cardW * scale);
            int h = (int)Math.Round(cardH * scale);
            int offsetY = (int)Math.Round(offsetYExtra * scale);
            return new Rectangle(
                (int)position.X - w / 2,
                (int)position.Y - (h / 2 + offsetY),
                w,
                h
            );
        }

        public void DisposeBase()
        {
            _pixelTexture?.Dispose();
            foreach (var tex in _roundedRectCache.Values) tex?.Dispose();
            _roundedRectCache.Clear();
            foreach (var tex in _perCornerRoundedRectCache.Values) tex?.Dispose();
            _perCornerRoundedRectCache.Clear();
        }
    }
}
