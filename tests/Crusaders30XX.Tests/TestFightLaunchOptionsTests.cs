using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Singletons;
using Xunit;

namespace Crusaders30XX.Tests;

public class TestFightLaunchOptionsTests
{
	[Fact]
	public void TryParse_accepts_case_insensitive_valid_command()
	{
		bool parsed = TestFightLaunchOptions.TryParse(
			new[] { "TEST-FIGHT", "Hammer", "Skeleton", "HARD" },
			out var options);

		Assert.True(parsed);
		Assert.Equal("hammer", options.WeaponId);
		Assert.Equal("skeleton", options.EnemyId);
		Assert.Equal(RunDifficulty.Hard, options.Difficulty);
	}

	[Fact]
	public void TryParse_returns_false_for_other_launch_modes()
	{
		bool parsed = TestFightLaunchOptions.TryParse(
			new[] { "snapshot", "card", "strike" },
			out var options);

		Assert.False(parsed);
		Assert.Null(options);
	}

	[Theory]
	[InlineData("axe", "skeleton", "hard")]
	[InlineData("hammer", "unknown_enemy", "hard")]
	[InlineData("hammer", "skeleton", "nightmare")]
	public void TryParse_rejects_unknown_arguments(string weapon, string enemy, string difficulty)
	{
		Assert.Throws<TestFightSetupException>(() =>
			TestFightLaunchOptions.TryParse(
				new[] { "test-fight", weapon, enemy, difficulty },
				out _));
	}

	[Fact]
	public void TryParse_rejects_missing_arguments()
	{
		var exception = Assert.Throws<TestFightSetupException>(() =>
			TestFightLaunchOptions.TryParse(
				new[] { "test-fight", "hammer", "skeleton" },
				out _));

		Assert.Contains("Usage:", exception.Message);
	}
}
