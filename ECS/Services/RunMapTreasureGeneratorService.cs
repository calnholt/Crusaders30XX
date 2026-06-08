using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapTreasureGeneratorService
	{
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
			int equipmentTreasureIndex = rng.Next(LocationMapConstants.RunMapTreasureCount);

			for (int treasureIndex = 0; treasureIndex < LocationMapConstants.RunMapTreasureCount; treasureIndex++)
			{
				if (!RunMapLandmarkPlacementService.TryPlace(rng, anchorNodes, nodes, placedPositions, out float x, out float y))
				{
					throw new InvalidOperationException(
						$"[RunMapTreasureGeneratorService] Failed to place treasure {treasureIndex}.");
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
					grantsEquipmentReward = treasureIndex == equipmentTreasureIndex,
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

		private static string TreasureId(int index) => $"treasure_{index}";
	}
}
