using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public static class LocationMapGeneratorService
	{
		public static (int seed, List<RunMapNode> nodes) Generate(int? seedOverride = null)
		{
			int seed = seedOverride ?? Random.Shared.Next();
			var rng = new Random(seed);
			var nodes = new List<RunMapNode>(LocationMapConstants.NodeCount);
			var depths = new int[LocationMapConstants.NodeCount];

			// Root
			nodes.Add(new RunMapNode
			{
				id = NodeId(0),
				worldX = LocationMapConstants.MapCenterX,
				worldY = LocationMapConstants.MapCenterY,
				parentIndex = -1,
				isRevealed = true,
				childIndices = new List<int>(),
			});
			depths[0] = 0;

			var enemyPool = EnemyPortraitContent.GetRunMapEnemyPool().ToList();
			if (enemyPool.Count == 0)
			{
				enemyPool.Add("skeleton");
			}

			// Topology: shallow-biased random tree with bounded fan-out per node
			for (int i = 1; i < LocationMapConstants.NodeCount; i++)
			{
				var eligibleParents = GetEligibleParentIndices(nodes);
				if (eligibleParents.Count == 0)
				{
					throw new InvalidOperationException(
						$"[LocationMapGeneratorService] No eligible parent for node {i}; check MaxChildrenPerNode.");
				}

				int parentIndex = PickParentIndex(rng, eligibleParents, depths);
				depths[i] = depths[parentIndex] + 1;
				var parent = nodes[parentIndex];
				PlaceChild(rng, nodes, parent, out float x, out float y);

				var node = new RunMapNode
				{
					id = NodeId(i),
					worldX = x,
					worldY = y,
					parentIndex = parentIndex,
					enemyId = PickEnemy(rng, enemyPool),
					childIndices = new List<int>(),
				};
				nodes.Add(node);
				parent.childIndices.Add(i);
			}

			nodes[0].enemyId = PickEnemy(rng, enemyPool);

			return (seed, nodes);
		}

		private static string NodeId(int index) => $"run_{index}";

		private static List<int> GetEligibleParentIndices(List<RunMapNode> nodes)
		{
			var list = new List<int>();
			for (int j = 0; j < nodes.Count; j++)
			{
				int childCount = nodes[j].childIndices?.Count ?? 0;
				if (childCount < LocationMapConstants.MaxChildrenPerNode)
				{
					list.Add(j);
				}
			}
			return list;
		}

		private static string PickEnemy(Random rng, List<string> pool)
		{
			if (pool == null || pool.Count == 0) return "skeleton";
			return pool[rng.Next(pool.Count)];
		}

		private static int PickParentIndex(Random rng, List<int> eligibleParents, int[] depths)
		{
			float total = 0f;
			var weights = new float[eligibleParents.Count];
			for (int i = 0; i < eligibleParents.Count; i++)
			{
				float w = 1f / (depths[eligibleParents[i]] + 1);
				weights[i] = w;
				total += w;
			}

			float roll = (float)rng.NextDouble() * total;
			float acc = 0f;
			for (int i = 0; i < eligibleParents.Count; i++)
			{
				acc += weights[i];
				if (roll <= acc) return eligibleParents[i];
			}
			return eligibleParents[eligibleParents.Count - 1];
		}

		private static void PlaceChild(Random rng, List<RunMapNode> nodes, RunMapNode parent, out float x, out float y)
		{
			const int maxAttempts = 48;
			for (int attempt = 0; attempt < maxAttempts; attempt++)
			{
				float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
				float dist = LocationMapConstants.MinStep +
					(float)rng.NextDouble() * (LocationMapConstants.MaxStep - LocationMapConstants.MinStep);
				float cx = parent.worldX + (float)Math.Cos(angle) * dist;
				float cy = parent.worldY + (float)Math.Sin(angle) * dist;
				cx = Clamp(cx, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin);
				cy = Clamp(cy, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin);

				if (!OverlapsExisting(nodes, cx, cy))
				{
					x = cx;
					y = cy;
					return;
				}
			}

			x = Clamp(parent.worldX + LocationMapConstants.MinStep, LocationMapConstants.MapMargin,
				LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin);
			y = parent.worldY;
		}

		private static bool OverlapsExisting(List<RunMapNode> nodes, float x, float y)
		{
			float minDistSq = LocationMapConstants.MinNodeSpacing * LocationMapConstants.MinNodeSpacing;
			foreach (var n in nodes)
			{
				float dx = n.worldX - x;
				float dy = n.worldY - y;
				if (dx * dx + dy * dy < minDistSq) return true;
			}
			return false;
		}

		private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
	}
}
