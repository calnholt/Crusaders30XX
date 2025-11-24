using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
    internal static class TemperanceAbilityService
    {
        public static void Activate(EntityManager entityManager, string abilityId)
        {
          var enemy = entityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
          var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
          if (player != null)
          {
            EventManager.Publish(new TriggerTemperance { Owner = player, AbilityId = abilityId });
          }
          switch (abilityId)
          {
            case "radiance":
            {
              EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Stun, Delta = 1 });
              break;
            }
            case "angelic_aura":
            {
              EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = 5 });
              break;
            }
            case "fling_fling":
            {
              for (int i = 0; i < 2; i++)
              {
                var kunai = EntityFactory.CreateCardFromDefinition(entityManager, $"kunai", CardData.CardColor.White, false, i + 1);
                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                EventManager.Publish(new CardMoveRequested { Card = kunai, Deck = deckEntity, Destination = CardZoneType.Hand, Reason = "FlingFling" });
              }
              break;
            }
            default:
              Console.WriteLine($"[TemperanceAbilityService] No activation logic for id={abilityId}");
              break;
          }
        }
    }
}


