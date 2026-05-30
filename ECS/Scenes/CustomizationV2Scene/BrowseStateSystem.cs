using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Temperance;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Browse")]
	public class BrowseStateSystem : Core.System
	{
		private List<string> _browseItems = new();
		private int _browseIndex;
		private WheelSlotType _currentSlot;

		public BrowseStateSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<WheelSegmentSelected>(OnSegmentSelected);
			EventManager.Subscribe<BrowseItemRequested>(OnBrowseRequested);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnSegmentSelected(WheelSegmentSelected evt)
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			_currentSlot = evt.SlotType;
			_browseItems = BuildItemListForSlot(evt.SlotType);
			_browseIndex = 0;

			// Try to set index to currently equipped item
			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout != null)
			{
				string equippedId = GetEquippedId(evt.SlotType, loadout);
				int idx = _browseItems.IndexOf(equippedId);
				if (idx >= 0) _browseIndex = idx;
			}

			UpdateLoadoutBrowseState();
		}

		private void OnBrowseRequested(BrowseItemRequested evt)
		{
			if (_browseItems.Count == 0) return;

			_browseIndex += evt.Direction;
			if (_browseIndex < 0) _browseIndex = _browseItems.Count - 1;
			if (_browseIndex >= _browseItems.Count) _browseIndex = 0;

			UpdateLoadoutBrowseState();
		}

		private void UpdateLoadoutBrowseState()
		{
			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout == null) return;
			loadout.BrowseIndex = _browseIndex;
			loadout.BrowseCount = _browseItems.Count;
		}

		public string GetBrowsedItemId()
		{
			if (_browseItems.Count == 0 || _browseIndex < 0 || _browseIndex >= _browseItems.Count) return "";
			return _browseItems[_browseIndex];
		}

		public int GetBrowseIndex() => _browseIndex;
		public int GetBrowseCount() => _browseItems.Count;
		public WheelSlotType GetCurrentSlot() => _currentSlot;

		private List<string> BuildItemListForSlot(WheelSlotType slot)
		{
			var items = new List<string>();
			var loadout = SaveCache.GetLoadout("loadout_1");
			switch (slot)
			{
				case WheelSlotType.Weapon:
					if (!string.IsNullOrEmpty(loadout?.weaponId))
					{
						items.Add(loadout.weaponId);
					}
					break;

				case WheelSlotType.Head:
					items.Add("");
					if (!string.IsNullOrEmpty(loadout?.headId)) items.Add(loadout.headId);
					break;

				case WheelSlotType.Chest:
					items.Add("");
					if (!string.IsNullOrEmpty(loadout?.chestId)) items.Add(loadout.chestId);
					break;

				case WheelSlotType.Arms:
					items.Add("");
					if (!string.IsNullOrEmpty(loadout?.armsId)) items.Add(loadout.armsId);
					break;

				case WheelSlotType.Legs:
					items.Add("");
					if (!string.IsNullOrEmpty(loadout?.legsId)) items.Add(loadout.legsId);
					break;

				case WheelSlotType.Temperance:
					items.Add("");
					if (!string.IsNullOrEmpty(loadout?.temperanceId)) items.Add(loadout.temperanceId);
					break;

				case WheelSlotType.Medal1:
				case WheelSlotType.Medal2:
				case WheelSlotType.Medal3:
					items.Add("");
					if (loadout?.medalIds != null)
					{
						foreach (var medalId in loadout.medalIds.OrderBy(id => id))
						{
							if (!string.IsNullOrEmpty(medalId)) items.Add(medalId);
						}
					}
					break;
			}
			return items;
		}

		private static string GetEquippedId(WheelSlotType slot, CustomizationV2LoadoutState st) => slot switch
		{
			WheelSlotType.Weapon => st.WorkingWeaponId,
			WheelSlotType.Head => st.WorkingHeadId,
			WheelSlotType.Chest => st.WorkingChestId,
			WheelSlotType.Arms => st.WorkingArmsId,
			WheelSlotType.Legs => st.WorkingLegsId,
			WheelSlotType.Temperance => st.WorkingTemperanceId,
			WheelSlotType.Medal1 => st.WorkingMedalIds?.Count > 0 ? st.WorkingMedalIds[0] : "",
			WheelSlotType.Medal2 => st.WorkingMedalIds?.Count > 1 ? st.WorkingMedalIds[1] : "",
			WheelSlotType.Medal3 => st.WorkingMedalIds?.Count > 2 ? st.WorkingMedalIds[2] : "",
			_ => ""
		};
	}
}
