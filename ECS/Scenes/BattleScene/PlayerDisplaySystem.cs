using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using System;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Renders the player's character portrait (Crusader) on the middle-left of the screen.
    /// </summary>
    [DebugTab("Player Display")]
    public class PlayerDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Texture2D _crusaderTexture;
        private float _elapsedSeconds;
        private Vector2 _attackDrawOffset = Vector2.Zero; // now sourced from PlayerAnimationState

        // Visual tuning
        [DebugEditable(DisplayName = "Portrait Height (% of screen height)", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float ScreenHeightCoverage { get; set; } = 0.30f; // relative to viewport height

        [DebugEditable(DisplayName = "Center Offset X (% of width)", Step = 0.01f, Min = -1.0f, Max = 1.0f)]
        public float CenterOffsetXPct { get; set; } = -0.26f; // negative = left, positive = right

        [DebugEditable(DisplayName = "Center Offset Y (% of height)", Step = 0.01f, Min = -1.0f, Max = 1.0f)]
        public float CenterOffsetYPct { get; set; } = -0.11f; // negative = up, positive = down

        public PlayerDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D crusaderTexture)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _crusaderTexture = crusaderTexture;

        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // Not entity-driven; draw is purely presentational
            return Array.Empty<Entity>();
        }

        public override void Update(GameTime gameTime)
        {
            _elapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var transform = player?.GetComponent<Transform>();

            if (_crusaderTexture != null && player != null)
            {
                int viewportW = _graphicsDevice.Viewport.Width;
                int viewportH = _graphicsDevice.Viewport.Height;

                float desiredHeight = ScreenHeightCoverage * viewportH;
                float baseScale = desiredHeight / _crusaderTexture.Height;
                float scale = baseScale; // no breathing

                // Center plus percentage-based offsets of the viewport size
                var basePosition = new Vector2(
                    viewportW * (0.5f + CenterOffsetXPct),
                    viewportH * (0.5f + CenterOffsetYPct)
                );
                var anim = player.GetComponent<PlayerAnimationState>();
                _attackDrawOffset = anim?.DrawOffset ?? Vector2.Zero;
                // Keep the Transform reflecting the base position and scale only (parallax owns Position)
                transform.BasePosition = basePosition;
                transform.Scale = new Vector2(scale, scale);
                var pinfo = player.GetComponent<PortraitInfo>();
                if (pinfo != null) { pinfo.CurrentScale = scale; pinfo.BaseScale = desiredHeight / (_crusaderTexture?.Height ?? 1); }
                transform.Rotation = 0f;
                transform.ZOrder = 0;
                var info = player.GetComponent<PortraitInfo>();
                info.TextureWidth = _crusaderTexture?.Width ?? 0;
                info.TextureHeight = _crusaderTexture?.Height ?? 0;
            }
            base.Update(gameTime);
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var transform = player?.GetComponent<Transform>();
            if (_crusaderTexture == null) return;
            if (transform == null) return;

            float texW = _crusaderTexture.Width;
            float texH = _crusaderTexture.Height;
            var origin = new Vector2(texW / 2f, texH / 2f); // center pivot
            var position = transform.Position + _attackDrawOffset;
            var scaleVec = transform.Scale; // base scale
            var animState = player.GetComponent<PlayerAnimationState>();
            if (animState != null)
            {
                scaleVec.X *= animState.ScaleMultiplier.X;
                scaleVec.Y *= animState.ScaleMultiplier.Y;
            }

            _spriteBatch.Draw(
                _crusaderTexture,
                position,
                sourceRectangle: null,
                color: Color.White,
                rotation: 0f,
                origin: origin,
                scale: scaleVec,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );

        }
        
    }
}


