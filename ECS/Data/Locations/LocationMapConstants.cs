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
		public const float MinStep = 350f;
		public const float MaxStep = 550f;
		public const float MinNodeSpacing = 180f;
		public const int DefaultRevealRadius = 300;
		public const int DefaultUnrevealedRadius = 50;
		public const int QuestRewardGold = 10;
		public const int MaxChildrenPerNode = 3;
	}
}
