using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapEventGeneratorService
	{
		private const int PlacementAttemptsPerEvent = 256;
		private const int FarAnchorCandidates = 6;
		private const int EventRngSalt = 0x7E9E7E9E;

		public static List<RunMapEvent> Generate(
			int runMapSeed,
			IReadOnlyList<RunMapNode> nodes,
			IReadOnlyList<RunMapShop> shops,
			IReadOnlyList<RunMapTreasure> treasures)
		{
			if (nodes == null || nodes.Count == 0) return new List<RunMapEvent>();

			var rng = new Random(runMapSeed ^ EventRngSalt);
			var typeIds = PickDistinctEventTypeIds(rng, LocationMapConstants.RunMapEventCount);
			var events = new List<RunMapEvent>(LocationMapConstants.RunMapEventCount);
			var placedPositions = BuildPlacedPositions(shops, treasures);
			var reachableIndices = RunMapReachabilityService.GetReachableNodeIndices(nodes);
			var anchorNodes = nodes
				.Select((n, i) => (Node: n, Index: i))
				.Where(x => x.Node != null && reachableIndices.Contains(x.Index))
				.Select(x => x.Node)
				.ToList();
			if (anchorNodes.Count == 0)
			{
				throw new InvalidOperationException(
					"[RunMapEventGeneratorService] No reachable quest nodes for Map event placement.");
			}

			for (int eventIndex = 0; eventIndex < LocationMapConstants.RunMapEventCount; eventIndex++)
			{
				if (!TryPlaceEvent(rng, anchorNodes, placedPositions, out float x, out float y))
				{
					throw new InvalidOperationException(
						$"[RunMapEventGeneratorService] Failed to place Map event {eventIndex} after {PlacementAttemptsPerEvent} attempts.");
				}

				placedPositions.Add((x, y));
				events.Add(new RunMapEvent
				{
					id = EventId(eventIndex),
					worldX = x,
					worldY = y,
					eventTypeId = typeIds[eventIndex % typeIds.Count],
					isCompleted = false,
				});
			}

			return events;
		}

		private static List<string> PickDistinctEventTypeIds(Random rng, int count)
		{
			var pool = EventFactory.GetAllEvents().Keys.ToList();
			if (pool.Count == 0)
			{
				throw new InvalidOperationException(
					"[RunMapEventGeneratorService] EventFactory has no narrative event types.");
			}

			var shuffled = pool.OrderBy(_ => rng.Next()).ToList();
			if (shuffled.Count >= count)
			{
				return shuffled.Take(count).ToList();
			}

			var result = new List<string>(count);
			for (int i = 0; i < count; i++)
			{
				result.Add(shuffled[i % shuffled.Count]);
			}

			return result;
		}

		private static List<(float x, float y)> BuildPlacedPositions(
			IReadOnlyList<RunMapShop> shops,
			IReadOnlyList<RunMapTreasure> treasures)
		{
			var placed = new List<(float x, float y)>();
			if (shops != null)
			{
				foreach (var shop in shops)
				{
					if (shop == null) continue;
					placed.Add((shop.worldX, shop.worldY));
				}
			}

			if (treasures != null)
			{
				foreach (var treasure in treasures)
				{
					if (treasure == null) continue;
					placed.Add((treasure.worldX, treasure.worldY));
				}
			}

			return placed;
		}

		private static bool TryPlaceEvent(
			Random rng,
			List<RunMapNode> anchorNodes,
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

			for (int attempt = 0; attempt < PlacementAttemptsPerEvent; attempt++)
			{
				var anchor = PickAnchorNode(rng, anchorNodes, placedLandmarks);
				float angle = (float)(rng.NextDouble() * Math.PI * 2);
				float dist = clearance + (float)rng.NextDouble() * (maxDist - clearance);
				float cx = anchor.worldX + (float)Math.Cos(angle) * dist;
				float cy = anchor.worldY + (float)Math.Sin(angle) * dist;

				if (cx < minX || cx > maxX || cy < minY || cy > maxY) continue;
				if (OverlapsBattleNodes(anchorNodes, cx, cy, clearanceSq)) continue;
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

		private static string EventId(int index) => $"event_{index}";
	}
}
