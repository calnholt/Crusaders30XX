namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapShopItem
	{
		public string cardId { get; set; } = string.Empty;
		public string color { get; set; } = string.Empty;
		public int price { get; set; } = LocationMapConstants.RunMapShopCardPrice;
		public bool isPurchased { get; set; }
		public float displayRotationDeg { get; set; }
	}
}
