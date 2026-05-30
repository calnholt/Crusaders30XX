using System;

namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapShopItem
	{
		public const string ItemTypeCard = "card";
		public const string ItemTypeMedal = "medal";

		public string itemType { get; set; } = ItemTypeCard;
		public string cardId { get; set; } = string.Empty;
		public string color { get; set; } = string.Empty;
		public int price { get; set; } = LocationMapConstants.RunMapShopCardPrice;
		public bool isPurchased { get; set; }
		public float displayRotationDeg { get; set; }

		public bool IsMedal =>
			string.Equals(itemType, ItemTypeMedal, StringComparison.OrdinalIgnoreCase);
	}
}
