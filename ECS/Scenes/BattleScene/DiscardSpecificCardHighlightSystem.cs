using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Discard-Specific Highlights")]
    public class DiscardSpecificCardHighlightSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
        private double _elapsedSeconds = 0.0;

        [DebugEditable(DisplayName = "Enabled", Step = 1, Min = 0, Max = 1)]
        public int Enabled01 { get; set; } = 1;

        [DebugEditable(DisplayName = "Sparkles Per Frame", Step = 1, Min = 0, Max = 300)]
        public int SparkleCount { get; set; } = 268;

        [DebugEditable(DisplayName = "Band Thickness (px)", Step = 1, Min = 1, Max = 60)]
        public int BandThicknessPx { get; set; } = 60;

        [DebugEditable(DisplayName = "Inside Offset (px)", Step = 1, Min = 0, Max = 60)]
        public int InsideOffsetPx { get; set; } = 8;

        [DebugEditable(DisplayName = "Outside Offset (px)", Step = 1, Min = 0, Max = 60)]
        public int OutsideOffsetPx { get; set; } = 0;

        [DebugEditable(DisplayName = "Size Min (px)", Step = 1, Min = 1, Max = 20)]
        public int SizeMinPx { get; set; } = 1;

        [DebugEditable(DisplayName = "Size Max (px)", Step = 1, Min = 1, Max = 24)]
        public int SizeMaxPx { get; set; } = 4;

        [DebugEditable(DisplayName = "Flicker Speed", Step = 1, Min = 0, Max = 120)]
        public int FlickerSeedsPerSecond { get; set; } = 10;

        [DebugEditable(DisplayName = "Alpha Min", Step = 0.05f, Min = 0f, Max = 1f)]
        public float AlphaMin { get; set; } = 0.5f;

        [DebugEditable(DisplayName = "Alpha Max", Step = 0.05f, Min = 0f, Max = 1f)]
        public float AlphaMax { get; set; } = 1.0f;

        [DebugEditable(DisplayName = "Use Red", Step = 1, Min = 0, Max = 1)]
        public int UseRed01 { get; set; } = 1;

        [DebugEditable(DisplayName = "Use White", Step = 1, Min = 0, Max = 1)]
        public int UseWhite01 { get; set; } = 1;

        [DebugEditable(DisplayName = "Use Black", Step = 1, Min = 0, Max = 1)]
        public int UseBlack01 { get; set; } = 1;

        public DiscardSpecificCardHighlightSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Deck>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            _elapsedSeconds = gameTime.TotalGameTime.TotalSeconds;
        }

        public void Draw()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            // Only show during enemy Block/Attack phases
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            if (phase == null || (phase.Sub != SubPhase.Block && phase.Sub != SubPhase.EnemyAttack)) return;

            if (Enabled01 == 0) return;

            // Current context
            var intentEntity = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
            var ctx = intentEntity?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId;
            if (string.IsNullOrEmpty(ctx)) return;

            foreach (var card in deck.Hand)
            {
                var mark = card.GetComponent<MarkedForSpecificDiscard>();
                if (mark == null || mark.ContextId != ctx) continue;
                var t = card.GetComponent<Transform>();
                var ui = card.GetComponent<UIElement>();
                if (t == null || ui == null) continue;
                var rect = ComputeCardBounds(card, t.Position);
                DrawSparkles(rect, t.Rotation, t.Scale);
            }
        }

        private Rectangle ComputeCardBounds(Entity cardEntity, Vector2 position)
        {
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var s = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int cw = s?.CardWidth ?? 250;
            int ch = s?.CardHeight ?? 350;
            int offsetYExtra = s?.CardOffsetYExtra ?? (int)System.Math.Round((s?.UIScale ?? 1f) * 25);
            return new Rectangle(
                (int)position.X - cw / 2,
                (int)position.Y - (ch / 2 + offsetYExtra),
                cw,
                ch
            );
        }

        private void DrawInsideBorderHighlight(Rectangle rect, float rotation, Vector2 scale)
        {
            // Inside-the-border rounded glow similar to CardHighlightSystem but inset
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var s = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int borderThickness = System.Math.Max(2, (s?.HighlightBorderThickness ?? 5) - 2);
            int cornerRadius = System.Math.Max(4, (s?.CardCornerRadius ?? 18) - 3);
            var inner = new Rectangle(
                rect.X + borderThickness,
                rect.Y + borderThickness,
                System.Math.Max(1, rect.Width - borderThickness * 2),
                System.Math.Max(1, rect.Height - borderThickness * 2)
            );
            var tex = GetRoundedRectTexture(inner.Width, inner.Height, cornerRadius);
            var center = new Vector2(inner.X + inner.Width / 2f, inner.Y + inner.Height / 2f);
            Color tint = new Color(255, 80, 80) * 0.28f;
            _spriteBatch.Draw(
                tex,
                position: center,
                sourceRectangle: null,
                color: tint,
                rotation: rotation,
                origin: new Vector2(tex.Width / 2f, tex.Height / 2f),
                scale: new Vector2(System.Math.Max(0.01f, scale.X), System.Math.Max(0.01f, scale.Y)),
                effects: SpriteEffects.None,
                layerDepth: 0f
            );
        }

        private void DrawRect(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private Texture2D GetRoundedRectTexture(int w, int h, int r)
        {
            var key = (w, h, r);
            if (_roundedRectCache.TryGetValue(key, out var tex)) return tex;
            var created = Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, r);
            _roundedRectCache[key] = created;
            return created;
        }

        private void DrawSparkles(Rectangle rect, float rotation, Vector2 scale)
        {
            // Red/White/Black sparkles distributed around the card near its edges (inside/outside)
            float t = (float)_elapsedSeconds;
            var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float cos = (float)System.Math.Cos(rotation);
            float sin = (float)System.Math.Sin(rotation);

            var palette = new List<Color>();
            if (UseRed01 != 0) palette.Add(Color.Red);
            if (UseWhite01 != 0) palette.Add(Color.White);
            if (UseBlack01 != 0) palette.Add(Color.Black);
            if (palette.Count == 0) { palette.Add(Color.Red); palette.Add(Color.White); palette.Add(Color.Black); }

            int seed = rect.X ^ rect.Y ^ rect.Width ^ rect.Height;
            int flickerStep = System.Math.Max(0, FlickerSeedsPerSecond);
            var rand = new System.Random(seed + (int)System.Math.Floor(t * flickerStep));
            int count = System.Math.Max(0, SparkleCount);
            int band = System.Math.Max(1, BandThicknessPx);
            int inside = System.Math.Max(0, InsideOffsetPx);
            int outside = System.Math.Max(0, OutsideOffsetPx);
            int smin = System.Math.Max(1, System.Math.Min(SizeMinPx, SizeMaxPx));
            int smax = System.Math.Max(smin, System.Math.Max(SizeMinPx, SizeMaxPx));
            float aMin = System.Math.Clamp(AlphaMin, 0f, 1f);
            float aMax = System.Math.Clamp(AlphaMax, 0f, 1f);
            if (aMax < aMin) { var tmp = aMax; aMax = aMin; aMin = tmp; }

            for (int i = 0; i < count; i++)
            {
                int side = rand.Next(0, 4);
                float u = (float)rand.NextDouble();
                // Offset across the normal direction: [-inside, +outside]
                float nrm = (float)rand.NextDouble() * (inside + outside) - inside;

                float halfW = rect.Width / 2f;
                float halfH = rect.Height / 2f;
                float lx = 0f, ly = 0f;
                switch (side)
                {
                    case 0: // top edge, outward is -Y
                        lx = -halfW + u * rect.Width;
                        ly = -halfH - nrm;
                        break;
                    case 1: // right edge, outward is +X
                        lx = halfW + nrm;
                        ly = -halfH + u * rect.Height;
                        break;
                    case 2: // bottom edge, outward is +Y
                        lx = -halfW + u * rect.Width;
                        ly = halfH + nrm;
                        break;
                    default: // left edge, outward is -X
                        lx = -halfW - nrm;
                        ly = -halfH + u * rect.Height;
                        break;
                }
                // Jitter along the tangent inside the band and clamp to card edge to avoid overshooting corners
                float tangentJitter = ((float)rand.NextDouble() * 2f - 1f) * band;
                if (side == 0 || side == 2) lx += tangentJitter; else ly += tangentJitter;
                float eps = 1f; // keep a tiny margin inside the edge
                if (side == 0 || side == 2)
                {
                    // Clamp X to within card width
                    lx = MathHelper.Clamp(lx, -halfW + eps, halfW - eps);
                }
                else
                {
                    // Clamp Y to within card height
                    ly = MathHelper.Clamp(ly, -halfH + eps, halfH - eps);
                }

                // Apply scale
                lx *= System.Math.Max(0.01f, scale.X);
                ly *= System.Math.Max(0.01f, scale.Y);

                // Rotate and translate to world
                float rx = lx * cos - ly * sin;
                float ry = lx * sin + ly * cos;
                var pos = new Vector2(center.X + rx, center.Y + ry);

                int size = smin + rand.Next(0, System.Math.Max(1, smax - smin + 1));
                var c = palette[i % palette.Count];
                float a = aMin + (aMax - aMin) * (float)rand.NextDouble();
                _spriteBatch.Draw(_pixelTexture, new Rectangle((int)(pos.X - size / 2f), (int)(pos.Y - size / 2f), size, size), c * a);
            }
        }
    }
}


