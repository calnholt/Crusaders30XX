using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
	public class DeckV2CardAdded
	{
		public string CardKey { get; set; }
	}

	public class DeckV2CardRemoved
	{
		public string CardKey { get; set; }
	}

	public class DeckV2DeckChanged { }

	public class WheelSegmentSelected
	{
		public int SegmentIndex { get; set; }
		public WheelSlotType SlotType { get; set; }
	}

	public class BrowseItemRequested
	{
		public int Direction { get; set; }
	}

	public class EquipBrowsedItemRequested
	{
		public WheelSlotType SlotType { get; set; }
	}

	public class V2EquipCompleted
	{
		public WheelSlotType SlotType { get; set; }
		public string ItemId { get; set; }
	}

	public class SwitchCustomizationV2Tab
	{
		public CustomizationV2TabType Tab { get; set; }
	}
}
