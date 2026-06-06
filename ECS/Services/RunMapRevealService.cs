using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapRevealService
	{
		public static bool IsWithinRevealRadius(
			float ax,
			float ay,
			float bx,
			float by,
			float radius = -1f)
		{
			if (radius < 0f) radius = LocationMapConstants.DefaultRevealRadius;
			float dx = ax - bx;
			float dy = ay - by;
			float radiusSq = radius * radius;
			return dx * dx + dy * dy <= radiusSq;
		}

		public static List<int> GetIndicesWithinRadius(
			IReadOnlyList<RunMapNode> nodes,
			float originX,
			float originY,
			float radius,
			int excludeIndex = -1)
		{
			var indices = new List<int>();
			if (nodes == null || nodes.Count == 0) return indices;

			for (int i = 0; i < nodes.Count; i++)
			{
				if (i == excludeIndex) continue;
				var node = nodes[i];
				if (node == null) continue;
				if (IsWithinRevealRadius(originX, originY, node.worldX, node.worldY, radius))
				{
					indices.Add(i);
				}
			}

			return indices;
		}

		public static List<string> SelectClosestUnrevealedNodeIds(
			IReadOnlyList<RunMapNode> nodes,
			float originX,
			float originY,
			float radius,
			int maxCount)
		{
			if (nodes == null || nodes.Count == 0 || maxCount <= 0) return new List<string>();

			var candidates = new List<(string id, float distSq)>();
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (!RunMapCombatNodePresentationService.IsRevealEligible(node)
					|| node.isRevealed
					|| string.IsNullOrEmpty(node.id))
				{
					continue;
				}
				if (!IsWithinRevealRadius(originX, originY, node.worldX, node.worldY, radius)) continue;

				float dx = node.worldX - originX;
				float dy = node.worldY - originY;
				candidates.Add((node.id, dx * dx + dy * dy));
			}

			return candidates
				.OrderBy(c => c.distSq)
				.Take(maxCount)
				.Select(c => c.id)
				.ToList();
		}
	}
}
