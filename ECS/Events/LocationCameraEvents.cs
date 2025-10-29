using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Events
{
	public class FocusLocationCameraEvent
	{
		public Vector2 WorldPos { get; set; }
	}

	public class LockLocationCameraEvent
	{
		public bool Locked { get; set; }
	}
}


