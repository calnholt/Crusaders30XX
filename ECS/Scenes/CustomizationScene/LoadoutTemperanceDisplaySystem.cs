using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Temperance;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    public class LoadoutTemperanceDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly LoadoutDeckPanelSystem _deckPanel;
        private readonly CustomizeTemperanceDisplaySystem _customizeTemperanceDisplaySystem;
        private int _entityId = 0;

        [DebugEditable(DisplayName = "Row Height", Step = 2, Min = 24, Max = 240)]
        public int RowHeight { get; set; } = 120;
        [DebugEditable(DisplayName = "Left Padding", Step = 1, Min = 0, Max = 200)]
        public int LeftPadding { get; set; } = 10;
        [DebugEditable(DisplayName = "Side Padding", Step = 1, Min = 0, Max = 200)]
        public int SidePadding { get; set; } = 10;
        [DebugEditable(DisplayName = "Top Offset From Header", Step = 1, Min = 0, Max = 200)]
        public int TopOffsetFromHeader { get; set; } = 0;
        [DebugEditable(DisplayName = "Name Scale", Step = 0.01f, Min = 0.1f, Max = 1.0f)]
        public float NameScale { get; set; } = 0.22f;
        [DebugEditable(DisplayName = "Text Scale", Step = 0.01f, Min = 0.1f, Max = 1.0f)]
        public float BodyTextScale { get; set; } = 0.18f;

        public LoadoutTemperanceDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, LoadoutDeckPanelSystem deckPanel, CustomizeTemperanceDisplaySystem customizeTemperanceDisplaySystem) : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _deckPanel = deckPanel;
            _customizeTemperanceDisplaySystem = customizeTemperanceDisplaySystem;
            // Clear cached UI entity on scene transitions or tab switches
            EventManager.Subscribe<ShowTransition>(_ => ClearEntity());
            EventManager.Subscribe<SetCustomizationTab>(_ => ClearEntity());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No-op; event publish occurs during Draw to ensure SpriteBatch is active
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null || st.SelectedTab != CustomizationTabType.Temperance) return;

            var equippedId = st.WorkingTemperanceId;
            if (string.IsNullOrEmpty(equippedId)) return;
            if (!TemperanceAbilityDefinitionCache.TryGet(equippedId, out var def) || def == null) return;

            int vw = Game1.VirtualWidth;
            int rightW = _deckPanel?.PanelWidth ?? 620;
            int x = vw - rightW + LeftPadding;
            int y = _deckPanel.HeaderHeight + _deckPanel.TopMargin + TopOffsetFromHeader;
            int w = rightW - (SidePadding * 2);
            int h = RowHeight;
            var rect = new Rectangle(x, y, w, h);
            EnsureEntity(rect);
            EventManager.Publish(new TemperanceAbilityRenderEvent { AbilityId = def.id, Bounds = rect, IsEquipped = true, NameScale = _customizeTemperanceDisplaySystem.NameScale, TextScale = _customizeTemperanceDisplaySystem.TextScale });
        }

        private Entity EnsureEntity(Rectangle bounds)
        {
            if (_entityId == 0 || EntityManager.GetEntity(_entityId) == null)
            {
                var e = EntityManager.CreateEntity("CustomizationTemperance_Equipped");
                EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = false });
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 60000 });
                _entityId = e.Id;
                return e;
            }
            var ex = EntityManager.GetEntity(_entityId);
            var ui = ex.GetComponent<UIElement>();
            if (ui != null) ui.Bounds = bounds;
            return ex;
        }

        private void ClearEntity()
        {
            if (_entityId != 0)
            {
                EntityManager.DestroyEntity(_entityId);
                _entityId = 0;
            }
        }
    }
}


