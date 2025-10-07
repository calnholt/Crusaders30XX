using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Attacks
{
	public class AttackDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public string target { get; set; } = "Player";
		public int damage { get; set; } = 0;
		public string positionType { get; set; } = "Linker"; // "Starter" | "Linker" | "Ender"
		public int ambushPercentage { get; set; } = 0;
		public bool isGeneric { get; set; } = false;
		public string text { get; set; } = "";
		public bool isTextConditionFulfilled { get; set; } = false;
		public Condition blockingCondition { get; set; }
		public Condition conditionals { get; set; }
		public EffectDefinition[] effectsOnAttack { get; set; } = System.Array.Empty<EffectDefinition>();
		public EffectDefinition[] effectsOnNotBlocked { get; set; } = System.Array.Empty<EffectDefinition>();
	}

	public class Condition
	{
		public string type { get; set; } // e.g., "OnHit"
	}

	public class Conditionals
	{
		public EffectDefinition[] effectsOnHit { get; set; } = System.Array.Empty<EffectDefinition>();
		public EffectDefinition[] effectsOnNotBlocked { get; set; } = System.Array.Empty<EffectDefinition>();
	}

	public class EffectDefinition
	{
		public string type { get; set; } // "Damage" | "ApplyStatus" | ...
		public int amount { get; set; } // for Damage / GainBlock
		public string status { get; set; } // for ApplyStatus
		public int stacks { get; set; } = 0;
		public string target { get; set; } // optional override ("Self" etc.)
		public int percentage { get; set; } = 100;
	}
}


