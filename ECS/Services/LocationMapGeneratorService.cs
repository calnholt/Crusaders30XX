using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public readonly struct RunMapSpreadMetrics
	{
		public int Seed { get; init; }
		public float BboxWidthFraction { get; init; }
		public float BboxHeightFraction { get; init; }
		public float MinPairwiseDistance { get; init; }
		public float MaxDistanceFromCenter { get; init; }

		public string ToLogLine()
		{
			return string.Format(
				System.Globalization.CultureInfo.InvariantCulture,
				"v={0} seed={1} bboxW={2:F2} bboxH={3:F2} minPair={4:F0} maxCenter={5:F0}",
				LocationMapConstants.MapGeneratorVersion,
				Seed,
				BboxWidthFraction,
				BboxHeightFraction,
				MinPairwiseDistance,
				MaxDistanceFromCenter);
		}
	}

	public static class LocationMapGeneratorService
	{
		private const int CandidateAttempts = 32;
		private const int GlobalScatterAttempts = 64;
		private const int SpiralAngleSteps = 16;
		private const int SpiralRingSteps = 12;
		private const int MaxGenerateAttempts = 128;

		public static (int seed, List<RunMapNode> nodes) Generate(int? seedOverride = null)
		{
			if (seedOverride.HasValue)
			{
				var seeded = GenerateCore(seedOverride.Value);
				if (!MeetsSpreadThresholds(ComputeSpreadMetrics(seeded.seed, seeded.nodes), seeded.nodes))
				{
					throw new InvalidOperationException(
						"[LocationMapGeneratorService] Seeded map failed spread thresholds.");
				}
				if (!RunMapReachabilityService.AreAllQuestNodesReachable(seeded.nodes))
				{
					throw new InvalidOperationException(
						"[LocationMapGeneratorService] Seeded map failed reachability thresholds.");
				}
				return seeded;
			}

			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < MaxGenerateAttempts; attempt++)
			{
				try
				{
					var result = GenerateCore(Random.Shared.Next());
					if (!MeetsSpreadThresholds(ComputeSpreadMetrics(result.seed, result.nodes), result.nodes))
					{
						throw new InvalidOperationException(
							"[LocationMapGeneratorService] Generated map failed spread thresholds.");
					}
					if (!RunMapReachabilityService.AreAllQuestNodesReachable(result.nodes))
					{
						throw new InvalidOperationException(
							"[LocationMapGeneratorService] Generated map failed reachability thresholds.");
					}
					return result;
				}
				catch (InvalidOperationException ex)
				{
					lastFailure = ex;
				}
			}

			throw new InvalidOperationException(
				$"[LocationMapGeneratorService] Failed to generate run map after {MaxGenerateAttempts} attempts.",
				lastFailure);
		}

		private static bool MeetsSpreadThresholds(RunMapSpreadMetrics metrics, List<RunMapNode> nodes)
		{
			if (metrics.BboxWidthFraction < LocationMapConstants.MinSpreadBboxWidthFraction) return false;
			if (metrics.BboxHeightFraction < LocationMapConstants.MinSpreadBboxHeightFraction) return false;
			if (metrics.MinPairwiseDistance < LocationMapConstants.MinSpreadPairwiseDistance) return false;
			if (CountNodesInTopPlayableBand(nodes) > LocationMapConstants.MaxNodesPerPlayableEdgeBand) return false;
			if (CountNodesInBottomPlayableBand(nodes) > LocationMapConstants.MaxNodesPerPlayableEdgeBand) return false;
			return true;
		}

		public static int CountNodesInTopPlayableBand(IReadOnlyList<RunMapNode> nodes)
		{
			float maxY = LocationMapConstants.MapMargin +
				LocationMapConstants.PlayableHeight * LocationMapConstants.PlayableEdgeBandFraction;
			return CountNodesInHorizontalBand(nodes, maxYInclusive: maxY);
		}

		public static int CountNodesInBottomPlayableBand(IReadOnlyList<RunMapNode> nodes)
		{
			float minY = LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin -
				LocationMapConstants.PlayableHeight * LocationMapConstants.PlayableEdgeBandFraction;
			return CountNodesInHorizontalBand(nodes, minYInclusive: minY);
		}

		private static int CountNodesInHorizontalBand(
			IReadOnlyList<RunMapNode> nodes,
			float maxYInclusive = float.MaxValue,
			float minYInclusive = float.MinValue)
		{
			if (nodes == null) return 0;
			int count = 0;
			foreach (var node in nodes)
			{
				if (node == null) continue;
				if (node.worldY <= maxYInclusive && node.worldY >= minYInclusive) count++;
			}
			return count;
		}

		private static (int seed, List<RunMapNode> nodes) GenerateCore(int seed)
		{
			var rng = new Random(seed);
			var nodes = new List<RunMapNode>(LocationMapConstants.NodeCount);
			var depths = new int[LocationMapConstants.NodeCount];

			var (rootX, rootY) = PlaceRoot(rng);
			nodes.Add(new RunMapNode
			{
				id = NodeId(0),
				worldX = rootX,
				worldY = rootY,
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
			AssignDualBattles(rng, nodes, enemyPool);

#if DEBUG
			var metrics = ComputeSpreadMetrics(seed, nodes);
			System.Console.WriteLine($"[LocationMapGenerator] {metrics.ToLogLine()}");
#endif

			return (seed, nodes);
		}

		public static RunMapSpreadMetrics ComputeSpreadMetrics(int seed, List<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0)
			{
				return new RunMapSpreadMetrics { Seed = seed };
			}

			float minX = float.MaxValue, maxX = float.MinValue;
			float minY = float.MaxValue, maxY = float.MinValue;
			float maxCenter = 0f;
			float centerX = LocationMapConstants.MapCenterX;
			float centerY = LocationMapConstants.MapCenterY;

			foreach (var n in nodes)
			{
				if (n == null) continue;
				minX = Math.Min(minX, n.worldX);
				maxX = Math.Max(maxX, n.worldX);
				minY = Math.Min(minY, n.worldY);
				maxY = Math.Max(maxY, n.worldY);
				float dx = n.worldX - centerX;
				float dy = n.worldY - centerY;
				maxCenter = Math.Max(maxCenter, (float)Math.Sqrt(dx * dx + dy * dy));
			}

			float bboxW = LocationMapConstants.PlayableWidth > 0f
				? (maxX - minX) / LocationMapConstants.PlayableWidth
				: 0f;
			float bboxH = LocationMapConstants.PlayableHeight > 0f
				? (maxY - minY) / LocationMapConstants.PlayableHeight
				: 0f;

			float minPair = float.MaxValue;
			for (int i = 0; i < nodes.Count; i++)
			{
				var a = nodes[i];
				if (a == null) continue;
				for (int j = i + 1; j < nodes.Count; j++)
				{
					var b = nodes[j];
					if (b == null) continue;
					float dx = a.worldX - b.worldX;
					float dy = a.worldY - b.worldY;
					float d = (float)Math.Sqrt(dx * dx + dy * dy);
					if (d < minPair) minPair = d;
				}
			}

			if (minPair == float.MaxValue) minPair = 0f;

			return new RunMapSpreadMetrics
			{
				Seed = seed,
				BboxWidthFraction = bboxW,
				BboxHeightFraction = bboxH,
				MinPairwiseDistance = minPair,
				MaxDistanceFromCenter = maxCenter,
			};
		}

		private static string NodeId(int index) => $"run_{index}";

		private static (float x, float y) PlaceRoot(Random rng)
		{
			float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
			float r = (float)Math.Sqrt(rng.NextDouble()) * LocationMapConstants.RootWiggleRadius;
			float x = LocationMapConstants.MapCenterX + (float)Math.Cos(angle) * r;
			float y = LocationMapConstants.MapCenterY + (float)Math.Sin(angle) * r;
			x = Clamp(x, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin);
			y = Clamp(y, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin);
			return (x, y);
		}

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

		private static void AssignDualBattles(Random rng, List<RunMapNode> nodes, List<string> enemyPool)
		{
			if (nodes == null || nodes.Count <= 1) return;

			int dualCount = Math.Min(
				LocationMapConstants.RunMapDualBattleQuestCount,
				nodes.Count - 1);

			var candidateIndices = Enumerable.Range(1, nodes.Count - 1)
				.OrderBy(_ => rng.Next())
				.Take(dualCount)
				.ToList();

			foreach (int index in candidateIndices)
			{
				var node = nodes[index];
				if (node == null) continue;

				string secondEnemy = rng.Next(2) == 0
					? LocationMapConstants.RunMapDualBattleFirstEnemyId
					: PickEnemy(rng, enemyPool);

				node.battleEnemyIds = new List<string>
				{
					LocationMapConstants.RunMapDualBattleFirstEnemyId,
					secondEnemy,
				};
				node.enemyId = LocationMapConstants.RunMapDualBattleFirstEnemyId;
			}
		}

		private static int PickParentIndex(Random rng, List<int> eligibleParents, int[] depths)
		{
			int maxDepth = 0;
			for (int i = 0; i < eligibleParents.Count; i++)
			{
				maxDepth = Math.Max(maxDepth, depths[eligibleParents[i]]);
			}

			float total = 0f;
			var weights = new float[eligibleParents.Count];
			for (int i = 0; i < eligibleParents.Count; i++)
			{
				float w = maxDepth - depths[eligibleParents[i]] + 1;
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
			int childCount = parent.childIndices?.Count ?? 0;
			if (childCount >= LocationMapConstants.MaxChildrenPerNode - 1)
			{
				if (TryGlobalScatterPlacement(rng, nodes, out x, out y)) return;
			}

			float bestX = 0f, bestY = 0f;
			float bestScore = -1f;
			bool found = false;

			for (int attempt = 0; attempt < CandidateAttempts; attempt++)
			{
				if (!TryRandomCandidate(rng, parent, nodes, out float cx, out float cy)) continue;
				float score = ScorePlacementCandidate(nodes, cx, cy);
				if (!found || score > bestScore)
				{
					found = true;
					bestScore = score;
					bestX = cx;
					bestY = cy;
				}
			}

			if (found)
			{
				x = bestX;
				y = bestY;
				return;
			}

			if (TrySpiralPlacement(nodes, parent, out x, out y)) return;
			if (TryGlobalScatterPlacement(rng, nodes, out x, out y)) return;

			throw new InvalidOperationException(
				$"[LocationMapGeneratorService] Failed to place child of {parent.id} without violating min spacing.");
		}

		private static bool TryGlobalScatterPlacement(Random rng, List<RunMapNode> nodes, out float x, out float y)
		{
			float minX = LocationMapConstants.MapMargin;
			float maxX = LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin;
			float minY = LocationMapConstants.MapMargin;
			float maxY = LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin;

			for (int attempt = 0; attempt < GlobalScatterAttempts; attempt++)
			{
				float cx = minX + (float)rng.NextDouble() * (maxX - minX);
				float cy = minY + (float)rng.NextDouble() * (maxY - minY);
				if (IsValidPlacement(nodes, cx, cy))
				{
					x = cx;
					y = cy;
					return true;
				}
			}

			x = 0f;
			y = 0f;
			return false;
		}

		private static bool TryRandomCandidate(Random rng, RunMapNode parent, List<RunMapNode> nodes, out float cx, out float cy)
		{
			float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
			float dist = LocationMapConstants.MinStep +
				(float)rng.NextDouble() * (LocationMapConstants.MaxStep - LocationMapConstants.MinStep);
			cx = parent.worldX + (float)Math.Cos(angle) * dist;
			cy = parent.worldY + (float)Math.Sin(angle) * dist;
			cx = Clamp(cx, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin);
			cy = Clamp(cy, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin);
			return IsValidPlacement(nodes, cx, cy);
		}

		private static bool TrySpiralPlacement(List<RunMapNode> nodes, RunMapNode parent, out float x, out float y)
		{
			float bestX = 0f, bestY = 0f;
			float bestScore = -1f;
			bool found = false;

			float maxDist = Math.Max(
				LocationMapConstants.MaxStep,
				LocationMapConstants.PlayableMinDimension * 0.45f);

			for (int ring = 1; ring <= SpiralRingSteps; ring++)
			{
				float t = ring / (float)SpiralRingSteps;
				float dist = LocationMapConstants.MinStep + t * (maxDist - LocationMapConstants.MinStep);
				for (int a = 0; a < SpiralAngleSteps; a++)
				{
					float angle = a * (float)(Math.PI * 2.0 / SpiralAngleSteps);
					float cx = parent.worldX + (float)Math.Cos(angle) * dist;
					float cy = parent.worldY + (float)Math.Sin(angle) * dist;
					cx = Clamp(cx, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin);
					cy = Clamp(cy, LocationMapConstants.MapMargin, LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin);
					if (!IsValidPlacement(nodes, cx, cy)) continue;

					float score = ScorePlacementCandidate(nodes, cx, cy);
					if (!found || score > bestScore)
					{
						found = true;
						bestScore = score;
						bestX = cx;
						bestY = cy;
					}
				}
			}

			if (found)
			{
				x = bestX;
				y = bestY;
				return true;
			}

			x = 0f;
			y = 0f;
			return false;
		}

		private static float DistanceFromMapCenter(float x, float y)
		{
			float dx = x - LocationMapConstants.MapCenterX;
			float dy = y - LocationMapConstants.MapCenterY;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}

		private static float MinDistanceToAnyNode(List<RunMapNode> nodes, float x, float y)
		{
			float minDist = 0f;
			bool any = false;
			foreach (var n in nodes)
			{
				if (n == null) continue;
				float dx = n.worldX - x;
				float dy = n.worldY - y;
				float d = (float)Math.Sqrt(dx * dx + dy * dy);
				if (!any || d < minDist)
				{
					minDist = d;
					any = true;
				}
			}

			return any ? minDist : 0f;
		}

		private static bool IsValidPlacement(List<RunMapNode> nodes, float x, float y)
		{
			if (OverlapsExisting(nodes, x, y)) return false;
			if (MinDistanceToPlayableEdge(x, y) < LocationMapConstants.MinPlacementEdgeInset) return false;
			return true;
		}

		private static float ScorePlacementCandidate(List<RunMapNode> nodes, float x, float y)
		{
			return MinDistanceToAnyNode(nodes, x, y)
				+ 0.25f * DistanceFromMapCenter(x, y)
				+ 0.4f * MinDistanceToPlayableEdge(x, y);
		}

		private static float MinDistanceToPlayableEdge(float x, float y)
		{
			float minX = LocationMapConstants.MapMargin;
			float maxX = LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin;
			float minY = LocationMapConstants.MapMargin;
			float maxY = LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin;
			float toLeft = x - minX;
			float toRight = maxX - x;
			float toTop = y - minY;
			float toBottom = maxY - y;
			return Math.Min(Math.Min(toLeft, toRight), Math.Min(toTop, toBottom));
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
