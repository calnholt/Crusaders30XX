using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapTreasureService
	{
		public static bool IsEnterable(RunMapTreasure treasure, IReadOnlyList<RunMapNode> nodes)
		{
			if (treasure == null || treasure.isClaimed || nodes == null || nodes.Count == 0) return false;

			return RunMapLandmarkAccessService.IsWithinCompletedQuestFog(
				treasure.worldX,
				treasure.worldY,
				nodes);
		}

		public static bool TryGetTreasure(
			string treasureId,
			IReadOnlyList<RunMapTreasure> treasures,
			out RunMapTreasure treasure,
			out int index)
		{
			treasure = null;
			index = -1;
			if (treasures == null || string.IsNullOrWhiteSpace(treasureId)) return false;

			for (int i = 0; i < treasures.Count; i++)
			{
				var t = treasures[i];
				if (t != null && string.Equals(t.id, treasureId, StringComparison.OrdinalIgnoreCase))
				{
					treasure = t;
					index = i;
					return true;
				}
			}

			return false;
		}
	}
}
