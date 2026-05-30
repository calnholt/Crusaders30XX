using System;
using System.Collections.Generic;
using System.Linq;

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
			"ninja",
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
			"medusa",
			"wyvern",
			// "blood_martyr",
			"sand_golem",
			// "sniper", // marksman - disabled from run encounters
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

		private static readonly HashSet<string> RunMapExcludedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"gleeber",
			"sand_corpse"
		};

		public static IReadOnlyList<string> GetRunMapEnemyPool()
		{
			return PortraitEnemyIds
				.Where(id => !RunMapExcludedIds.Contains(id))
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
	}
}
