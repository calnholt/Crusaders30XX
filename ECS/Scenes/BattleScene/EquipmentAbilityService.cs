using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Equipment;
using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	internal static class EquipmentAbilityService
	{
		public static void Activate(EntityManager entityManager, string equipmentId, AbilityDefinition ability)
		{
			Console.WriteLine($"[EquipmentAbilityService] Executing equipment effect for {ability.id}");
			var player = entityManager.GetEntity("Player");
			var enemy = entityManager.GetEntity("Enemy");
			if (ability.requiresUseOnActivate)
			{
				EventManager.Publish(new EquipmentUseResolved { EquipmentId = equipmentId, Delta = 1 });
			}
			switch (ability.id)
			{
				case "focus_visor":
					EventManager.Publish(new RequestDrawCardsEvent { Count = Math.Max(1, ability.effectCount) });
					EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
					EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
					break;
				case "purging_bracers":
					EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Aggression, Delta = ability.effectCount });
					EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
					EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
					break;
				case "lightning_grieves":
					EventManager.Publish(new ModifyActionPointsEvent { Delta = Math.Max(1, ability.effectCount) });
					EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
					EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
					break;
				case "pierced_heart_plate":
					EventManager.Publish(new ModifyCourageEvent { Delta = Math.Max(1, ability.effectCount) });
					EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
					EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
					break;
				default:
					Console.WriteLine($"[EquipmentAbilityService] No effect handler for {ability.effect}");
					break;
			}
		}

		public static void ActivateByEquipmentId(EntityManager entityManager, string equipmentId)
		{
			if (!EquipmentDefinitionCache.TryGet(equipmentId, out var def) || def?.abilities == null) return;
			var ability = def.abilities.FirstOrDefault(a => a.type == "Activate");
			Console.WriteLine($"[EquipmentAbilityService] ActivateByEquipmentId: {equipmentId} ability: {ability?.effect}");
			if (ability == null) return;
			var timesUsed = entityManager.GetEntitiesWithComponent<EquipmentUsedState>().FirstOrDefault().GetComponent<EquipmentUsedState>().UsesByEquipmentId?.GetValueOrDefault(equipmentId);
			if (timesUsed == null) timesUsed = 0;
			Console.WriteLine($"[EquipmentAbilityService] ActivateByEquipmentId: {equipmentId} timesUsed: {timesUsed}");
			if (ability.requiresUseOnActivate && timesUsed >= def.blockUses) return;
			Activate(entityManager, equipmentId, ability);
			if (ability.destroyOnActivate)
			{
				EventManager.Publish(new EquipmentDestroyed { EquipmentId = equipmentId });
			}
		}
	}
}


