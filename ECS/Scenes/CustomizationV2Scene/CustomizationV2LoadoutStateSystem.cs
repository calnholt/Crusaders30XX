using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 State")]
	public class CustomizationV2LoadoutStateSystem : Core.System
	{
		public CustomizationV2LoadoutStateSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<V2EquipCompleted>(OnEquipCompleted);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			EnsureLoadoutStateLoaded();
		}

		private void EnsureLoadoutStateLoaded()
		{
			var existing = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault();
			if (existing != null) return;

			var stateEntity = EntityManager.CreateEntity("CV2_LoadoutState");
			var st = new CustomizationV2LoadoutState();

			if (LoadoutDefinitionCache.TryGet("loadout_1", out var def) && def != null)
			{
				st.WorkingWeaponId = def.weaponId ?? string.Empty;
				st.WorkingHeadId = def.headId ?? string.Empty;
				st.WorkingChestId = def.chestId ?? string.Empty;
				st.WorkingArmsId = def.armsId ?? string.Empty;
				st.WorkingLegsId = def.legsId ?? string.Empty;
				st.WorkingTemperanceId = def.temperanceId ?? string.Empty;
				st.WorkingMedalIds = new List<string>(def.medalIds ?? new List<string>());
			}

			EntityManager.AddComponent(stateEntity, st);
		}

		private void OnEquipCompleted(V2EquipCompleted evt)
		{
			if (evt == null) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			SaveToDisk();
		}

		private void SaveToDisk()
		{
			var st = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (st == null) return;

			if (!LoadoutDefinitionCache.TryGet("loadout_1", out var def) || def == null)
			{
				def = new LoadoutDefinition { id = "loadout_1", name = "Loadout 1" };
			}

			def.weaponId = st.WorkingWeaponId;
			def.headId = st.WorkingHeadId;
			def.chestId = st.WorkingChestId;
			def.armsId = st.WorkingArmsId;
			def.legsId = st.WorkingLegsId;
			def.temperanceId = st.WorkingTemperanceId;
			def.medalIds = new List<string>(st.WorkingMedalIds ?? new List<string>());

			SaveCache.SaveLoadout(def);
			Console.WriteLine("[CV2] Loadout auto-saved.");
		}
	}
}
