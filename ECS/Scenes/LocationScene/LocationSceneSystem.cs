using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
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

    public LocationSceneSystem(EntityManager entityManager, SystemManager sm, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font) : base(entityManager)
    {
      _systemManager = sm;
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
      _font = font;
    }

    protected override IEnumerable<Entity> GetRelevantEntities()
    {
      yield break;
    }

    public void Draw()
    {

    }
    private void AddLocationSystems()
		{
			if (!_firstLoad) return;
			_firstLoad = false;
    }

    protected override void UpdateEntity(Entity entity, GameTime gameTime)
    {
      throw new System.NotImplementedException();
    }
  }
}