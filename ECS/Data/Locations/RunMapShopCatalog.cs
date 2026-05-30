using System;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Data.Locations
{
	/// <summary>
	/// Flavor names and scene backgrounds for procedurally placed run-map shops.
	/// </summary>
	public static class RunMapShopCatalog
	{
		public static readonly string[] DisplayNames =
		{
			"Mountain Merchant",
			"Oasis Traders",
			"Sunscorched Bazaar",
			"Dusty Caravan",
			"Wandering Relic Seller",
			"Cactus Crown Curios",
			"Sandstone Salvager",
			"Mirage Market",
			"Scarab's Hoard",
			"Dune Warden's Wares",
			"Nomad's Knapsack",
			"Buried Vault Vendor",
			"Crimson Tent Trader",
			"Bleached Bone Exchange",
			"Shrine of Small Fortunes",
		};

		/// <summary>MonoGame content asset names (no extension).</summary>
		public static readonly string[] BackgroundAssets =
		{
			"desert-background-oasis",
			"desert-background-dune-city",
			"desert-background-city",
			"desert-background-cactus",
			"desert-background-dry-cactus",
			"desert-background-graves",
			"desert-background-crumbling-city",
			"desert-background-web-dune",
			"desert-background-prison",
			"desert-background-tundra",
			"desert-background",
			"dungeon-background",
			"gothic-background-alley",
			"forest-background",
			"cathedral-background",
		};

		public static List<string> PickDisplayNames(Random rng, int count)
		{
			return PickWithoutReplacement(rng, DisplayNames, count);
		}

		public static List<string> PickBackgroundAssets(Random rng, int count)
		{
			return PickWithoutReplacement(rng, BackgroundAssets, count);
		}

		private static List<string> PickWithoutReplacement(Random rng, IReadOnlyList<string> pool, int count)
		{
			var result = new List<string>(count);
			if (pool == null || pool.Count == 0 || count <= 0) return result;

			var shuffled = pool.OrderBy(_ => rng.Next()).ToList();
			for (int i = 0; i < count && i < shuffled.Count; i++)
			{
				result.Add(shuffled[i]);
			}

			while (result.Count < count)
			{
				result.Add(shuffled[rng.Next(shuffled.Count)]);
			}

			return result;
		}
	}
}
