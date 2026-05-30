using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Data.Save
{
	public class SaveFile
	{
		public const int CURRENT_VERSION = 2;

		public int version { get; set; } = 0;
        public int gold { get; set; } = 0;
		public int runMapSeed { get; set; }
		public List<RunMapNode> runMapNodes { get; set; } = new List<RunMapNode>();
		public List<string> collection { get; set; } = new List<string>();
		public List<SaveItem> items { get; set; } = new List<SaveItem>();
		public string lastLocation { get; set; } = string.Empty;
		public List<LoadoutDefinition> loadouts { get; set; } = new List<LoadoutDefinition>();
		public List<string> seenTutorials { get; set; } = new List<string>();
		public Dictionary<string, CardMastery> cardMastery { get; set; } = new Dictionary<string, CardMastery>();
		public Dictionary<string, AchievementProgress> achievements { get; set; } = new Dictionary<string, AchievementProgress>();
	}

	public class SaveItem
	{
		public string id { get; set; }
		public int amount { get; set; }
	}

	public class CardMastery
	{
		public string cardId { get; set; }
		public int level { get; set; }
		public int points { get; set; }
	}
}
