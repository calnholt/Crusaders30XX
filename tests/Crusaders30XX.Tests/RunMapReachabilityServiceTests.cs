using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapReachabilityServiceTests
{
	[Fact]
	public void Connected_chain_within_reveal_radius_is_fully_reachable()
	{
		float step = LocationMapConstants.DefaultRevealRadius * 0.9f;
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 3000f, worldY = 1500f, parentIndex = -1 },
			new() { id = "run_1", worldX = 3000f + step, worldY = 1500f, parentIndex = 0 },
			new() { id = "run_2", worldX = 3000f + step * 2f, worldY = 1500f, parentIndex = 1 },
		};

		Assert.True(RunMapReachabilityService.AreAllQuestNodesReachable(nodes));
		Assert.Equal(3, RunMapReachabilityService.SimulateRevealClosure(nodes).RevealedCount);
	}

	[Fact]
	public void Completed_parent_can_reveal_a_nearby_hellrift()
	{
		var nodes = new List<RunMapNode>
		{
			new()
			{
				id = "run_5",
				worldX = 5534.3f,
				worldY = 463.4f,
				parentIndex = -1,
			},
			new()
			{
				id = "run_6",
				combatNodeType = RunMapCombatNodeType.Hellrift,
				worldX = 4702.6f,
				worldY = 748.1f,
				parentIndex = 0,
			},
		};

		var result = RunMapReachabilityService.SimulateRevealClosure(nodes);

		Assert.Equal(2, result.RevealedCount);
		Assert.Empty(result.UnreachableNodeIds);
	}

	[Fact]
	public void Two_clusters_beyond_reveal_radius_are_not_reachable()
	{
		float gap = LocationMapConstants.DefaultRevealRadius * 2.5f;
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 1000f, worldY = 1500f, parentIndex = -1 },
			new() { id = "run_1", worldX = 1000f + gap, worldY = 1500f, parentIndex = 0 },
		};

		Assert.False(RunMapReachabilityService.AreAllQuestNodesReachable(nodes));
		var result = RunMapReachabilityService.SimulateRevealClosure(nodes);
		Assert.Equal(1, result.RevealedCount);
		Assert.Contains("run_1", result.UnreachableNodeIds);
	}

	[Fact]
	public void Fourth_neighbor_in_range_is_reached_on_second_completion_wave()
	{
		float r = LocationMapConstants.DefaultRevealRadius;
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 3000f, worldY = 1500f, parentIndex = -1 },
			new() { id = "run_1", worldX = 3000f + r * 0.5f, worldY = 1500f, parentIndex = 0 },
			new() { id = "run_2", worldX = 3000f, worldY = 1500f + r * 0.6f, parentIndex = 0 },
			new() { id = "run_3", worldX = 3000f - r * 0.7f, worldY = 1500f, parentIndex = 0 },
			new() { id = "run_4", worldX = 3000f, worldY = 1500f - r * 0.8f, parentIndex = 0 },
		};

		Assert.True(RunMapReachabilityService.AreAllQuestNodesReachable(nodes));
	}

	[Fact]
	public void User_save_seed_layout_is_not_reachable()
	{
		var nodes = BuildUserSaveUnreachableLayout();

		Assert.False(RunMapReachabilityService.AreAllQuestNodesReachable(nodes));
		var result = RunMapReachabilityService.SimulateRevealClosure(nodes);
		Assert.Equal(9, result.RevealedCount);
		Assert.Contains("run_1", result.UnreachableNodeIds);
	}

	[Fact]
	public void GetReachableNodeIndices_matches_simulation_revealed_set()
	{
		var nodes = BuildUserSaveUnreachableLayout();
		var reachable = RunMapReachabilityService.GetReachableNodeIndices(nodes);

		Assert.Equal(9, reachable.Count);
		Assert.Contains(0, reachable);
		Assert.DoesNotContain(1, reachable);
	}

	private static List<RunMapNode> BuildUserSaveUnreachableLayout()
	{
		return new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 2943.9385f, worldY = 1863.2103f, parentIndex = -1 },
			new() { id = "run_1", worldX = 1962.9395f, worldY = 1890.26f, parentIndex = 0 },
			new() { id = "run_2", worldX = 3771.8633f, worldY = 1528.1742f, parentIndex = 0 },
			new() { id = "run_3", worldX = 1247.2947f, worldY = 2512.8828f, parentIndex = 0 },
			new() { id = "run_4", worldX = 875.73694f, worldY = 1588.2977f, parentIndex = 3 },
			new() { id = "run_5", worldX = 1718.5184f, worldY = 975.73804f, parentIndex = 1 },
			new() { id = "run_6", worldX = 1346.1552f, worldY = 1830.6698f, parentIndex = 1 },
			new() { id = "run_7", worldX = 670.32947f, worldY = 2343.7324f, parentIndex = 3 },
			new() { id = "run_8", worldX = 3531.6711f, worldY = 1983.2711f, parentIndex = 3 },
			new() { id = "run_9", worldX = 3776.2214f, worldY = 2336.5803f, parentIndex = 1 },
			new() { id = "run_10", worldX = 487.12863f, worldY = 1821.6788f, parentIndex = 7 },
			new() { id = "run_11", worldX = 4594.0405f, worldY = 1779.8887f, parentIndex = 9 },
			new() { id = "run_12", worldX = 1197.2538f, worldY = 1092.5134f, parentIndex = 6 },
			new() { id = "run_13", worldX = 4260.078f, worldY = 735.17474f, parentIndex = 2 },
			new() { id = "run_14", worldX = 2638.2642f, worldY = 766.68176f, parentIndex = 5 },
			new() { id = "run_15", worldX = 1278.6611f, worldY = 472.26556f, parentIndex = 5 },
			new() { id = "run_16", worldX = 5313.8545f, worldY = 1100.5958f, parentIndex = 11 },
			new() { id = "run_17", worldX = 511.9317f, worldY = 803.7167f, parentIndex = 15 },
			new() { id = "run_18", worldX = 4626.033f, worldY = 2514.5215f, parentIndex = 9 },
			new() { id = "run_19", worldX = 5435.6025f, worldY = 2214.3672f, parentIndex = 18 },
		};
	}
}
