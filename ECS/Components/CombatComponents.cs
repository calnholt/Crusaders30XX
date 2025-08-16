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
}


