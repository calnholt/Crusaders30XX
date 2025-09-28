using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    public class CustomizationRootSystem : Core.System
    {
        private readonly SystemManager _systemManager;
        private readonly World _world;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ContentManager _content;
        private readonly SpriteFont _font;

        private CustomizationSceneSystem _customizationSceneSystem;
        private CardLibraryPanelSystem _libraryPanelSystem;
        private LoadoutDeckPanelSystem _deckPanelSystem;
        private LoadoutEditSystem _loadoutEditSystem;
        private DeckInfoDisplaySystem _deckInfoDisplaySystem;
        private CustomizationBackgroundSystem _backgroundSystem;
        private SectionTabMenuDisplaySystem _sectionTabMenuDisplaySystem;
        private CustomizeTemperanceDisplaySystem _customizeTemperanceDisplaySystem;
        private AvailableCardDisplaySystem _availableCardDisplaySystem;
        private LoadoutCardDisplaySystem _loadoutCardDisplaySystem;
        private AvailableTemperanceDisplaySystem _availableTemperanceDisplaySystem;
        private LoadoutTemperanceDisplaySystem _loadoutTemperanceDisplaySystem;
        private CustomizationStateManagementSystem _customizationStateManagementSystem;

        public CustomizationRootSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice gd, SpriteBatch sb, ContentManager content, SpriteFont font) : base(em)
        {
            _systemManager = sm;
            _world = world;
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _content = content;
            _font = font;

            // Construct sub-systems (drawn via Draw())
            _customizationSceneSystem = new CustomizationSceneSystem(EntityManager, _graphicsDevice, _spriteBatch, _font);
            _libraryPanelSystem = new CardLibraryPanelSystem(EntityManager, _world, _graphicsDevice, _spriteBatch, _font);
            _deckPanelSystem = new LoadoutDeckPanelSystem(EntityManager, _world, _graphicsDevice, _spriteBatch, _font);
            _loadoutEditSystem = new LoadoutEditSystem(EntityManager);
            _deckInfoDisplaySystem = new DeckInfoDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _font);
            _backgroundSystem = new CustomizationBackgroundSystem(EntityManager, _graphicsDevice, _spriteBatch, _content);
            _sectionTabMenuDisplaySystem = new SectionTabMenuDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _font, _libraryPanelSystem, _deckPanelSystem);
            _customizeTemperanceDisplaySystem = new CustomizeTemperanceDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _font, _libraryPanelSystem, _deckPanelSystem);
            _availableCardDisplaySystem = new AvailableCardDisplaySystem(EntityManager, _world, _graphicsDevice, _spriteBatch, _libraryPanelSystem);
            _loadoutCardDisplaySystem = new LoadoutCardDisplaySystem(EntityManager, _world, _graphicsDevice, _spriteBatch, _deckPanelSystem);
            _availableTemperanceDisplaySystem = new AvailableTemperanceDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _libraryPanelSystem, _customizeTemperanceDisplaySystem);
            _loadoutTemperanceDisplaySystem = new LoadoutTemperanceDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _deckPanelSystem, _customizeTemperanceDisplaySystem);
            _customizationStateManagementSystem = new CustomizationStateManagementSystem(EntityManager);

            world.AddSystem(_customizationSceneSystem);
            world.AddSystem(_libraryPanelSystem);
            world.AddSystem(_deckPanelSystem);
            world.AddSystem(_loadoutEditSystem);
            world.AddSystem(_deckInfoDisplaySystem);
            world.AddSystem(_backgroundSystem);
            world.AddSystem(_sectionTabMenuDisplaySystem);
            world.AddSystem(_customizeTemperanceDisplaySystem);
            world.AddSystem(_availableCardDisplaySystem);
            world.AddSystem(_loadoutCardDisplaySystem);
            world.AddSystem(_availableTemperanceDisplaySystem);
            world.AddSystem(_loadoutTemperanceDisplaySystem);
            world.AddSystem(_customizationStateManagementSystem);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No per-entity logic here; child systems handle updates
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            _backgroundSystem.Draw();
            FrameProfiler.Measure("SectionTabMenuDisplaySystem.Draw", _sectionTabMenuDisplaySystem.Draw);
            FrameProfiler.Measure("CustomizeTemperanceDisplaySystem.Draw", _customizeTemperanceDisplaySystem.Draw);
            FrameProfiler.Measure("CardLibraryPanelSystem.Draw", _libraryPanelSystem.Draw);
            FrameProfiler.Measure("LoadoutDeckPanelSystem.Draw", _deckPanelSystem.Draw);
            FrameProfiler.Measure("AvailableCardDisplaySystem.Draw", _availableCardDisplaySystem.Draw);
            FrameProfiler.Measure("LoadoutCardDisplaySystem.Draw", _loadoutCardDisplaySystem.Draw);
            FrameProfiler.Measure("AvailableTemperanceDisplaySystem.Draw", _availableTemperanceDisplaySystem.Draw);
            FrameProfiler.Measure("LoadoutTemperanceDisplaySystem.Draw", _loadoutTemperanceDisplaySystem.Draw);
            FrameProfiler.Measure("CustomizationSceneSystem.Draw", _customizationSceneSystem.Draw);
            FrameProfiler.Measure("DeckInfoDisplaySystem.Draw", _deckInfoDisplaySystem.Draw);
        }
    }
}


