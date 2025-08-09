using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Renders a draw pile rectangle at bottom-right with the current draw pile count
    /// </summary>
    public class DrawPileDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        public DrawPileDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No-op: drawing only
        }

        public void Draw()
        {
            var deckEntity = GetRelevantEntities().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;

            int rectW = CardConfig.DRAW_PILE_WIDTH;
            int rectH = CardConfig.DRAW_PILE_HEIGHT;
            int m = CardConfig.DRAW_PILE_MARGIN;
            var rect = new Rectangle(w - rectW - m, h - rectH - m, rectW, rectH);

            // Panel
            _spriteBatch.Draw(_pixel, rect, new Color(20, 20, 20) * 0.75f);
            // Border
            DrawBorder(rect, Color.White, 2);

            // Count text centered
            if (_font != null)
            {
                string text = deck.DrawPile.Count.ToString();
                var size = _font.MeasureString(text) * CardConfig.DRAW_PILE_TEXT_SCALE;
                var pos = new Vector2(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f);
                _spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, CardConfig.DRAW_PILE_TEXT_SCALE, SpriteEffects.None, 0f);
            }
        }

        private void DrawBorder(Rectangle r, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
        }
    }
}

