using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Shop Scene System")]
	public class ShopSceneSystem : Core.System
	{
		private readonly SystemManager _systemManager;
		private readonly World _world;
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private bool _firstLoad = true;

		private ShopBackgroundDisplaySystem _shopBackgroundDisplaySystem;
		private CustomizeButtonDisplaySystem _customizeButtonDisplaySystem;
		private ForSaleDisplaySystem _forSaleDisplaySystem;

		public ShopSceneSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice gd, SpriteBatch sb, ContentManager content) : base(em)
		{
			_systemManager = sm;
			_world = world;
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			EventManager.Subscribe<LoadSceneEvent>(_ =>
			{
				if (_.Scene != SceneId.Shop) return;
				EventManager.Publish(new UpdateLocationNameEvent { Title = "Shop" });
				AddShopSystems();
			});
			EventManager.Subscribe<DeleteCachesEvent>(_ =>
			{
				_world.RemoveSystem(_shopBackgroundDisplaySystem);
				_world.RemoveSystem(_forSaleDisplaySystem);
				_world.RemoveSystem(_customizeButtonDisplaySystem);
				_firstLoad = true;
			});
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		public void Draw()
		{
			FrameProfiler.Measure("ShopBackgroundDisplaySystem.Draw", _shopBackgroundDisplaySystem.Draw);
			FrameProfiler.Measure("ForSaleDisplaySystem.Draw", _forSaleDisplaySystem.Draw);
			FrameProfiler.Measure("CustomizeButtonDisplaySystem.Draw", _customizeButtonDisplaySystem.Draw);
		}

		private void AddShopSystems()
		{
			if (!_firstLoad) return;
			_firstLoad = false;
			_shopBackgroundDisplaySystem = new ShopBackgroundDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_shopBackgroundDisplaySystem);
			_forSaleDisplaySystem = new ForSaleDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_forSaleDisplaySystem);
			_customizeButtonDisplaySystem = new CustomizeButtonDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_world.AddSystem(_customizeButtonDisplaySystem);
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }
	}
}


