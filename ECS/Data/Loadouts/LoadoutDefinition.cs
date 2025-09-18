using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Loadouts
{
	public class LoadoutDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public List<string> cardIds { get; set; } = new();
		public string weaponId { get; set; }
		public string temperanceId { get; set; }
		public string chestId { get; set; }
		public string legsId { get; set; }
		public string armsId { get; set; }
		public string headId { get; set; }
		public List<string> medalIds { get; set; } = new();
	}
}



