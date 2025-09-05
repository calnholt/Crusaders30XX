using Crusaders30XX.ECS.Core;

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
}


