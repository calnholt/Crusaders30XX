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
	public class QueuedEnemies : IComponent
	{
		public Entity Owner { get; set; }
		public List<string> EnemyIds { get; set; } = new List<string>();
	}
}



