using System.Collections.Generic;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
	public enum TutorialBattle
	{
		Gleeber = 1,
		SandCorpse = 2,
	}

	public enum TutorialAction
	{
		AssignBlock,
		ConfirmBlocks,
		PlayCard,
		PledgeCard,
		PayCost,
		EndTurn,
	}

	public class GuidedTutorial : IComponent
	{
		public Entity Owner { get; set; }
		public TutorialBattle Battle { get; set; } = TutorialBattle.Gleeber;
		public int Turn { get; set; } = 1;
		public int PlayerHp { get; set; } = 25;
		public bool StockHandPrepared { get; set; }
		public bool ActionRequirementsComplete { get; set; }
		public bool IsCompleted { get; set; }
		public SubPhase RequiredPhase { get; set; } = SubPhase.Block;
		public List<string> ValidPlayCardIds { get; set; } = new();
		public List<string> PlayedCardIds { get; set; } = new();
		public List<string> PledgedCardIds { get; set; } = new();
		public List<string> BlockedCardIdsThisTurn { get; set; } = new();
		public int ConfirmedAttackCountThisTurn { get; set; }
	}

	public class StockHand : IComponent
	{
		public Entity Owner { get; set; }
		public TutorialBattle Battle { get; set; }
		public int Turn { get; set; }
	}

	public class TutorialEnemy : IComponent
	{
		public Entity Owner { get; set; }
	}
}
