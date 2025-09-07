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
			if (e.Current == SubPhase.StartBattle || e.Current == SubPhase.EnemyStart)
			{
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				if (player == null) return;
				var st = GetOrCreateState(player);
				st.CourageGainedThisBattle = 0;
				st.TriggeredThisBattle.Clear();
			}
		}

		private void OnModifyCourage(ModifyCourageEvent e)
		{
			if (e.Delta <= 0) return;
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var st = GetOrCreateState(player);
			st.CourageGainedThisBattle += e.Delta;

			foreach (var ability in EnumerateEquippedAbilities(player))
			{
				if (ability.trigger == "CourageGainedThreshold")
				{
					if (ability.oncePerBattle && st.TriggeredThisBattle.Contains(ability.id)) continue;
					if (st.CourageGainedThisBattle >= Math.Max(1, ability.threshold))
					{
						EquipmentAbilityService.Activate(EntityManager, ability);
						if (ability.oncePerBattle) st.TriggeredThisBattle.Add(ability.id);
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

		private IEnumerable<AbilityDefintion> EnumerateEquippedAbilities(Entity player)
		{
			var eq = player.GetComponent<EquippedEquipment>();
			if (eq == null) yield break;
			foreach (var id in new[] { eq.HeadId, eq.ChestId, eq.ArmsId, eq.LegsId })
			{
				if (string.IsNullOrWhiteSpace(id)) continue;
				if (!EquipmentDefinitionCache.TryGet(id, out var def) || def?.abilities == null) continue;
				foreach (var a in def.abilities) yield return a;
			}
		}
	}
}


