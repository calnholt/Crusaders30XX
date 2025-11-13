using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Customize Equipment")]
    public class CustomizeEquipmentDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly Texture2D _pixel;
        private readonly Dictionary<(int w,int h,int r), Texture2D> _roundedCache = new();

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

        public CustomizeEquipmentDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font, CardLibraryPanelSystem libraryPanel, LoadoutDeckPanelSystem deckPanel) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _libraryPanel = libraryPanel;
            _deckPanel = deckPanel;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<ShowTransition>(_ => { /* nothing persistent here */ });
            EventManager.Subscribe<EquipmentRenderEvent>(OnEquipmentRender);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No stateful UI elements; left/right lists are driven by Available/Loadout systems
        }

        private void OnEquipmentRender(EquipmentRenderEvent e)
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || !IsEquipmentTab(st.SelectedTab)) return;
            if (_font == null) return;
            if (!EquipmentDefinitionCache.TryGet(e.EquipmentId, out var def) || def == null) return;

            var r = e.Bounds;
            var rounded = GetRounded(r.Width, r.Height, CornerRadius);
            _spriteBatch.Draw(rounded, r, Color.White);
            string title = (def.name ?? def.id);
            var tsize = _font.MeasureString(title) * e.NameScale;
            var tpos = new Vector2(r.X + 10, r.Y + 8);
            _spriteBatch.DrawString(_font, title, tpos + new Vector2(1,1), Color.Black, 0f, Vector2.Zero, e.NameScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, title, tpos, Color.Black, 0f, Vector2.Zero, e.NameScale, SpriteEffects.None, 0f);

            // Body: list ability texts, line-wrapped
            int contentW = System.Math.Max(10, r.Width - 20);
            float y2 = tpos.Y + (tsize.Y) + 6f;
            var bodyLines = BuildAbilityTextLines(def);
            foreach (var line in WrapText(bodyLines, e.TextScale, contentW))
            {
                _spriteBatch.DrawString(_font, line, new Vector2(r.X + 10, y2), Color.Black, 0f, Vector2.Zero, e.TextScale, SpriteEffects.None, 0f);
                y2 += _font.LineSpacing * e.TextScale;
                if (y2 > r.Bottom - 8) break;
            }
        }

        private Texture2D GetRounded(int w, int h, int r)
        {
            var key = (w: w, h: h, r: System.Math.Max(0, System.Math.Min(r, System.Math.Min(w, h) / 2)));
            if (_roundedCache.TryGetValue((key.w, key.h, key.r), out var tex) && tex != null) return tex;
            tex = Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, key.r);
            _roundedCache[(key.w, key.h, key.r)] = tex;
            return tex;
        }

        private string BuildAbilityTextLines(EquipmentDefinition def)
        {
            if (def?.abilities == null || def.abilities.Count == 0) return string.Empty;
            var parts = new System.Text.StringBuilder();
            for (int i = 0; i < def.abilities.Count; i++)
            {
                var a = def.abilities[i];
                if (a == null) continue;
                string line = a.text ?? string.Empty;
                if (a.type == "Activate")
                {
                    string cost = a.isFreeAction ? "free action" : "1AP";
                    line = $"Activate ({cost}): " + line;
                    if (a.requiresUseOnActivate) line += " Lose one use.";
                    if (a.destroyOnActivate) line += " Destroy this.";
                }
                parts.Append(line);
                if (i < def.abilities.Count - 1) parts.Append("\n\n");
            }
            return parts.ToString();
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

        private bool IsEquipmentTab(CustomizationTabType tab)
        {
            return tab == CustomizationTabType.Weapon || tab == CustomizationTabType.Head || tab == CustomizationTabType.Chest || tab == CustomizationTabType.Arms || tab == CustomizationTabType.Legs;
        }

        private string GetEquippedIdForTab(CustomizationState st, CustomizationTabType tab)
        {
            switch (tab)
            {
                case CustomizationTabType.Weapon: return st.WorkingWeaponId;
                case CustomizationTabType.Head: return st.WorkingHeadId;
                case CustomizationTabType.Chest: return st.WorkingChestId;
                case CustomizationTabType.Arms: return st.WorkingArmsId;
                case CustomizationTabType.Legs: return st.WorkingLegsId;
                default: return string.Empty;
            }
        }
    }
}


