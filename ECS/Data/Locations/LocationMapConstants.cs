using System;

namespace Crusaders30XX.ECS.Data.Locations
{
	public static class LocationMapConstants
	{
		public const int NodeCount = 20;
		public const int BaseMapWidth = 6000;
		public const int BaseMapHeight = 3000;
		public static float MapCenterX => BaseMapWidth * 0.5f;
		public static float MapCenterY => BaseMapHeight * 0.5f;
		public const float MapMargin = 200f;

		// Bump when placement algorithm changes; clears run_map_generator.log on increase.
		public const int MapGeneratorVersion = 6;

		public static float PlayableWidth => BaseMapWidth - 2f * MapMargin;
		public static float PlayableHeight => BaseMapHeight - 2f * MapMargin;
		public static float PlayableMinDimension => Math.Min(PlayableWidth, PlayableHeight);

		private const float MinNodeSpacingFloor = 400f;
		private const float MinStepFloor = 500f;
		private const float MaxStepFloor = 1000f;

		// Pan-to-explore: generous gaps and wide parent-child hops.
		public static float MinNodeSpacing => Math.Max(MinNodeSpacingFloor, PlayableMinDimension * 0.10f);

		public const float MinSpreadBboxWidthFraction = 0.40f;
		public const float MinSpreadBboxHeightFraction = 0.30f;
		public static float MinSpreadPairwiseDistance => MinNodeSpacing * 0.98f;
		public static float MinStep => Math.Max(MinStepFloor, PlayableMinDimension * 0.09f);
		public static float MaxStep => Math.Max(MaxStepFloor, PlayableMinDimension * 0.18f);

		// Root quest node offset from geometric center (large wiggle).
		public static float RootWiggleRadius => PlayableWidth * 0.20f;

		// Quest map icon (~140px) plus default fog feather headroom (FogDisplaySystem.FeatherPx up to 64).
		public const int QuestIconRadiusReferencePx = 140;
		public const int FogFeatherHeadroomPx = 64;
		public const int MinFogRadiusForIconPx = QuestIconRadiusReferencePx + FogFeatherHeadroomPx;

		/// <summary>
		/// Fog clear radius for completed quests. Sized for nodes placed up to <see cref="MaxStep"/> away.
		/// </summary>
		public static int DefaultRevealRadius => (int)Math.Ceiling(MaxStep);

		/// <summary>Starting radius for completion cutscene lerp; must fit the quest icon inside the hole.</summary>
		public static int DefaultUnrevealedRadius => MinFogRadiusForIconPx;
		public const int QuestRewardGold = 20;
		public const int QuestRewardGoldDualBattle = 50;
		public const int RunMapDualBattleQuestCount = 4;
		public const string RunMapDualBattleFirstEnemyId = "skeleton";
		public const int MaxChildrenPerNode = 3;

		/// <summary>Max quest nodes revealed when a single quest is completed (cutscene + save).</summary>
		public const int MaxQuestRevealsPerCompletion = 3;

		/// <summary>Playable strip along each map edge; too many nodes here fails spread validation.</summary>
		public const float PlayableEdgeBandFraction = 0.12f;

		public const int MaxNodesPerPlayableEdgeBand = 2;

		/// <summary>Placement rejects candidates closer than this to any playable border.</summary>
		public static float MinPlacementEdgeInset => Math.Max(250f, PlayableMinDimension * 0.08f);
		public const int RunMapShopCount = 3;
		public const int RunMapTreasureCount = 2;
		public const int RunMapEventCount = 2;
		public const int RunMapTreasureGoldMin = 10;
		public const int RunMapTreasureGoldMax = 30;
		public const int RunMapTreasureMinUnlockDepth = 2;
		public const int RunMapShopItemsPerShop = 3;
		public const int RunMapShopCardPrice = 30;
		public const int RunMapShopMedalPrice = 50;

		// Shops sit inside completed-quest fog; clearance is icon-scale, not map MinNodeSpacing.
		public const float RunMapShopClearanceFromQuest = 400f;

		private const float RunMapShopMinSeparationFloor = 1000f;

		/// <summary>Minimum distance between shop markers (~38% of playable height).</summary>
		public static float RunMapShopMinSeparation =>
			Math.Max(RunMapShopMinSeparationFloor, PlayableMinDimension * 0.38f);
	}
}
