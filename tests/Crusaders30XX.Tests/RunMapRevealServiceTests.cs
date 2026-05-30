using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapRevealServiceTests
{
	[Fact]
	public void SelectClosestUnrevealedNodeIds_caps_at_max_per_completion()
	{
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 3844f, worldY = 1051f, isRevealed = true },
			new() { id = "run_1", worldX = 4480f, worldY = 320f },
			new() { id = "run_2", worldX = 3580f, worldY = 200f },
			new() { id = "run_3", worldX = 5664f, worldY = 2519f },
			new() { id = "run_7", worldX = 4024f, worldY = 200f },
			new() { id = "run_8", worldX = 4920f, worldY = 200f },
		};

		var picked = RunMapRevealService.SelectClosestUnrevealedNodeIds(
			nodes,
			3844f,
			1051f,
			LocationMapConstants.DefaultRevealRadius,
			LocationMapConstants.MaxQuestRevealsPerCompletion);

		Assert.Equal(LocationMapConstants.MaxQuestRevealsPerCompletion, picked.Count);
		Assert.Contains("run_2", picked);
		Assert.Contains("run_1", picked);
		Assert.Contains("run_7", picked);
		Assert.DoesNotContain("run_8", picked);
		Assert.DoesNotContain("run_3", picked);
	}

	[Fact]
	public void SelectClosestUnrevealedNodeIds_returns_fewer_when_not_enough_in_radius()
	{
		var nodes = new List<RunMapNode>
		{
			new() { id = "run_0", worldX = 3000f, worldY = 1500f, isRevealed = true },
			new() { id = "run_1", worldX = 3200f, worldY = 1500f },
		};

		var picked = RunMapRevealService.SelectClosestUnrevealedNodeIds(
			nodes,
			3000f,
			1500f,
			500f,
			LocationMapConstants.MaxQuestRevealsPerCompletion);

		Assert.Single(picked);
		Assert.Equal("run_1", picked[0]);
	}
}
