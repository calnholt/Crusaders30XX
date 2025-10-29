using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
	public class CursorStateEvent
	{
		public Vector2 Position { get; set; }
		public bool IsAPressed { get; set; }
		public bool IsAPressedEdge { get; set; }
		public float Coverage { get; set; }
		public Entity TopEntity { get; set; }
	}

	public class SetCursorEnabledEvent
	{
		public bool Enabled { get; set; }
	}
}


