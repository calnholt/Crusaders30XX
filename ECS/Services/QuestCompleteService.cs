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
		public struct QuestCompletionResult
		{
			public bool IsNewlyCompleted;
			public string LocationId;
			public string QuestId;
			public int RewardGold;
		}
		/// <summary>
		/// If the player completed a quest for the current location and it's not already saved, save it and trigger the POI reveal cutscene.
		/// </summary>
		public static QuestCompletionResult SaveIfCompletedHighest(Crusaders30XX.ECS.Core.EntityManager entityManager)
		{
			var result = new QuestCompletionResult { IsNewlyCompleted = false, LocationId = string.Empty, QuestId = string.Empty, RewardGold = 0 };
			if (entityManager == null) return result;
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

				if (string.IsNullOrEmpty(locationId)) return result;
				LocationDefinitionCache.TryGet(locationId, out var def);
				if (def == null) return result;
				int totalPointsOfInterest = System.Math.Max(0, def.pointsOfInterest.Count);
				int chosenIndex = questIndex ?? SaveCache.GetValueOrDefault(locationId, 0); // default to current highest if unknown
				int clampedIndex = System.Math.Max(0, System.Math.Min(chosenIndex, totalPointsOfInterest > 0 ? totalPointsOfInterest - 1 : 0));
				
				// Check if the quest at clampedIndex exists and is not already completed
				if (clampedIndex >= 0 && clampedIndex < totalPointsOfInterest)
				{
					var poi = def.pointsOfInterest[clampedIndex];
					var questIdStr = poi?.id;
					result.LocationId = locationId ?? string.Empty;
					result.QuestId = questIdStr ?? string.Empty;
					
					// Check if this is a Dungeon POI (replayable, don't mark as completed)
					if (poi?.type == PointOfInterestType.Dungeon)
					{
						Console.WriteLine($"[QuestCompleteService] Completed dungeon {locationId}/{questIdStr}");
						// Award gold reward for dungeon completion
						int reward = poi?.rewardGold ?? 0;
						if (reward > 0)
						{
							SaveCache.AddGold(reward);
							result.RewardGold = reward;
						}
						// Do NOT mark as completed (dungeons are replayable)
						// Do NOT trigger POI reveal cutscene
						result.IsNewlyCompleted = false;
						return result;
					}
					
					if (!string.IsNullOrEmpty(questIdStr) && !SaveCache.IsQuestCompleted(locationId, questIdStr))
					{
						Console.WriteLine($"[QuestCompleteService] Completed point of interest {locationId}/{questIdStr}");
						SaveCache.SetQuestCompleted(locationId, questIdStr, true);
						// Set flags for POI reveal cutscene when transitioning to Location scene
						StateSingleton.HasPendingLocationPoiReveal = true;
						StateSingleton.PendingPoiId = questIdStr;
						// Award reward if present
						int reward = poi?.rewardGold ?? 0;
						if (reward > 0)
						{
							SaveCache.AddGold(reward);
							result.RewardGold = reward;
						}
						result.IsNewlyCompleted = true;
					}
				}
			}
			catch { }
			return result;
		}
	}
}


