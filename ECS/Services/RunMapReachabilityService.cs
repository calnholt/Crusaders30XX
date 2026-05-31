using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public readonly struct RunMapRevealSimulationResult
	{
		public int RevealedCount { get; init; }
		public IReadOnlyList<string> UnreachableNodeIds { get; init; }
	}

	public static class RunMapReachabilityService
	{
		public static bool AreAllQuestNodesReachable(IReadOnlyList<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0) return false;
			var result = SimulateRevealClosure(nodes);
			return result.RevealedCount >= nodes.Count;
		}

		public static HashSet<int> GetReachableNodeIndices(IReadOnlyList<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0) return new HashSet<int>();

			var snapshot = SnapshotRevealFlags(nodes);
			try
			{
				var revealed = RunSimulation(nodes);
				return revealed;
			}
			finally
			{
				RestoreRevealFlags(nodes, snapshot);
			}
		}

		public static RunMapRevealSimulationResult SimulateRevealClosure(IReadOnlyList<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0)
			{
				return new RunMapRevealSimulationResult
				{
					RevealedCount = 0,
					UnreachableNodeIds = Array.Empty<string>(),
				};
			}

			var snapshot = SnapshotRevealFlags(nodes);
			HashSet<int> revealed;
			try
			{
				revealed = RunSimulation(nodes);
			}
			finally
			{
				RestoreRevealFlags(nodes, snapshot);
			}

			var unreachable = new List<string>();
			for (int i = 0; i < nodes.Count; i++)
			{
				if (revealed.Contains(i)) continue;
				var node = nodes[i];
				unreachable.Add(node?.id ?? $"run_{i}");
			}

			return new RunMapRevealSimulationResult
			{
				RevealedCount = revealed.Count,
				UnreachableNodeIds = unreachable,
			};
		}

		private static HashSet<int> RunSimulation(IReadOnlyList<RunMapNode> nodes)
		{
			var revealed = BuildInitialRevealedSet(nodes);
			var completed = new HashSet<int>();
			SyncRevealFlags(nodes, revealed, completed);

			float revealRadius = LocationMapConstants.DefaultRevealRadius;
			int maxReveals = LocationMapConstants.MaxQuestRevealsPerCompletion;
			bool progressed;

			do
			{
				progressed = false;
				var fightable = new List<int>();
				for (int i = 0; i < nodes.Count; i++)
				{
					if (revealed.Contains(i) && !completed.Contains(i))
					{
						fightable.Add(i);
					}
				}

				if (fightable.Count == 0) break;

				foreach (int index in fightable)
				{
					completed.Add(index);
					var node = nodes[index];
					if (node == null) continue;

					var toReveal = RunMapRevealService.SelectClosestUnrevealedNodeIds(
						nodes,
						node.worldX,
						node.worldY,
						revealRadius,
						maxReveals);

					foreach (string id in toReveal)
					{
						int childIndex = FindNodeIndexById(nodes, id);
						if (childIndex < 0 || revealed.Contains(childIndex)) continue;
						revealed.Add(childIndex);
						nodes[childIndex].isRevealed = true;
						progressed = true;
					}
				}
			}
			while (progressed);

			return revealed;
		}

		private static HashSet<int> BuildInitialRevealedSet(IReadOnlyList<RunMapNode> nodes)
		{
			var revealed = new HashSet<int>();
			if (nodes.Count > 0)
			{
				revealed.Add(0);
			}

			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node != null && node.isRevealed)
				{
					revealed.Add(i);
				}
			}

			return revealed;
		}

		private static void SyncRevealFlags(
			IReadOnlyList<RunMapNode> nodes,
			HashSet<int> revealed,
			HashSet<int> completed)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null) continue;
				node.isRevealed = revealed.Contains(i);
				node.isCompleted = completed.Contains(i);
			}
		}

		private static (bool[] revealed, bool[] completed) SnapshotRevealFlags(IReadOnlyList<RunMapNode> nodes)
		{
			var revealed = new bool[nodes.Count];
			var completed = new bool[nodes.Count];
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null) continue;
				revealed[i] = node.isRevealed;
				completed[i] = node.isCompleted;
			}

			return (revealed, completed);
		}

		private static void RestoreRevealFlags(
			IReadOnlyList<RunMapNode> nodes,
			(bool[] revealed, bool[] completed) snapshot)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null) continue;
				node.isRevealed = snapshot.revealed[i];
				node.isCompleted = snapshot.completed[i];
			}
		}

		private static int FindNodeIndexById(IReadOnlyList<RunMapNode> nodes, string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId)) return -1;

			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node != null && string.Equals(node.id, nodeId, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}
	}
}
