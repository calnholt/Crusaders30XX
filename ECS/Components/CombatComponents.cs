using System.Collections.Generic;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
	/// <summary>
	/// Enemy arsenal of attack IDs that can be planned/executed.
	/// </summary>
	public class EnemyArsenal : IComponent
	{
		public Entity Owner { get; set; }
		public List<string> AttackIds { get; set; } = new();
	}

	/// <summary>
	/// Per-enemy list of planned attacks for the current/next turns.
	/// </summary>
	public class AttackIntent : IComponent
	{
		public Entity Owner { get; set; }
		public List<PlannedAttack> Planned { get; set; } = new();
	}

	/// <summary>
	/// Optional preview list of planned attacks for the next turn.
	/// </summary>
	public class NextTurnAttackIntent : IComponent
	{
		public Entity Owner { get; set; }
		public List<PlannedAttack> Planned { get; set; } = new();
	}

	public class PlannedAttack
	{
		public string AttackId;
		public int ResolveStep;
		public string ContextId;
		public bool WasBlocked;
	}

	/// <summary>
	/// Tracks per-attack block progress counters keyed by contextId.
	/// Example: Counters[contextId]["played_Red"] = 1
	/// </summary>
	public class BlockProgress : IComponent
	{
		public Entity Owner { get; set; }
		public Dictionary<string, Dictionary<string, int>> Counters { get; set; } = new();
	}

	/// <summary>
	/// Marker component; an entity with this and a Transform defines the enemy attack banner anchor position.
	/// The Transform.Position is the center-bottom point of the banner.
	/// </summary>
	public class EnemyAttackBannerAnchor : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Marks a card as currently assigned as block to a specific attack context and carries its animation state.
	/// </summary>
	public class AssignedBlockCard : IComponent
	{
		public Entity Owner { get; set; }
		public string ContextId { get; set; }
		public int BlockAmount { get; set; }
		public long AssignedAtTicks { get; set; }
		public enum PhaseState { Pullback, Launch, Impact, Idle, Returning }
		public PhaseState Phase { get; set; } = PhaseState.Pullback;
		public Microsoft.Xna.Framework.Vector2 StartPos { get; set; }
		public Microsoft.Xna.Framework.Vector2 TargetPos { get; set; }
		public Microsoft.Xna.Framework.Vector2 CurrentPos { get; set; }
		public float StartScale { get; set; } = 1f;
		public float TargetScale { get; set; } = 0.4f;
		public float CurrentScale { get; set; } = 1f;
		public float Elapsed { get; set; } = 0f;
		public bool ImpactPlayed { get; set; } = false;
	}
}


