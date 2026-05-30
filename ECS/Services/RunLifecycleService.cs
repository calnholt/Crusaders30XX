using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Services
{
	public static class RunLifecycleService
	{
		/// <summary>
		/// Persists meta progress and replaces on-disk run state with a fresh run.
		/// </summary>
		public static void EndCurrentRun(EntityManager entityManager = null)
		{
			AchievementManager.SaveProgress();
			if (entityManager != null)
			{
				RunDeckService.DestroyRunDeck(entityManager);
			}
			SaveCache.StartNewRun();
		}
	}
}
