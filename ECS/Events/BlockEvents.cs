namespace Crusaders30XX.ECS.Events
{
	public class ModifyStoredBlock
	{
		public int Delta; // positive to add, negative to consume
	}

	public class BlockAssignmentChanged
	{
		public string ContextId;
		public Crusaders30XX.ECS.Core.Entity Card;
		public int DeltaBlock;
		public string Color; // optional, e.g., "Red", "White", "Black"
	}
}


