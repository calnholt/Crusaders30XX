using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Save;
using System;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Available Medals")]
    public class AvailableMedalDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly CardLibraryPanelSystem _libraryPanel;
        private readonly CustomizeMedalDisplaySystem _customizeMedalDisplay;
        private readonly Dictionary<string, int> _entityIds = new();
        private CursorStateEvent _cursorEvent;

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

        public AvailableMedalDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, CardLibraryPanelSystem libraryPanel, CustomizeMedalDisplaySystem customizeMedalDisplay) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _libraryPanel = libraryPanel;
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
            if (StateSingleton.IsActive) return;
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Medals) return;

            var collection = SaveCache.GetCollectionSet();
            var equipped = st.WorkingMedalIds ?? new List<string>();
            var all = MedalFactory.GetAllMedals().Values
                .Where(d => collection.Contains(d.Id))
                .Where(d => !equipped.Contains(d.Id))
                .OrderBy(d => (d.Name ?? d.Id) ?? string.Empty)
                .ToList();

            int x = 0 + LeftPadding;
            int yBase = _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + TopOffsetFromHeader;
            int w = _libraryPanel.PanelWidth - (SidePadding * 2);
            int h = RowHeight;

            var visibleIds = new HashSet<string>(all.Select(d => d.Id));
            var stale = _entityIds.Keys.Where(k => !visibleIds.Contains(k)).ToList();
            foreach (var sid in stale)
            {
                var eid = _entityIds[sid];
                EntityManager.DestroyEntity(eid);
                _entityIds.Remove(sid);
            }

            bool click = _cursorEvent != null && _cursorEvent.IsAPressedEdge;
            var clickPoint = click ? new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y)) : Point.Zero;
            bool canAdd = (st.WorkingMedalIds?.Count ?? 0) < 3;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                int y = yBase + i * (h + ItemSpacing) - st.LeftScroll;
                var bounds = new Rectangle(x, y, w, h);
                var e2 = EnsureEntity(d.Id, bounds, interactable: canAdd);
                var ui = e2?.GetComponent<UIElement>();
                // Check both CursorStateEvent bounds and UIElement.IsClicked for robustness
                bool clicked = (click && bounds.Contains(clickPoint)) || (ui != null && ui.IsClicked);
                if (canAdd && clicked)
                {
                    EventManager.Publish(new AddMedalToLoadoutRequested { MedalId = d.Id });
                }
            }
        }

        public void Draw()
        {
            if (StateSingleton.IsActive) return;
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Medals) return;

            var collection = SaveCache.GetCollectionSet();
            var equipped = st.WorkingMedalIds ?? new List<string>();
                var all = MedalFactory.GetAllMedals().Values
                .Where(d => collection.Contains(d.Id))
                .Where(d => !equipped.Contains(d.Id))
                .OrderBy(d => (d.Name ?? d.Id) ?? string.Empty)
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
                EventManager.Publish(new MedalRenderEvent { MedalId = d.Id, Bounds = bounds, IsEquipped = false, NameScale = _customizeMedalDisplay.NameScale, TextScale = _customizeMedalDisplay.TextScale });
            }
        }

        private Entity EnsureEntity(string id, Rectangle bounds, bool interactable)
        {
            if (!_entityIds.TryGetValue(id, out var entId) || EntityManager.GetEntity(entId) == null)
            {
                var e = EntityManager.CreateEntity($"CustomizationMedal_Available_{id}");
                EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = interactable });
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 60000 });
                _entityIds[id] = e.Id;
                return e;
            }
            var ex = EntityManager.GetEntity(_entityIds[id]);
            var ui = ex.GetComponent<UIElement>();
            if (ui != null)
            {
                ui.Bounds = bounds;
                ui.IsInteractable = interactable;
            }
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
    }
}




