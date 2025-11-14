using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Events;
using System;
using System.Linq;

namespace Crusaders30XX.ECS.Services
{
	/// <summary>
	/// Service that applies tribulation effects to the player based on quest ID.
	/// Effects are keyed entirely by quest ID - no string parsing needed.
	/// </summary>
	public static class ApplyTribulationService
	{
		/// <summary>
		/// Applies tribulation effects for the given quest ID.
		/// Looks up quest data and applies effects based on hardcoded quest ID matching.
		/// </summary>
		public static void ApplyTribulationEffect(Entity player, string questId)
		{
			if (player == null || string.IsNullOrEmpty(questId)) return;

			// Apply effects based on quest ID
			// Example: desert_3 applies burn 1 at start of battle
			switch (questId)
			{
				// case "desert_3":
				// 	Console.WriteLine($"[ApplyTribulationService] Applying burn 1 to player for quest {questId}");
				// 	EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Burn, Delta = 1 });
				// 	break;
				// case "desert_4":
				// 	EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Poison, Delta = 1 });
				// 	break;
				default:
					// Unknown quest ID - no effect
					break;
			}
		}
	}
}

