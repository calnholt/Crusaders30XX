using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using System.Runtime.CompilerServices;
using System;

namespace Crusaders30XX.ECS.Systems
{
	public static class DamagePredictionService
	{
		public static int ComputeFullDamage(AttackDefinition definition)
		{
			if (definition == null) return 0;

			return definition.damage + 
        
        (definition.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);
		}

		public static int GetAegisAmount(EntityManager entityManager)
		{
			var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var passives = player?.GetComponent<AppliedPassives>()?.Passives;
			if (passives == null) return 0;
			var value = passives.TryGetValue(AppliedPassiveType.Aegis, out var aegis) ? aegis : 0;
			return Math.Max(value, 0);
		}

		public static int GetAssignedBlockForContext(EntityManager entityManager, string contextId)
		{
			if (string.IsNullOrEmpty(contextId)) return 0;
			// Use EnemyAttackProgress exclusively
			foreach (var e in entityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId)
				{
					return p.AssignedBlockTotal;
				}
			}
			return 0;
		}
		public static int ComputeActualDamage(AttackDefinition definition, EntityManager entityManager, string contextId, bool isBlocked)
		{
			int full = ComputeFullDamage(definition);
			int aegis = GetAegisAmount(entityManager);
			int assigned = GetAssignedBlockForContext(entityManager, contextId);
			int preventedBlockCondition = isBlocked ? (definition.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount) : 0;
			int reduced = aegis + assigned;
			int actual = full - reduced - preventedBlockCondition;
			return actual < 0 ? 0 : actual;
		}

    public static int ComputePreventedDamage(AttackDefinition definition, EntityManager entityManager, string contextId, bool isBlocked)
    {
      int aegis = GetAegisAmount(entityManager);
      int assigned = GetAssignedBlockForContext(entityManager, contextId);
      int preventedBlockCondition = isBlocked ? (definition.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount) : 0;
      return aegis + assigned + preventedBlockCondition;
    }

	}
}


