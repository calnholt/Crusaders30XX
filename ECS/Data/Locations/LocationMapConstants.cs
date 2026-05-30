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
		public const int MapGeneratorVersion = 2;

		public static float PlayableWidth => BaseMapWidth - 2f * MapMargin;
		public static float PlayableHeight => BaseMapHeight - 2f * MapMargin;
		public static float PlayableMinDimension => Math.Min(PlayableWidth, PlayableHeight);

		private const float MinNodeSpacingFloor = 400f;
		private const float MinStepFloor = 500f;
		private const float MaxStepFloor = 1000f;

		// Pan-to-explore: generous gaps and wide parent-child hops.
		public static float MinNodeSpacing => Math.Max(MinNodeSpacingFloor, PlayableMinDimension * 0.07f);
		public static float MinStep => Math.Max(MinStepFloor, PlayableMinDimension * 0.09f);
		public static float MaxStep => Math.Max(MaxStepFloor, PlayableMinDimension * 0.18f);

		// Root quest node offset from geometric center (large wiggle).
		public static float RootWiggleRadius => PlayableWidth * 0.20f;

		public const int DefaultRevealRadius = 300;
		public const int DefaultUnrevealedRadius = 50;
		public const int QuestRewardGold = 10;
		public const int MaxChildrenPerNode = 3;
	}
}
