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
						var assignedBlockCards = entityManager.GetEntitiesWithComponent<AssignedBlockCard>()
							.Where(e => !e.GetComponent<AssignedBlockCard>().IsEquipment)
							.ToList();
						var cards = entityManager.GetEntitiesWithComponent<CardData>().ToList();
						cards.ForEach(e => entityManager.RemoveComponent<ExhaustOnBlock>(e));
						if (assignedBlockCards.Count == effect.amount)
						{
							Console.WriteLine($"[EnemySpecialAttackService] Glass Cannon special effect executed");

							// Use the existing snapshot values on the progress component so we
							// don't corrupt block totals when cards are later removed.
							// Recompute() sets DamageBeforePrevention to the full predicted damage
							// before calling this method.
							int fullDamage = p.DamageBeforePrevention > 0 ? p.DamageBeforePrevention : def.damage;
							int blockFromCards = Math.Max(0, p.AssignedBlockTotal);

							p.ActualDamage = 0;
							p.IsConditionMet = true;
							p.BaseDamage = def.damage;
							p.AegisTotal = 0;

							// Attribute the remaining prevented damage to the GlassCannon effect
							// so the UI can show e.g. "X block, condition Y" without inflating
							// the raw block total.
							p.PreventedDamageFromBlockCondition = Math.Max(0, fullDamage - blockFromCards);
							p.TotalPreventedDamage = fullDamage;

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