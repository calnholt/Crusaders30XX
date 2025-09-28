using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using System;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Loadout Deck Panel")]
    public class LoadoutDeckPanelSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly World _world;
        private MouseState _prevMouse;
        private readonly Dictionary<string, int> _cardEntityIds = new Dictionary<string, int>();

        [DebugEditable(DisplayName = "Right Panel Width", Step = 4, Min = 100, Max = 2000)]
        public int PanelWidth { get; set; } = 620;
        [DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 64)]
        public int RowGap { get; set; } = 18;
        [DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 200)]
        public int TopMargin { get; set; } = 30;
        [DebugEditable(DisplayName = "Grid Columns", Step = 1, Min = 1, Max = 6)]
        public int Columns { get; set; } = 3;
        [DebugEditable(DisplayName = "Card Scale", Step = 0.05f, Min = 0.1f, Max = 1.0f)]
        public float CardScale { get; set; } = 0.75f;

        [DebugEditable(DisplayName = "Header Height", Step = 2, Min = 0, Max = 200)]
        public int HeaderHeight { get; set; } = 86;
        [DebugEditable(DisplayName = "Header Text Scale", Step = 0.01f, Min = 0.1f, Max = 2.0f)]
        public float HeaderTextScale { get; set; } = 0.35f;
        [DebugEditable(DisplayName = "Header Pad X", Step = 1, Min = 0, Max = 200)]
        public int HeaderPadX { get; set; } = 12;
        [DebugEditable(DisplayName = "Header Pad Y", Step = 1, Min = 0, Max = 200)]
        public int HeaderPadY { get; set; } = 6;

        public LoadoutDeckPanelSystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
        {
            _world = world;
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _prevMouse = Mouse.GetState();
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<ShowTransition>(_ => OnShowTransition());
        }

        private void OnShowTransition()
        {
            if (_cardEntityIds.Count == 0) return;
            Console.WriteLine("[LoadoutDeckPanelSystem] Clearing cached deck card entities");
            foreach (var entityId in _cardEntityIds.Values)
            {
                EntityManager.DestroyEntity(entityId);
            }
            _cardEntityIds.Clear();
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
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;
            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            int vw = _graphicsDevice.Viewport.Width;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;

            int panelX = vw - PanelWidth;
            int panelY = 0;
            int panelH = _graphicsDevice.Viewport.Height;
            int colW = (int)(cardW * CardScale) + 20;
            int col = Math.Max(1, Columns);

            if (new Rectangle(panelX, panelY, PanelWidth, panelH).Contains(mouse.Position))
            {
                int delta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
                st.RightScroll = Math.Max(0, st.RightScroll - delta / 2);
            }

            // Build sorted view of working cards for consistent draw/click order
            var sortedEntriesForClick = GetSortedWorkingEntries(st);

            // Clamp scroll to content height (so clicks align with visible content)
            int totalItemsForClamp = sortedEntriesForClick.Count;
            int rowsForClamp = Math.Max(0, (totalItemsForClamp + col - 1) / col);
            int cardScaledHForClamp = (int)(cardH * CardScale);
            int gapsTotalForClamp = rowsForClamp > 0 ? (rowsForClamp - 1) * RowGap : 0;
            int contentHeightForClamp = HeaderHeight + TopMargin + rowsForClamp * cardScaledHForClamp + gapsTotalForClamp;
            int maxScrollForClamp = Math.Max(0, contentHeightForClamp - panelH);
            if (st.RightScroll > maxScrollForClamp) st.RightScroll = maxScrollForClamp;

            // Layout for click detection only (drawing handled in Draw)
            int idx = 0;
            foreach (var view in sortedEntriesForClick)
            {
                string id = view.id;
                var color = view.color;
                if (!CardDefinitionCache.TryGet(id, out var def) || def == null) { idx++; continue; }
                if (def.isWeapon) { idx++; continue; }

                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + HeaderHeight + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.RightScroll;

                var rect = new Rectangle(x - (int)(cardW * CardScale / 2), y - (int)(cardH * CardScale / 2), (int)(cardW * CardScale), (int)(cardH * CardScale));
                if (click && rect.Contains(mouse.Position))
                {
                    EventManager.Publish(new RemoveCardFromLoadoutRequested { CardKey = view.key, Index = null });
                    break;
                }
                idx++;
            }
            _prevMouse = Mouse.GetState();
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Deck) return;
            int vw = _graphicsDevice.Viewport.Width;
            int cardW = GetCvs().CardWidth;
            int cardH = GetCvs().CardHeight;
            int panelX = vw - PanelWidth;
            int panelY = 0;
            int colW = (int)(cardW * CardScale) + 20;
            int col = Math.Max(1, Columns);

            var sortedEntries = GetSortedWorkingEntries(st);

            // Clamp scroll to content height
            int totalItems = sortedEntries.Count;
            int rows = Math.Max(0, (totalItems + col - 1) / col);
            int cardScaledH = (int)(cardH * CardScale);
            int gapsTotal = rows > 0 ? (rows - 1) * RowGap : 0;
            int contentHeight = HeaderHeight + TopMargin + rows * cardScaledH + gapsTotal;
            int maxScroll = Math.Max(0, contentHeight - _graphicsDevice.Viewport.Height);
            if (st.RightScroll > maxScroll) st.RightScroll = maxScroll;

            // Background
            int panelH = _graphicsDevice.Viewport.Height;
            var bgRect = new Rectangle(panelX, panelY, PanelWidth, panelH);
            _spriteBatch.Draw(_pixel, bgRect, new Color(0, 0, 0, 160));

            int idx = 0;
            foreach (var view in sortedEntries)
            {
                string id = view.id;
                var color = view.color;
                if (!CardDefinitionCache.TryGet(id, out var def) || def == null) { idx++; continue; }
                if (def.isWeapon) { idx++; continue; }
                var card = EnsureTempCard(def, color);

                int r = idx / col;
                int c = idx % col;
                int x = panelX + c * colW + (colW / 2);
                int y = panelY + HeaderHeight + TopMargin + r * ((int)(cardH * CardScale) + RowGap) + (int)(cardH * CardScale / 2) - st.RightScroll;
                if (card != null)
                {
                    EventManager.Publish(new CardRenderScaledEvent { Card = card, Position = new Vector2(x, y), Scale = CardScale });
                }
                idx++;
            }

            // Header drawn last so it overlays scrolled content
            var headerRect = new Rectangle(panelX, panelY, PanelWidth, HeaderHeight);
            _spriteBatch.Draw(_pixel, headerRect, new Color(30, 30, 30, 220));

            // Resolve loadout name
            string header = "Loadout";
            var defOk = Crusaders30XX.ECS.Data.Loadouts.LoadoutDefinitionCache.TryGet("loadout_1", out var loadoutDef) && loadoutDef != null;
            if (defOk && !string.IsNullOrEmpty(loadoutDef.name)) header = loadoutDef.name;
            if (_font != null)
            {
                var pos = new Vector2(panelX + HeaderPadX, panelY + HeaderPadY);
                _spriteBatch.DrawString(_font, header, pos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, HeaderTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, header, pos, Color.White, 0f, Vector2.Zero, HeaderTextScale, SpriteEffects.None, 0f);
            }
        }

        private Entity EnsureTempCard(CardDefinition def, CardData.CardColor color)
        {
            if (TransitionStateSingleton.IsActive) return null;
            string key = $"{def.id}|{color}";
            if (_cardEntityIds.TryGetValue(key, out var existingId))
            {
                var existing = EntityManager.GetEntity(existingId);
                if (existing != null) return existing;
                _cardEntityIds.Remove(key);
            }

            var created = EntityFactory.CreateCardFromDefinition(_world, def.id, color);
            if (created != null)
            {
                _cardEntityIds[key] = created.Id;
            }
            return created;
        }

        private CardVisualSettings GetCvs()
        {
            return EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>();
        }

        private CardData.CardColor ParseColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return CardData.CardColor.White;
            switch (color.Trim().ToLowerInvariant())
            {
                case "red": return CardData.CardColor.Red;
                case "black": return CardData.CardColor.Black;
                case "white":
                default: return CardData.CardColor.White;
            }
        }

        // Cost parsing no longer needed here; costs are rendered directly from CardDefinition

        private List<(string key, string id, CardData.CardColor color, string name)> GetSortedWorkingEntries(CustomizationState st)
        {
            var result = new List<(string key, string id, CardData.CardColor color, string name)>();
            foreach (var entry in st.WorkingCardIds)
            {
                string id = entry;
                var color = CardData.CardColor.White;
                int sep = entry.IndexOf('|');
                if (sep >= 0)
                {
                    id = entry.Substring(0, sep);
                    var colorKey = entry.Substring(sep + 1);
                    color = ParseColor(colorKey);
                }
                if (!CardDefinitionCache.TryGet(id, out var def) || def == null) continue;
                if (def.isWeapon) continue;
                string name = (def.name ?? def.id) ?? string.Empty;
                result.Add((entry, id, color, name));
            }
            int ColorOrder(CardData.CardColor c)
            {
                switch (c)
                {
                    case CardData.CardColor.White: return 0;
                    case CardData.CardColor.Red: return 1;
                    case CardData.CardColor.Black: return 2;
                    default: return 3;
                }
            }
            result = result
                .OrderBy(t => t.name.ToLowerInvariant())
                .ThenBy(t => ColorOrder(t.color))
                .ToList();
            return result;
        }
    }
}


