using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
	public class ChangeBattlePhaseEvent
	{
		public SubPhase Current { get; set; }
		public SubPhase Previous { get; set; }

	}

	// New: Proceed-to-next-phase intent; a coordinator will transition using PhaseState2
	public class ProceedToNextPhase { }

	public class ShowConfirmButtonEvent { }

	public class BattlePhaseAnimationCompleteEvent 
	{
		public SubPhase SubPhase;
	}

	public class ShowStartOfBattleAnimationEvent { }
}
