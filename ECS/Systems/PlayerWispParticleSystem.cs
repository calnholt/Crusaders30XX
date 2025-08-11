using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Draws glowing red wisps around the player portrait area.
    /// Anchors to the same transform as PlayerDisplaySystem.
    /// </summary>
    public class PlayerWispParticleSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;

        private float _elapsedSeconds;

        // Uses Transform from PlayerPortraitAnchor instead of duplicating layout constants

        // Wispy particle settings
        private const float WispSpawnRatePerSecond = 10f;
        private const int WispMaxCount = 150;
        private const float WispMinLifetime = 3f;
        private const float WispMaxLifetime = 4f;
        private const float WispMinSpeed = 40f;   // px/sec upward
        private const float WispMaxSpeed = 56f;
        private const float WispMinSwayAmplitude = 5f; // px
        private const float WispMaxSwayAmplitude = 15f;
        private const float WispMinSwayHz = 0.7f;
        private const float WispMaxSwayHz = 1f;

        // Adjustable visual radius range in pixels (core circle), before glow multiplier
        private float _wispMinRadiusPx = 5f;
        private float _wispMaxRadiusPx = 10f;
        public float WispMinRadiusPx { get => _wispMinRadiusPx; set => _wispMinRadiusPx = MathF.Max(0.1f, MathF.Min(value, _wispMaxRadiusPx)); }
        public float WispMaxRadiusPx { get => _wispMaxRadiusPx; set => _wispMaxRadiusPx = MathF.Max(value, _wispMinRadiusPx); }

        // Transparency controls (multipliers applied after lifetime fade)
        private float _wispCoreAlphaMultiplier = 1f;
        private float _wispGlowAlphaMultiplier = 1f;

        private readonly List<WispParticle> _wisps = new();
        private readonly Random _random = new Random();
        private float _spawnAccumulator;
        private Texture2D _wispTexture; // soft radial sprite

        private struct WispParticle
        {
            public Vector2 StartPosition;
            public float Age;
            public float Lifetime;
            public float UpwardSpeed;
            public float SwayAmplitude;
            public float SwayAngularVelocity; // radians/sec
            public float SwayPhase;            // radians
            public float SizeScale;
        }

        public PlayerWispParticleSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            // Purely presentational
            return Array.Empty<Entity>();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsedSeconds += dt;

            EnsureWispTexture();
            if (!TryGetAnchor(out var anchorTransform, out var portraitInfo)) return;

            var portraitPosition = anchorTransform.Position;
            var portraitScale = anchorTransform.Scale.X; // uniform scale
            float texW = portraitInfo?.TextureWidth ?? 0;
            float texH = portraitInfo?.TextureHeight ?? 0;

            // Spawn new wisps based on rate, accumulating fractional spawns
            _spawnAccumulator += WispSpawnRatePerSecond * dt;
            int toSpawn = (int)_spawnAccumulator;
            if (toSpawn > 0)
            {
                _spawnAccumulator -= toSpawn;
                for (int i = 0; i < toSpawn && _wisps.Count < WispMaxCount; i++)
                {
                    SpawnWisp(portraitPosition, portraitScale, texW, texH);
                }
            }

            // Update existing wisps
            for (int i = _wisps.Count - 1; i >= 0; i--)
            {
                var p = _wisps[i];
                p.Age += dt;
                if (p.Age >= p.Lifetime)
                {
                    _wisps.RemoveAt(i);
                    continue;
                }
                // advance sway phase
                p.SwayPhase += p.SwayAngularVelocity * dt;
                _wisps[i] = p;
            }

            base.Update(gameTime);
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            if (_wispTexture == null) return;
            if (!TryGetAnchor(out var anchorTransform, out var portraitInfo)) return;
            var portraitPosition = anchorTransform.Position;
            var portraitScale = anchorTransform.Scale.X;
            float texW = portraitInfo?.TextureWidth ?? 0;
            float texH = portraitInfo?.TextureHeight ?? 0;
            DrawWisps(portraitPosition, portraitScale, texW, texH);
        }

        private void SpawnWisp(Vector2 portraitPosition, float portraitScale, float texW, float texH)
        {
            // Spawn region: around lower half of portrait, small random ring around center
            float halfW = 0.45f * texW * portraitScale;
            float halfH = 0.4f * texH * portraitScale; // bias lower area
            float rx = (float)(_random.NextDouble() * 2 - 1);
            float ry = (float)(_random.NextDouble() * 2 - 1);
            var spawnOffset = new Vector2(rx * halfW, ry * halfH + 0.2f * texH * portraitScale);

            // Convert desired pixel radius to sprite scale based on radial texture radius
            float texRadius = _wispTexture != null ? _wispTexture.Width / 2f : 24f;
            float desiredRadiusPx = MathHelper.Lerp(_wispMinRadiusPx, _wispMaxRadiusPx, (float)_random.NextDouble())
                                     * (0.7f + 0.3f * portraitScale);
            float sizeScale = desiredRadiusPx / texRadius;

            var p = new WispParticle
            {
                StartPosition = portraitPosition + spawnOffset,
                Age = 0f,
                Lifetime = MathHelper.Lerp(WispMinLifetime, WispMaxLifetime, (float)_random.NextDouble()),
                UpwardSpeed = MathHelper.Lerp(WispMinSpeed, WispMaxSpeed, (float)_random.NextDouble()),
                SwayAmplitude = MathHelper.Lerp(WispMinSwayAmplitude, WispMaxSwayAmplitude, (float)_random.NextDouble()) * (0.6f + 0.4f * portraitScale),
                SwayAngularVelocity = MathHelper.TwoPi * MathHelper.Lerp(WispMinSwayHz, WispMaxSwayHz, (float)_random.NextDouble()),
                SwayPhase = MathHelper.TwoPi * (float)_random.NextDouble(),
                SizeScale = sizeScale
            };

            _wisps.Add(p);
        }

        private void DrawWisps(Vector2 portraitPosition, float portraitScale, float texW, float texH)
        {
            if (_wispTexture == null || _wisps.Count == 0) return;

            for (int i = 0; i < _wisps.Count; i++)
            {
                var p = _wisps[i];
                float t = MathHelper.Clamp(p.Age / p.Lifetime, 0f, 1f);

                // position over time: upward drift + horizontal sway
                float up = -p.UpwardSpeed * p.Age; // negative Y is up
                float swayX = p.SwayAmplitude * MathF.Sin(p.SwayPhase);
                var pos = p.StartPosition + new Vector2(swayX, up);

                // ease-in fade and ease-out alpha
                float alphaIn = MathF.Min(1f, t * 3f);
                float alphaOut = 1f - t;
                float alpha = MathF.Pow(alphaIn * alphaOut, 1.2f);

                // color: glowing red
                byte r = 255;
                byte g = 50;
                byte b = 60;

                // scale grows slightly as it rises
                float s = p.SizeScale * (1f + 0.15f * t);
                var colorCore = new Color(r, g, b) * (_wispCoreAlphaMultiplier * alpha);
                var colorGlow = new Color(r, g, b) * (_wispGlowAlphaMultiplier * alpha);

                var originPx = new Vector2(_wispTexture.Width / 2f, _wispTexture.Height / 2f);

                // soft outer glow
                _spriteBatch.Draw(_wispTexture, pos, null, colorGlow, 0f, originPx, s * 1.8f, SpriteEffects.None, 0f);
                // bright core
                _spriteBatch.Draw(_wispTexture, pos, null, colorCore, 0f, originPx, s, SpriteEffects.None, 0f);
            }
        }

        private void EnsureWispTexture()
        {
            if (_wispTexture != null) return;

            // Create a small radial gradient texture (soft circle)
            int size = 48;
            _wispTexture = new Texture2D(_graphicsDevice, size, size, false, SurfaceFormat.Color);
            var data = new Color[size * size];
            float radius = size / 2f;
            var center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float t = MathHelper.Clamp(d / radius, 0f, 1f);
                    // Inverted smoothstep for soft center, feathered edges
                    float a = 1f - t * t * (3f - 2f * t);
                    data[y * size + x] = Color.FromNonPremultiplied(255, 255, 255, (int)(a * 255));
                }
            }
            _wispTexture.SetData(data);
        }

        private bool TryGetAnchor(out Components.Transform transform, out Components.PlayerPortraitInfo info)
        {
            transform = null;
            info = null;
            var anchor = EntityManager.GetEntitiesWithComponent<Components.PlayerPortraitAnchor>().FirstOrDefault();
            if (anchor == null) return false;
            transform = anchor.GetComponent<Components.Transform>();
            info = anchor.GetComponent<Components.PlayerPortraitInfo>();
            return transform != null;
        }
    }
}
