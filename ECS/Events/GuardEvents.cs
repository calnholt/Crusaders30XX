using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
	public class AddGuardEvent
	{
		public Entity Enemy { get; set; }
		public int Value { get; set; }
	}

	public class GuardConsumedEvent
	{
		public Entity Enemy { get; set; }
		public int GuardValue { get; set; }
		public int RemainingCount { get; set; }
	}

	public class GuardGainedEvent
	{
		public Entity Enemy { get; set; }
		public int GuardValue { get; set; }
	}
}
