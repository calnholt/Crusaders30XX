using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Passives Display")]
    public class AppliedPassivesDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private Texture2D _roundedBg;
        private int _cacheW;
        private int _cacheH;
        private int _cacheR;

        [DebugEditable(DisplayName = "Offset Y", Step = 1, Min = -500, Max = 500)]
        public int OffsetY { get; set; } = 4;

        [DebugEditable(DisplayName = "Padding X", Step = 1, Min = 0, Max = 100)]
        public int PadX { get; set; } = 12;

        [DebugEditable(DisplayName = "Padding Y", Step = 1, Min = 0, Max = 100)]
        public int PadY { get; set; } = 3;

        [DebugEditable(DisplayName = "Spacing", Step = 1, Min = 0, Max = 100)]
        public int Spacing { get; set; } = 6;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 32)]
        public int CornerRadius { get; set; } = 16;

        [DebugEditable(DisplayName = "Background R", Step = 1, Min = 0, Max = 255)]
        public int BgR { get; set; } = 0;
        [DebugEditable(DisplayName = "Background G", Step = 1, Min = 0, Max = 255)]
        public int BgG { get; set; } = 0;
        [DebugEditable(DisplayName = "Background B", Step = 1, Min = 0, Max = 255)]
        public int BgB { get; set; } = 0;
        [DebugEditable(DisplayName = "Background A", Step = 1, Min = 0, Max = 255)]
        public int BgA { get; set; } = 150;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.05f, Max = 2f)]
        public float TextScale { get; set; } = 0.12f;

        [DebugEditable(DisplayName = "Ripple Seconds", Step = 0.05f, Min = 0.05f, Max = 2f)]
        public float RippleSeconds { get; set; } = 0.35f;

        [DebugEditable(DisplayName = "Ripple Max Scale", Step = 0.05f, Min = 1f, Max = 3f)]
        public float RippleMaxScale { get; set; } = 2.35f;

        [DebugEditable(DisplayName = "Ripple Min Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
        public float RippleMinAlpha { get; set; } = 0f;

        private class Ripple
        {
            public float Elapsed;
            public float Duration;
        }

        // Track a transient ripple per owner+passive key
        private readonly System.Collections.Generic.Dictionary<(int ownerId, AppliedPassiveType type), Ripple> _ripples = new();

        public AppliedPassivesDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            EventManager.Subscribe<PassiveTriggered>(OnPassiveTriggered);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<AppliedPassives>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Progress ripple animations once per frame (anchor on smallest entity id that matches)
            var ids = EntityManager.GetEntitiesWithComponent<AppliedPassives>().Select(en => en.Id).ToList();
            if (ids.Count == 0) return;
            int anchorId = ids.Min();
            if (entity.Id != anchorId) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (dt <= 0f || _ripples.Count == 0) return;
            var keys = _ripples.Keys.ToList();
            foreach (var k in keys)
            {
                var rp = _ripples[k];
                rp.Elapsed += dt;
                if (rp.Elapsed >= rp.Duration)
                {
                    _ripples.Remove(k);
                }
            }
        }

        public void Draw()
        {
            var entities = GetRelevantEntities().ToList();
            if (entities.Count == 0) return;

            foreach (var e in entities)
            {
                var ap = e.GetComponent<AppliedPassives>();
                var t = e.GetComponent<Transform>();
                if (ap == null || ap.Passives == null || ap.Passives.Count == 0 || t == null) continue;

                // Anchor baseline at bottom of HP bar if available; else just below entity
                int baseX = (int)System.Math.Round(t.Position.X);
                int baseY;
                var hpAnchor = e.GetComponent<HPBarAnchor>();
                if (hpAnchor != null)
                {
                    baseY = hpAnchor.Rect.Bottom + OffsetY;
                }
                else
                {
                    // Fallback under portrait
                    float visualHalfHeight = 0f;
                    var pInfo = e.GetComponent<PortraitInfo>();
                    if (pInfo != null)
                    {
                        float baseScale = (pInfo.BaseScale > 0f) ? pInfo.BaseScale : 1f;
                        visualHalfHeight = System.Math.Max(visualHalfHeight, (pInfo.TextureHeight * baseScale) * 0.5f);
                    }
                    baseY = (int)System.Math.Round(t.Position.Y + visualHalfHeight + 20 + OffsetY);
                }

                // Render each passive as "<stacks> <Name>" chip, left-to-right centered under entity
                var items = ap.Passives.Select(kv => new { Type = kv.Key, Count = kv.Value, Label = $"{kv.Value} {kv.Key}" }).ToList();
                if (items.Count == 0) continue;

                var sizes = items.Select(it => _font.MeasureString(it.Label) * TextScale).ToList();
                var chipWidths = sizes.Select(s => (int)System.Math.Ceiling(s.X) + PadX * 2).ToList();
                int totalWidth = chipWidths.Sum() + System.Math.Max(0, (items.Count - 1) * Spacing);
                int x = baseX - totalWidth / 2;

                for (int i = 0; i < items.Count; i++)
                {
                    int w = chipWidths[i];
                    int h = (int)System.Math.Ceiling(sizes[i].Y) + PadY * 2;
                    EnsureRounded(w, h, System.Math.Min(CornerRadius, System.Math.Min(w, h) / 2));
                    // Ripple overlay (independent of chip background)
                    var key = (e.Id, items[i].Type);
                    if (_ripples.TryGetValue(key, out var rp))
                    {
                        float progress = MathHelper.Clamp(rp.Elapsed / System.Math.Max(0.0001f, rp.Duration), 0f, 1f);
                        float scale = MathHelper.Lerp(1f, RippleMaxScale, progress);
                        float alpha = MathHelper.Lerp(1f, RippleMinAlpha, progress);
                        int scaledW = (int)System.Math.Round(w * scale);
                        int scaledH = (int)System.Math.Round(h * scale);
                        int cx = x + w / 2;
                        int cy = baseY + h / 2;
                        var rippleRect = new Rectangle(cx - scaledW / 2, cy - scaledH / 2, scaledW, scaledH);
                        var rippleColor = Color.FromNonPremultiplied(BgR, BgG, BgB, (byte)System.Math.Round(MathHelper.Clamp(alpha, 0f, 1f) * 255f));
                        _spriteBatch.Draw(_roundedBg, rippleRect, rippleColor);
                    }
                    // Base chip
                    var chipRect = new Rectangle(x, baseY, w, h);
                    var chipBg = Color.FromNonPremultiplied(BgR, BgG, BgB, (byte)BgA);
                    _spriteBatch.Draw(_roundedBg, chipRect, chipBg);
                    var textPos = new Vector2(x + (w - sizes[i].X) / 2f, baseY + (h - sizes[i].Y) / 2f);
                    _spriteBatch.DrawString(_font, items[i].Label, textPos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
                    x += w + Spacing;
                }
            }
        }

        private void OnPassiveTriggered(PassiveTriggered e)
        {
            if (e?.Owner == null) return;
            _ripples[(e.Owner.Id, e.Type)] = new Ripple { Elapsed = 0f, Duration = System.Math.Max(0.05f, RippleSeconds) };
        }

        private void EnsureRounded(int w, int h, int r)
        {
            if (_roundedBg == null || _cacheW != w || _cacheH != h || _cacheR != r)
            {
                _roundedBg?.Dispose();
                _roundedBg = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, r);
                _cacheW = w; _cacheH = h; _cacheR = r;
            }
        }
        [DebugAction("Simulate Burn Trigger")]
        public void Debug_SimulateBurnTrigger()
        {
            EventManager.Publish(new PassiveTriggered { Owner = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn });
            EventManager.Publish(new PassiveTriggered { Owner = EntityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Burn });
        }
    }
}


