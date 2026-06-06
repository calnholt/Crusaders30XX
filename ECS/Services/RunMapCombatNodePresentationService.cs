using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Rendering;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapCombatNodePresentationService
	{
		public static bool IsVisible(RunMapNode node) =>
			node != null && (node.isRevealed || node.isCompleted);

		public static bool IsHellrift(RunMapNode node) =>
			node?.combatNodeType == RunMapCombatNodeType.Hellrift;

		public static PointOfInterestType GetPoiType(RunMapNode node) =>
			IsHellrift(node) ? PointOfInterestType.Hellrift : PointOfInterestType.Quest;

		public static bool IsRevealEligible(RunMapNode node) =>
			node != null && PoiVisualStyle.IsCombatPoiType(GetPoiType(node));

		public static string GetTitle(RunMapNode node, int nodeIndex) =>
			IsHellrift(node) ? "The Gate" : $"Quest {nodeIndex + 1}";

		public static int GetRewardGold(RunMapNode node)
		{
			if (node == null || IsHellrift(node)) return 0;
			return node.ResolveBattleEnemyIds().Count > 1
				? LocationMapConstants.QuestRewardGoldMultiBattle
				: LocationMapConstants.QuestRewardGold;
		}
	}
}
