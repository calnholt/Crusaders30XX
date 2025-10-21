using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Dialog;
using System.Collections.Generic;
using Crusaders30XX.ECS.Utils;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Dialog Overlay")] 
    public class DialogDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ContentManager _content;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private Texture2D _rounded;
        private int _cachedW, _cachedH, _cachedR;

        // Debug-editable layout
        [DebugEditable(DisplayName = "Panel Height %", Step = 0.01f, Min = 0.1f, Max = 0.6f)]
        public float PanelHeightPercent { get; set; } = 0.28f;

        [DebugEditable(DisplayName = "Panel Padding", Step = 1, Min = 0, Max = 64)]
        public int PanelPadding { get; set; } = 16;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
        public int CornerRadius { get; set; } = 12;

        [DebugEditable(DisplayName = "Panel Alpha", Step = 5, Min = 0, Max = 255)]
        public int PanelAlpha { get; set; } = 200;

        [DebugEditable(DisplayName = "Body Text Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
        public float BodyScale { get; set; } = 0.18f;

        [DebugEditable(DisplayName = "Nameplate Height", Step = 1, Min = 12, Max = 80)]
        public int NameplateHeight { get; set; } = 28;

        [DebugEditable(DisplayName = "Nameplate Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
        public float NameplateScale { get; set; } = 0.22f;

        [DebugEditable(DisplayName = "Portrait Width %", Step = 0.01f, Min = 0f, Max = 0.5f)]
        public float PortraitWidthPercent { get; set; } = 0.18f;

        [DebugEditable(DisplayName = "Z Order", Step = 10, Min = 0, Max = 100000)]
        public int ZOrder { get; set; } = 50000;

        [DebugEditable(DisplayName = "Chars / Second", Step = 1f, Min = 1f, Max = 120f)]
        public float CharsPerSecond { get; set; } = 80f;

        private int _cachedLineIndex = -1;
        private string _cachedFilteredMessage = string.Empty;
        private float _revealProgressSec = 0f;
        private int _revealedChars = 0;
        private bool _lineComplete = false;

        public bool IsOverlayActive
        {
            get
            {
                var e = EntityManager.GetEntitiesWithComponent<DialogOverlayState>().FirstOrDefault();
                var st = e?.GetComponent<DialogOverlayState>();
                return st?.IsActive ?? false;
            }
        }

        public DialogDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content, SpriteFont font) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _content = content;
            _font = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            EnsureOverlayEntity();
            var overlayEntity = EntityManager.GetEntity("DialogOverlay");
            var ui = overlayEntity?.GetComponent<UIElement>();
            var state = overlayEntity?.GetComponent<DialogOverlayState>();
            if (ui == null || state == null) return;

            // Only interactable while active so it doesn't capture hover/clicks
            ui.IsInteractable = state.IsActive;
            ui.Bounds = state.IsActive
                ? new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height)
                : new Rectangle(0, 0, 0, 0);

            if (!state.IsActive)
            {
                ui.IsClicked = false;
                ui.IsHovered = false;
                return;
            }

            // Ensure typewriter state matches the current line
            if (_cachedLineIndex != state.Index)
            {
                ResetTypewriterForCurrentLine(state);
            }

            // Advance reveal based on elapsed time
            if (!_lineComplete && CharsPerSecond > 0f)
            {
                _revealProgressSec += (float)gameTime.ElapsedGameTime.TotalSeconds;
                int add = (int)System.Math.Floor(_revealProgressSec * CharsPerSecond);
                if (add > 0)
                {
                    _revealProgressSec -= add / CharsPerSecond;
                    _revealedChars = System.Math.Min(_revealedChars + add, _cachedFilteredMessage.Length);
                    _lineComplete = _revealedChars >= _cachedFilteredMessage.Length;
                }
            }

            // Click behavior (first completes, next advances)
            if (ui.IsClicked)
            {
                ui.IsClicked = false;
                if (!_lineComplete)
                {
                    _revealedChars = _cachedFilteredMessage.Length;
                    _lineComplete = true;
                }
                else
                {
                    state.Index++;
                    if (state.Index >= (state.Lines?.Count ?? 0))
                    {
                        state.IsActive = false;
                        EventManager.Publish(new DialogEnded());
                    }
                    else
                    {
                        ResetTypewriterForCurrentLine(state);
                    }
                }
            }
        }

        public void Open(DialogDefinition def)
        {
            if (def == null || def.lines == null || def.lines.Count == 0) return;
            EnsureOverlayEntity();
            var e = EntityManager.GetEntity("DialogOverlay");
            var st = e.GetComponent<DialogOverlayState>();
            st.Lines = def.lines;
            st.Index = 0;
            st.IsActive = true;
            ResetTypewriterForCurrentLine(st);
        }

        public void Draw()
        {
            if (_font == null) return;
            var e = EntityManager.GetEntity("DialogOverlay");
            var st = e?.GetComponent<DialogOverlayState>();
            if (st == null || !st.IsActive) return;
            if (_cachedLineIndex != st.Index)
            {
                ResetTypewriterForCurrentLine(st);
            }

            int vw = _graphicsDevice.Viewport.Width;
            int vh = _graphicsDevice.Viewport.Height;
            int panelH = (int)System.Math.Round(vh * MathHelper.Clamp(PanelHeightPercent, 0.05f, 0.9f));
            var panelRect = new Rectangle(0, vh - panelH, vw, panelH);

            int r = System.Math.Max(0, System.Math.Min(CornerRadius, System.Math.Min(panelRect.Width, panelRect.Height) / 2));
            bool rebuild = _rounded == null || _cachedW != panelRect.Width || _cachedH != panelRect.Height || _cachedR != r;
            if (rebuild)
            {
                _rounded?.Dispose();
                _rounded = Crusaders30XX.ECS.Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, panelRect.Width, panelRect.Height, r);
                _cachedW = panelRect.Width; _cachedH = panelRect.Height; _cachedR = r;
            }

            // Draw panel background
            var backColor = new Color(0, 0, 0, System.Math.Clamp(PanelAlpha, 0, 255));
            _spriteBatch.Draw(_rounded, panelRect, backColor);

            // Current line
            var line = (st.Index >= 0 && st.Index < (st.Lines?.Count ?? 0)) ? st.Lines[st.Index] : null;
            string actor = line?.actor ?? string.Empty;
            string message = line?.message ?? string.Empty;

            // Left portrait box width
            int portraitW = (int)System.Math.Round(vw * MathHelper.Clamp(PortraitWidthPercent, 0f, 0.5f));

            // Nameplate (red box) above text area, anchored to panel top-left (after portrait pad)
            var nameplateRect = new Rectangle(panelRect.X + portraitW + PanelPadding,
                panelRect.Y + PanelPadding - NameplateHeight,
                System.Math.Max(60, (int)System.Math.Round(_font.MeasureString(actor).X * NameplateScale) + PanelPadding * 2),
                NameplateHeight);
            _spriteBatch.Draw(_pixel, nameplateRect, Color.DarkRed);
            var namePos = new Vector2(nameplateRect.X + PanelPadding / 2f, nameplateRect.Y + nameplateRect.Height / 2f - (_font.MeasureString(actor).Y * NameplateScale) / 2f);
            _spriteBatch.DrawString(_font, actor, namePos, Color.White, 0f, Vector2.Zero, NameplateScale, SpriteEffects.None, 0f);

            // Body text area inside panel, to the right of portrait
            int textX = panelRect.X + portraitW + PanelPadding;
            int textY = panelRect.Y + PanelPadding;
            int textW = panelRect.Width - portraitW - PanelPadding * 2;
            int textH = panelRect.Height - PanelPadding * 2;

            int visLen = System.Math.Min(_revealedChars, _cachedFilteredMessage?.Length ?? 0);
            if (visLen < 0) visLen = 0;
            var lines = BuildStableWrappedVisible(_cachedFilteredMessage ?? string.Empty, visLen, textW);
            float y = textY + (NameplateHeight + PanelPadding * 0.5f);
            foreach (var l in lines)
            {
                _spriteBatch.DrawString(_font, l, new Vector2(textX, y), Color.White, 0f, Vector2.Zero, BodyScale, SpriteEffects.None, 0f);
                y += _font.MeasureString(l).Y * BodyScale;
            }

            // Draw portrait image if available
            Texture2D portrait = ResolvePortrait(actor);
            if (portrait != null)
            {
                int pad = System.Math.Max(0, PanelPadding);
                int availW = portraitW - pad * 2;
                int availH = panelRect.Height - pad * 2;
                if (availW > 0 && availH > 0)
                {
                    float scale = System.Math.Min(availW / (float)portrait.Width, availH / (float)portrait.Height);
                    int drawW = System.Math.Max(1, (int)System.Math.Round(portrait.Width * scale));
                    int drawH = System.Math.Max(1, (int)System.Math.Round(portrait.Height * scale));
                    int drawX = panelRect.X + pad + (portraitW - drawW) / 2;
                    int drawY = panelRect.Y + pad + (panelRect.Height - drawH) / 2;
                    _spriteBatch.Draw(portrait, new Rectangle(drawX, drawY, drawW, drawH), Color.White);
                }
            }
        }

        private Texture2D ResolvePortrait(string actor)
        {
            if (string.IsNullOrWhiteSpace(actor)) return null;
            string key = actor.Trim().ToLowerInvariant();
            try
            {
                if (key == "angel") return _content.Load<Texture2D>("guardian_angel");
                if (key == "crusader") return _content.Load<Texture2D>("Crusader");
            }
            catch { }
            return null;
        }

        private void EnsureOverlayEntity()
        {
            var e = EntityManager.GetEntity("DialogOverlay");
            if (e == null)
            {
                e = EntityManager.CreateEntity("DialogOverlay");
                var t = new Transform { Position = Vector2.Zero, ZOrder = ZOrder };
                var ui = new UIElement { Bounds = new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height), IsInteractable = true };
                EntityManager.AddComponent(e, t);
                EntityManager.AddComponent(e, ui);
                EntityManager.AddComponent(e, new DialogOverlayState());
                EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
            }
            else
            {
                var t = e.GetComponent<Transform>();
                if (t != null) t.ZOrder = ZOrder;
            }
        }

        private void ResetTypewriterForCurrentLine(DialogOverlayState st)
        {
            _cachedLineIndex = st.Index;
            var raw = (st.Index >= 0 && st.Index < (st.Lines?.Count ?? 0)) ? st.Lines[st.Index].message : string.Empty;
            _cachedFilteredMessage = TextUtils.FilterUnsupportedGlyphs(_font, raw ?? string.Empty);
            _revealProgressSec = 0f;
            _revealedChars = 0;
            _lineComplete = _cachedFilteredMessage.Length == 0;
        }

        private List<string> BuildStableWrappedVisible(string fullText, int visibleCharCount, int maxWidth)
        {
            var result = new List<string>();
            if (_font == null || maxWidth <= 0)
            {
                result.Add(string.Empty);
                return result;
            }

            string full = fullText ?? string.Empty;
            // Convert the visible character count to exclude newline characters,
            // since WrapText returns lines without embedded newlines.
            int budget = 0;
            int counted = 0;
            for (int i = 0; i < full.Length && counted < visibleCharCount; i++)
            {
                char ch = full[i];
                counted++;
                if (ch != '\n' && ch != '\r') budget++;
            }

            if (budget <= 0)
            {
                result.Add(string.Empty);
                return result;
            }

            var wrapped = TextUtils.WrapText(_font, full, BodyScale, maxWidth);
            int remaining = budget;
            foreach (var ln in wrapped)
            {
                if (remaining <= 0) break;
                if (ln.Length <= remaining)
                {
                    result.Add(ln);
                    remaining -= ln.Length;
                }
                else
                {
                    result.Add(ln.Substring(0, remaining));
                    remaining = 0;
                    break;
                }
            }
            if (result.Count == 0) result.Add(string.Empty);
            return result;
        }
    }
}


