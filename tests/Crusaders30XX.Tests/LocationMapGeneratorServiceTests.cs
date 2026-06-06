using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;
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
	public void Generate_never_assigns_a_boss_to_normal_quest_battles()
	{
		for (int attempt = 0; attempt < 32; attempt++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var normalEnemyIds = nodes
				.Where(node => node.combatNodeType == RunMapCombatNodeType.Quest)
				.SelectMany(node => node.ResolveBattleEnemyIds());

			foreach (string enemyId in normalEnemyIds)
			{
				var enemy = EnemyFactory.Create(enemyId);
				Assert.NotNull(enemy);
				Assert.False(enemy.IsBoss, $"seed {seed} assigned boss {enemyId} to a normal quest");
			}
		}
	}

	[Fact]
	public void Generate_assigns_configured_multi_battle_sequences()
	{
		var (_, nodes) = LocationMapGeneratorService.Generate(123456);
		var multiBattleNodes = nodes
			.Where(node => node.combatNodeType == RunMapCombatNodeType.Quest)
			.Where(node => node.ResolveBattleEnemyIds().Count > 1)
			.ToList();

		Assert.InRange(
			multiBattleNodes.Count,
			LocationMapConstants.RunMapMultiBattleQuestCount - 1,
			LocationMapConstants.RunMapMultiBattleQuestCount);
		foreach (var node in multiBattleNodes)
		{
			var enemyIds = node.ResolveBattleEnemyIds();
			Assert.Equal(2, enemyIds.Count);
			Assert.Equal(LocationMapConstants.RunMapMultiBattleFirstEnemyId, enemyIds[0]);
			Assert.Equal(enemyIds[0], node.enemyId);
		}
	}

	[Fact]
	public void Generate_places_one_gate_on_a_deepest_leaf()
	{
		for (int attempt = 0; attempt < 32; attempt++)
		{
			var (_, nodes) = LocationMapGeneratorService.Generate();
			var depths = RunMapNodeDepthHelper.ComputeDepths(nodes);
			var gateEntries = nodes
				.Select((node, index) => new { node, index })
				.Where(x => x.node.combatNodeType == RunMapCombatNodeType.Hellrift)
				.ToList();

			Assert.Equal(LocationMapConstants.NodeCount, nodes.Count);
			Assert.Single(gateEntries);
			Assert.Equal(LocationMapConstants.NodeCount - 1,
				nodes.Count(node => node.combatNodeType == RunMapCombatNodeType.Quest));

			var gate = gateEntries[0];
			Assert.Empty(gate.node.childIndices);
			Assert.True(depths[gate.index] >= 6);
			Assert.Equal(
				depths.Where((_, index) => nodes[index].childIndices.Count == 0).Max(),
				depths[gate.index]);
			Assert.Equal(new[] { "fallen_shepherd" }, gate.node.ResolveBattleEnemyIds());
		}
	}

	[Fact]
	public void Gate_presentation_is_hidden_until_normal_reveal_and_has_no_reward()
	{
		var gate = new RunMapNode
		{
			combatNodeType = RunMapCombatNodeType.Hellrift,
			battleEnemyIds = new System.Collections.Generic.List<string> { "fallen_shepherd" },
		};

		Assert.False(RunMapCombatNodePresentationService.IsVisible(gate));
		Assert.Equal("The Gate", RunMapCombatNodePresentationService.GetTitle(gate, 12));
		Assert.Equal(0, RunMapCombatNodePresentationService.GetRewardGold(gate));
		Assert.Equal(
			Crusaders30XX.ECS.Components.PointOfInterestType.Hellrift,
			RunMapCombatNodePresentationService.GetPoiType(gate));

		gate.isRevealed = true;
		Assert.True(RunMapCombatNodePresentationService.IsVisible(gate));
	}
}
