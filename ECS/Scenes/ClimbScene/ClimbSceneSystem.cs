using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Scene")]
	public class ClimbSceneSystem : Core.System
	{
		private readonly World _world;
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private bool _firstLoad = true;
		private ClimbBackgroundDisplaySystem _backgroundDisplaySystem;
		private ClimbHeaderLayoutSystem _headerLayoutSystem;
		private ClimbHeaderDisplaySystem _headerDisplaySystem;
		private ClimbColumnLayoutSystem _columnLayoutSystem;
		private ClimbColumnDisplaySystem _columnDisplaySystem;
		private ClimbToastDisplaySystem _toastDisplaySystem;

		public ClimbSceneSystem(EntityManager entityManager, SystemManager systemManager, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;

			EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
			EventManager.Subscribe<DeleteCachesEvent>(_ => RemoveClimbSystems());
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		private void OnLoadScene(LoadSceneEvent evt)
		{
			if (evt.Scene != SceneId.Climb)
			{
				DeactivateClimbUiEntities(EntityManager);
				EventManager.Publish(new HideLocationNameEvent());
				return;
			}

			SaveCache.ClearPendingBattle();
			SaveCache.EnsureClimbState();
			EventManager.Publish(new ChangeMusicTrack { Track = MusicTrack.Map });
			EventManager.Publish(new HideLocationNameEvent());
			AddClimbSystems();
		}

		private void AddClimbSystems()
		{
			if (!_firstLoad) return;
			_firstLoad = false;

			_backgroundDisplaySystem = new ClimbBackgroundDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_backgroundDisplaySystem);
			_headerLayoutSystem = new ClimbHeaderLayoutSystem(EntityManager);
			_world.AddSystem(_headerLayoutSystem);
			_headerDisplaySystem = new ClimbHeaderDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_headerDisplaySystem);
			_columnLayoutSystem = new ClimbColumnLayoutSystem(EntityManager);
			_world.AddSystem(_columnLayoutSystem);
			_columnDisplaySystem = new ClimbColumnDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch, _content);
			_world.AddSystem(_columnDisplaySystem);
			_toastDisplaySystem = new ClimbToastDisplaySystem(EntityManager, _graphicsDevice, _spriteBatch);
			_world.AddSystem(_toastDisplaySystem);
		}

		private void RemoveClimbSystems()
		{
			DeactivateClimbUiEntities(EntityManager);
			_world.RemoveSystem(_backgroundDisplaySystem);
			_world.RemoveSystem(_headerLayoutSystem);
			_world.RemoveSystem(_headerDisplaySystem);
			_world.RemoveSystem(_columnLayoutSystem);
			_world.RemoveSystem(_columnDisplaySystem);
			_world.RemoveSystem(_toastDisplaySystem);
			_firstLoad = true;
		}

		public void Draw()
		{
			if (_backgroundDisplaySystem != null) FrameProfiler.Measure("ClimbBackgroundDisplaySystem.Draw", _backgroundDisplaySystem.Draw);
			if (_headerDisplaySystem != null) FrameProfiler.Measure("ClimbHeaderDisplaySystem.Draw", _headerDisplaySystem.Draw);
			if (_columnDisplaySystem != null) FrameProfiler.Measure("ClimbColumnDisplaySystem.Draw", _columnDisplaySystem.Draw);
			if (_toastDisplaySystem != null) FrameProfiler.Measure("ClimbToastDisplaySystem.Draw", _toastDisplaySystem.Draw);
		}

		public static void DeactivateClimbUiEntities(EntityManager entityManager)
		{
			if (entityManager == null) return;

			var climbEntities = entityManager.GetAllEntities()
				.Where(entity =>
					entity.GetComponent<ClimbSceneRoot>() != null
					|| entity.GetComponent<ClimbHeaderElement>() != null
					|| entity.GetComponent<ClimbTimelineElement>() != null
					|| entity.GetComponent<ClimbResourceBarElement>() != null
					|| entity.GetComponent<ClimbLoadoutButton>() != null
					|| entity.GetComponent<ClimbColumnPresentation>() != null
					|| entity.GetComponent<ClimbSlotPresentation>() != null)
				.ToList();

			foreach (var entity in climbEntities)
			{
				var ui = entity.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.Bounds = Rectangle.Empty;
					ui.IsInteractable = false;
					ui.IsHidden = true;
					ui.IsHovered = false;
					ui.IsClicked = false;
				}

				var preview = entity.GetComponent<ClimbPreviewState>();
				preview?.Clear();
			}
		}
	}
}
