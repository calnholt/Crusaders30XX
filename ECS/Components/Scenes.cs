using Crusaders30XX.ECS.Core;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Components
{
	public enum SceneId
	{
		Menu,
		Customization,
		Battle,
		None
	}

	public class SceneState : IComponent
	{
		public Entity Owner { get; set; }
		public SceneId Current { get; set; } = SceneId.Menu;
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
		public Crusaders30XX.ECS.Core.Entity Owner { get; set; }
		public System.Collections.Generic.List<string> WorkingCardIds { get; set; } = new System.Collections.Generic.List<string>();
		public System.Collections.Generic.List<string> OriginalCardIds { get; set; } = new System.Collections.Generic.List<string>();
		public int LeftScroll { get; set; } = 0;
		public int RightScroll { get; set; } = 0;
	}
}



