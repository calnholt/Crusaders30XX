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
			return SceneId.Location;
		}

		public static void OnTitleMenuClicked(World world)
		{
			var scene = ResolveDirectTransitionScene();
			if (scene == null)
			{
				WayStationRunSetupService.BeginStartQuestBattle(world);
				return;
			}

			EventManager.Publish(new ShowTransition { Scene = scene.Value, SkipHold = true });
		}
	}
}
