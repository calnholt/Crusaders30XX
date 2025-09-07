using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Equipment
{
	public class EquipmentDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public string slot { get; set; } // Head | Chest | Arms | Legs
		public string block { get; set; }
		public string blockUses { get; set; }
		public string color { get; set; }
		public List<AbilityDefintion> abilities { get; set; } = new();
	}
}


