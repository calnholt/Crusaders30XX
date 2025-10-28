using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Components
{
	public class PointOfInterest : IComponent
	{
		public Entity Owner { get; set; }
		public Vector2 WorldPosition { get; set; } = Vector2.Zero;
		public int RevealRadius { get; set; } = 300;
		public bool IsCompleted { get; set; } = false;
		public bool IsRevealed { get; set; } = false;
		public int UnrevealedRadius { get; set; } = 50;
	}
}


