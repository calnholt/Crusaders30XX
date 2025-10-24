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
	[DebugTab("Location Scene System")]
	public class LocationSceneSystem : Core.System
	{
    private readonly SystemManager _systemManager;
    private readonly World _world;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ContentManager _content;
    private readonly SpriteFont _font;
    private bool _firstLoad = true;
		private LocationMapDisplaySystem _locationMapDisplaySystem;

    public LocationSceneSystem(EntityManager entityManager, SystemManager sm, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font) : base(entityManager)
    {
      _systemManager = sm;
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
      _font = font;
      EventManager.Subscribe<LoadSceneEvent>(_ => {
        if (_.Scene != SceneId.Location) return;
        AddLocationSystems();
      });
    }

    protected override IEnumerable<Entity> GetRelevantEntities()
    {
      yield break;
    }

    public void Draw()
    {
			AddLocationSystems();
			_locationMapDisplaySystem?.Draw();
    }
    private void AddLocationSystems()
		{
			if (!_firstLoad) return;
			_firstLoad = false;
			_locationMapDisplaySystem = new LocationMapDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_world.AddSystem(_locationMapDisplaySystem);
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
      throw new System.NotImplementedException();
    }
  }
}