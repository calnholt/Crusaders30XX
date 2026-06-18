using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public static class ClimbEventService
	{
		public static bool UpdateLifecycle()
		{
			var save = SaveCache.GetAll();
			var climb = SaveCache.GetClimbState();
			if (climb == null) return false;

			var before = Snapshot(climb);
			ClimbRuleService.UpdateEventSlots(climb, save?.runMapSeed ?? 0);
			bool encountersChanged = ClimbRuleService.ReplenishEncounterSlots(climb, save?.runMapSeed ?? 0);
			bool changed = encountersChanged || !string.Equals(before, Snapshot(climb), StringComparison.Ordinal);
			if (changed) SaveCache.SaveClimbState(climb);
			return changed;
		}

		public static bool TryLaunchEvent(EntityManager entityManager, string eventSlotId)
		{
			if (entityManager == null || string.IsNullOrWhiteSpace(eventSlotId)) return false;
			var save = SaveCache.GetAll();
			var climb = SaveCache.GetClimbState();
			if (climb == null) return false;

			if (climb.pendingEvent != null
				&& string.Equals(climb.pendingEvent.eventSlotId, eventSlotId, StringComparison.OrdinalIgnoreCase)
				&& EventFactory.Create(climb.pendingEvent.eventTypeId) != null)
			{
				EventManager.Publish(new ShowNarrativeEventOverlay
				{
					RunMapEventId = string.Empty,
					EventTypeId = climb.pendingEvent.eventTypeId,
				});
				return true;
			}

			ClimbRuleService.UpdateEventSlots(climb, save?.runMapSeed ?? 0);
			var slot = climb.eventSlots?.FirstOrDefault(s =>
				s != null
				&& !s.isCompleted
				&& string.Equals(s.id, eventSlotId, StringComparison.OrdinalIgnoreCase));

			if (slot == null || !ClimbRuleService.IsEventVisible(slot, climb.time)) return false;
			if (EventFactory.Create(slot.eventTypeId) == null) return false;

			int previousTime = climb.time;
			ClimbRuleService.ApplyTime(climb, slot.timeCost);
			if (ClimbRuleService.ShouldRefreshShopAtTime(previousTime, climb.time))
			{
				ClimbRuleService.RefreshShopSlots(climb, save?.runMapSeed ?? 0, SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId));
			}

			climb.pendingEvent = new ClimbPendingEventSave
			{
				eventSlotId = slot.id,
				eventTypeId = slot.eventTypeId,
			};
			ClimbRuleService.UpdateEventSlots(climb, save?.runMapSeed ?? 0);
			ClimbRuleService.ReplenishEncounterSlots(climb, save?.runMapSeed ?? 0);
			SaveCache.SaveClimbState(climb);

			EventManager.Publish(new ShowNarrativeEventOverlay
			{
				RunMapEventId = string.Empty,
				EventTypeId = slot.eventTypeId,
			});
			return true;
		}

		public static bool TryCompletePendingEvent(EntityManager entityManager, string eventTypeId)
		{
			var save = SaveCache.GetAll();
			var climb = SaveCache.GetClimbState();
			var pending = climb?.pendingEvent;
			if (pending == null || string.IsNullOrWhiteSpace(pending.eventSlotId)) return false;
			if (!string.IsNullOrWhiteSpace(eventTypeId)
				&& !string.Equals(pending.eventTypeId, eventTypeId, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			var slot = climb.eventSlots?.FirstOrDefault(s =>
				s != null
				&& string.Equals(s.id, pending.eventSlotId, StringComparison.OrdinalIgnoreCase));
			if (slot != null)
			{
				slot.isCompleted = true;
			}
			climb.pendingEvent = null;
			ClimbRuleService.UpdateEventSlots(climb, save?.runMapSeed ?? 0);
			ClimbRuleService.ReplenishEncounterSlots(climb, save?.runMapSeed ?? 0);
			SaveCache.SaveClimbState(climb);

			if (ClimbRuleService.HasPendingFinalEncounter(climb))
			{
				ClimbEncounterService.TryQueuePendingFinalEncounter(entityManager);
			}
			return true;
		}

		private static string Snapshot(ClimbSaveState climb)
		{
			string slots = climb?.eventSlots == null
				? string.Empty
				: string.Join("|", climb.eventSlots.Select(s =>
					s == null
						? "null"
						: $"{s.id},{s.eventTypeId},{s.generatedAtTime},{s.visibleStartTime},{s.visibleEndTime},{s.timeCost},{s.seen},{s.isCompleted}"));
			string shown = climb?.shownEventTypeIds == null ? string.Empty : string.Join(",", climb.shownEventTypeIds);
			string pending = climb?.pendingEvent == null
				? string.Empty
				: $"{climb.pendingEvent.eventSlotId},{climb.pendingEvent.eventTypeId}";
			return $"{climb?.time ?? 0}:{climb?.nextEventSlotId ?? 0}:{shown}:{pending}:{slots}";
		}
	}
}
