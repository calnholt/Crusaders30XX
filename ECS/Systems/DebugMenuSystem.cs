using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System to render a simple toggleable debug menu and its buttons
    /// </summary>
    public class DebugMenuSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private Texture2D _pixel;

        public DebugMenuSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<DebugMenu>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No periodic updates needed
        }

        public void Draw()
        {
            var menuEntity = GetRelevantEntities().FirstOrDefault();
            if (menuEntity == null) return;

            var menu = menuEntity.GetComponent<DebugMenu>();
            var transform = menuEntity.GetComponent<Transform>();
            var ui = menuEntity.GetComponent<UIElement>();

            if (menu == null || transform == null || ui == null) return;
            if (!menu.IsOpen) return;

            // Panel
            var panelRect = ui.Bounds;
            DrawFilledRect(panelRect, Color.Black * 0.6f);
            DrawRect(panelRect, Color.White, 2);

            // Title
            if (_font != null)
            {
                _spriteBatch.DrawString(_font, "Debug Menu", new Vector2(panelRect.X + 10, panelRect.Y + 10), Color.White);
                _spriteBatch.DrawString(_font, "Hand", new Vector2(panelRect.X + 10, panelRect.Y + 50), Color.LightGreen);
            }

            // Buttons under Hand section
            var buttons = EntityManager.GetEntitiesWithComponent<UIButton>().ToList();
            foreach (var btnEntity in buttons)
            {
                var btn = btnEntity.GetComponent<UIButton>();
                var btnUI = btnEntity.GetComponent<UIElement>();
                if (btn == null || btnUI == null) continue;

                var rect = btnUI.Bounds;
                var bgColor = btnUI.IsHovered ? Color.DimGray : Color.Gray;
                DrawFilledRect(rect, bgColor * 0.9f);
                DrawRect(rect, Color.White, 1);
                if (_font != null && !string.IsNullOrEmpty(btn.Label))
                {
                    _spriteBatch.DrawString(_font, btn.Label, new Vector2(rect.X + 10, rect.Y + 10), Color.White);
                }
            }
        }

        private void DrawFilledRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixel, rect, color);
        }

        private void DrawRect(Rectangle rect, Color color, int thickness)
        {
            // top
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // bottom
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // left
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // right
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}

