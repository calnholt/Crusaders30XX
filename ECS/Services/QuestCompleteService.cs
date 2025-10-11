using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
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
				var qs = entityManager.GetEntitiesWithComponent<QuestSelectState>().FirstOrDefault()?.GetComponent<QuestSelectState>();
				if (qs == null || string.IsNullOrEmpty(qs.LocationId)) return;
				var all = LocationDefinitionCache.GetAll();
				if (all == null) return;
				if (!all.TryGetValue(qs.LocationId, out var loc) || loc?.quests == null) return;
				int totalQuests = System.Math.Max(0, loc.quests.Count);
				int completed = SaveCache.GetValueOrDefault(qs.LocationId, 0);
				int clampedIndex = System.Math.Max(0, System.Math.Min(qs.SelectedQuestIndex, totalQuests > 0 ? totalQuests - 1 : 0));
				if (clampedIndex == completed && completed < totalQuests)
				{
					SaveCache.SetValue(qs.LocationId, completed + 1);
				}
			}
			catch { }
		}
	}
}


