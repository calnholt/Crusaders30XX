using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Collects frame timing and draws a small overlay with FPS and a rolling graph.
    /// </summary>
    public class ProfilerSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;

        private const int MaxSamples = 180; // ~3s at 60 FPS
        private readonly Queue<float> _frameTimes = new Queue<float>(MaxSamples);
        private float _accumulatedTime;
        private int _frameCount;
        private float _fps;

        // Overlay layout (full-screen overlay with margin)
        private int _overlayMargin = 16;
        public int OverlayMargin { get => _overlayMargin; set => _overlayMargin = Math.Max(0, value); }

        // Graph style
        private int _headerBaseHeight = 48; // minimum header height
        private int _headerPadding = 8;     // padding inside header
        public int HeaderBaseHeight { get => _headerBaseHeight; set => _headerBaseHeight = Math.Max(0, value); }
        public int HeaderPadding { get => _headerPadding; set => _headerPadding = Math.Max(0, value); }
        private const int AxisLabelWidth = 44; // space for Y-axis labels
        private const float GraphMaxFps = 120f;
        private byte _graphBackgroundAlpha = 0;   // more transparent white fill
        private byte _graphGridLineAlpha = 12;    // more transparent grid lines
        public byte GraphBackgroundAlpha { get => _graphBackgroundAlpha; set => _graphBackgroundAlpha = value; }
        public byte GraphGridLineAlpha { get => _graphGridLineAlpha; set => _graphGridLineAlpha = value; }

        private Texture2D _whiteTex;
        private float _whiteAlphaMultiplier = 1f;
        public float WhiteAlphaMultiplier { get => _whiteAlphaMultiplier; set => _whiteAlphaMultiplier = MathHelper.Clamp(value, 0f, 1f); }

        public ProfilerSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<ProfilerOverlay>();
        }

        public override void Update(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Track instantaneous FPS using averaging over 0.5s
            _accumulatedTime += dt;
            _frameCount++;
            if (_accumulatedTime >= 0.5f)
            {
                _fps = _frameCount / _accumulatedTime;
                _frameCount = 0;
                _accumulatedTime = 0f;
            }

            // Push dt to rolling window for graph (cap to MaxSamples)
            _frameTimes.Enqueue(dt);
            while (_frameTimes.Count > MaxSamples)
            {
                _frameTimes.Dequeue();
            }

            EnsureWhiteTexture();

            base.Update(gameTime);
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // no-op per entity
        }

        public void Draw()
        {
            var e = GetRelevantEntities().FirstOrDefault();
            if (e == null) return;
            var overlay = e.GetComponent<ProfilerOverlay>();
            if (overlay == null || !overlay.IsOpen) return;

            EnsureWhiteTexture();

            var viewport = _graphicsDevice.Viewport;
            int panelX = _overlayMargin;
            int panelY = _overlayMargin;
            int panelW = Math.Max(0, viewport.Width - _overlayMargin * 2);
            int panelH = Math.Max(0, viewport.Height - _overlayMargin * 2);

            // Background overlay (semi-transparent)
            DrawRect(new Rectangle(panelX, panelY, panelW, panelH), new Color(0, 0, 0, 160));

            // Title and metrics (use measured line spacing to avoid cramped text)
            string title = "Profiler";
            string fpsStr = $"FPS: {Math.Round(_fps):0}";
            float lastDt = _frameTimes.Count > 0 ? _frameTimes.Last() : 0f;
            string msStr = lastDt > 0 ? $"Frame: {(lastDt * 1000f):0.0} ms" : "Frame: -- ms";

            float lineH = _font.LineSpacing;
            // compute header height dynamically to fit two lines + padding
            int headerHeight = Math.Max(_headerBaseHeight, (int)(lineH * 2 + _headerPadding * 2));

            var titlePos = new Vector2(panelX + _headerPadding, panelY + _headerPadding);
            var fpsPos = new Vector2(panelX + _headerPadding, panelY + _headerPadding + lineH);
            float fpsWidth = _font.MeasureString(fpsStr).X;
            var msPos = new Vector2(fpsPos.X + fpsWidth + 24f, fpsPos.Y);

            _spriteBatch.DrawString(_font, title, titlePos, Color.White);
            _spriteBatch.DrawString(_font, fpsStr, fpsPos, Color.White);
            _spriteBatch.DrawString(_font, msStr, msPos, Color.White);

            // Graph area
            int gx = panelX + AxisLabelWidth + 12;
            int gy = panelY + Math.Max(_headerBaseHeight, (int)(lineH * 2 + _headerPadding * 2));
            int gw = Math.Max(1, panelW - (gx - panelX) - 12);
            int gh = Math.Max(1, panelH - Math.Max(_headerBaseHeight, (int)(lineH * 2 + _headerPadding * 2)) - 16);

            // Y-axis ticks and labels (0..GraphMaxFps)
            int[] ticks = [0, 30, 60, 90, (int)GraphMaxFps];
            foreach (int t in ticks)
            {
                int ty = gy + gh - (int)(gh * (t / GraphMaxFps));
                // tick line
                DrawRect(new Rectangle(gx, ty, gw, 1), new Color(255, 255, 255, (int)_graphGridLineAlpha));
                // label at left
                var labelPos = new Vector2(panelX + 10, ty - 8);
                _spriteBatch.DrawString(_font, t.ToString(), labelPos, Color.White);
            }

            // Plot line of FPS over time (higher is better)
            if (_frameTimes.Count >= 2)
            {
                // Convert dt to FPS; clamp to [0, 120] for scale
                var samples = _frameTimes.Select(t => t > 0 ? Math.Min(GraphMaxFps, 1f / t) : GraphMaxFps).ToArray();
                int n = samples.Length;
                for (int i = 1; i < n; i++)
                {
                    float t0 = samples[i - 1];
                    float t1 = samples[i];
                    int x0 = gx + (i - 1) * gw / (n - 1);
                    int x1 = gx + i * gw / (n - 1);
                    int y0 = gy + gh - (int)(gh * (t0 / GraphMaxFps));
                    int y1 = gy + gh - (int)(gh * (t1 / GraphMaxFps));
                    DrawLine(x0, y0, x1, y1, new Color(0, 200, 255, 200));
                }

                // Annotate latest value near the right edge
                float lastFps = samples[^1];
                int lx = gx + gw - 2;
                int ly = gy + gh - (int)(gh * (lastFps / GraphMaxFps));
                string lastLabel = $"{lastFps:0}";
                _spriteBatch.DrawString(_font, lastLabel, new Vector2(lx - 32, ly - 16), Color.Cyan);
            }
        }

        private void EnsureWhiteTexture()
        {
            if (_whiteTex != null) return;
            _whiteTex = new Texture2D(_graphicsDevice, 1, 1);
            _whiteTex.SetData(new[] { Color.White });
        }

        private void DrawRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_whiteTex, rect, ApplyWhiteAlpha(color));
        }

        private void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            // Bresenham low-cost line drawing via stretch-draw
            int dx = x1 - x0;
            int dy = y1 - y0;
            float len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
            float angle = (float)Math.Atan2(dy, dx);
            _spriteBatch.Draw(_whiteTex, new Rectangle(x0, y0, (int)len, 2), null, ApplyWhiteAlpha(color), angle, Vector2.Zero, SpriteEffects.None, 0f);
        }

        private Color ApplyWhiteAlpha(Color color)
        {
            byte a = (byte)MathHelper.Clamp((int)(color.A * _whiteAlphaMultiplier), 0, 255);
            return new Color(color.R, color.G, color.B, a);
        }
    }
}
