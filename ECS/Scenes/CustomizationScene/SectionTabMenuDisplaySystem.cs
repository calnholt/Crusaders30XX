using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Customization Tabs")]
    public class SectionTabMenuDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly Texture2D _pixel;
        private readonly Dictionary<CustomizationTabType, int> _tabIds = new Dictionary<CustomizationTabType, int>();

        [DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 200)]
        public int TopMargin { get; set; } = 18;

        [DebugEditable(DisplayName = "Button Height", Step = 2, Min = 18, Max = 140)]
        public int ButtonHeight { get; set; } = 64;

        [DebugEditable(DisplayName = "Horizontal Spacing", Step = 2, Min = 0, Max = 80)]
        public int Spacing { get; set; } = 16;

        [DebugEditable(DisplayName = "Button Padding X", Step = 2, Min = 0, Max = 80)]
        public int PadX { get; set; } = 26;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.025f, Min = 0.1f, Max = 1.0f)]
        public float TextScale { get; set; } = 0.15f;

        [DebugEditable(DisplayName = "Row Spacing", Step = 1, Min = 0, Max = 100)]
        public int RowSpacing { get; set; } = 7;

        [DebugEditable(DisplayName = "ZOrder", Step = 100, Min = 0, Max = 100000)]
        public int ZOrder { get; set; } = 60000;

        public SectionTabMenuDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font, CardLibraryPanelSystem libraryPanel, LoadoutDeckPanelSystem deckPanel) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _libraryPanel = libraryPanel;
            _deckPanel = deckPanel;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<ShowTransition>(_ => OnShowTransition());
        }

        private void OnShowTransition()
        {
            foreach (var id in _tabIds.Values)
            {
                EntityManager.DestroyEntity(id);
            }
            _tabIds.Clear();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;

            var layout = ComputeLayout();
            foreach (var item in layout)
            {
                var e = EnsureTabEntity(item.Type, item.Bounds);
                var ui = e?.GetComponent<UIElement>();
                if (ui != null && ui.IsClicked)
                {
                    EventManager.Publish(new SetCustomizationTab { Tab = item.Type });
                }
            }
        }

        public void Draw()
        {
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || _font == null) return;

            var layout = ComputeLayout();
            foreach (var item in layout)
            {
                bool selected = st.SelectedTab == item.Type;
                var bg = selected ? Color.White : Color.Black;
                var fg = selected ? Color.Black : Color.White;
                _spriteBatch.Draw(_pixel, item.Bounds, bg);
                string label = item.Label;
                var size = _font.MeasureString(label) * TextScale;
                var pos = new Vector2(
                    item.Bounds.X + (item.Bounds.Width - size.X) / 2f,
                    item.Bounds.Y + (item.Bounds.Height - size.Y) / 2f
                );
                _spriteBatch.DrawString(_font, label, pos + new Vector2(1,1), selected ? Color.Gray : Color.Black, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, label, pos, fg, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
            }
        }

        private (int left, int width, int y) ComputeCenterStrip()
        {
            int vw = Game1.VirtualWidth;
            int left = _libraryPanel?.PanelWidth ?? 640;
            int right = _deckPanel?.PanelWidth ?? 620;
            int x0 = left;
            int x1 = System.Math.Max(x0, vw - right);
            int width = System.Math.Max(0, x1 - x0);
            int y = TopMargin;
            return (x0, width, y);
        }

        private List<(CustomizationTabType Type, Rectangle Bounds, string Label)> ComputeLayout()
        {
            var strip = ComputeCenterStrip();
            var types = System.Enum.GetValues(typeof(Crusaders30XX.ECS.Components.CustomizationTabType))
                .Cast<CustomizationTabType>()
                .ToList();

            // Split into two rows (roughly equal halves)
            int half = (types.Count + 1) / 2;
            var row1 = types.Take(half).ToList();
            var row2 = types.Skip(half).ToList();

            // Measure widths for each row
            (int totalW, int[] widths) MeasureRow(System.Collections.Generic.List<CustomizationTabType> row)
            {
                int[] w = new int[row.Count];
                int total = 0;
                for (int i = 0; i < row.Count; i++)
                {
                    string label = GetLabel(row[i]);
                    int wi = (int)System.Math.Ceiling((_font?.MeasureString(label).X ?? 0f) * TextScale) + PadX * 2;
                    w[i] = wi;
                    total += wi;
                    if (i > 0) total += Spacing;
                }
                return (total, w);
            }

            var (total1, widths1) = MeasureRow(row1);
            var (total2, widths2) = MeasureRow(row2);

            int y1 = strip.y;
            int y2 = strip.y + ButtonHeight + RowSpacing;
            int startX1 = strip.left + System.Math.Max(0, (strip.width - total1) / 2);
            int startX2 = strip.left + System.Math.Max(0, (strip.width - total2) / 2);

            var list = new List<(CustomizationTabType, Rectangle, string)>();
            int x = startX1;
            for (int i = 0; i < row1.Count; i++)
            {
                string label = GetLabel(row1[i]);
                var bounds = new Rectangle(x, y1, widths1[i], ButtonHeight);
                list.Add((row1[i], bounds, label));
                x += widths1[i] + Spacing;
            }
            x = startX2;
            for (int i = 0; i < row2.Count; i++)
            {
                string label = GetLabel(row2[i]);
                var bounds = new Rectangle(x, y2, widths2[i], ButtonHeight);
                list.Add((row2[i], bounds, label));
                x += widths2[i] + Spacing;
            }
            return list;
        }

        private string GetLabel(CustomizationTabType t)
        {
            switch (t)
            {
                case CustomizationTabType.Deck: return "Deck";
                case CustomizationTabType.Weapon: return "Weapon";
                case CustomizationTabType.Head: return "Head";
                case CustomizationTabType.Chest: return "Chest";
                case CustomizationTabType.Arms: return "Arms";
                case CustomizationTabType.Legs: return "Legs";
                case CustomizationTabType.Temperance: return "Temperance";
                case CustomizationTabType.Medals: return "Medals";
                default: return t.ToString();
            }
        }

        private Entity EnsureTabEntity(CustomizationTabType type, Rectangle bounds)
        {
            if (!_tabIds.TryGetValue(type, out var id) || EntityManager.GetEntity(id) == null)
            {
                var e = EntityManager.CreateEntity($"CustomizationTab_{type}");
                EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = ZOrder });
                _tabIds[type] = e.Id;
                return e;
            }
            var existing = EntityManager.GetEntity(id);
            var ui = existing.GetComponent<UIElement>();
            if (ui != null) ui.Bounds = bounds;
            var tr = existing.GetComponent<Transform>();
            if (tr != null) tr.ZOrder = ZOrder;
            return existing;
        }
    }
}


