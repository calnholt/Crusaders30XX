using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Medals
{
	public class MedalDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public string target { get; set; } = "Player"; // Player | Enemy
		public string text { get; set; }
		public string trigger { get; set; } // e.g., "StartOfBattle"
		public Dictionary<string, string> parameters { get; set; } = new();
	}
}



