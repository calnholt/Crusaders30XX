using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Available Equipment")]
    public class AvailableEquipmentDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private readonly CustomizeEquipmentDisplaySystem _customizeEquipmentDisplaySystem;
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

        public AvailableEquipmentDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, CardLibraryPanelSystem libraryPanel, CustomizeEquipmentDisplaySystem customizeEquipmentDisplaySystem) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _libraryPanel = libraryPanel;
            _customizeEquipmentDisplaySystem = customizeEquipmentDisplaySystem;
            _prevMouse = Mouse.GetState();
            EventManager.Subscribe<ShowTransition>(_ => ClearEntities());
            EventManager.Subscribe<SetCustomizationTab>(_ => ClearEntities());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            if (StateSingleton.IsActive) return;
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            if (!IsEquipmentTab(st.SelectedTab)) return;

            string equippedId = GetEquippedIdForTab(st, st.SelectedTab) ?? string.Empty;
            string slot = GetSlotName(st.SelectedTab);

            var collection = SaveCache.GetCollectionSet();
            var all = EquipmentDefinitionCache.GetAll().Values
                .Where(d => string.Equals((d.slot ?? string.Empty).Trim(), slot, System.StringComparison.OrdinalIgnoreCase))
                .Where(d => collection.Contains(d.id))
                .Where(d => (d.id ?? string.Empty) != equippedId)
                .OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
                .ToList();

            int x = 0 + LeftPadding;
            int yBase = _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + TopOffsetFromHeader;
            int w = _libraryPanel.PanelWidth - (SidePadding * 2);
            int h = RowHeight;
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
                var ebtn = EnsureEntity(d.id, bounds);
                if (click && bounds.Contains(mouse.Position))
                {
                    EventManager.Publish(new UpdateEquipmentLoadoutRequested { Slot = st.SelectedTab, EquipmentId = d.id });
                }
            }
            _prevMouse = mouse;
        }

        public void Draw()
        {
            if (StateSingleton.IsActive) return;
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            if (!IsEquipmentTab(st.SelectedTab)) return;

            string equippedId = GetEquippedIdForTab(st, st.SelectedTab) ?? string.Empty;
            string slot = GetSlotName(st.SelectedTab);

            var collection = SaveCache.GetCollectionSet();
            var all = EquipmentDefinitionCache.GetAll().Values
                .Where(d => string.Equals((d.slot ?? string.Empty).Trim(), slot, System.StringComparison.OrdinalIgnoreCase))
                .Where(d => collection.Contains(d.id))
                .Where(d => (d.id ?? string.Empty) != equippedId)
                .OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
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
                EventManager.Publish(new EquipmentRenderEvent { EquipmentId = d.id, Bounds = bounds, IsEquipped = false, NameScale = _customizeEquipmentDisplaySystem.NameScale, TextScale = _customizeEquipmentDisplaySystem.TextScale });
            }
        }

        private Entity EnsureEntity(string id, Rectangle bounds)
        {
            if (!_entityIds.TryGetValue(id, out var entId) || EntityManager.GetEntity(entId) == null)
            {
                var e = EntityManager.CreateEntity($"CustomizationEquipment_Available_{id}");
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

        private void ClearEntities()
        {
            if (_entityIds.Count == 0) return;
            foreach (var id in _entityIds.Values)
            {
                EntityManager.DestroyEntity(id);
            }
            _entityIds.Clear();
        }

        private bool IsEquipmentTab(CustomizationTabType tab)
        {
            return tab == CustomizationTabType.Weapon || tab == CustomizationTabType.Head || tab == CustomizationTabType.Chest || tab == CustomizationTabType.Arms || tab == CustomizationTabType.Legs;
        }

        private string GetSlotName(CustomizationTabType tab)
        {
            switch (tab)
            {
                case CustomizationTabType.Weapon: return "Weapon";
                case CustomizationTabType.Head: return "Head";
                case CustomizationTabType.Chest: return "Chest";
                case CustomizationTabType.Arms: return "Arms";
                case CustomizationTabType.Legs: return "Legs";
                default: return string.Empty;
            }
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



