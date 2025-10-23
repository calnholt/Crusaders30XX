using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using System;
using System.Linq;

namespace Crusaders30XX.ECS.Services
{
	public static class QuestCompleteService
	{
		/// <summary>
		/// If the player completed the highest unlocked quest for the current location, increment progress in save.
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
				var all = LocationDefinitionCache.GetAll();
				if (all == null) return;
				if (!all.TryGetValue(locationId, out var loc) || loc?.quests == null) return;
				int totalQuests = System.Math.Max(0, loc.quests.Count);
				int completed = SaveCache.GetValueOrDefault(locationId, 0);
				int chosenIndex = questIndex ?? completed; // default to current highest if unknown
				int clampedIndex = System.Math.Max(0, System.Math.Min(chosenIndex, totalQuests > 0 ? totalQuests - 1 : 0));
				if (clampedIndex == completed && completed < totalQuests)
				{
					Console.WriteLine($"[QuestCompleteService] Completed quest {locationId} -> {completed + 1}/{totalQuests}");
					SaveCache.SetValue(locationId, completed + 1);
				}
			}
			catch { }
		}
	}
}


