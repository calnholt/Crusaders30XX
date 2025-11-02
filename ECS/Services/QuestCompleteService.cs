using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Systems;
using System;
using System.Linq;

namespace Crusaders30XX.ECS.Services
{
	public static class QuestCompleteService
	{
		/// <summary>
		/// If the player completed a quest for the current location and it's not already saved, save it and trigger the POI reveal cutscene.
		/// </summary>
		public static void SaveIfCompletedHighest(Crusaders30XX.ECS.Core.EntityManager entityManager)
		{
			if (entityManager == null) return;
			try
			{
				// Prefer the queued quest context (persists across scenes) and fall back to QuestSelectState
				var qe = entityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault()?.GetComponent<QueuedEvents>();
				string locationId = qe?.LocationId;
				int? questIndex = qe?.QuestIndex;
				if (string.IsNullOrEmpty(locationId))
				{
					var qs = entityManager.GetEntitiesWithComponent<QuestSelectState>().FirstOrDefault()?.GetComponent<QuestSelectState>();
					locationId = qs?.LocationId;
					questIndex = qs?.SelectedQuestIndex;
				}

				if (string.IsNullOrEmpty(locationId)) return;
				LocationDefinitionCache.TryGet(locationId, out var def);
				if (def == null) return;
				int totalPointsOfInterest = System.Math.Max(0, def.pointsOfInterest.Count);
				int chosenIndex = questIndex ?? SaveCache.GetValueOrDefault(locationId, 0); // default to current highest if unknown
				int clampedIndex = System.Math.Max(0, System.Math.Min(chosenIndex, totalPointsOfInterest > 0 ? totalPointsOfInterest - 1 : 0));
				
				// Check if the quest at clampedIndex exists and is not already completed
				if (clampedIndex >= 0 && clampedIndex < totalPointsOfInterest)
				{
					var poi = def.pointsOfInterest[clampedIndex];
					var questIdStr = poi?.id;
					if (!string.IsNullOrEmpty(questIdStr) && !SaveCache.IsQuestCompleted(locationId, questIdStr))
					{
						Console.WriteLine($"[QuestCompleteService] Completed point of interest {locationId}/{questIdStr}");
						SaveCache.SetQuestCompleted(locationId, questIdStr, true);
						// Set flags for POI reveal cutscene when transitioning to Location scene
						TransitionStateSingleton.HasPendingLocationPoiReveal = true;
						TransitionStateSingleton.PendingPoiId = questIdStr;
					}
				}
			}
			catch { }
		}
	}
}


