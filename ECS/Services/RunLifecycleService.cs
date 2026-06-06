using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Services
{
	public static class RunLifecycleService
	{
		/// <summary>
		/// Persists meta progress, destroys run entities, and leaves the save without an active run.
		/// </summary>
		public static void EndCurrentRun(EntityManager entityManager = null)
		{
			AchievementManager.SaveProgress();
			if (entityManager != null)
			{
				RunScopedStateService.ClearRunCardRestrictionComponents(entityManager);
				RunDeckService.DestroyRunDeck(entityManager);
			}
			SaveCache.ClearRunScopedState();
			if (entityManager != null)
			{
				RunPlayerService.DestroyRunPlayer(entityManager);
			}
			SaveCache.MarkRunInactive();
		}
	}
}
