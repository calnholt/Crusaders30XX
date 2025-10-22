using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
	public class StartBattleRequested { }
	public class LoadSceneEvent { 
		public SceneId Scene;
	}

	public class DeleteCachesEvent { public SceneId Scene; }

	public class QuestSelected
	{
		public string LocationId;
		public int QuestIndex;
	}

	public class SetCustomizationTab
	{
		public CustomizationTabType Tab;
	}
}


