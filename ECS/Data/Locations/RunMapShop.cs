using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapShop
	{
		public string id { get; set; } = string.Empty;
		public string displayName { get; set; } = string.Empty;
		public string backgroundAsset { get; set; } = string.Empty;
		public float worldX { get; set; }
		public float worldY { get; set; }
		public List<RunMapShopItem> items { get; set; } = new List<RunMapShopItem>();
	}
}
