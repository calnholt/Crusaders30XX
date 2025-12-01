using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Save
{
	public class SaveFile
	{
        public int gold { get; set; } = 0;
		public List<string> completedQuests { get; set; } = new List<string>();
		public List<string> collection { get; set; } = new List<string>();
		public List<SaveItem> items { get; set; } = new List<SaveItem>();
	}

	public class SaveItem
	{
		public string id { get; set; }
		public int amount { get; set; }
	}
}
