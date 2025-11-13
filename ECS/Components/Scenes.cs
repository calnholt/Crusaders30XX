using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Dialog;
using Crusaders30XX.ECS.Data.Locations;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Components
{
	public enum SceneId
	{
		TitleMenu,
		Internal_QueueEventsMenu,
		WorldMap,
		Customization,
		Battle,
		Location,
		Shop,
		None
	}

	public class SceneState : IComponent
	{
		public Entity Owner { get; set; }
		public SceneId Current { get; set; } = SceneId.Internal_QueueEventsMenu;
	}

	/// <summary>
	/// Marks which scene owns an entity for automatic cleanup on scene transitions.
	/// </summary>
	public class OwnedByScene : IComponent
	{
		public Entity Owner { get; set; }
		public SceneId Scene { get; set; } = SceneId.None;
	}

	/// <summary>
	/// Marker component for entities that should persist across scene transitions.
	/// </summary>
	public class DontDestroyOnLoad : IComponent
	{
		public Entity Owner { get; set; }
		public SceneId Scene { get; set; } = SceneId.None;
	}
	public class DontDestroyOnReload : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Holds a queue of enemy ids selected from the menu to spawn when battle starts.
	/// </summary>
	public class QueuedEvents : IComponent
	{
		public Entity Owner { get; set; }
		public List<QueuedEvent> Events { get; set; } = new List<QueuedEvent>();
		public int CurrentIndex = -1;
		public bool IsFirst = false;
		// Quest context for dialog lookup
		public string LocationId { get; set; } = string.Empty;
		public int QuestIndex { get; set; } = 0;
	}

	public class QueuedEvent 
	{
		public string EventId;
		public QueuedEventType EventType = QueuedEventType.Enemy;
		public List<EnemyModification> Modifications { get; set; } = new List<EnemyModification>();
	}

	public enum QueuedEventType
	{
		Enemy,
		Event,
		Shop,
		Church
	}

	public class EntityListOverlay : IComponent
	{
		public Crusaders30XX.ECS.Core.Entity Owner { get; set; }
		public bool IsOpen { get; set; } = false;
		public int PanelX { get; set; } = 40;
		public int PanelY { get; set; } = 40;
		public int PanelWidth { get; set; } = 520;
		public int PanelHeight { get; set; } = 600;
		public float TextScale { get; set; } = 0.15f;
		public int RowHeight { get; set; } = 24;
		public int Padding { get; set; } = 8;
		public float ScrollOffset { get; set; } = 0f;
	}

	/// <summary>
	/// State for the Customization scene: working deck list and scroll positions.
	/// </summary>
	public class CustomizationState : IComponent
	{
		public Entity Owner { get; set; }
		public List<string> WorkingCardIds { get; set; } = new List<string>();
		public List<string> OriginalCardIds { get; set; } = new List<string>();
		public int LeftScroll { get; set; } = 0;
		public int RightScroll { get; set; } = 0;
		public CustomizationTabType SelectedTab { get; set; } = CustomizationTabType.Deck;
		public string WorkingTemperanceId { get; set; } = string.Empty;
		public string OriginalTemperanceId { get; set; } = string.Empty;
		public string WorkingWeaponId { get; set; } = string.Empty;
		public string OriginalWeaponId { get; set; } = string.Empty;
		public string WorkingHeadId { get; set; } = string.Empty;
		public string OriginalHeadId { get; set; } = string.Empty;
		public string WorkingChestId { get; set; } = string.Empty;
		public string OriginalChestId { get; set; } = string.Empty;
		public string WorkingArmsId { get; set; } = string.Empty;
		public string OriginalArmsId { get; set; } = string.Empty;
		public string WorkingLegsId { get; set; } = string.Empty;
		public string OriginalLegsId { get; set; } = string.Empty;
		public List<string> WorkingMedalIds { get; set; } = new List<string>();
		public List<string> OriginalMedalIds { get; set; } = new List<string>();
	}
	public enum CustomizationTabType
	{
		Deck,
		Weapon,
		Head,
		Chest,
		Arms,
		Legs,
		Temperance,
		Medals,
	}

	/// <summary>
	/// Quest selection overlay state for WorldMap scene.
	/// </summary>
	public class QuestSelectState : IComponent
	{
		public Entity Owner { get; set; }
		public bool IsOpen { get; set; } = false;
		public string LocationId { get; set; } = string.Empty;
		public int SelectedQuestIndex { get; set; } = 0;
	}

	/// <summary>
	/// Marker for quest selection left arrow UI.
	/// </summary>
	public class QuestArrowLeft : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Marker for quest selection right arrow UI.
	/// </summary>
	public class QuestArrowRight : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Marker for quest selection back button UI.
	/// </summary>
	public class QuestBackButton : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Marker for location select "Customize" button UI.
	/// </summary>
	public class LocationCustomizeButton : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Marker for quest selection start area (click to begin quest).
	/// </summary>
	public class QuestStartArea : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Overlay state for battle dialog sequences.
	/// </summary>
	public class DialogOverlayState : IComponent
	{
		public Entity Owner { get; set; }
		public bool IsActive { get; set; } = false;
		public List<DialogLine> Lines { get; set; } = new List<DialogLine>();
		public int Index { get; set; } = 0;
	}

		/// <summary>
		/// Overlay state for the simple quest reward modal shown after the last battle in a quest.
		/// </summary>
		public class QuestRewardOverlayState : IComponent
		{
			public Entity Owner { get; set; }
			public bool IsOpen { get; set; } = false;
			public string Message { get; set; } = "Quest Complete";
		}

	public class PendingQuestDialog : IComponent
	{
		public Entity Owner { get; set; }
		public string DialogId { get; set; } = string.Empty;
		public bool WillShowDialog { get; set; } = false;
	}

	/// <summary>
	/// Anchor entity for the Ambush intro text; position is center of the text.
	/// </summary>
	public class AmbushTextAnchor : IComponent
	{
		public Entity Owner { get; set; }
	}

	/// <summary>
	/// Anchor entity for the Ambush timer bar; position is the center of the bar.
	/// </summary>
	public class AmbushTimerAnchor : IComponent
	{
		public Entity Owner { get; set; }
	}
}



