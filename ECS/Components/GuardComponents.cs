using System.Collections.Generic;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
	public class GuardQueue : IComponent
	{
		public Entity Owner { get; set; }
		public List<int> Queue { get; set; } = new();
	}
}
