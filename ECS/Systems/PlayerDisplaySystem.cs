using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        // Visual tuning
        private const float ScreenHeightCoverage = 0.3f; // portrait height relative to viewport height
        private const float CenterOffsetX = -600f; // horizontal offset (+right, -left) from screen center
        private const float CenterOffsetY = 0f; // vertical offset (+down, -up) from screen center

        // Breathing animation (ease in/out)
        private const float BreathScaleAmplitude = 0.06f; // total swing around base (± amplitude/2)
        private const float BreathSpeedHz = 0.25f;        // cycles per second

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
            return System.Array.Empty<Entity>();
        }

        public override void Update(GameTime gameTime)
        {
            _elapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
            base.Update(gameTime);
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            if (_crusaderTexture == null) return;

            int viewportW = _graphicsDevice.Viewport.Width;
            int viewportH = _graphicsDevice.Viewport.Height;

            // Base scale to cover a portion of the screen height
            float desiredHeight = ScreenHeightCoverage * viewportH;
            float baseScale = desiredHeight / _crusaderTexture.Height;

            // Smooth breathing factor centered around 1.0 using cosine for ease-in/out
            float phase = 2f * System.MathF.PI * BreathSpeedHz * _elapsedSeconds;
            float breathFactor = 1f + (BreathScaleAmplitude * 0.5f) * System.MathF.Cos(phase);
            float scale = baseScale * breathFactor;

            // Center alignment with adjustable offset; scale about texture center for stable breathing
            float texW = _crusaderTexture.Width;
            float texH = _crusaderTexture.Height;
            var origin = new Vector2(texW / 2f, texH / 2f); // center pivot
            var position = new Vector2(viewportW / 2f + CenterOffsetX, viewportH / 2f + CenterOffsetY);

            _spriteBatch.Draw(
                _crusaderTexture,
                position,
                sourceRectangle: null,
                color: Color.White,
                rotation: 0f,
                origin: origin,
                scale: scale,
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }
    }
}


