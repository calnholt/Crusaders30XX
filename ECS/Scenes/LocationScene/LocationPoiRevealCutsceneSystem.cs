using System;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
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
					RevealGraphChildren(p);
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

		private void RevealGraphChildren(PointOfInterest completedPoi)
		{
			if (completedPoi?.ChildPoiIds == null) return;
			foreach (string childId in completedPoi.ChildPoiIds)
			{
				if (string.IsNullOrEmpty(childId)) continue;
				var childEntity = EntityManager
					.GetEntitiesWithComponent<PointOfInterest>()
					.FirstOrDefault(e => e.GetComponent<PointOfInterest>()?.Id == childId);
				var childPoi = childEntity?.GetComponent<PointOfInterest>();
				if (childPoi == null || childPoi.IsRevealed) continue;
				childPoi.IsRevealed = true;
				childPoi.DisplayRadius = childPoi.UnrevealedRadius;
				var childUi = childEntity.GetComponent<UIElement>();
				if (childUi != null && !childPoi.IsCompleted)
				{
					childUi.IsInteractable = true;
					childUi.IsHidden = false;
				}
				EventManager.Publish(new POIRevealedEvent { PoiId = childId });
			}
		}

		[DebugActionInt("Cutscene", Step = 1, Min = 0, Max = 19, Default = 0)]
		public void debug_cutscene(int id)
		{
			StateSingleton.HasPendingLocationPoiReveal = true;
			StateSingleton.PendingPoiId = $"run_{id}";
			TryBeginCutsceneFromTransitionState();
		}
	}

	

}


