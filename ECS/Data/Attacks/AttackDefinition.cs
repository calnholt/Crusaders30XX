using System.Collections.Generic;
using System.Linq;

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
		public EffectDefinition[] specialEffects { get; set; } = System.Array.Empty<EffectDefinition>();

		public AttackDefinition DeepCopy()
		{
			return new AttackDefinition
			{
				id = this.id,
				name = this.name,
				target = this.target,
				damage = this.damage,
				positionType = this.positionType,
				ambushPercentage = this.ambushPercentage,
				isGeneric = this.isGeneric,
				text = this.text,
				isTextConditionFulfilled = this.isTextConditionFulfilled,

				// Assuming Condition also has DeepCopy()
				blockingCondition = this.blockingCondition?.DeepCopy(),
				conditionals = this.conditionals?.DeepCopy(),

				// Clone arrays (and their items, if needed)
				effectsOnAttack = this.effectsOnAttack
					.Select(e => e.DeepCopy())  // if EffectDefinition supports DeepCopy
					.ToArray(),

				effectsOnNotBlocked = this.effectsOnNotBlocked
					.Select(e => e.DeepCopy())
					.ToArray(),

				specialEffects = this.specialEffects
					.Select(e => e.DeepCopy())
					.ToArray()
			};
		}
	}

	public class Condition
	{
		public string type { get; set; } // e.g., "OnHit"

		public Condition DeepCopy()
		{
			return new Condition
			{
				type = this.type
			};
		}
	}

	public class EffectDefinition
	{
		public string type { get; set; } // "Damage" | "ApplyStatus" | ...
		public int amount { get; set; } // for Damage / GainBlock
		public string status { get; set; } // for ApplyStatus
		public int stacks { get; set; } = 0;
		public string target { get; set; } // optional override ("Self" etc.)
		public int percentage { get; set; } = 100;

		public EffectDefinition DeepCopy()
		{
			return new EffectDefinition
			{
				type = this.type,
				amount = this.amount,
				status = this.status,
				stacks = this.stacks,
				target = this.target,
				percentage = this.percentage
			};
		}
	}
}


