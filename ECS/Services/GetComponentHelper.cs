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
    }
}