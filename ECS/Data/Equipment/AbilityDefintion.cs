using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Equipment
{
	/// <summary>
	/// Defines a single equipment ability in data. Note: name intentionally spelled AbilityDefintion per user request.
	/// </summary>
	public class AbilityDefinition
	{
		public string id { get; set; }
		public string equipmentId { get; set; }
		public string type { get; set; } // e.g., "Trigger, Activate, etc."
		public string trigger { get; set; } // e.g., "CourageGainedThreshold"
		public int threshold { get; set; } = 1;
		public bool oncePerBattle { get; set; } = false;
		public string effect { get; set; } // e.g., "DrawCards"
		public int effectCount { get; set; } = 1;
		public string text { get; set; }
		public Dictionary<string, string> parameters { get; set; } = new();
		// Activate-specific flags
		public bool isFreeAction { get; set; } = false;
		public bool destroyOnActivate { get; set; } = false;
		public string target { get; set; } = "Enemy";
	}
}


