using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
	/// <summary>
	/// Marks an entity as a run-persistent deck card tied to a loadout card key (cardId|Color).
	/// </summary>
	public class RunDeckCard : IComponent
	{
		public Entity Owner { get; set; }
		public string CardKey { get; set; } = string.Empty;
	}
}
