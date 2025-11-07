using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    internal static class EnemyAttackEffectService
    {
        public static void Apply(EntityManager entityManager, AttackDefinition attackDefinition)
        {
          var attackId = attackDefinition.id;
            Console.WriteLine($"[EnemyAttackEffectService]: {attackId}");
          var battleStateInfo = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
          var enemyEntity = entityManager.GetEntity("Enemy");
          var enemyPassives = enemyEntity.GetComponent<AppliedPassives>();

          if (enemyPassives.Passives.TryGetValue(AppliedPassiveType.Aggression, out int aggressionStacks) && aggressionStacks > 0)
          {
            attackDefinition.damage += aggressionStacks;
            // TODO: should be handled better by the system
            EventManager.Publish(new PassiveTriggered { Owner = enemyEntity, Type = AppliedPassiveType.Aggression });
            TimerScheduler.Schedule(0.3f, () => {
              EventManager.Publish(new RemovePassive { Owner = enemyEntity, Type = AppliedPassiveType.Aggression });
            });
          }
          switch (attackId)
          {
            case "nightveil_guillotine":
              battleStateInfo.TurnTracking.TryGetValue("slice", out int sliceCount);
              battleStateInfo.TurnTracking.TryGetValue("dice", out int diceCount);
              Console.WriteLine($"[EnemyAttackEffectService]: slice: {sliceCount} // dice: {diceCount}");
              if (sliceCount > 0 && diceCount > 0)
              {
                attackDefinition.damage += 4;
                attackDefinition.effectsOnNotBlocked = [ new EffectDefinition { type = "Penance", amount = 2 } ];
                attackDefinition.blockingCondition = new Condition { type = "OnHit" };
                attackDefinition.isTextConditionFulfilled = true;
              }
              break;
          }
        }
    }
}