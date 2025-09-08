using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Events;
using System;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Renders the player's character portrait (Crusader) on the middle-left of the screen.
    /// </summary>
    public class PlayerDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly Texture2D _crusaderTexture;
        private float _elapsedSeconds;
        private Vector2 _attackDrawOffset = Vector2.Zero; // now sourced from PlayerAnimationState

        // Visual tuning
        private const float ScreenHeightCoverage = 0.3f; // portrait height relative to viewport height
        private const float CenterOffsetX = -600f; // horizontal offset (+right, -left) from screen center
        private const float CenterOffsetY = -100f; // vertical offset (+down, -up) from screen center
        private Entity _anchorEntity;
        private Transform _anchorTransform;

        public PlayerDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D crusaderTexture)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _crusaderTexture = crusaderTexture;

            // Use the actual player entity as the portrait anchor
            _anchorEntity = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (_anchorEntity == null)
            {
                return;
            }
            _anchorTransform = _anchorEntity.GetComponent<Transform>();
            if (_anchorTransform == null)
            {
                _anchorTransform = new Transform();
                EntityManager.AddComponent(_anchorEntity, _anchorTransform);
            }
            var info = _anchorEntity.GetComponent<PortraitInfo>();
            if (info == null)
            {
                info = new PortraitInfo();
                EntityManager.AddComponent(_anchorEntity, info);
            }
            info.TextureWidth = _crusaderTexture?.Width ?? 0;
            info.TextureHeight = _crusaderTexture?.Height ?? 0;

        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // Not entity-driven; draw is purely presentational
            return System.Array.Empty<Entity>();
        }

        public override void Update(GameTime gameTime)
        {
            _elapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update shared anchor transform so other systems (e.g., wisps) can follow
            if (_crusaderTexture != null && _anchorTransform != null)
            {
                int viewportW = _graphicsDevice.Viewport.Width;
                int viewportH = _graphicsDevice.Viewport.Height;

                float desiredHeight = ScreenHeightCoverage * viewportH;
                float baseScale = desiredHeight / _crusaderTexture.Height;
                float scale = baseScale; // no breathing

                var basePosition = new Vector2(viewportW / 2f + CenterOffsetX, viewportH / 2f + CenterOffsetY);
                // Draw offset now maintained by PlayerAnimationSystem via PlayerAnimationState
                var anim = _anchorEntity.GetComponent<PlayerAnimationState>();
                _attackDrawOffset = anim?.DrawOffset ?? Vector2.Zero;
                // Keep the Transform reflecting the base position and scale only
                _anchorTransform.Position = basePosition;
                _anchorTransform.Scale = new Vector2(scale, scale);
                var pinfo = _anchorEntity.GetComponent<PortraitInfo>();
                if (pinfo != null) { pinfo.CurrentScale = scale; pinfo.BaseScale = desiredHeight / (_crusaderTexture?.Height ?? 1); }
                _anchorTransform.Rotation = 0f;
                _anchorTransform.ZOrder = 0;
            }
            base.Update(gameTime);
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            if (_crusaderTexture == null) return;
            if (_anchorTransform == null) return;

            float texW = _crusaderTexture.Width;
            float texH = _crusaderTexture.Height;
            var origin = new Vector2(texW / 2f, texH / 2f); // center pivot
            var position = _anchorTransform.Position + _attackDrawOffset;
            var scaleVec = _anchorTransform.Scale; // may be non-uniform when buff anim plays

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

            // (Wisps drawn by PlayerWispParticleSystem)
        }
        
    }
}


