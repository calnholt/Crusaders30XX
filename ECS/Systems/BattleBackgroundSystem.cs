using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
        /// Renders a battle background with cover scaling (no stretch). Anchors to bottom; centers horizontally.
    /// </summary>
    [DebugTab("Battle Background")]
    public class BattleBackgroundSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ContentManager _content;
        private Texture2D _background;

        // Debug-adjustable vertical offset for background positioning
        [DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -2000, Max = 2000)]
        public int OffsetY { get; set; } = 0;

        public BattleBackgroundSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
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
                // Ensure a single Battlefield component exists; update to requested location when provided.
                Battlefield battlefield = null;
                foreach (var e in EntityManager.GetEntitiesWithComponent<Battlefield>())
                {
                    battlefield = e.GetComponent<Battlefield>();
                    if (battlefield != null) break;
                }
                if (battlefield == null)
                {
                    var worldEntity = EntityManager.CreateEntity("Battlefield");
                    battlefield = new Battlefield();
                    EntityManager.AddComponent(worldEntity, battlefield);
                }

                if (evt.Location.HasValue)
                {
                    battlefield.Location = evt.Location.Value;
                }

                // Load the background texture from the Battlefield location unless an explicit path is given.
                string path = null;
                if (!string.IsNullOrWhiteSpace(evt.TexturePath))
                {
                    path = evt.TexturePath;
                }
                else
                {
                    path = battlefield.Location switch
                    {
                        BattleLocation.Desert => "desert-background",
                        BattleLocation.Forest => "forest-background",
                        BattleLocation.Cathedral => "cathedral-background",
                        _ => null
                    };
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
            int y = viewportH - drawH + OffsetY;

            var dest = new Rectangle(x, y, drawW, drawH);
            _spriteBatch.Draw(_background, dest, Color.White);
        }
    }
}


