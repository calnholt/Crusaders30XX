using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapShopGeneratorService
	{
		private const int PlacementAttemptsPerShop = 256;
		private const int FarAnchorCandidates = 6;
		private const int ShopRngSalt = 0x5A4F5A4F;

		private static readonly string[] ColorNames = { "Red", "White", "Black" };
		private static readonly HashSet<string> StarterPool = new HashSet<string>(
			StartingDeckGeneratorService.DefaultStarterCardPool,
			StringComparer.OrdinalIgnoreCase);

		public static List<RunMapShop> Generate(int runMapSeed, IReadOnlyList<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0) return new List<RunMapShop>();

			var rng = new Random(runMapSeed ^ ShopRngSalt);
			var cardPool = BuildCardPool();
			var shops = new List<RunMapShop>(LocationMapConstants.RunMapShopCount);
			var placedPositions = new List<(float x, float y)>();
			var anchorNodes = nodes.Where(n => n != null).ToList();
			if (anchorNodes.Count == 0) return shops;

			var displayNames = RunMapShopCatalog.PickDisplayNames(rng, LocationMapConstants.RunMapShopCount);
			var backgroundAssets = RunMapShopCatalog.PickBackgroundAssets(rng, LocationMapConstants.RunMapShopCount);
			int medalShopIndex = rng.Next(LocationMapConstants.RunMapShopCount);

			for (int shopIndex = 0; shopIndex < LocationMapConstants.RunMapShopCount; shopIndex++)
			{
				if (!TryPlaceShop(rng, anchorNodes, placedPositions, out float x, out float y))
				{
					throw new InvalidOperationException(
						$"[RunMapShopGeneratorService] Failed to place shop {shopIndex} after {PlacementAttemptsPerShop} attempts.");
				}

				placedPositions.Add((x, y));
				var items = RollShopItems(rng, cardPool);
				if (shopIndex == medalShopIndex)
				{
					InjectRandomMedalOffer(rng, items);
				}
				shops.Add(new RunMapShop
				{
					id = ShopId(shopIndex),
					displayName = displayNames[shopIndex],
					backgroundAsset = backgroundAssets[shopIndex],
					worldX = x,
					worldY = y,
					items = items,
				});
			}

			return shops;
		}

		private static List<string> BuildCardPool()
		{
			var pool = new List<string>();
			foreach (var card in CardFactory.GetAllCards().Values)
			{
				if (card == null || !card.CanAddToLoadout || card.IsWeapon || card.IsToken) continue;
				string cardId = card.CardId;
				if (string.IsNullOrWhiteSpace(cardId) || StarterPool.Contains(cardId)) continue;
				pool.Add(cardId);
			}

			return pool.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		private static List<RunMapShopItem> RollShopItems(Random rng, List<string> cardPool)
		{
			var items = new List<RunMapShopItem>(LocationMapConstants.RunMapShopItemsPerShop);
			if (cardPool.Count == 0) return items;

			var pickedIds = cardPool.OrderBy(_ => rng.Next())
				.Take(LocationMapConstants.RunMapShopItemsPerShop)
				.ToList();
			var pickedColors = ColorNames.OrderBy(_ => rng.Next())
				.Take(LocationMapConstants.RunMapShopItemsPerShop)
				.ToList();

			for (int i = 0; i < LocationMapConstants.RunMapShopItemsPerShop; i++)
			{
				items.Add(new RunMapShopItem
				{
					cardId = pickedIds[i % pickedIds.Count],
					color = pickedColors[i],
					price = LocationMapConstants.RunMapShopCardPrice,
					isPurchased = false,
					displayRotationDeg = rng.Next(-5, 6),
				});
			}

			return items;
		}

		private static void InjectRandomMedalOffer(Random rng, List<RunMapShopItem> items)
		{
			if (items == null || items.Count == 0) return;

			var medalIds = MedalFactory.GetAllMedals().Keys.ToList();
			if (medalIds.Count == 0) return;

			string medalId = medalIds[rng.Next(medalIds.Count)];
			int slotIndex = rng.Next(items.Count);
			items[slotIndex] = new RunMapShopItem
			{
				itemType = RunMapShopItem.ItemTypeMedal,
				cardId = medalId,
				color = string.Empty,
				price = LocationMapConstants.RunMapShopMedalPrice,
				isPurchased = false,
				displayRotationDeg = rng.Next(-5, 6),
			};
		}

		/// <summary>
		/// Place near a random quest node so the shop lies inside that node's completed fog,
		/// without using MinNodeSpacing (which exceeds DefaultRevealRadius).
		/// </summary>
		private static bool TryPlaceShop(
			Random rng,
			List<RunMapNode> anchorNodes,
			List<(float x, float y)> placedShops,
			out float x,
			out float y)
		{
			float clearance = LocationMapConstants.RunMapShopClearanceFromQuest;
			float clearanceSq = clearance * clearance;
			float maxDist = LocationMapConstants.DefaultRevealRadius;
			float shopSep = LocationMapConstants.RunMapShopMinSeparation;
			float shopSepSq = shopSep * shopSep;

			float minX = LocationMapConstants.MapMargin + clearance;
			float maxX = LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin - clearance;
			float minY = LocationMapConstants.MapMargin + clearance;
			float maxY = LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin - clearance;

			for (int attempt = 0; attempt < PlacementAttemptsPerShop; attempt++)
			{
				var anchor = PickAnchorNode(rng, anchorNodes, placedShops);
				float angle = (float)(rng.NextDouble() * Math.PI * 2);
				float dist = clearance + (float)rng.NextDouble() * (maxDist - clearance);
				float cx = anchor.worldX + (float)Math.Cos(angle) * dist;
				float cy = anchor.worldY + (float)Math.Sin(angle) * dist;

				if (cx < minX || cx > maxX || cy < minY || cy > maxY) continue;
				if (OverlapsBattleNode(anchorNodes, cx, cy, clearanceSq)) continue;
				if (OverlapsPlacedShops(placedShops, cx, cy, shopSepSq)) continue;

				x = cx;
				y = cy;
				return true;
			}

			x = 0f;
			y = 0f;
			return false;
		}

		private static bool OverlapsBattleNode(IReadOnlyList<RunMapNode> nodes, float x, float y, float clearanceSq)
		{
			foreach (var node in nodes)
			{
				if (node == null) continue;
				float dx = x - node.worldX;
				float dy = y - node.worldY;
				if (dx * dx + dy * dy < clearanceSq) return true;
			}

			return false;
		}

		private static RunMapNode PickAnchorNode(
			Random rng,
			List<RunMapNode> anchorNodes,
			List<(float x, float y)> placedShops)
		{
			if (placedShops.Count == 0)
			{
				return anchorNodes[rng.Next(anchorNodes.Count)];
			}

			var ranked = anchorNodes
				.Select(n =>
				{
					float minDistSq = float.MaxValue;
					foreach (var shop in placedShops)
					{
						float dx = n.worldX - shop.x;
						float dy = n.worldY - shop.y;
						float dSq = dx * dx + dy * dy;
						if (dSq < minDistSq) minDistSq = dSq;
					}
					return (Node: n, MinDistSq: minDistSq);
				})
				.OrderByDescending(x => x.MinDistSq)
				.Take(FarAnchorCandidates)
				.ToList();

			if (ranked.Count == 0)
			{
				return anchorNodes[rng.Next(anchorNodes.Count)];
			}

			return ranked[rng.Next(ranked.Count)].Node;
		}

		private static bool OverlapsPlacedShops(List<(float x, float y)> placedShops, float x, float y, float sepSq)
		{
			foreach (var shop in placedShops)
			{
				float dx = x - shop.x;
				float dy = y - shop.y;
				if (dx * dx + dy * dy < sepSq) return true;
			}

			return false;
		}

		private static string ShopId(int index) => $"shop_{index}";
	}
}
