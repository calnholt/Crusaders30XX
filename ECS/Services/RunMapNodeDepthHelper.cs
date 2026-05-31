using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapNodeDepthHelper
	{
		public static int[] ComputeDepths(IReadOnlyList<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0) return System.Array.Empty<int>();

			var depths = new int[nodes.Count];
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null)
				{
					depths[i] = 0;
					continue;
				}

				int parent = node.parentIndex;
				if (parent < 0 || parent >= nodes.Count)
				{
					depths[i] = 0;
				}
				else
				{
					depths[i] = depths[parent] + 1;
				}
			}

			return depths;
		}

		public static int GetDepth(IReadOnlyList<RunMapNode> nodes, int nodeIndex)
		{
			if (nodes == null || nodeIndex < 0 || nodeIndex >= nodes.Count) return 0;
			var depths = ComputeDepths(nodes);
			return depths[nodeIndex];
		}
	}
}
