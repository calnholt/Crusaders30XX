using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapTreasureServiceTests
{
	[Fact]
	public void ComputeDepths_assigns_root_zero_and_increments_by_parent()
	{
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", parentIndex = -1 },
			new() { id = "run_1", parentIndex = 0 },
			new() { id = "run_2", parentIndex = 1 },
		};

		var depths = RunMapNodeDepthHelper.ComputeDepths(nodes);

		Assert.Equal(0, depths[0]);
		Assert.Equal(1, depths[1]);
		Assert.Equal(2, depths[2]);
	}

	[Fact]
	public void IsEnterable_true_within_completed_first_node_fog()
	{
		float radius = LocationMapConstants.DefaultRevealRadius;
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 0f, worldY = 0f, parentIndex = -1, isCompleted = true },
		};

		var treasure = new RunMapTreasure
		{
			id = "treasure_0",
			worldX = radius * 0.5f,
			worldY = 0f,
			isClaimed = false,
		};

		Assert.True(RunMapTreasureService.IsEnterable(treasure, nodes));
	}

	[Fact]
	public void IsEnterable_false_outside_completed_node_fog()
	{
		float radius = LocationMapConstants.DefaultRevealRadius;
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 0f, worldY = 0f, parentIndex = -1, isCompleted = true },
		};

		var treasure = new RunMapTreasure
		{
			id = "treasure_0",
			worldX = radius + 1f,
			worldY = 0f,
			isClaimed = false,
		};

		Assert.False(RunMapTreasureService.IsEnterable(treasure, nodes));
	}

	[Fact]
	public void IsEnterable_false_when_claimed()
	{
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 0f, worldY = 0f, parentIndex = -1, isCompleted = true },
		};

		var treasure = new RunMapTreasure
		{
			id = "treasure_0",
			worldX = 0f,
			worldY = 0f,
			isClaimed = true,
		};

		Assert.False(RunMapTreasureService.IsEnterable(treasure, nodes));
	}
}
