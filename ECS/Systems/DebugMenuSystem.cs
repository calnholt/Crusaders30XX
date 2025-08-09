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

            // Layout constants
            int viewportW = _graphicsDevice.Viewport.Width;
            int viewportH = _graphicsDevice.Viewport.Height;
            int margin = 20;
            int panelWidth = 280;
            int padding = 12;
            int spacing = 10;
            int buttonHeight = 34;
            float titleScale = 0.6f;
            float sectionScale = 0.55f;
            float buttonTextScale = 0.55f;

            // Compute dynamic panel placement (top-right)
            int panelX = viewportW - panelWidth - margin;
            int panelY = margin + 60; // a bit lower from top edge

            int cursorY = panelY + padding;

            // First pass: measure and record button rects without drawing
            int measureCursorY = cursorY;
            var buttons = EntityManager.GetEntitiesWithComponent<UIButton>().ToList();
            var plannedButtons = new List<(Entity entity, Rectangle rect, string label)>();

            if (_font != null)
            {
                measureCursorY += (int)(_font.LineSpacing * titleScale) + spacing; // Debug Menu title
                measureCursorY += (int)(_font.LineSpacing * sectionScale) + spacing; // Hand section header
            }
            foreach (var btnEntity in buttons)
            {
                var btn = btnEntity.GetComponent<UIButton>();
                var btnUI = btnEntity.GetComponent<UIElement>();
                if (btn == null || btnUI == null) continue;
                var rect = new Rectangle(panelX + padding, measureCursorY, panelWidth - padding * 2, buttonHeight);
                plannedButtons.Add((btnEntity, rect, btn.Label));
                measureCursorY += buttonHeight + spacing;
            }

            // Compute panel rect from measured content
            int panelHeight = (measureCursorY - spacing) - panelY + padding;
            var panelRect = new Rectangle(panelX, panelY, panelWidth, Math.Max(panelHeight, padding * 2 + 40));
            ui.Bounds = panelRect; // keep UI bounds updated

            // Draw panel first (so content appears on top)
            DrawFilledRect(panelRect, new Color(15, 30, 55) * 0.95f);
            DrawRect(panelRect, Color.White, 2);

            // Second pass: draw headers and buttons, updating bounds for hit-test
            int drawCursorY = cursorY;
            if (_font != null)
            {
                DrawStringScaled("Debug Menu", new Vector2(panelX + padding, drawCursorY), Color.White, titleScale);
                drawCursorY += (int)(_font.LineSpacing * titleScale) + spacing;
                DrawStringScaled("Hand", new Vector2(panelX + padding, drawCursorY), Color.LightGreen, sectionScale);
                drawCursorY += (int)(_font.LineSpacing * sectionScale) + spacing;
            }
            foreach (var planned in plannedButtons)
            {
                var btn = planned.entity.GetComponent<UIButton>();
                var btnUI = planned.entity.GetComponent<UIElement>();
                var rect = new Rectangle(planned.rect.X, drawCursorY, planned.rect.Width, planned.rect.Height);
                btnUI.Bounds = rect; // sync bounds now

                var bgColor = btnUI.IsHovered ? new Color(120, 120, 120) : new Color(70, 70, 70);
                DrawFilledRect(rect, bgColor);
                DrawRect(rect, Color.White, 1);

                if (_font != null && !string.IsNullOrEmpty(planned.label))
                {
                    var size = _font.MeasureString(planned.label) * buttonTextScale;
                    int textX = rect.X + (int)((rect.Width - size.X) / 2f);
                    int textY = rect.Y + (int)((rect.Height - size.Y) / 2f);
                    DrawStringScaled(planned.label, new Vector2(textX, textY), Color.White, buttonTextScale);
                }
                drawCursorY += buttonHeight + spacing;
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

        private void DrawStringScaled(string text, Vector2 position, Color color, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}

