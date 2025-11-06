using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Save
{
	public class SaveFile
	{
        public int gold { get; set; } = 0;
		public List<SaveLocation> locations { get; set; } = new List<SaveLocation>();
	}

	public class SaveLocation
	{
		public string id { get; set; }
		public List<SaveQuest> events { get; set; } = new List<SaveQuest>();
	}

	public class SaveQuest
	{
		public string id { get; set; }
		public bool completed { get; set; }
	}
}




