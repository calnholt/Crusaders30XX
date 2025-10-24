using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Components
{
	/// <summary>
	/// Shared camera state for the Location scene. Updated by LocationMapDisplaySystem each frame.
	/// </summary>
	public class LocationCameraState : IComponent
	{
		public Entity Owner { get; set; }
		public Vector2 Center { get; set; } = Vector2.Zero;   // world-space camera center
		public Vector2 Origin { get; set; } = Vector2.Zero;   // world-space top-left of viewport
		public int ViewportW { get; set; } = 0;
		public int ViewportH { get; set; } = 0;
	}
}


