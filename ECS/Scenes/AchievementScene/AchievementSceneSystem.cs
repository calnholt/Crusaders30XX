using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Root system for the Achievement scene.
    /// Manages lifecycle of child display systems.
    /// </summary>
    [DebugTab("Achievement Scene")]
    public class AchievementSceneSystem : Core.System
    {
        private readonly SystemManager _systemManager;
        private readonly World _world;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ContentManager _content;
        private bool _firstLoad = true;

        private AchievementGridDisplaySystem _gridDisplaySystem;
        private AchievementDescriptionDisplaySystem _descriptionDisplaySystem;
        private AchievementMeterDisplaySystem _meterDisplaySystem;
        private AchievementTitleDisplaySystem _titleDisplaySystem;
        private AchievementBackButtonDisplaySystem _backButtonDisplaySystem;

        public AchievementSceneSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice gd, SpriteBatch sb, ContentManager content)
            : base(em)
        {
            _systemManager = sm;
            _world = world;
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _content = content;

            EventManager.Subscribe<LoadSceneEvent>(_ =>
            {
                if (_.Scene != SceneId.Achievement) return;
                AddAchievementSystems();
            });

            EventManager.Subscribe<DeleteCachesEvent>(_ =>
            {
                if (_gridDisplaySystem != null) _world.RemoveSystem(_gridDisplaySystem);
                if (_descriptionDisplaySystem != null) _world.RemoveSystem(_descriptionDisplaySystem);
                if (_meterDisplaySystem != null) _world.RemoveSystem(_meterDisplaySystem);
                if (_titleDisplaySystem != null) _world.RemoveSystem(_titleDisplaySystem);
                if (_backButtonDisplaySystem != null) _world.RemoveSystem(_backButtonDisplaySystem);
                _firstLoad = true;
            });
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            yield break;
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            if (_gridDisplaySystem != null)
                FrameProfiler.Measure("AchievementGridDisplaySystem.Draw", _gridDisplaySystem.Draw);
            if (_descriptionDisplaySystem != null)
                FrameProfiler.Measure("AchievementDescriptionDisplaySystem.Draw", _descriptionDisplaySystem.Draw);
            if (_meterDisplaySystem != null)
                FrameProfiler.Measure("AchievementMeterDisplaySystem.Draw", _meterDisplaySystem.Draw);
            if (_titleDisplaySystem != null)
                FrameProfiler.Measure("AchievementTitleDisplaySystem.Draw", _titleDisplaySystem.Draw);
            if (_backButtonDisplaySystem != null)
                FrameProfiler.Measure("AchievementBackButtonDisplaySystem.Draw", _backButtonDisplaySystem.Draw);
        }

        private void AddAchievementSystems()
        {
            if (!_firstLoad) return;
            _firstLoad = false;

            _gridDisplaySystem = new AchievementGridDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
            _world.AddSystem(_gridDisplaySystem);

            _descriptionDisplaySystem = new AchievementDescriptionDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
            _world.AddSystem(_descriptionDisplaySystem);

            _meterDisplaySystem = new AchievementMeterDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
            _world.AddSystem(_meterDisplaySystem);

            _titleDisplaySystem = new AchievementTitleDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
            _world.AddSystem(_titleDisplaySystem);

            _backButtonDisplaySystem = new AchievementBackButtonDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
            _world.AddSystem(_backButtonDisplaySystem);
        }
    }
}
