using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	/// <summary>
	/// Maps enemy ids to MonoGame content asset names for battle portraits.
	/// </summary>
	public static class EnemyPortraitContent
	{
		private static readonly HashSet<string> PortraitEnemyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"demon",
			"mummy",
			// "ninja",
			"ogre",
			"skeleton",
			"skeletal_archer",
			"spider",
			"succubus",
			"cactus",
			"dust_wuurm",
			"sorcerer",
			"ice_demon",
			"glacial_guardian",
			"cinderbolt_demon",
			"fire_skeleton",
			"berserker",
			"shadow",
			// "medusa",
			"wyvern",
			// "blood_martyr",
			"sand_golem",
			"fallen_shepherd",
			// "sniper", // marksman - disabled from run encounters
			// "training_demon", // test-fight only
		};

		public static string ToAssetName(string enemyId)
		{
			if (string.IsNullOrEmpty(enemyId)) return string.Empty;
			var parts = enemyId.Split('_');
			return string.Join("_", parts.Select(p =>
			{
				if (string.IsNullOrEmpty(p)) return p;
				return char.ToUpperInvariant(p[0]) + p.Substring(1);
			}));
		}

		public static bool HasPortrait(string enemyId) =>
			!string.IsNullOrEmpty(enemyId) && PortraitEnemyIds.Contains(enemyId);

		public static IReadOnlyList<string> GetRunMapEnemyPool()
		{
			return EnemyFactory.GetAllEnemies()
				.Where(entry => entry.Value != null
					&& !entry.Value.IsBoss
					&& !entry.Value.IsTutorialOnly
					&& HasPortrait(entry.Key)
				)
				.Select(entry => entry.Key)
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
	}
}
