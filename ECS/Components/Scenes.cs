using Crusaders30XX.ECS.Core;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Components
{
	public enum SceneId
	{
		Menu,
		Battle
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
		public int CurrentIndex = 0;
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
		public float TextScale { get; set; } = 0.6f;
		public int RowHeight { get; set; } = 24;
		public int Padding { get; set; } = 8;
		public float ScrollOffset { get; set; } = 0f;
	}
}



