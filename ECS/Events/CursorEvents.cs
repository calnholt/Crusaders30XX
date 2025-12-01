using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
	public enum InputMethod
	{
		Mouse,
		Gamepad,
		Keyboard
	}

	public class CursorStateEvent
	{
		public Vector2 Position { get; set; }
		public bool IsAPressed { get; set; }
		public bool IsAPressedEdge { get; set; }
		public float Coverage { get; set; }
		public Entity TopEntity { get; set; }
		public InputMethod Source { get; set; }
	}

	public class SetCursorEnabledEvent
	{
		public bool Enabled { get; set; }
	}

	public class HotKeySelectEvent
	{
		public Entity Entity { get; set; }
	}
}


