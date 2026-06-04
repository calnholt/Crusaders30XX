using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
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

			Assert.True(top <= LocationMapConstants.MaxNodesPerPlayableEdgeBand,
				$"seed {seed} had {top} nodes in top edge band");
			Assert.True(bottom <= LocationMapConstants.MaxNodesPerPlayableEdgeBand,
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

		Assert.True(top <= LocationMapConstants.MaxNodesPerPlayableEdgeBand,
			$"Regenerated seed {problematicSeed} still clusters {top} nodes along the top edge");
	}

	[Fact]
	public void Generate_assigns_battle_enemy_list_for_every_node()
	{
		var (_, nodes) = LocationMapGeneratorService.Generate(123456);

		foreach (var node in nodes)
		{
			Assert.NotNull(node.battleEnemyIds);
			Assert.NotEmpty(node.battleEnemyIds);
			Assert.Equal(node.battleEnemyIds, node.ResolveBattleEnemyIds());
			Assert.Equal(node.battleEnemyIds[0], node.enemyId);
		}
	}

	[Fact]
	public void Generate_assigns_configured_multi_battle_sequences()
	{
		var (_, nodes) = LocationMapGeneratorService.Generate(123456);
		var multiBattleNodes = nodes
			.Where(node => node.ResolveBattleEnemyIds().Count > 1)
			.ToList();

		Assert.Equal(LocationMapConstants.RunMapMultiBattleQuestCount, multiBattleNodes.Count);
		foreach (var node in multiBattleNodes)
		{
			var enemyIds = node.ResolveBattleEnemyIds();
			Assert.Equal(2, enemyIds.Count);
			Assert.Equal(LocationMapConstants.RunMapMultiBattleFirstEnemyId, enemyIds[0]);
			Assert.Equal(enemyIds[0], node.enemyId);
		}
	}
}
