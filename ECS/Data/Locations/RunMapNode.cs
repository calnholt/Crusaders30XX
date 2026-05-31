using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapNode
	{
		public string id { get; set; } = string.Empty;
		public float worldX { get; set; }
		public float worldY { get; set; }
		public string enemyId { get; set; } = string.Empty;
		/// <summary>When count is greater than 1, the quest chains one battle per id in order.</summary>
		public List<string> battleEnemyIds { get; set; } = new List<string>();
		public int parentIndex { get; set; } = -1;
		public List<int> childIndices { get; set; } = new List<int>();
		public bool isRevealed { get; set; }
		public bool isCompleted { get; set; }

		public bool IsDualBattle => battleEnemyIds != null && battleEnemyIds.Count > 1;

		public IReadOnlyList<string> ResolveBattleEnemyIds()
		{
			if (battleEnemyIds != null && battleEnemyIds.Count > 0)
			{
				return battleEnemyIds;
			}

			if (!string.IsNullOrEmpty(enemyId))
			{
				return new List<string> { enemyId };
			}

			return System.Array.Empty<string>();
		}
	}
}
