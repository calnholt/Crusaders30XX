using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class EventStartSystem : Core.System
	{
		public EventStartSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<HotKeyHoldCompletedEvent>(OnHotKeyHoldCompleted);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnHotKeyHoldCompleted(HotKeyHoldCompletedEvent evt)
		{
			var ent = evt?.Entity;
			if (ent == null) return;

			var hotKey = ent.GetComponent<HotKey>();
			Entity eventEntity = ent;
			if (hotKey?.ParentEntity != null)
			{
				eventEntity = hotKey.ParentEntity;
			}

			var poi = eventEntity.GetComponent<PointOfInterest>();
			if (poi == null || poi.Type != PointOfInterestType.Event) return;

			string eventId = !string.IsNullOrEmpty(poi.EventId) ? poi.EventId : poi.Id;
			if (string.IsNullOrEmpty(eventId)) return;

			if (!SaveCache.TryGetRunEvent(eventId, out var mapEvent, out _)) return;
			if (!RunMapEventService.IsEnterable(mapEvent, SaveCache.GetRunMapNodes())) return;
			if (NarrativeEventModalDisplaySystem.IsOverlayOpen(EntityManager)) return;

			EventManager.Publish(new ShowNarrativeEventOverlay
			{
				RunMapEventId = eventId,
				EventTypeId = mapEvent.eventTypeId
			});
		}
	}
}
