using System;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.Diagnostics
{
	public sealed class TestFightLaunchOptions
	{
		public const string Command = "test-fight";

		public string WeaponId { get; init; } = string.Empty;
		public string EnemyId { get; init; } = string.Empty;
		public RunDifficulty Difficulty { get; init; } = RunDifficulty.Easy;

		public static bool TryParse(string[] args, out TestFightLaunchOptions options)
		{
			options = null;
			if (args == null || args.Length == 0 ||
				!string.Equals(args[0], Command, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (args.Length != 4)
			{
				throw new TestFightSetupException(
					"Usage: dotnet run -- test-fight <sword|dagger|hammer> <enemy-id> <easy|normal|hard>");
			}

			string weaponId = args[1].Trim().ToLowerInvariant();
			if (weaponId is not ("sword" or "dagger" or "hammer"))
			{
				throw new TestFightSetupException(
					$"Unknown test-fight weapon '{args[1]}'. Expected sword, dagger, or hammer.");
			}

			string enemyId = args[2].Trim().ToLowerInvariant();
			if (!EnemyFactory.IsRegistered(enemyId))
			{
				throw new TestFightSetupException($"Unknown test-fight enemy '{args[2]}'.");
			}

			if (!TryParseDifficulty(args[3], out var difficulty))
			{
				throw new TestFightSetupException(
					$"Unknown test-fight difficulty '{args[3]}'. Expected easy, normal, or hard.");
			}

			options = new TestFightLaunchOptions
			{
				WeaponId = weaponId,
				EnemyId = enemyId,
				Difficulty = difficulty,
			};
			return true;
		}

		private static bool TryParseDifficulty(string value, out RunDifficulty difficulty)
		{
			switch (value?.Trim().ToLowerInvariant())
			{
				case "easy":
					difficulty = RunDifficulty.Easy;
					return true;
				case "normal":
					difficulty = RunDifficulty.Normal;
					return true;
				case "hard":
					difficulty = RunDifficulty.Hard;
					return true;
				default:
					difficulty = RunDifficulty.Easy;
					return false;
			}
		}
	}

	public sealed class TestFightSetupException : Exception
	{
		public TestFightSetupException(string message) : base(message) { }
	}
}
