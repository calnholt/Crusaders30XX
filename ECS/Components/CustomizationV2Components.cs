using System.Collections.Generic;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
	public enum CustomizationV2TabType
	{
		Deck,
		Loadout
	}

	public enum WheelSlotType
	{
		Weapon,
		Head,
		Chest,
		Arms,
		Legs,
		Temperance,
		Medal1,
		Medal2,
		Medal3
	}

	public class CustomizationV2NavigationState : IComponent
	{
		public Entity Owner { get; set; }
		public CustomizationV2TabType ActiveTab { get; set; } = CustomizationV2TabType.Deck;
	}

	public class CustomizationV2DeckState : IComponent
	{
		public Entity Owner { get; set; }
		public List<string> DeckCardKeys { get; set; } = new();
		public int AvailableScroll { get; set; } = 0;
		public int DeckListScroll { get; set; } = 0;
		public string LoadoutId { get; set; } = "loadout_1";
		public Dictionary<string, float> AddFlashTimers { get; set; } = new();
		public Dictionary<string, float> RemoveSlideTimers { get; set; } = new();
	}

	public class CustomizationV2LoadoutState : IComponent
	{
		public Entity Owner { get; set; }
		public int HoveredSegmentIndex { get; set; } = -1;
		public int BrowseIndex { get; set; } = 0;
		public int BrowseCount { get; set; } = 0;
		public string WorkingWeaponId { get; set; } = string.Empty;
		public string WorkingHeadId { get; set; } = string.Empty;
		public string WorkingChestId { get; set; } = string.Empty;
		public string WorkingArmsId { get; set; } = string.Empty;
		public string WorkingLegsId { get; set; } = string.Empty;
		public string WorkingTemperanceId { get; set; } = string.Empty;
		public List<string> WorkingMedalIds { get; set; } = new();
	}

	public class WheelSegment : IComponent
	{
		public Entity Owner { get; set; }
		public WheelSlotType SlotType { get; set; }
		public int SegmentIndex { get; set; }
	}

	public class CenterHub : IComponent
	{
		public Entity Owner { get; set; }
	}

	public class EquippedPanelRow : IComponent
	{
		public Entity Owner { get; set; }
		public WheelSlotType SlotType { get; set; }
		public int RowIndex { get; set; }
	}

	public class BrowseArrow : IComponent
	{
		public Entity Owner { get; set; }
		public int Direction { get; set; }
	}
}
