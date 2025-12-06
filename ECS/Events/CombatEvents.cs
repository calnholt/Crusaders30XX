using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using MonoGame.Extended.Collections;

namespace Crusaders30XX.ECS.Events
{

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
		public int Percentage;
		public string attackId;
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

	public class ResolvingEnemyDamageEvent
	{
		public string ContextId;
		public int BaseDamage;
		public int AssignedBlock;
		public bool WillHit;
	}

	public class EnemyDamageAppliedEvent
	{
		public string ContextId;
		public int FinalDamage;
		public bool WasHit;
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

	public class BuffAnimationComplete
	{
		public bool TargetIsPlayer;
	}

	// Shows a temporary "Stunned!" overlay on the enemy
	public class ShowStunnedOverlay
	{
		public string ContextId;
	}

	// Fired when a battle is won (enemy defeated), to trigger scene transition
	public class ShowTransition 
	{ 
		public SceneId Scene;
	}

	public class TransitionCompleteEvent
	{
		public SceneId Scene;
	}

	public class DialogEnded
	{
		
	}

	public class TriggerEnemyAttackDisplayEvent
	{
		public string ContextId;
	}
}
