using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Services;

public static class AchievementService
{
  public static void CheckAchievements(EntityManager entityManager)
  {
    var player = entityManager.GetEntity("Player");
    var enemy = entityManager.GetEntity("Enemy");
    var playerAppliedPassives = player.GetComponent<AppliedPassives>();
    var save = SaveCache.GetAll();
    // if (save.lastLocation == "desert_1")

  }
}