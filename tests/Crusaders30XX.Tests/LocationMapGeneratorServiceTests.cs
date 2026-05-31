using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class LocationMapGeneratorServiceTests
{
	[Fact]
	public void Generate_maps_respect_top_and_bottom_edge_band_limits()
	{
		const int attempts = 48;
		int passed = 0;

		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			int top = LocationMapGeneratorService.CountNodesInTopPlayableBand(nodes);
			int bottom = LocationMapGeneratorService.CountNodesInBottomPlayableBand(nodes);

			Assert.True(top <= ECS.Data.Locations.LocationMapConstants.MaxNodesPerPlayableEdgeBand,
				$"seed {seed} had {top} nodes in top edge band");
			Assert.True(bottom <= ECS.Data.Locations.LocationMapConstants.MaxNodesPerPlayableEdgeBand,
				$"seed {seed} had {bottom} nodes in bottom edge band");
			Assert.True(RunMapReachabilityService.AreAllQuestNodesReachable(nodes),
				$"seed {seed} had unreachable quest nodes");
			passed++;
		}

		Assert.Equal(attempts, passed);
	}

	[Fact]
	public void User_save_seed_would_not_place_five_nodes_in_top_band()
	{
		const int problematicSeed = 1956459933;
		var (_, nodes) = LocationMapGeneratorService.Generate(problematicSeed);
		int top = LocationMapGeneratorService.CountNodesInTopPlayableBand(nodes);

		Assert.True(top <= ECS.Data.Locations.LocationMapConstants.MaxNodesPerPlayableEdgeBand,
			$"Regenerated seed {problematicSeed} still clusters {top} nodes along the top edge");
	}
}
