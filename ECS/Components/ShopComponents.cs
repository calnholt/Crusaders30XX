using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
	public enum ForSaleItemType
	{
		Card,
		Medal,
		Equipment
	}

	public class ForSaleItem : IComponent
	{
		public Entity Owner { get; set; }

		public string Id { get; set; } = string.Empty;
		public ForSaleItemType ItemType { get; set; } = ForSaleItemType.Card;
		public int Price { get; set; } = 0;
		public bool IsPurchased { get; set; } = false;
		public string DisplayName { get; set; } = string.Empty;
		public string SourceShopName { get; set; } = string.Empty;
	}
}





