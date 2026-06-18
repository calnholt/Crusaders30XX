using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Locations;

namespace Crusaders30XX.ECS.Data.Save
{
	public class SaveFile
	{
		public const int CURRENT_VERSION = 11;

		public int version { get; set; } = 0;
		public bool isRunActive { get; set; }
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
		public bool guidedTutorialCompleted { get; set; }
		public Dictionary<string, CardMastery> cardMastery { get; set; } = new Dictionary<string, CardMastery>();
		public Dictionary<string, AchievementProgress> achievements { get; set; } = new Dictionary<string, AchievementProgress>();
		/// <summary>Run-long applied passive type name to stack count (e.g. Frostbite).</summary>
		public Dictionary<string, int> runLongPassives { get; set; } = new Dictionary<string, int>();
		/// <summary>Run deck card key to run-long restriction names (Frozen, Sealed, Brittle, Colorless). Shackle is battle-only and is not persisted.</summary>
		public Dictionary<string, List<string>> runCardRestrictions { get; set; } = new Dictionary<string, List<string>>();
		/// <summary>Loadout card keys from the rolled starting deck at new-run creation.</summary>
		public List<string> starterCardKeys { get; set; } = new List<string>();
		/// <summary>Current run card keys acquired by encounter reward exchange or climb shop replacement.</summary>
		public List<string> tradedCardKeys { get; set; } = new List<string>();
		/// <summary>Exact unresolved deck reward offer shown after a quest reward.</summary>
		public DeckRewardOfferSave pendingDeckRewardOffer { get; set; }
		public ClimbSaveState climb { get; set; } = new ClimbSaveState();
	}

	public class ClimbSaveState
	{
		public int time { get; set; }
		public ClimbResourceSave resources { get; set; } = new ClimbResourceSave();
		public List<ClimbShopSlotSave> shopSlots { get; set; } = new List<ClimbShopSlotSave>();
		public List<ClimbEncounterSlotSave> encounterSlots { get; set; } = new List<ClimbEncounterSlotSave>();
		public List<ClimbEventSlotSave> eventSlots { get; set; } = new List<ClimbEventSlotSave>();
		public List<string> shownMedalIds { get; set; } = new List<string>();
		public List<string> shownEquipmentIds { get; set; } = new List<string>();
		public List<string> shownEventTypeIds { get; set; } = new List<string>();
		public int nextEventSlotId { get; set; }
		public ClimbReplacementOfferSave pendingReplacementOffer { get; set; }
		public ClimbEncounterRewardSave pendingEncounterReward { get; set; }
		public ClimbPendingEventSave pendingEvent { get; set; }
	}

	public class ClimbResourceSave
	{
		public int red { get; set; } = 1;
		public int white { get; set; } = 1;
		public int black { get; set; } = 1;
	}

	public class ClimbShopSlotSave
	{
		public string id { get; set; } = string.Empty;
		public string kind { get; set; } = ClimbShopSlotKinds.Empty;
		public string itemId { get; set; } = string.Empty;
		public string cardKey { get; set; } = string.Empty;
		public int deckIndex { get; set; } = -1;
		public ClimbResourceSave cost { get; set; } = new ClimbResourceSave { red = 0, white = 0, black = 0 };
		public int timeCost { get; set; }
		public bool isSold { get; set; }
		public int generatedAtTime { get; set; }
	}

	public class ClimbEncounterSlotSave
	{
		public string id { get; set; } = string.Empty;
		public string enemyId { get; set; } = string.Empty;
		public int generatedAtTime { get; set; }
		public int duration { get; set; }
		public int timeCost { get; set; }
		public ClimbResourceSave rewardResources { get; set; } = new ClimbResourceSave { red = 0, white = 0, black = 0 };
		public bool hasDeckReward { get; set; } = true;
		public bool isCompleted { get; set; }
		public bool isFinal { get; set; }
	}

	public class ClimbEventSlotSave
	{
		public string id { get; set; } = string.Empty;
		public string eventTypeId { get; set; } = string.Empty;
		public int generatedAtTime { get; set; }
		public int visibleStartTime { get; set; }
		public int visibleEndTime { get; set; }
		public int timeCost { get; set; } = 1;
		public bool seen { get; set; }
		public bool isCompleted { get; set; }
	}

	public class ClimbReplacementOfferSave
	{
		public int shopSlotIndex { get; set; } = -1;
		public string incomingCardKey { get; set; } = string.Empty;
		public ClimbResourceSave cost { get; set; } = new ClimbResourceSave { red = 0, white = 0, black = 0 };
	}

	public class ClimbEncounterRewardSave
	{
		public string encounterSlotId { get; set; } = string.Empty;
		public ClimbResourceSave resources { get; set; } = new ClimbResourceSave { red = 0, white = 0, black = 0 };
		public DeckRewardOfferSave deckRewardOffer { get; set; }
		public bool pendingFinalEncounter { get; set; }
	}

	public class ClimbPendingEventSave
	{
		public string eventSlotId { get; set; } = string.Empty;
		public string eventTypeId { get; set; } = string.Empty;
	}

	public static class ClimbShopSlotKinds
	{
		public const string Empty = "empty";
		public const string Medal = "medal";
		public const string Equipment = "equipment";
		public const string Upgrade = "upgrade";
		public const string Replacement = "replacement";
	}

	public class DeckRewardOfferSave
	{
		public int rewardGold { get; set; }
		public List<DeckRewardOfferOptionSave> options { get; set; } = new List<DeckRewardOfferOptionSave>();
	}

	public class DeckRewardOfferOptionSave
	{
		public string kind { get; set; } = DeckRewardOfferKinds.Exchange;
		public int loadoutIndex { get; set; } = -1;
		public string outgoingCardKey { get; set; } = string.Empty;
		public string incomingCardKey { get; set; } = string.Empty;
		public string upgradedCardKey { get; set; } = string.Empty;
	}

	public static class DeckRewardOfferKinds
	{
		public const string Exchange = "exchange";
		public const string Upgrade = "upgrade";
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
