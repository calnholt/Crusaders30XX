using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapEventGeneratorService
	{
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
				if (!RunMapLandmarkPlacementService.TryPlace(rng, anchorNodes, nodes, placedPositions, out float x, out float y))
				{
					throw new InvalidOperationException(
						$"[RunMapEventGeneratorService] Failed to place Map event {eventIndex}.");
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

		private static string EventId(int index) => $"event_{index}";
	}
}
