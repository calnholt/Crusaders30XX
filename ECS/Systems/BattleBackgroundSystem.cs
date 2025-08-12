using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
        /// Renders a battle background with cover scaling (no stretch). Anchors to bottom; centers horizontally.
    /// </summary>
    public class BattleBackgroundSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Microsoft.Xna.Framework.Content.ContentManager _content;
        private Texture2D _background;

        public BattleBackgroundSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Microsoft.Xna.Framework.Content.ContentManager content)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _content = content;

            EventManager.Subscribe<ChangeBattleLocationEvent>(OnChangeLocation);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // No entities needed; system-level rendering
            yield break;
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeLocation(ChangeBattleLocationEvent evt)
        {
            try
            {
                string path = null;
                if (evt.Location.HasValue)
                {
                    path = evt.Location.Value switch
                    {
                        BattleLocation.Desert => "desert-background",
                        BattleLocation.Forest => "forest-background",
                        BattleLocation.Cathedral => "cathedral-background",
                        _ => null
                    };
                }
                if (!string.IsNullOrWhiteSpace(evt.TexturePath))
                {
                    path = evt.TexturePath;
                }
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _background = _content.Load<Texture2D>(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load background: {ex.Message}");
            }
        }

        public void Draw()
        {
            if (_background == null) return;

            int viewportW = _graphicsDevice.Viewport.Width;
            int viewportH = _graphicsDevice.Viewport.Height;

            int texW = _background.Width;
            int texH = _background.Height;

            // Compute cover scale (no stretch; scale up until both dimensions cover viewport)
            float scaleX = viewportW / (float)texW;
            float scaleY = viewportH / (float)texH;
            float scale = Math.Max(scaleX, scaleY);

            int drawW = (int)Math.Round(texW * scale);
            int drawH = (int)Math.Round(texH * scale);

            // Position: anchor bottom (prioritize showing bottom), center horizontally
            int x = (viewportW - drawW) / 2;
            int y = viewportH - drawH;

            var dest = new Rectangle(x, y, drawW, drawH);
            _spriteBatch.Draw(_background, dest, Color.White);
        }
    }
}


