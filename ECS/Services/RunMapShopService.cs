using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapShopService
	{
		public static bool IsEnterable(RunMapShop shop, IReadOnlyList<RunMapNode> nodes)
		{
			if (shop == null || nodes == null || nodes.Count == 0) return false;

			float shopX = shop.worldX;
			float shopY = shop.worldY;
			float revealRadius = LocationMapConstants.DefaultRevealRadius;
			float revealRadiusSq = revealRadius * revealRadius;

			foreach (var node in nodes)
			{
				if (node == null || !node.isCompleted) continue;

				float dx = shopX - node.worldX;
				float dy = shopY - node.worldY;
				if (dx * dx + dy * dy <= revealRadiusSq) return true;
			}

			return false;
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
