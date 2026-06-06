using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Services
{
	public static class BattleInputGate
	{
		public static bool IsBattleInputFrozen(EntityManager entityManager)
		{
			var phase = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			return (phase != null && phase.DefeatPresentationActive)
				|| StateSingleton.IsActive;
		}
	}
}
