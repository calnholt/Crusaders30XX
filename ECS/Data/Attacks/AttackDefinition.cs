using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Attacks
{
	public class AttackDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public string target { get; set; } = "Player";
		public string positionType { get; set; } = "Linker"; // "Starter" | "Linker" | "Ender"
		public ConditionNode conditionsBlocked { get; set; }
		public EffectDefinition[] effectsOnHit { get; set; } = System.Array.Empty<EffectDefinition>();
		public EffectDefinition[] effectsOnNotBlocked { get; set; } = System.Array.Empty<EffectDefinition>();
	}

	public class ConditionNode
	{
		public string kind { get; set; } // "All" | "Any" | "Not" | "Leaf"
		public ConditionNode[] children { get; set; } = System.Array.Empty<ConditionNode>();
		public string leafType { get; set; } // e.g., "PlayColorAtLeastN"
		public Dictionary<string, string> @params { get; set; }
	}

	public class EffectDefinition
	{
		public string type { get; set; } // "Damage" | "ApplyStatus" | ...
		public int amount { get; set; } // for Damage / GainBlock
		public string status { get; set; } // for ApplyStatus
		public int stacks { get; set; } = 0;
		public string target { get; set; } // optional override ("Self" etc.)
	}
}


