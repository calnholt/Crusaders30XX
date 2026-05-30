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
			public bool HasCardReward;
			public string RewardCardKey;
		}

		/// <summary>
		/// If the player completed a run-map quest node and it's not already saved, persist and trigger POI reveal cutscene.
		/// </summary>
		public static QuestCompletionResult SaveIfCompletedHighest(Crusaders30XX.ECS.Core.EntityManager entityManager)
		{
			var result = new QuestCompletionResult { IsNewlyCompleted = false, LocationId = string.Empty, QuestId = string.Empty, RewardGold = 0 };
			if (entityManager == null) return result;
			try
			{
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
				result.LocationId = locationId;

				int chosenIndex = questIndex ?? 0;
				var nodes = SaveCache.GetRunMapNodes();
				int clampedIndex = Math.Max(0, Math.Min(chosenIndex, nodes.Count > 0 ? nodes.Count - 1 : 0));

				if (clampedIndex < 0 || clampedIndex >= nodes.Count) return result;

				var node = nodes[clampedIndex];
				if (node == null || string.IsNullOrEmpty(node.id)) return result;

				result.QuestId = node.id;

				if (node.isCompleted) return result;

				Console.WriteLine($"[QuestCompleteService] Completed run map node {locationId}/{node.id}");
				SaveCache.SetRunNodeCompleted(node.id, true);

				StateSingleton.HasPendingLocationPoiReveal = true;
				StateSingleton.PendingPoiId = node.id;

				int reward = LocationMapConstants.QuestRewardGold;
				if (reward > 0)
				{
					SaveCache.AddGold(reward);
					result.RewardGold = reward;
				}
				result.IsNewlyCompleted = true;

				var cardReward = QuestCardRewardService.TryGrantRandomCard();
				if (cardReward.Granted)
				{
					result.HasCardReward = true;
					result.RewardCardKey = cardReward.CardKey;
				}
			}
			catch { }
			return result;
		}
	}
}
