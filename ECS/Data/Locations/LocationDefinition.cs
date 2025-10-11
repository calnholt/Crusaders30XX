using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Locations
{
	public class LocationDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public List<List<QuestDefinition>> quests { get; set; }
	}

	public class QuestDefinition
	{
		public string id { get; set; }
		public string type { get; set; }
	}
}


