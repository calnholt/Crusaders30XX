using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Attacks;

namespace Crusaders30XX.ECS.Systems
{
    internal static class EnemyAttackEffectService
    {
        public static void Apply(EntityManager entityManager, AttackDefinition attackDefinition)
        {
          var attackId = attackDefinition.id;
          var battleStateInfo = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
          switch (attackId)
          {
            case "nightveil_guillotine":
              battleStateInfo.TurnTracking.TryGetValue("slice", out int sliceCount);
              battleStateInfo.TurnTracking.TryGetValue("dice", out int diceCount);
              if (sliceCount > 0 && diceCount > 0)
              {
                attackDefinition.damage += 4;
                attackDefinition.effectsOnNotBlocked.Append(new EffectDefinition { type = "Penance", amount = 2 });
                attackDefinition.blockingCondition = new Condition { type = "OnHit" };
                attackDefinition.isTextConditionFulfilled = true;
              }
              break;
          }
        }
    }
}