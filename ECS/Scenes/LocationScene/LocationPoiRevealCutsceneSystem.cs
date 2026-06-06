using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Services;
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
		private const float AnimDuration = 1.0f;
		private float _startRadius;
		private float _targetRadius;
		private bool _focusedCamera;
		private HashSet<string> _allowedRevealIds;

		public static bool HasExpandingFog { get; private set; }
		public static Vector2 ExpandingFogWorldCenter { get; private set; }
		public static float ExpandingFogRadius { get; private set; }

		public static bool TryGetExpandingFog(out Vector2 worldCenter, out float radius)
		{
			if (!HasExpandingFog)
			{
				worldCenter = Vector2.Zero;
				radius = 0f;
				return false;
			}

			worldCenter = ExpandingFogWorldCenter;
			radius = ExpandingFogRadius;
			return true;
		}

		public LocationPoiRevealCutsceneSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<LoadSceneEvent>(_ =>
			{
				if (_.Scene != SceneId.Location) return;
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
				TryBeginCutsceneFromTransitionState();
				if (!_active)
				{
					ClearExpandingFog();
					return;
				}
			}

			if (_poiEntity == null)
			{
				_poiEntity = EntityManager
					.GetEntitiesWithComponent<PointOfInterest>()
					.FirstOrDefault(e => e.GetComponent<PointOfInterest>()?.Id == _targetPoiId);
				if (_poiEntity != null)
				{
					var p = _poiEntity.GetComponent<PointOfInterest>();
					p.IsCompleted = true;
					_startRadius = p.UnrevealedRadius;
					_targetRadius = p.RevealRadius;
					p.DisplayRadius = _startRadius;
					_allowedRevealIds = BuildAllowedRevealIds(p);
					FocusCameraOnCompletedPoi(p);
				}
				else
				{
					return;
				}
			}

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_animTime += MathHelper.Max(0f, dt);
			var poi = _poiEntity.GetComponent<PointOfInterest>();
			float t = MathHelper.Clamp(_animTime / AnimDuration, 0f, 1f);
			float currentRadius = MathHelper.Lerp(_startRadius, _targetRadius, t);
			poi.DisplayRadius = currentRadius;

			UpdateExpandingFog(poi, currentRadius);
			RevealCombatPoisWithinCurrentRadius(poi, currentRadius);

			if (t >= 1f)
			{
				poi.DisplayRadius = poi.RevealRadius;
				EndCutscene();
			}
		}

		private void FocusCameraOnCompletedPoi(PointOfInterest poi)
		{
			if (_focusedCamera || poi == null) return;
			_focusedCamera = true;
			EventManager.Publish(new FocusLocationCameraEvent { WorldPos = poi.WorldPosition });
		}

		private static void UpdateExpandingFog(PointOfInterest completedPoi, float currentRadius)
		{
			HasExpandingFog = true;
			ExpandingFogWorldCenter = completedPoi.WorldPosition;
			ExpandingFogRadius = currentRadius;
		}

		private static void ClearExpandingFog()
		{
			HasExpandingFog = false;
			ExpandingFogWorldCenter = Vector2.Zero;
			ExpandingFogRadius = 0f;
		}

		private HashSet<string> BuildAllowedRevealIds(PointOfInterest completedPoi)
		{
			var nodes = SaveCache.GetRunMapNodes();
			var ids = RunMapRevealService.SelectClosestUnrevealedNodeIds(
				nodes,
				completedPoi.WorldPosition.X,
				completedPoi.WorldPosition.Y,
				completedPoi.RevealRadius,
				LocationMapConstants.MaxQuestRevealsPerCompletion);
			return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
		}

		private void RevealCombatPoisWithinCurrentRadius(PointOfInterest completedPoi, float currentRadius)
		{
			if (completedPoi == null || _allowedRevealIds == null || _allowedRevealIds.Count == 0) return;

			float originX = completedPoi.WorldPosition.X;
			float originY = completedPoi.WorldPosition.Y;

			foreach (var entity in EntityManager.GetEntitiesWithComponent<PointOfInterest>())
			{
				var combatPoi = entity.GetComponent<PointOfInterest>();
				if (!TryRevealCombatPoi(
					combatPoi,
					_allowedRevealIds,
					completedPoi.Id,
					originX,
					originY,
					currentRadius,
					SaveCache.TryRevealRunNode))
				{
					continue;
				}

				var ui = entity.GetComponent<UIElement>();
				if (ui != null && !combatPoi.IsCompleted)
				{
					ui.IsInteractable = true;
					ui.IsHidden = false;
				}

				EventManager.Publish(new POIRevealedEvent { PoiId = combatPoi.Id });
			}
		}

		internal static bool TryRevealCombatPoi(
			PointOfInterest combatPoi,
			ISet<string> allowedRevealIds,
			string completedPoiId,
			float originX,
			float originY,
			float currentRadius,
			Func<string, bool> persistReveal)
		{
			if (combatPoi == null || !PoiVisualStyle.IsCombatPoiType(combatPoi.Type)) return false;
			if (combatPoi.IsRevealed || combatPoi.Id == completedPoiId) return false;
			if (allowedRevealIds == null || !allowedRevealIds.Contains(combatPoi.Id)) return false;
			if (!RunMapRevealService.IsWithinRevealRadius(
				originX,
				originY,
				combatPoi.WorldPosition.X,
				combatPoi.WorldPosition.Y,
				currentRadius))
			{
				return false;
			}
			if (persistReveal == null || !persistReveal(combatPoi.Id)) return false;

			combatPoi.IsRevealed = true;
			combatPoi.DisplayRadius = 0f;
			return true;
		}

		private void TryBeginCutsceneFromTransitionState()
		{
			if (!StateSingleton.HasPendingLocationPoiReveal || string.IsNullOrEmpty(StateSingleton.PendingPoiId)) return;
			_active = true;
			_targetPoiId = StateSingleton.PendingPoiId;
			_poiEntity = null;
			_animTime = 0f;
			_focusedCamera = false;
			_allowedRevealIds = null;
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
			_focusedCamera = false;
			ClearExpandingFog();
			EventManager.Publish(new SetCursorEnabledEvent { Enabled = true });
			EventManager.Publish(new LockLocationCameraEvent { Locked = false });
			StateSingleton.HasPendingLocationPoiReveal = false;
			StateSingleton.PendingPoiId = null;
			StateSingleton.IsActive = false;
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
