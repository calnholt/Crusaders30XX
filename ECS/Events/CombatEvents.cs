using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
	public class StartEnemyTurn { }
	public class EndEnemyTurn { }

	public class IntentPlanned
	{
		public string AttackId;
		public string ContextId;
		public int Step;
		public string TelegraphText;
	}

	public class CardPlayed
	{
		public Entity Card;
		public string Color; // "Red" | "White" | "Black"
	}

	public class ResolveAttack
	{
		public string ContextId;
	}

	public class ApplyEffect
	{
		public string EffectType;
		public int Amount;
		public string Status;
		public int Stacks;
		public Entity Source;
		public Entity Target;
	}

	public class AttackResolved
	{
		public string ContextId;
		public bool WasBlocked;
	}
}


