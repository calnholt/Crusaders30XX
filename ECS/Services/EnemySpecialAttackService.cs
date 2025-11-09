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
            var assignedBlockCards = entityManager.GetEntitiesWithComponent<AssignedBlockCard>().Where(e => !e.GetComponent<AssignedBlockCard>().IsEquipment).ToList();
            var cards = entityManager.GetEntitiesWithComponent<CardData>().ToList();
            cards.ForEach(e => entityManager.RemoveComponent<ExhaustOnBlock>(e));
            if (assignedBlockCards.Count == effect.amount)
            {
              Console.WriteLine($"[EnemySpecialAttackService] Glass Cannon special effect executed");
              p.ActualDamage = 0;
              p.PreventedDamageFromBlockCondition = 0;
              p.TotalPreventedDamage = def.damage;
              p.IsConditionMet = true;
              p.BaseDamage = def.damage;
              p.AssignedBlockTotal = def.damage;
              
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