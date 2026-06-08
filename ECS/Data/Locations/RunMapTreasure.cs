namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapTreasure
	{
		public string id { get; set; } = string.Empty;
		public float worldX { get; set; }
		public float worldY { get; set; }
		public int rewardGold { get; set; }
		public bool grantsEquipmentReward { get; set; }
		public bool isClaimed { get; set; }
	}
}
