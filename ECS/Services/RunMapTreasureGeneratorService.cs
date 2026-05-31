using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapTreasureGeneratorService
	{
		private const int PlacementAttemptsPerTreasure = 256;
		private const int FarAnchorCandidates = 6;
		private const int TreasureRngSalt = 0x7E457E45;

		public static List<RunMapTreasure> Generate(
			int runMapSeed,
			IReadOnlyList<RunMapNode> nodes,
			IReadOnlyList<RunMapShop> shops)
		{
			if (nodes == null || nodes.Count == 0) return new List<RunMapTreasure>();

			var rng = new Random(runMapSeed ^ TreasureRngSalt);
			var depths = RunMapNodeDepthHelper.ComputeDepths(nodes);
			var reachableIndices = RunMapReachabilityService.GetReachableNodeIndices(nodes);
			var anchorNodes = nodes
				.Select((n, i) => (Node: n, Index: i))
				.Where(x => x.Node != null
					&& reachableIndices.Contains(x.Index)
					&& depths[x.Index] >= LocationMapConstants.RunMapTreasureMinUnlockDepth)
				.Select(x => x.Node)
				.ToList();
			if (anchorNodes.Count == 0)
			{
				throw new InvalidOperationException(
					"[RunMapTreasureGeneratorService] No quest nodes at minimum unlock depth for Treasure Chest placement.");
			}

			var treasures = new List<RunMapTreasure>(LocationMapConstants.RunMapTreasureCount);
			var placedPositions = BuildPlacedPositions(shops);

			for (int treasureIndex = 0; treasureIndex < LocationMapConstants.RunMapTreasureCount; treasureIndex++)
			{
				if (!TryPlaceTreasure(rng, anchorNodes, nodes, placedPositions, out float x, out float y))
				{
					throw new InvalidOperationException(
						$"[RunMapTreasureGeneratorService] Failed to place treasure {treasureIndex} after {PlacementAttemptsPerTreasure} attempts.");
				}

				placedPositions.Add((x, y));
				int gold = rng.Next(
					LocationMapConstants.RunMapTreasureGoldMin,
					LocationMapConstants.RunMapTreasureGoldMax + 1);
				treasures.Add(new RunMapTreasure
				{
					id = TreasureId(treasureIndex),
					worldX = x,
					worldY = y,
					rewardGold = gold,
					isClaimed = false,
				});
			}

			return treasures;
		}

		private static List<(float x, float y)> BuildPlacedPositions(IReadOnlyList<RunMapShop> shops)
		{
			var placed = new List<(float x, float y)>();
			if (shops == null) return placed;

			foreach (var shop in shops)
			{
				if (shop == null) continue;
				placed.Add((shop.worldX, shop.worldY));
			}

			return placed;
		}

		private static bool TryPlaceTreasure(
			Random rng,
			List<RunMapNode> anchorNodes,
			IReadOnlyList<RunMapNode> allNodes,
			List<(float x, float y)> placedLandmarks,
			out float x,
			out float y)
		{
			float clearance = LocationMapConstants.RunMapShopClearanceFromQuest;
			float clearanceSq = clearance * clearance;
			float maxDist = LocationMapConstants.DefaultRevealRadius;
			float landmarkSep = LocationMapConstants.RunMapShopMinSeparation;
			float landmarkSepSq = landmarkSep * landmarkSep;

			float minX = LocationMapConstants.MapMargin + clearance;
			float maxX = LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin - clearance;
			float minY = LocationMapConstants.MapMargin + clearance;
			float maxY = LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin - clearance;

			for (int attempt = 0; attempt < PlacementAttemptsPerTreasure; attempt++)
			{
				var anchor = PickAnchorNode(rng, anchorNodes, placedLandmarks);
				float angle = (float)(rng.NextDouble() * Math.PI * 2);
				float dist = clearance + (float)rng.NextDouble() * (maxDist - clearance);
				float cx = anchor.worldX + (float)Math.Cos(angle) * dist;
				float cy = anchor.worldY + (float)Math.Sin(angle) * dist;

				if (cx < minX || cx > maxX || cy < minY || cy > maxY) continue;
				if (OverlapsBattleNodes(allNodes, cx, cy, clearanceSq)) continue;
				if (OverlapsPlacedLandmarks(placedLandmarks, cx, cy, landmarkSepSq)) continue;

				x = cx;
				y = cy;
				return true;
			}

			x = 0f;
			y = 0f;
			return false;
		}

		private static bool OverlapsBattleNodes(IReadOnlyList<RunMapNode> nodes, float x, float y, float clearanceSq)
		{
			foreach (var node in nodes)
			{
				if (node == null) continue;
				float dx = x - node.worldX;
				float dy = y - node.worldY;
				if (dx * dx + dy * dy < clearanceSq) return true;
			}

			return false;
		}

		private static bool OverlapsPlacedLandmarks(List<(float x, float y)> placed, float x, float y, float sepSq)
		{
			foreach (var landmark in placed)
			{
				float dx = x - landmark.x;
				float dy = y - landmark.y;
				if (dx * dx + dy * dy < sepSq) return true;
			}

			return false;
		}

		private static RunMapNode PickAnchorNode(
			Random rng,
			List<RunMapNode> anchorNodes,
			List<(float x, float y)> placedLandmarks)
		{
			if (placedLandmarks.Count == 0)
			{
				return anchorNodes[rng.Next(anchorNodes.Count)];
			}

			var ranked = anchorNodes
				.Select(n =>
				{
					float minDistSq = float.MaxValue;
					foreach (var landmark in placedLandmarks)
					{
						float dx = n.worldX - landmark.x;
						float dy = n.worldY - landmark.y;
						float dSq = dx * dx + dy * dy;
						if (dSq < minDistSq) minDistSq = dSq;
					}
					return (Node: n, MinDistSq: minDistSq);
				})
				.OrderByDescending(x => x.MinDistSq)
				.Take(FarAnchorCandidates)
				.ToList();

			if (ranked.Count == 0)
			{
				return anchorNodes[rng.Next(anchorNodes.Count)];
			}

			return ranked[rng.Next(ranked.Count)].Node;
		}

		private static string TreasureId(int index) => $"treasure_{index}";
	}
}
