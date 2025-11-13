using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Temperance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Events;
using System;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Customize Temperance")]
    public class CustomizeTemperanceDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly Texture2D _pixel;
        private readonly Dictionary<(int w,int h,int r), Texture2D> _roundedCache = new();
        private readonly Dictionary<string, int> _abilityButtonIds = new();

        [DebugEditable(DisplayName = "Row Height", Step = 2, Min = 24, Max = 240)]
        public int RowHeight { get; set; } = 120;

        [DebugEditable(DisplayName = "Left Margin", Step = 2, Min = 0, Max = 200)]
        public int LeftMargin { get; set; } = 10;

        [DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 200)]
        public int TopMargin { get; set; } = 60;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.025f, Min = 0.1f, Max = 1.0f)]
        public float TextScale { get; set; } = 0.13f;

        [DebugEditable(DisplayName = "Name Scale", Step = 0.025f, Min = 0.1f, Max = 1.0f)]
        public float NameScale { get; set; } = 0.17f;

        [DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
        public int CornerRadius { get; set; } = 10;

        [DebugEditable(DisplayName = "ZOrder", Step = 100, Min = 0, Max = 100000)]
        public int ZOrder { get; set; } = 60000;

        public CustomizeTemperanceDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font, CardLibraryPanelSystem libraryPanel, LoadoutDeckPanelSystem deckPanel) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _libraryPanel = libraryPanel;
            _deckPanel = deckPanel;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<ShowTransition>(_ => OnShowTransition());
            EventManager.Subscribe<TemperanceAbilityRenderEvent>(OnTemperanceRender);
        }

        private void OnShowTransition()
        {
            foreach (var id in _abilityButtonIds.Values)
            {
                EntityManager.DestroyEntity(id);
            }
            _abilityButtonIds.Clear();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // var scene = entity.GetComponent<SceneState>();
            // if (scene == null || scene.Current != SceneId.Customization) return;
            // var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            // if (st == null || st.SelectedTab != CustomizationTabType.Temperance) return;

            // // Build available list excluding currently equipped
            // var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            // var equipped = player?.GetComponent<EquippedTemperanceAbility>();
            // string equippedId = equipped?.AbilityId ?? string.Empty;
            // var all = TemperanceAbilityDefinitionCache.GetAll();

            // var layout = ComputeLeftLayout(all.Keys.Where(id => id != equippedId).ToList());
            // foreach (var item in layout)
            // {
            //     var e = EnsureAbilityButton(item.Id, item.Bounds);
            //     var ui = e?.GetComponent<UIElement>();
            //     if (ui != null && ui.IsClicked)
            //     {
            //         if (player != null)
            //         {
            //             if (equipped == null)
            //             {
            //                 equipped = new EquippedTemperanceAbility { AbilityId = item.Id };
            //                 EntityManager.AddComponent(player, equipped);
            //             }
            //             else
            //             {
            //                 equipped.AbilityId = item.Id;
            //             }
            //         }
            //     }
            // }
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Temperance || _font == null) return;

            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var equipped = player?.GetComponent<EquippedTemperanceAbility>();
            string equippedId = equipped?.AbilityId ?? string.Empty;
            TemperanceAbilityDefinition def = null;
            if (!string.IsNullOrEmpty(equippedId)) TemperanceAbilityDefinitionCache.TryGet(equippedId, out def);

            // Left side now published by AvailableTemperanceDisplaySystem via events

            // Right side: equipped card block in loadout panel
            if (def != null)
            {
                int vw = _graphicsDevice.Viewport.Width;
                int rightW = _deckPanel?.PanelWidth ?? 620;
                int x = vw - rightW + LeftMargin;
                int y = TopMargin + 40;
                int w = rightW - LeftMargin * 2;
                int h = Math.Min(260, _graphicsDevice.Viewport.Height - y - 20);
                var rect = new Rectangle(x, y, w, h);
                // Publish render event; handler will draw during active SpriteBatch (mimics CardDisplaySystem)
                EventManager.Publish(new TemperanceAbilityRenderEvent { AbilityId = def.id, Bounds = rect, IsEquipped = true, NameScale = NameScale, TextScale = TextScale });
            }
        }

        private void OnTemperanceRender(TemperanceAbilityRenderEvent e)
        {
            // Only render during our Draw pass to ensure SpriteBatch.Begin is active
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Temperance) return;
            if (_font == null) return;
            if (!TemperanceAbilityDefinitionCache.TryGet(e.AbilityId, out var def) || def == null) return;
            var r = e.Bounds;
            var rounded = GetRounded(r.Width, r.Height, CornerRadius);
            var bg = Color.White;
            var fg = Color.Black;
            _spriteBatch.Draw(rounded, r, bg);
            string title = (def.name ?? def.id) + " - " + def.threshold.ToString();
            var tsize = _font.MeasureString(title) * e.NameScale;
            var tpos = new Vector2(r.X + 10, r.Y + 8);
            _spriteBatch.DrawString(_font, title, tpos + new Vector2(1,1), Color.Black, 0f, Vector2.Zero, e.NameScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, title, tpos, fg, 0f, Vector2.Zero, e.NameScale, SpriteEffects.None, 0f);
            string body = def.text ?? string.Empty;
            int contentW = Math.Max(10, r.Width - 20);
            float y2 = tpos.Y + (tsize.Y) + 6f;
            foreach (var line in WrapText(body, e.TextScale, contentW))
            {
                _spriteBatch.DrawString(_font, line, new Vector2(r.X + 10, y2), fg, 0f, Vector2.Zero, e.TextScale, SpriteEffects.None, 0f);
                y2 += _font.LineSpacing * e.TextScale;
                if (y2 > r.Bottom - 8) break;
            }
        }

        private List<(string Id, Rectangle Bounds)> ComputeLeftLayout(List<string> ids)
        {
            int panelW = _libraryPanel?.PanelWidth ?? 640;
            int x = 0 + LeftMargin;
            int y = TopMargin + 40;
            int w = panelW - LeftMargin * 2;
            var list = new List<(string, Rectangle)>();
            for (int i = 0; i < ids.Count; i++)
            {
                var r = new Rectangle(x, y + i * (RowHeight + 10), w, RowHeight);
                list.Add((ids[i], r));
            }
            return list;
        }

        private Entity EnsureAbilityButton(string id, Rectangle bounds)
        {
            if (!_abilityButtonIds.TryGetValue(id, out var entId) || EntityManager.GetEntity(entId) == null)
            {
                var e = EntityManager.CreateEntity($"Temp_Ability_{id}");
                EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = ZOrder });
                _abilityButtonIds[id] = e.Id;
                return e;
            }
            var existing = EntityManager.GetEntity(entId);
            var ui = existing.GetComponent<UIElement>();
            if (ui != null) ui.Bounds = bounds;
            var tr = existing.GetComponent<Transform>();
            if (tr != null) tr.ZOrder = ZOrder;
            return existing;
        }

        private Texture2D GetRounded(int w, int h, int r)
        {
            var key = (w: w, h: h, r: Math.Max(0, Math.Min(r, Math.Min(w, h) / 2)));
            if (_roundedCache.TryGetValue((key.w, key.h, key.r), out var tex) && tex != null) return tex;
            tex = Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, key.r);
            _roundedCache[(key.w, key.h, key.r)] = tex;
            return tex;
        }

        private List<string> WrapText(string text, float scale, int maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text) || _font == null)
            {
                lines.Add(string.Empty);
                return lines;
            }
            string[] words = text.Replace("\r", "").Split('\n');
            foreach (var rawLine in words)
            {
                string line = string.Empty;
                foreach (var word in rawLine.Split(' '))
                {
                    string test = string.IsNullOrEmpty(line) ? word : (line + " " + word);
                    float w = _font.MeasureString(test).X * scale;
                    if (w > maxWidth && !string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                        line = word;
                    }
                    else
                    {
                        line = test;
                    }
                }
                lines.Add(line);
            }
            return lines;
        }
    }
}


