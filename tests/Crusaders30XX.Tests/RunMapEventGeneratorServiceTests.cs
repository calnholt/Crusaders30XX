using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapEventGeneratorServiceTests
{
	[Fact]
	public void Generate_places_full_landmark_set_for_reported_problem_seed()
	{
		const int seed = 1809532620;
		var (_, nodes) = LocationMapGeneratorService.Generate(seed);
		var shops = RunMapShopGeneratorService.Generate(seed, nodes);
		var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);
		var events = RunMapEventGeneratorService.Generate(seed, nodes, shops, treasures);

		Assert.Equal(LocationMapConstants.RunMapShopCount, shops.Count);
		Assert.Equal(LocationMapConstants.RunMapTreasureCount, treasures.Count);
		Assert.Equal(LocationMapConstants.RunMapEventCount, events.Count);
		AssertAllLandmarksEnterable(nodes, shops, treasures, events);
	}

	[Theory]
	[InlineData(123456)]
	[InlineData(1956459933)]
	[InlineData(1809532620)]
	public void Generate_places_full_landmark_set_for_deterministic_seed_sweep(int seed)
	{
		var (_, nodes) = LocationMapGeneratorService.Generate(seed);
		var shops = RunMapShopGeneratorService.Generate(seed, nodes);
		var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);
		var events = RunMapEventGeneratorService.Generate(seed, nodes, shops, treasures);

		Assert.Equal(LocationMapConstants.RunMapShopCount, shops.Count);
		Assert.Equal(LocationMapConstants.RunMapTreasureCount, treasures.Count);
		Assert.Equal(LocationMapConstants.RunMapEventCount, events.Count);
		AssertAllLandmarksEnterable(nodes, shops, treasures, events);
	}

	[Fact]
	public void Generate_places_events_enterable_after_first_quest_completed_with_distinct_types()
	{
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);
			var events = RunMapEventGeneratorService.Generate(seed, nodes, shops, treasures);
			Assert.Equal(LocationMapConstants.RunMapEventCount, events.Count);

			var typeIds = events.Select(e => e.eventTypeId).ToList();
			Assert.Equal(LocationMapConstants.RunMapEventCount, typeIds.Distinct().Count());

			float revealRadius = LocationMapConstants.DefaultRevealRadius;
			float revealRadiusSq = revealRadius * revealRadius;

			foreach (var mapEvent in events)
			{
				Assert.False(string.IsNullOrWhiteSpace(mapEvent.eventTypeId));

				foreach (var node in nodes)
				{
					if (node != null)
					{
						node.isRevealed = false;
						node.isCompleted = false;
					}
				}

				bool unlocked = false;
				for (int n = 0; n < nodes.Count; n++)
				{
					var node = nodes[n];
					if (node == null) continue;
					float dx = mapEvent.worldX - node.worldX;
					float dy = mapEvent.worldY - node.worldY;
					if (dx * dx + dy * dy > revealRadiusSq) continue;

					node.isRevealed = true;
					node.isCompleted = true;
					unlocked = RunMapEventService.IsEnterable(mapEvent, nodes);
					break;
				}

				Assert.True(
					unlocked,
					$"seed {seed} Map event {mapEvent.id} not enterable when a nearby quest is completed");
			}
		}
	}

	private static void AssertAllLandmarksEnterable(
		List<RunMapNode> nodes,
		IReadOnlyList<RunMapShop> shops,
		IReadOnlyList<RunMapTreasure> treasures,
		IReadOnlyList<RunMapEvent> events)
	{
		MarkAllNodesCompleted(nodes);
		foreach (var shop in shops)
		{
			Assert.True(RunMapShopService.IsEnterable(shop, nodes));
		}

		var depths = RunMapNodeDepthHelper.ComputeDepths(nodes);
		for (int i = 0; i < nodes.Count; i++)
		{
			var node = nodes[i];
			if (node == null) continue;
			node.isRevealed = true;
			node.isCompleted = depths[i] >= LocationMapConstants.RunMapTreasureMinUnlockDepth;
		}

		foreach (var treasure in treasures)
		{
			Assert.True(RunMapTreasureService.IsEnterable(treasure, nodes));
		}

		MarkAllNodesCompleted(nodes);
		foreach (var mapEvent in events)
		{
			Assert.True(RunMapEventService.IsEnterable(mapEvent, nodes));
		}
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
