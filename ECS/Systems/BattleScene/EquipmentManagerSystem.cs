using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Listens to gameplay events and activates equipped equipment abilities when their triggers match.
	/// Uses BattleStateInfo on the player to keep per-battle counters and once-per-battle flags.
	/// </summary>
	public class EquipmentManagerSystem : Core.System
	{
		public EquipmentManagerSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
			EventManager.Subscribe<ModifyCourageEvent>(OnModifyCourage);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnPhaseChanged(ChangeBattlePhaseEvent e)
		{
			if (e.Current == SubPhase.StartBattle)
			{
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				if (player == null) return;
				var st = GetOrCreateState(player);
				st.EquipmentTriggeredThisBattle.Clear();
			}
		}

		private void OnModifyCourage(ModifyCourageEvent e)
		{
			if (e.Delta <= 0) return;
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var st = GetOrCreateState(player);
			var courage = player.GetComponent<Courage>();

			foreach (var ability in EnumerateEquippedAbilities(player))
			{
				if (ability.trigger == "CourageGainedThreshold")
				{
					if (ability.oncePerBattle && st.EquipmentTriggeredThisBattle.Contains(ability.id)) continue;
					if (courage.Amount >= Math.Max(1, ability.threshold))
					{
						EquipmentAbilityService.Activate(EntityManager, ability.id, ability);
						if (ability.oncePerBattle) st.EquipmentTriggeredThisBattle.Add(ability.id);
						// Emit pulse event for UI feedback
						var equipEntity = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
							.FirstOrDefault(en => en.GetComponent<EquippedEquipment>()?.EquippedOwner == player
								&& !string.IsNullOrWhiteSpace(en.GetComponent<EquippedEquipment>()?.EquipmentId)
								&& EquipmentDefinitionCache.TryGet(en.GetComponent<EquippedEquipment>().EquipmentId, out var def2)
								&& def2?.abilities?.Any(a => a.id == ability.id) == true);
						if (equipEntity != null)
						{
							EventManager.Publish(new EquipmentAbilityTriggered { Equipment = equipEntity, EquipmentId = equipEntity.GetComponent<EquippedEquipment>()?.EquipmentId });
						}
					}
				}
			}
		}

		private BattleStateInfo GetOrCreateState(Entity player)
		{
			var st = player.GetComponent<BattleStateInfo>();
			if (st == null)
			{
				st = new BattleStateInfo { Owner = player };
				EntityManager.AddComponent(player, st);
			}
			return st;
		}

		private IEnumerable<AbilityDefinition> EnumerateEquippedAbilities(Entity player)
		{
			var equipmentEntities = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
				.Where(e => e.GetComponent<EquippedEquipment>().EquippedOwner == player);
			foreach (var e in equipmentEntities)
			{
				var comp = e.GetComponent<EquippedEquipment>();
				if (comp == null || string.IsNullOrWhiteSpace(comp.EquipmentId)) continue;
				if (!EquipmentDefinitionCache.TryGet(comp.EquipmentId, out var def) || def?.abilities == null) continue;
				foreach (var a in def.abilities) yield return a;
			}
		}
	}
}


