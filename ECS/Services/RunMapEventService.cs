using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapEventService
	{
		public static bool IsEnterable(RunMapEvent mapEvent, IReadOnlyList<RunMapNode> nodes)
		{
			if (mapEvent == null || mapEvent.isCompleted || nodes == null || nodes.Count == 0) return false;

			return RunMapLandmarkAccessService.IsWithinCompletedQuestFog(
				mapEvent.worldX,
				mapEvent.worldY,
				nodes);
		}

		public static bool TryGetEvent(
			string eventId,
			IReadOnlyList<RunMapEvent> events,
			out RunMapEvent mapEvent,
			out int index)
		{
			mapEvent = null;
			index = -1;
			if (events == null || string.IsNullOrWhiteSpace(eventId)) return false;

			for (int i = 0; i < events.Count; i++)
			{
				var e = events[i];
				if (e != null && string.Equals(e.id, eventId, StringComparison.OrdinalIgnoreCase))
				{
					mapEvent = e;
					index = i;
					return true;
				}
			}

			return false;
		}
	}
}
