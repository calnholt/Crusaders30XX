using Crusaders30XX.ECS.Components;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Events
{
	public class StartBattleRequested { }
	public class LoadSceneEvent {
		public SceneId Scene;
		public SceneId PreviousScene { get; set; } = SceneId.None;
	}

	public class DeleteCachesEvent { public SceneId Scene; }

	public class QuestSelected
	{
		public string LocationId;
		public int QuestIndex;
		public string QuestId;
	}

	public class ShowQuestRewardOverlay
	{
		public string Message;
		public string TitleLine1;
		public string TitleLine2;
		public int RewardGold;
		public bool HasCardReward;
		public string RewardCardKey;
		public List<string> RewardCardKeys = new List<string>();
	}

	public class TreasureChestOpened
	{
		public int RewardGold;
		public string RewardMedalId;
	}

	public class ShowNarrativeEventOverlay
	{
		public string RunMapEventId;
		public string EventTypeId;
	}

	public class NarrativeEventOverlayClosedEvent
	{
		public string RunMapEventId;
		public string EventTypeId;
		public int OptionIndex;
	}
}
