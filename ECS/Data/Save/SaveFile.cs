using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Data.Save
{
	public class SaveFile
	{
		public const int CURRENT_VERSION = 6;

		public int version { get; set; } = 0;
		public bool isRunActive { get; set; } = true;
        public int gold { get; set; } = 0;
		public int runMapSeed { get; set; }
		public List<RunMapNode> runMapNodes { get; set; } = new List<RunMapNode>();
		public List<RunMapShop> runMapShops { get; set; } = new List<RunMapShop>();
		public List<RunMapTreasure> runMapTreasures { get; set; } = new List<RunMapTreasure>();
		public List<RunMapEvent> runMapEvents { get; set; } = new List<RunMapEvent>();
		public List<SaveItem> items { get; set; } = new List<SaveItem>();
		public string lastLocation { get; set; } = string.Empty;
		/// <summary>Run-map node id when the player entered battle but has not returned to the location map.</summary>
		public string pendingBattleNodeId { get; set; } = string.Empty;
		public List<LoadoutDefinition> loadouts { get; set; } = new List<LoadoutDefinition>();
		public List<string> seenTutorials { get; set; } = new List<string>();
		public Dictionary<string, CardMastery> cardMastery { get; set; } = new Dictionary<string, CardMastery>();
		public Dictionary<string, AchievementProgress> achievements { get; set; } = new Dictionary<string, AchievementProgress>();
		/// <summary>Run-long applied passive type name to stack count (e.g. Frostbite).</summary>
		public Dictionary<string, int> runLongPassives { get; set; } = new Dictionary<string, int>();
		/// <summary>Run deck card key to run-long restriction names (Frozen, Sealed, Brittle, Colorless). Shackle is battle-only and is not persisted.</summary>
		public Dictionary<string, List<string>> runCardRestrictions { get; set; } = new Dictionary<string, List<string>>();
		/// <summary>Loadout card keys from the rolled starting deck at new-run creation.</summary>
		public List<string> starterCardKeys { get; set; } = new List<string>();
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
