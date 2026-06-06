using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Services
{
	public static class TitleMenuResumeService
	{
		public static SceneId? ResolveDirectTransitionScene()
		{
			if (!SaveCache.IsRunActive()) return SceneId.WayStation;
			if (!SaveCache.IsStartQuestCompleted()) return null;
			if (SaveCache.TryGetResumableBattleNode(out _)) return null;
			return SceneId.Location;
		}

		public static void OnTitleMenuClicked(World world)
		{
			if (!SaveCache.IsRunActive())
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.WayStation, SkipHold = true });
				return;
			}

			if (!SaveCache.IsStartQuestCompleted())
			{
				WayStationRunSetupService.BeginStartQuestBattle(world);
				return;
			}

			if (SaveCache.TryGetResumableBattleNode(out var nodeId))
			{
				WayStationRunSetupService.BeginBattleFromNodeId(world, nodeId);
				return;
			}

			EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
		}
	}
}
