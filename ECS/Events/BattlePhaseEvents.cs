using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
	public class ChangeBattlePhaseEvent
	{
		public BattlePhase Next { get; set; }
		public BattlePhase Previous { get; set; }

	}
}


