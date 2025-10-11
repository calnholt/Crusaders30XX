using Crusaders30XX.ECS.Core;
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
		None
	}

	public class SceneState : IComponent
	{
		public Entity Owner { get; set; }
		public SceneId Current { get; set; } = SceneId.Internal_QueueEventsMenu;
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
	}

	public class QueuedEvent 
	{
		public string EventId;
		public QueuedEventType EventType = QueuedEventType.Enemy;

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
}



