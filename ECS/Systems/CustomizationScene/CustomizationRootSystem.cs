using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

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

            world.AddSystem(_customizationSceneSystem);
            world.AddSystem(_libraryPanelSystem);
            world.AddSystem(_deckPanelSystem);
            world.AddSystem(_loadoutEditSystem);
            world.AddSystem(_deckInfoDisplaySystem);
            world.AddSystem(_backgroundSystem);
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
            _libraryPanelSystem.Draw();
            _deckPanelSystem.Draw();
            _customizationSceneSystem.Draw();
            _deckInfoDisplaySystem.Draw();
        }
    }
}


