using System;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("LocationPoiRevealCutsceneSystem")]
	public class LocationPoiRevealCutsceneSystem : Core.System
	{
		private bool _active;
		private string _targetPoiId;
		private Entity _poiEntity;
		private float _animTime;
		private const float AnimDuration = 1.0f; // seconds, linear
		private float _startRadius;
		private float _targetRadius;

		public LocationPoiRevealCutsceneSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<LoadSceneEvent>(_ =>
			{
				if (_.Scene != SceneId.Location) return;
				Console.WriteLine($"[LocationPoiRevealCutsceneSystem] LoadSceneEvent -> TryBeginCutsceneFromTransitionState");
				TryBeginCutsceneFromTransitionState();
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			if (!_active)
			{
				// If we missed LoadSceneEvent race, start from transition flags
				TryBeginCutsceneFromTransitionState();
				if (!_active) return;
			}

			// Ensure target POI entity is located
			if (_poiEntity == null)
			{
				_poiEntity = EntityManager
					.GetEntitiesWithComponent<PointOfInterest>()
					.FirstOrDefault(e => e.GetComponent<PointOfInterest>()?.Id == _targetPoiId);
				if (_poiEntity != null)
				{
					var p = _poiEntity.GetComponent<PointOfInterest>();
					p.IsCompleted = true; // ensure completed
					_startRadius = p.UnrevealedRadius;
					_targetRadius = p.RevealRadius;
					p.DisplayRadius = _startRadius;
					// Focus camera now that we have position
					EventManager.Publish(new FocusLocationCameraEvent { WorldPos = p.WorldPosition });
				}
				else
				{
					return; // wait for POIs to spawn
				}
			}

			// Animate radius
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_animTime += MathHelper.Max(0f, dt);
			var poi = _poiEntity.GetComponent<PointOfInterest>();
			float t = MathHelper.Clamp(_animTime / AnimDuration, 0f, 1f);
			poi.DisplayRadius = MathHelper.Lerp(_startRadius, _targetRadius, t);

			// Reveal neighbors that become visible within any unlocker's DisplayRadius
			var all = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e2 => new { E = e2, P = e2.GetComponent<PointOfInterest>() })
				.Where(x => x.P != null)
				.ToList();
			var unlockers = all.Where(x => x.P.IsCompleted || x.P.IsRevealed).ToList();
			foreach (var x in all)
			{
				if (x.P.IsCompleted || x.P.IsRevealed) continue;
				foreach (var u in unlockers)
				{
					float dx = x.P.WorldPosition.X - u.P.WorldPosition.X;
					float dy = x.P.WorldPosition.Y - u.P.WorldPosition.Y;
					float r = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
					if ((dx * dx) + (dy * dy) <= (r * r))
					{
						x.P.IsRevealed = true;
						x.P.DisplayRadius = x.P.UnrevealedRadius;
						EventManager.Publish(new POIRevealedEvent { PoiId = x.P.Id });
						break;
					}
				}
			}

			if (t >= 1f)
			{
				poi.DisplayRadius = poi.RevealRadius;
				EndCutscene();
			}
		}

		private void TryBeginCutsceneFromTransitionState()
		{
			if (!StateSingleton.HasPendingLocationPoiReveal || string.IsNullOrEmpty(StateSingleton.PendingPoiId)) return;
			Console.WriteLine($"[LocationPoiRevealCutsceneSystem] LoadSceneEvent -> TryBeginCutsceneFromTransitionState -> active");
			_active = true;
			_targetPoiId = StateSingleton.PendingPoiId;
			_poiEntity = null;
			_animTime = 0f;
			// Lock interactions and camera immediately
			StateSingleton.IsActive = true;
			EventManager.Publish(new SetCursorEnabledEvent { Enabled = false });
			EventManager.Publish(new LockLocationCameraEvent { Locked = true });
		}

		private void EndCutscene()
		{
			_active = false;
			_poiEntity = null;
			_targetPoiId = null;
			_animTime = 0f;
			EventManager.Publish(new SetCursorEnabledEvent { Enabled = true });
			EventManager.Publish(new LockLocationCameraEvent { Locked = false });
			StateSingleton.HasPendingLocationPoiReveal = false;
			StateSingleton.PendingPoiId = null;
			StateSingleton.IsActive = false;
		}

		[DebugAction("Cutscene")]
		public void debug_cutscene()
		{
			StateSingleton.HasPendingLocationPoiReveal = true;
			StateSingleton.PendingPoiId = "desert_1";
			TryBeginCutsceneFromTransitionState();
		}
	}

	

}


