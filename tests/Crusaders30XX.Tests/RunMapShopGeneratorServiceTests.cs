using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapShopGeneratorServiceTests
{
	[Fact]
	public void Generate_places_all_shops_within_completed_quest_fog_for_reachable_map()
	{
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			Assert.Equal(LocationMapConstants.RunMapShopCount, shops.Count);

			MarkAllNodesCompleted(nodes);
			foreach (var shop in shops)
			{
				Assert.True(
					RunMapShopService.IsEnterable(shop, nodes),
					$"seed {seed} shop {shop.id} not enterable when all quests completed");
			}
		}
	}

	[Fact]
	public void Generate_never_offers_default_starter_pool_cards()
	{
		var starterPool = new HashSet<string>(
			StartingDeckGeneratorService.DefaultStarterCardPool,
			System.StringComparer.OrdinalIgnoreCase);
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			foreach (var shop in shops)
			{
				if (shop?.items == null) continue;
				foreach (var item in shop.items)
				{
					if (item == null || item.IsMedal) continue;
					Assert.False(
						starterPool.Contains(item.cardId),
						$"seed {seed} shop {shop.id} offered starter pool card {item.cardId}");
				}
			}
		}
	}

	[Fact]
	public void User_save_seed_fails_map_generation()
	{
		const int problematicSeed = 1365672886;

		Assert.Throws<System.InvalidOperationException>(
			() => LocationMapGeneratorService.Generate(problematicSeed));
	}

	private static void MarkAllNodesCompleted(List<RunMapNode> nodes)
	{
		foreach (var node in nodes)
		{
			if (node == null) continue;
			node.isRevealed = true;
			node.isCompleted = true;
		}
	}
}
