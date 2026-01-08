using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Utils;
using System.Collections.Generic;
using System;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Passives Display")]
    public class AppliedPassivesDisplaySystem : Core.System
    {
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly System.Collections.Generic.Dictionary<(int w, int h, int r), Texture2D> _roundedCache = new();
        private readonly System.Collections.Generic.Dictionary<(int ownerId, AppliedPassiveType type), Entity> _tooltipUiByKey = new();

        [DebugEditable(DisplayName = "Offset Y", Step = 1, Min = -500, Max = 500)]
        public int OffsetY { get; set; } = 15;

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

        public AppliedPassivesDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            EventManager.Subscribe<PassiveTriggered>(OnPassiveTriggered);
            EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCachesEvent);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadSceneEvent);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<AppliedPassives>();
        }

        private void OnDeleteCachesEvent(DeleteCachesEvent evt)
        {
            _tooltipUiByKey.Values.ToList().ForEach(ui => EntityManager.DestroyEntity(ui.Id));
            _roundedCache.Clear();
            _tooltipUiByKey.Clear();
            _ripples.Clear();
        }

        private void OnLoadSceneEvent(LoadSceneEvent evt)
        {
            var player = evt.Scene == SceneId.Battle ? EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault() : null;
            if (player == null) return;
            var appliedPassives = player.GetComponent<AppliedPassives>();
            if (appliedPassives == null) return;
            foreach (var kv in appliedPassives.Passives)
            {
                _ripples[(player.Id, kv.Key)] = new Ripple { Elapsed = 0f, Duration = Math.Max(0.05f, RippleSeconds) };
            }
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
                bool isPlayer = e.GetComponent<Player>() != null;
                var ap = e.GetComponent<AppliedPassives>();
                var t = e.GetComponent<Transform>();
                if (ap == null || ap.Passives == null || t == null)
                {
                    CleanupTooltipUiForOwner(e.Id, new System.Collections.Generic.HashSet<AppliedPassiveType>());
                    continue;
                }
                if (ap.Passives.Count == 0)
                {
                    CleanupTooltipUiForOwner(e.Id, new System.Collections.Generic.HashSet<AppliedPassiveType>());
                    continue;
                }

                // Anchor baseline at bottom of HP bar if available; else just below entity
                int baseX = (int)Math.Round(t.Position.X);
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
                        visualHalfHeight = Math.Max(visualHalfHeight, (pInfo.TextureHeight * baseScale) * 0.5f);
                    }
                    baseY = (int)Math.Round(t.Position.Y + visualHalfHeight + 20 + OffsetY);
                }

                // Render each passive as "<stacks> <Name>" chip, left-to-right centered under entity
                var items = ap.Passives.Select(kv => new { Type = kv.Key, Count = kv.Value, Label = $"{(ShowStacks(kv.Key) ? $"{kv.Value} " : "")}{StringUtils.ToSentenceCase(kv.Key.ToString())}" }).ToList();
                if (items.Count == 0)
                {
                    CleanupTooltipUiForOwner(e.Id, new System.Collections.Generic.HashSet<AppliedPassiveType>());
                    continue;
                }

                var sizes = items.Select(it => _font.MeasureString(it.Label) * TextScale).ToList();
                var chipWidths = sizes.Select(s => (int)Math.Ceiling(s.X) + PadX * 2).ToList();
                int totalWidth = chipWidths.Sum() + Math.Max(0, (items.Count - 1) * Spacing);
                int x = baseX - totalWidth / 2;

				for (int i = 0; i < items.Count; i++)
                {
                    int w = chipWidths[i];
                    int h = (int)Math.Ceiling(sizes[i].Y) + PadY * 2;
					int r = Math.Min(CornerRadius, Math.Min(w, h) / 2);
					var chipTexture = GetRounded(w, h, r);
                    // Ripple overlay (independent of chip background)
                    var key = (e.Id, items[i].Type);
                    if (_ripples.TryGetValue(key, out var rp))
                    {
                        float progress = MathHelper.Clamp(rp.Elapsed / Math.Max(0.0001f, rp.Duration), 0f, 1f);
                        float scale = MathHelper.Lerp(1f, RippleMaxScale, progress);
                        float alpha = MathHelper.Lerp(1f, RippleMinAlpha, progress);
                        int scaledW = (int)Math.Round(w * scale);
                        int scaledH = (int)Math.Round(h * scale);
                        int cx = x + w / 2;
                        int cy = baseY + h / 2;
                        var rippleRect = new Rectangle(cx - scaledW / 2, cy - scaledH / 2, scaledW, scaledH);
                        var rippleColor = Color.FromNonPremultiplied(BgR, BgG, BgB, (byte)Math.Round(MathHelper.Clamp(alpha, 0f, 1f) * 255f));
						_spriteBatch.Draw(chipTexture, rippleRect, rippleColor);
                    }
                    // Base chip
                    var chipRect = new Rectangle(x, baseY, w, h);
                    var chipBg = Color.FromNonPremultiplied(BgR, BgG, BgB, (byte)BgA);
					_spriteBatch.Draw(chipTexture, chipRect, chipBg);
                    var textPos = new Vector2(x + (w - sizes[i].X) / 2f, baseY + (h - sizes[i].Y) / 2f);
                    _spriteBatch.DrawString(_font, items[i].Label, textPos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
                    UpdateTooltipUi(key, chipRect, PassiveTooltipTextService.GetText(items[i].Type, isPlayer, items[i].Count));
                    x += w + Spacing;
                }
                // Remove any tooltip UI for passives no longer present
                var presentTypes = new System.Collections.Generic.HashSet<AppliedPassiveType>(items.Select(it => it.Type));
                CleanupTooltipUiForOwner(e.Id, presentTypes);
            }
        }

        private Boolean ShowStacks(AppliedPassiveType type)
        {
            return !(new List<AppliedPassiveType> {AppliedPassiveType.Stealth, AppliedPassiveType.MindFog, AppliedPassiveType.Enflamed }).Contains(type);
        }

        private void OnPassiveTriggered(PassiveTriggered e)
        {
            if (e?.Owner == null) return;
            _ripples[(e.Owner.Id, e.Type)] = new Ripple { Elapsed = 0f, Duration = Math.Max(0.05f, RippleSeconds) };
        }

		private Texture2D GetRounded(int w, int h, int r)
		{
			var key = (w, h, r);
			if (_roundedCache.TryGetValue(key, out var tex)) return tex;
			var created = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, r);
			_roundedCache[key] = created;
			return created;
		}

        private void UpdateTooltipUi((int ownerId, AppliedPassiveType type) key, Rectangle rect, string text)
        {
            if (!_tooltipUiByKey.TryGetValue(key, out var uiEntity) || uiEntity == null)
            {
                uiEntity = EntityManager.CreateEntity($"UI_PassiveTooltip_{key.ownerId}_{key.type}");
                EntityManager.AddComponent(uiEntity, new Transform { Position = new Vector2(rect.X, rect.Y), ZOrder = 10001 });
                EntityManager.AddComponent(uiEntity, new UIElement { Bounds = rect, IsInteractable = false, Tooltip = text ?? string.Empty, TooltipPosition = TooltipPosition.Below });
                _tooltipUiByKey[key] = uiEntity;
            }
            else
            {
                var tr = uiEntity.GetComponent<Transform>();
                if (tr != null)
                {
                    tr.Position = new Vector2(rect.X, rect.Y);
                    tr.ZOrder = 10001;
                }
                var ui = uiEntity.GetComponent<UIElement>();
                if (ui != null)
                {
                    ui.Bounds = rect;
                    ui.Tooltip = text ?? string.Empty;
                    ui.TooltipPosition = TooltipPosition.Below;
                    ui.IsInteractable = false;
                }
            }
        }
        private void CleanupTooltipUiForOwner(int ownerId, System.Collections.Generic.HashSet<AppliedPassiveType> presentTypes)
        {
            var keysForOwner = _tooltipUiByKey.Keys.Where(k => k.ownerId == ownerId).ToList();
            foreach (var key in keysForOwner)
            {
                if (!presentTypes.Contains(key.type))
                {
                    if (_tooltipUiByKey.TryGetValue(key, out var uiEntity) && uiEntity != null)
                    {
                        EntityManager.DestroyEntity(uiEntity.Id);
                    }
                    _tooltipUiByKey.Remove(key);
                }
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


