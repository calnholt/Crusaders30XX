using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using System;

namespace Crusaders30XX.ECS.Systems
{
    public class LoadoutMedalDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly CustomizeMedalDisplaySystem _customizeMedalDisplay;
        private readonly Dictionary<string, int> _entityByKey = new();
        private CursorStateEvent _cursorEvent;

        [DebugEditable(DisplayName = "Row Height", Step = 2, Min = 24, Max = 240)]
        public int RowHeight { get; set; } = 120;
        [DebugEditable(DisplayName = "Left Padding", Step = 1, Min = 0, Max = 200)]
        public int LeftPadding { get; set; } = 10;
        [DebugEditable(DisplayName = "Side Padding", Step = 1, Min = 0, Max = 200)]
        public int SidePadding { get; set; } = 10;
        [DebugEditable(DisplayName = "Top Offset From Header", Step = 1, Min = 0, Max = 200)]
        public int TopOffsetFromHeader { get; set; } = 0;
        [DebugEditable(DisplayName = "Item Spacing", Step = 1, Min = 0, Max = 64)]
        public int ItemSpacing { get; set; } = 10;

        public LoadoutMedalDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, LoadoutDeckPanelSystem deckPanel, CustomizeMedalDisplaySystem customizeMedalDisplay) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _deckPanel = deckPanel;
            _customizeMedalDisplay = customizeMedalDisplay;
            EventManager.Subscribe<ShowTransition>(_ => ClearEntities());
            EventManager.Subscribe<SetCustomizationTab>(_ => ClearEntities());
            EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
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
            if (st == null || st.SelectedTab != CustomizationTabType.Medals) return;

            int vw = _graphicsDevice.Viewport.Width;
            int rightW = _deckPanel?.PanelWidth ?? 620;
            int x = vw - rightW + LeftPadding;
            int yBase = _deckPanel.HeaderHeight + _deckPanel.TopMargin + TopOffsetFromHeader;
            int w = rightW - (SidePadding * 2);
            int h = RowHeight;

            bool click = _cursorEvent != null && _cursorEvent.IsAPressedEdge;
            var clickPoint = click ? new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y)) : Point.Zero;
            var working = st.WorkingMedalIds ?? new List<string>();

            // prune stale
            var visibleKeys = new HashSet<string>(Enumerable.Range(0, working.Count).Select(i => $"{i}|{working[i]}"));
            var stale = _entityByKey.Keys.Where(k => !visibleKeys.Contains(k)).ToList();
            foreach (var k in stale)
            {
                var eid = _entityByKey[k];
                EntityManager.DestroyEntity(eid);
                _entityByKey.Remove(k);
            }

            for (int i = 0; i < working.Count; i++)
            {
                string id = working[i];
                int y = yBase + i * (h + ItemSpacing) - st.RightScroll;
                var bounds = new Rectangle(x, y, w, h);
                var e2 = EnsureEntity(i, id, bounds);
                if (click && bounds.Contains(clickPoint))
                {
                    EventManager.Publish(new RemoveMedalFromLoadoutRequested { MedalId = id, Index = i });
                }
            }
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Medals) return;

            int vw = _graphicsDevice.Viewport.Width;
            int rightW = _deckPanel?.PanelWidth ?? 620;
            int x = vw - rightW + LeftPadding;
            int yBase = _deckPanel.HeaderHeight + _deckPanel.TopMargin + TopOffsetFromHeader;
            int w = rightW - (SidePadding * 2);
            int h = RowHeight;

            var working = st.WorkingMedalIds ?? new List<string>();
            for (int i = 0; i < working.Count; i++)
            {
                int y = yBase + i * (h + ItemSpacing) - st.RightScroll;
                var bounds = new Rectangle(x, y, w, h);
                EventManager.Publish(new MedalRenderEvent { MedalId = working[i], Bounds = bounds, IsEquipped = true, NameScale = _customizeMedalDisplay.NameScale, TextScale = _customizeMedalDisplay.TextScale });
            }
        }

        private Entity EnsureEntity(int index, string id, Rectangle bounds)
        {
            string key = $"{index}|{id}";
            if (!_entityByKey.TryGetValue(key, out var entId) || EntityManager.GetEntity(entId) == null)
            {
                var e = EntityManager.CreateEntity($"CustomizationMedal_Equipped_{index}_{id}");
                EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 60000 });
                _entityByKey[key] = e.Id;
                return e;
            }
            var ex = EntityManager.GetEntity(_entityByKey[key]);
            var ui = ex.GetComponent<UIElement>();
            if (ui != null) ui.Bounds = bounds;
            return ex;
        }

        private void ClearEntities()
        {
            if (_entityByKey.Count == 0) return;
            foreach (var id in _entityByKey.Values)
            {
                EntityManager.DestroyEntity(id);
            }
            _entityByKey.Clear();
        }
    }
}




