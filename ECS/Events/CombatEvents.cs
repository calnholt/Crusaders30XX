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

	// Fired when the absorb tween completes and the enemy is about to attack
	public class EnemyAbsorbComplete
	{
		public string ContextId;
	}

	// Fired when the enemy attack animation should deal damage to the player
	public class EnemyAttackImpactNow
	{
		public string ContextId;
	}

	// New: explicit signal to start the enemy's attack animation
	public class StartEnemyAttackAnimation
	{
		public string ContextId;
	}

	// Player attack animation start and impact events
	public class StartPlayerAttackAnimation { }
	public class PlayerAttackImpactNow { }

	// Generic buff animation start for either player or enemy
	public class StartBuffAnimation
	{
		public bool TargetIsPlayer;
	}

	// Shows a temporary "Stunned!" overlay on the enemy
	public class ShowStunnedOverlay
	{
		public string ContextId;
	}

	// Request to apply stun stacks to the enemy (positive to add, negative to remove)
	public class ApplyStun
	{
		public int Delta;
	}
}


