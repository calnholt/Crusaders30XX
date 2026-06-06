using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Services
{
	internal static class RunMapLandmarkPlacementService
	{
		private const int RingSteps = 12;
		private const int AngleSteps = 64;

		private static readonly float[] LandmarkSeparationTiers = { 1f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f };

		public static bool TryPlace(
			Random rng,
			IReadOnlyList<RunMapNode> anchorNodes,
			IReadOnlyList<RunMapNode> clearanceNodes,
			IReadOnlyList<(float x, float y)> placedLandmarks,
			out float x,
			out float y)
		{
			if (rng == null) throw new ArgumentNullException(nameof(rng));
			if (anchorNodes == null || anchorNodes.Count == 0)
			{
				x = 0f;
				y = 0f;
				return false;
			}

			clearanceNodes ??= anchorNodes;
			placedLandmarks ??= Array.Empty<(float x, float y)>();

			foreach (float separationMultiplier in LandmarkSeparationTiers)
			{
				if (TryPlaceAtSeparationTier(
					rng,
					anchorNodes,
					clearanceNodes,
					placedLandmarks,
					LocationMapConstants.RunMapShopMinSeparation * separationMultiplier,
					out x,
					out y))
				{
					return true;
				}
			}

			x = 0f;
			y = 0f;
			return false;
		}

		private static bool TryPlaceAtSeparationTier(
			Random rng,
			IReadOnlyList<RunMapNode> anchorNodes,
			IReadOnlyList<RunMapNode> clearanceNodes,
			IReadOnlyList<(float x, float y)> placedLandmarks,
			float landmarkSeparation,
			out float x,
			out float y)
		{
			float bestX = 0f;
			float bestY = 0f;
			float bestScore = float.MinValue;
			bool found = false;

			float clearance = LocationMapConstants.RunMapShopClearanceFromQuest;
			float maxDist = LocationMapConstants.DefaultRevealRadius;
			float minX = LocationMapConstants.MapMargin + clearance;
			float maxX = LocationMapConstants.BaseMapWidth - LocationMapConstants.MapMargin - clearance;
			float minY = LocationMapConstants.MapMargin + clearance;
			float maxY = LocationMapConstants.BaseMapHeight - LocationMapConstants.MapMargin - clearance;
			float angleOffset = (float)(rng.NextDouble() * Math.PI * 2.0);
			float ringOffset = (float)rng.NextDouble();

			for (int anchorIndex = 0; anchorIndex < anchorNodes.Count; anchorIndex++)
			{
				var anchor = anchorNodes[anchorIndex];
				if (anchor == null) continue;

				for (int ring = 0; ring < RingSteps; ring++)
				{
					float ringT = (ring + ringOffset) / RingSteps;
					float dist = clearance + ringT * (maxDist - clearance);

					for (int angleIndex = 0; angleIndex < AngleSteps; angleIndex++)
					{
						float angle = angleOffset + angleIndex * (float)(Math.PI * 2.0 / AngleSteps);
						float cx = anchor.worldX + (float)Math.Cos(angle) * dist;
						float cy = anchor.worldY + (float)Math.Sin(angle) * dist;

						if (cx < minX || cx > maxX || cy < minY || cy > maxY) continue;
						if (OverlapsNodes(clearanceNodes, cx, cy, clearance)) continue;
						if (OverlapsLandmarks(placedLandmarks, cx, cy, landmarkSeparation)) continue;

						float score = ScoreCandidate(
							rng,
							clearanceNodes,
							placedLandmarks,
							cx,
							cy,
							minX,
							maxX,
							minY,
							maxY);
						if (!found || score > bestScore)
						{
							found = true;
							bestScore = score;
							bestX = cx;
							bestY = cy;
						}
					}
				}
			}

			x = bestX;
			y = bestY;
			return found;
		}

		private static float ScoreCandidate(
			Random rng,
			IReadOnlyList<RunMapNode> nodes,
			IReadOnlyList<(float x, float y)> placedLandmarks,
			float x,
			float y,
			float minX,
			float maxX,
			float minY,
			float maxY)
		{
			float landmarkDistance = MinDistanceToLandmarks(placedLandmarks, x, y);
			float nodeDistance = MinDistanceToNodes(nodes, x, y);
			float edgeDistance = Math.Min(Math.Min(x - minX, maxX - x), Math.Min(y - minY, maxY - y));
			float tieBreak = (float)rng.NextDouble();
			return landmarkDistance + 0.35f * nodeDistance + 0.15f * edgeDistance + tieBreak;
		}

		private static bool OverlapsNodes(IReadOnlyList<RunMapNode> nodes, float x, float y, float minDistance)
		{
			float minDistanceSq = minDistance * minDistance;
			foreach (var node in nodes)
			{
				if (node == null) continue;
				float dx = x - node.worldX;
				float dy = y - node.worldY;
				if (dx * dx + dy * dy < minDistanceSq) return true;
			}

			return false;
		}

		private static bool OverlapsLandmarks(
			IReadOnlyList<(float x, float y)> placedLandmarks,
			float x,
			float y,
			float minDistance)
		{
			float minDistanceSq = minDistance * minDistance;
			foreach (var landmark in placedLandmarks)
			{
				float dx = x - landmark.x;
				float dy = y - landmark.y;
				if (dx * dx + dy * dy < minDistanceSq) return true;
			}

			return false;
		}

		private static float MinDistanceToNodes(IReadOnlyList<RunMapNode> nodes, float x, float y)
		{
			float minDistance = float.MaxValue;
			foreach (var node in nodes)
			{
				if (node == null) continue;
				float dx = x - node.worldX;
				float dy = y - node.worldY;
				float distance = (float)Math.Sqrt(dx * dx + dy * dy);
				if (distance < minDistance) minDistance = distance;
			}

			return minDistance == float.MaxValue ? 0f : minDistance;
		}

		private static float MinDistanceToLandmarks(
			IReadOnlyList<(float x, float y)> placedLandmarks,
			float x,
			float y)
		{
			if (placedLandmarks.Count == 0) return LocationMapConstants.RunMapShopMinSeparation;

			float minDistance = float.MaxValue;
			foreach (var landmark in placedLandmarks)
			{
				float dx = x - landmark.x;
				float dy = y - landmark.y;
				float distance = (float)Math.Sqrt(dx * dx + dy * dy);
				if (distance < minDistance) minDistance = distance;
			}

			return minDistance == float.MaxValue ? 0f : minDistance;
		}
	}
}
