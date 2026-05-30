using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Systems;
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

			if (LocationPoiRevealCutsceneSystem.TryGetExpandingFog(out Vector2 expandingCenter, out float expandingRadius))
			{
				if (RunMapRevealService.IsWithinRevealRadius(
					shopX, shopY, expandingCenter.X, expandingCenter.Y, expandingRadius))
				{
					return true;
				}
			}

			foreach (var node in nodes)
			{
				if (node == null || !node.isCompleted) continue;

				if (RunMapRevealService.IsWithinRevealRadius(
					shopX, shopY, node.worldX, node.worldY, revealRadius))
				{
					return true;
				}
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
