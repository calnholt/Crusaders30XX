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
			IReadOnlyList<RunMapNode> nodes)
		{
			if (nodes == null || nodes.Count == 0) return false;

			float revealRadius = LocationMapConstants.DefaultRevealRadius;

			if (LocationPoiRevealCutsceneSystem.TryGetExpandingFog(out Vector2 expandingCenter, out float expandingRadius))
			{
				if (RunMapRevealService.IsWithinRevealRadius(
					landmarkX, landmarkY, expandingCenter.X, expandingCenter.Y, expandingRadius))
				{
					return true;
				}
			}

			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
				if (node == null || !node.isCompleted) continue;

				if (RunMapRevealService.IsWithinRevealRadius(
					landmarkX, landmarkY, node.worldX, node.worldY, revealRadius))
				{
					return true;
				}
			}

			return false;
		}
	}
}
