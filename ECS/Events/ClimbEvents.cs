using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Events
{
	public class ClimbShopSlotSelectedEvent
	{
		public int SlotIndex { get; set; } = -1;
	}

	public class ClimbEncounterSlotSelectedEvent
	{
		public string SlotId { get; set; } = string.Empty;
	}

	public class ClimbEventSlotSelectedEvent
	{
		public string SlotId { get; set; } = string.Empty;
	}

	public class ClimbPreviewStartedEvent
	{
		public string SourceSlotId { get; set; } = string.Empty;
		public int Amount { get; set; }
		public ClimbResourceSave ProjectedResources { get; set; }
	}

	public class ClimbPreviewClearedEvent
	{
	}

	public class ClimbLoadoutOpenRequestedEvent
	{
	}
}
