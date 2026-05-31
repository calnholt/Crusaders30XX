using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapLandmarkAccessService
	{
		public static bool IsWithinCompletedQuestFog(
			float landmarkX,
			float landmarkY,
			IReadOnlyList<RunMapNode> nodes,
			int minCompletedDepth = 0)
		{
			if (nodes == null || nodes.Count == 0) return false;

			float revealRadius = LocationMapConstants.DefaultRevealRadius;
			int[] depths = minCompletedDepth > 0 ? RunMapNodeDepthHelper.ComputeDepths(nodes) : null;

			if (LocationPoiRevealCutsceneSystem.TryGetExpandingFog(out Vector2 expandingCenter, out float expandingRadius))
			{
				if (RunMapRevealService.IsWithinRevealRadius(
					landmarkX, landmarkY, expandingCenter.X, expandingCenter.Y, expandingRadius))
				{
					if (minCompletedDepth <= 0 || IsExpandingFogFromMinDepth(nodes, depths, minCompletedDepth))
					{
						return true;
					}
				}
			}

			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null || !node.isCompleted) continue;
				if (minCompletedDepth > 0 && (depths == null || depths[i] < minCompletedDepth)) continue;

				if (RunMapRevealService.IsWithinRevealRadius(
					landmarkX, landmarkY, node.worldX, node.worldY, revealRadius))
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsExpandingFogFromMinDepth(
			IReadOnlyList<RunMapNode> nodes,
			int[] depths,
			int minCompletedDepth)
		{
			if (!LocationPoiRevealCutsceneSystem.TryGetExpandingFog(out Vector2 center, out _))
			{
				return false;
			}

			const float centerEpsilon = 1f;
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null || !node.isCompleted) continue;
				if (depths == null || depths[i] < minCompletedDepth) continue;

				float dx = node.worldX - center.X;
				float dy = node.worldY - center.Y;
				if (dx * dx + dy * dy <= centerEpsilon * centerEpsilon)
				{
					return true;
				}
			}

			return false;
		}
	}
}
