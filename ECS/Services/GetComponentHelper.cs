using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Services
{
    public static class GetComponentHelper
    {
        public static EnemyAttackBase GetPlannedAttack(EntityManager entityManager)
        {
            var intent = entityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
            if (intent == null) return null;
            var planned = intent.GetComponent<AttackIntent>().Planned.FirstOrDefault();
            if (planned == null) return null;
            return planned.AttackDefinition;
        }

        public static string GetContextId(EntityManager entityManager)
        {
            var intent = entityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
            if (intent == null) return null;
            return intent.GetComponent<AttackIntent>().Planned.FirstOrDefault()?.ContextId;
        }

        public static BattleStateInfo GetBattleStateInfo(EntityManager entityManager)
        {
            var battleStateInfo = entityManager.GetEntitiesWithComponent<BattleStateInfo>().FirstOrDefault();
            if (battleStateInfo == null) return null;
            return battleStateInfo.GetComponent<BattleStateInfo>();
        }

        public static AppliedPassives GetAppliedPassives(EntityManager entityManager, string targetId)
        {
            var target = entityManager.GetEntity(targetId);
            var appliedPassives = target.GetComponent<AppliedPassives>();
            if (appliedPassives == null) return null;
            return appliedPassives;
        }

        public static bool IsLastBattleOfQuest(EntityManager entityManager)
        {
            var queuedEvents = entityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            if (queuedEvents == null) return false;
            var qe = queuedEvents.GetComponent<QueuedEvents>();
            return qe.Events.Count == 1 || qe.CurrentIndex == qe.Events.Count - 1;
        }
    }
}