using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    internal static class TemperanceAbilityService
    {
        public static void Activate(EntityManager entityManager, string abilityId)
        {
          var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
          var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
          switch (abilityId)
          {
            case "radiance":
            {
              // Example temperance ability that stuns instead of heals per user's edit
              EventManager.Publish(new ApplyStun { Delta = +1 });
              break;
            }
            default:
              System.Console.WriteLine($"[TemperanceAbilityService] No activation logic for id={abilityId}");
              break;
          }
        }
    }
}


