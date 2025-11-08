using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Attacks;

namespace Crusaders30XX.ECS.Services
{
	public static class EnemySpecialAttackService
	{
		public static bool ExecuteSpecialEffect(AttackDefinition def, EntityManager entityManager)
		{
      var p = entityManager.GetEntitiesWithComponent<EnemyAttackProgress>().FirstOrDefault().GetComponent<EnemyAttackProgress>();
      foreach (var effect in def.specialEffects)
      {
        switch (effect.type)
        {
          case "GlassCannon":
          {
            var glassCannonAmount = effect.amount;
            if (p.PlayedCards == glassCannonAmount)
            {
              Console.WriteLine($"[EnemySpecialAttackService] Glass Cannon special effect executed");
              p.ActualDamage = 0;
              p.PreventedDamageFromBlockCondition = 0;
              p.TotalPreventedDamage = def.damage;
              p.IsConditionMet = true;
              p.BaseDamage = def.damage;
              p.AssignedBlockTotal = def.damage;
              var assignedBlockCards = entityManager.GetEntitiesWithComponent<AssignedBlockCard>().ToList();
              foreach (var e in assignedBlockCards)
              {
                entityManager.AddComponent(e, new ExhaustOnBlock { Owner = e });
              }
              return true;
            }
            return false;
          }
          default:
            return false;
        }
      }
      return false;
    }

  }

}