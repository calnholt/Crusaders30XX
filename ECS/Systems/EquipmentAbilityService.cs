using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Equipment;
using System;

namespace Crusaders30XX.ECS.Systems
{
	internal static class EquipmentAbilityService
	{
		public static void Activate(EntityManager entityManager, AbilityDefinition ability)
		{
			Console.WriteLine($"[EquipmentAbilityService] Executing equipment effect for {ability.id}");
			switch (ability.effect)
			{
				case "DrawCards":
					EventManager.Publish(new RequestDrawCardsEvent { Count = System.Math.Max(1, ability.effectCount) });
					break;
				default:
					Console.WriteLine($"[EquipmentAbilityService] No effect handler for {ability.effect}");
					break;
			}
		}
	}
}


