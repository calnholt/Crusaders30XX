using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapShopService
	{
		public static bool IsEnterable(RunMapShop shop, IReadOnlyList<RunMapNode> nodes)
		{
			if (shop == null || nodes == null || nodes.Count == 0) return false;

			return RunMapLandmarkAccessService.IsWithinCompletedQuestFog(
				shop.worldX,
				shop.worldY,
				nodes,
				minCompletedDepth: 0);
		}

		public static bool TryGetShop(string shopId, IReadOnlyList<RunMapShop> shops, out RunMapShop shop, out int index)
		{
			shop = null;
			index = -1;
			if (shops == null || string.IsNullOrWhiteSpace(shopId)) return false;

			for (int i = 0; i < shops.Count; i++)
			{
				var s = shops[i];
				if (s != null && string.Equals(s.id, shopId, StringComparison.OrdinalIgnoreCase))
				{
					shop = s;
					index = i;
					return true;
				}
			}

			return false;
		}
	}
}
