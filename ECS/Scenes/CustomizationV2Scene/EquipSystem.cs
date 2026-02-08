using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class EquipSystem : Core.System
	{
		private readonly BrowseStateSystem _browseSystem;

		public EquipSystem(EntityManager em, BrowseStateSystem browseSystem) : base(em)
		{
			_browseSystem = browseSystem;
			EventManager.Subscribe<EquipBrowsedItemRequested>(OnEquipRequested);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnEquipRequested(EquipBrowsedItemRequested evt)
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout == null) return;

			string itemId = _browseSystem.GetBrowsedItemId();
			EquipToSlot(evt.SlotType, itemId, loadout);
			EventManager.Publish(new V2EquipCompleted { SlotType = evt.SlotType, ItemId = itemId });
		}

		private void EquipToSlot(WheelSlotType slot, string itemId, CustomizationV2LoadoutState st)
		{
			switch (slot)
			{
				case WheelSlotType.Weapon:
					st.WorkingWeaponId = itemId;
					break;
				case WheelSlotType.Head:
					st.WorkingHeadId = itemId;
					break;
				case WheelSlotType.Chest:
					st.WorkingChestId = itemId;
					break;
				case WheelSlotType.Arms:
					st.WorkingArmsId = itemId;
					break;
				case WheelSlotType.Legs:
					st.WorkingLegsId = itemId;
					break;
				case WheelSlotType.Temperance:
					st.WorkingTemperanceId = itemId;
					break;
				case WheelSlotType.Medal1:
					EquipMedal(0, itemId, st);
					break;
				case WheelSlotType.Medal2:
					EquipMedal(1, itemId, st);
					break;
				case WheelSlotType.Medal3:
					EquipMedal(2, itemId, st);
					break;
			}
		}

		private void EquipMedal(int slotIndex, string medalId, CustomizationV2LoadoutState st)
		{
			if (st.WorkingMedalIds == null) st.WorkingMedalIds = new List<string>();

			// Ensure list is long enough
			while (st.WorkingMedalIds.Count <= slotIndex)
			{
				st.WorkingMedalIds.Add("");
			}

			// If empty string, just clear the slot
			if (string.IsNullOrEmpty(medalId))
			{
				st.WorkingMedalIds[slotIndex] = "";
				return;
			}

			// Check for duplicate - swap if another slot has this medal
			for (int i = 0; i < st.WorkingMedalIds.Count; i++)
			{
				if (i != slotIndex && string.Equals(st.WorkingMedalIds[i], medalId, StringComparison.OrdinalIgnoreCase))
				{
					// Swap: give the other slot what this slot had
					st.WorkingMedalIds[i] = st.WorkingMedalIds[slotIndex];
					break;
				}
			}

			st.WorkingMedalIds[slotIndex] = medalId;
		}
	}
}
