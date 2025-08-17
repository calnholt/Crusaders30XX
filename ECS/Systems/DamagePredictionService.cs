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

			return (definition.effectsOnHit ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount) + 
        
        (definition.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);
		}

		public static int GetStoredBlockAmount(EntityManager entityManager)
		{
			var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var stored = player?.GetComponent<StoredBlock>();
			return stored?.Amount ?? 0;
		}

		public static int GetAssignedBlockForContext(EntityManager entityManager, string contextId)
		{
			if (string.IsNullOrEmpty(contextId)) return 0;
			var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			var progress = player?.GetComponent<BlockProgress>();
			if (progress == null) return 0;
			if (!progress.Counters.TryGetValue(contextId, out var counters) || counters == null) return 0;
			return counters.TryGetValue("assignedBlockTotal", out var val) ? val : 0;
		}
		public static int ComputeActualDamage(AttackDefinition definition, EntityManager entityManager, string contextId)
		{
			int full = ComputeFullDamage(definition);
			int stored = GetStoredBlockAmount(entityManager);
			int assigned = GetAssignedBlockForContext(entityManager, contextId);
			int reduced = stored + assigned;
			int actual = full - reduced;
			return actual < 0 ? 0 : actual;
		}

    public static int ComputePreventedDamage(AttackDefinition definition, EntityManager entityManager, string contextId, bool isBlocked)
    {
      int stored = GetStoredBlockAmount(entityManager);
      int assigned = GetAssignedBlockForContext(entityManager, contextId);
      int preventedBlockCondition = isBlocked ? (definition.effectsOnNotBlocked ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount) : 0;
      return stored + assigned + preventedBlockCondition;
    }

	}
}


