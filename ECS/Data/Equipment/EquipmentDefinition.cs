using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Equipment
{
	public class EquipmentDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public string slot { get; set; } // Head | Chest | Arms | Legs
		public string text { get; set; }
		public List<AbilityDefintion> abilities { get; set; } = new();
	}
}


