using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Temperance;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using System;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Available Temperance")]
    public class AvailableTemperanceDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private readonly CustomizeTemperanceDisplaySystem _customizeTemperanceDisplaySystem;
        private readonly Dictionary<string, int> _entityIds = new();
        private MouseState _prevMouse;

        [DebugEditable(DisplayName = "Row Height", Step = 2, Min = 24, Max = 240)]
        public int RowHeight { get; set; } = 120;
        [DebugEditable(DisplayName = "Item Spacing", Step = 1, Min = 0, Max = 64)]
        public int ItemSpacing { get; set; } = 10;
        [DebugEditable(DisplayName = "Left Padding", Step = 1, Min = 0, Max = 200)]
        public int LeftPadding { get; set; } = 10;
        [DebugEditable(DisplayName = "Side Padding", Step = 1, Min = 0, Max = 200)]
        public int SidePadding { get; set; } = 10;
        [DebugEditable(DisplayName = "Top Offset From Header", Step = 1, Min = 0, Max = 200)]
        public int TopOffsetFromHeader { get; set; } = 0;

        public AvailableTemperanceDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, CardLibraryPanelSystem libraryPanel, CustomizeTemperanceDisplaySystem customizeTemperanceDisplaySystem) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _libraryPanel = libraryPanel;
            _customizeTemperanceDisplaySystem = customizeTemperanceDisplaySystem;
            _prevMouse = Mouse.GetState();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            if (TransitionStateSingleton.IsActive) return;
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Temperance) return;

            var equippedId = st.WorkingTemperanceId ?? string.Empty;

            var all = TemperanceAbilityDefinitionCache.GetAll().Values
                .Where(d => d.id != equippedId)
                .OrderBy(d => (d.name ?? d.id) ?? string.Empty)
                .ToList();

            int x = 0 + LeftPadding;
            int yBase = _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + TopOffsetFromHeader;
            int w = _libraryPanel.PanelWidth - (SidePadding * 2);
            int h = RowHeight;
            // Prune entities no longer visible (e.g., after selection changes)
            var visibleIds = new HashSet<string>(all.Select(d => d.id));
            var stale = _entityIds.Keys.Where(k => !visibleIds.Contains(k)).ToList();
            foreach (var sid in stale)
            {
                var eid = _entityIds[sid];
                EntityManager.DestroyEntity(eid);
                _entityIds.Remove(sid);
            }
            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                int y = yBase + i * (h + ItemSpacing) - st.LeftScroll;
                var bounds = new Rectangle(x, y, w, h);
                var e = EnsureEntity(d.id, bounds);
                if (click && bounds.Contains(mouse.Position))
                {
                    EventManager.Publish(new UpdateTemperanceLoadoutRequested { TemperanceId = d.id });
                }
            }
            _prevMouse = mouse;
        }

        public void Draw()
        {
            if (TransitionStateSingleton.IsActive) return;
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Temperance) return;

            string equippedId = st.WorkingTemperanceId ?? string.Empty;

            var all = TemperanceAbilityDefinitionCache.GetAll().Values
                .Where(d => d.id != equippedId)
                .OrderBy(d => (d.name ?? d.id) ?? string.Empty)
                .ToList();

            int x = 0 + LeftPadding;
            int yBase = _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + TopOffsetFromHeader;
            int w = _libraryPanel.PanelWidth - (SidePadding * 2);
            int h = RowHeight;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                int y = yBase + i * (h + ItemSpacing) - st.LeftScroll;
                var bounds = new Rectangle(x, y, w, h);
                EventManager.Publish(new TemperanceAbilityRenderEvent { AbilityId = d.id, Bounds = bounds, IsEquipped = false, NameScale = _customizeTemperanceDisplaySystem.NameScale, TextScale = _customizeTemperanceDisplaySystem.TextScale });
            }
        }

        private Entity EnsureEntity(string id, Rectangle bounds)
        {
            if (!_entityIds.TryGetValue(id, out var entId) || EntityManager.GetEntity(entId) == null)
            {
                var e = EntityManager.CreateEntity($"CustomizationTemperance_Available_{id}");
                EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 60000 });
                _entityIds[id] = e.Id;
                return e;
            }
            var ex = EntityManager.GetEntity(_entityIds[id]);
            var ui = ex.GetComponent<UIElement>();
            if (ui != null) ui.Bounds = bounds;
            return ex;
        }
    }
}


