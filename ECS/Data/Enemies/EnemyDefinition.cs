using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Enemies
{
	public class EnemyDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public int hp { get; set; } = 1;
		public List<string> attackIds { get; set; } = new();
	}
}



