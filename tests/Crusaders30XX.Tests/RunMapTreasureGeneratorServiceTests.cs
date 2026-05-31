using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapTreasureGeneratorServiceTests
{
	[Fact]
	public void Generate_places_treasures_enterable_after_depth_two_quests_completed()
	{
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);
			Assert.Equal(LocationMapConstants.RunMapTreasureCount, treasures.Count);

			var depths = RunMapNodeDepthHelper.ComputeDepths(nodes);
			for (int n = 0; n < nodes.Count; n++)
			{
				var node = nodes[n];
				if (node == null) continue;
				node.isRevealed = true;
				node.isCompleted = depths[n] >= LocationMapConstants.RunMapTreasureMinUnlockDepth;
			}

			foreach (var treasure in treasures)
			{
				Assert.True(
					RunMapTreasureService.IsEnterable(treasure, nodes),
					$"seed {seed} treasure {treasure.id} not enterable when depth-{LocationMapConstants.RunMapTreasureMinUnlockDepth}+ quests completed");
			}
		}
	}
}
