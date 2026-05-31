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
	public void IsEnterable_requires_completed_depth_two_fog_not_depth_one()
	{
		float radius = LocationMapConstants.DefaultRevealRadius;
		float treasureX = radius * 0.5f;
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 0f, worldY = 0f, parentIndex = -1, isCompleted = true },
			new() { id = "run_1", worldX = treasureX, worldY = 0f, parentIndex = 0, isCompleted = true },
			new() { id = "run_2", worldX = treasureX, worldY = 0f, parentIndex = 1, isCompleted = false },
		};

		var depths = RunMapNodeDepthHelper.ComputeDepths(nodes);
		Assert.Equal(1, depths[1]);
		Assert.Equal(2, depths[2]);

		var treasure = new RunMapTreasure
		{
			id = "treasure_0",
			worldX = treasureX,
			worldY = 0f,
			isClaimed = false,
		};

		Assert.False(RunMapTreasureService.IsEnterable(treasure, nodes));

		nodes[2].isCompleted = true;
		Assert.True(RunMapTreasureService.IsEnterable(treasure, nodes));
	}

	[Fact]
	public void IsEnterable_false_when_claimed()
	{
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 0f, worldY = 0f, parentIndex = -1, isCompleted = true },
			new() { id = "run_1", worldX = 0f, worldY = 0f, parentIndex = 0, isCompleted = true },
			new() { id = "run_2", worldX = 0f, worldY = 0f, parentIndex = 1, isCompleted = true },
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
