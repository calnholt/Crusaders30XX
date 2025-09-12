using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Medals;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Listens to battle phase changes and triggers equipped medals when their triggers match.
	/// Initially supports StartOfBattle heal for Medal of Saint Luke.

	/// </summary>
	public class MedalManagerSystem : Core.System
	{
		public MedalManagerSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnPhaseChanged(ChangeBattlePhaseEvent e)
		{
			if (e.Current != SubPhase.StartBattle) return;
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			foreach (var medal in EnumerateEquippedMedals(player))
			{
				if (string.Equals(medal.trigger, "StartOfBattle", StringComparison.OrdinalIgnoreCase))
				{
					ActivateMedal(player, medal);
				}
			}
		}

		private IEnumerable<MedalDefinition> EnumerateEquippedMedals(Entity player)
		{
			var medalEntities = EntityManager.GetEntitiesWithComponent<EquippedMedal>()
				.Where(e => e.GetComponent<EquippedMedal>().EquippedOwner == player);
			foreach (var e in medalEntities)
			{
				var comp = e.GetComponent<EquippedMedal>();
				if (comp == null || string.IsNullOrWhiteSpace(comp.MedalId)) continue;
				if (!MedalDefinitionCache.TryGet(comp.MedalId, out var def) || def == null) continue;
				yield return def;
			}
		}

		private void ActivateMedal(Entity player, MedalDefinition medal)
		{
			switch (medal.id)
			{
				case "st_luke":
					ApplyStLuke(player, medal);
					break;
				default:
					break;
			}
		}

		private void ApplyStLuke(Entity player, MedalDefinition medal)
		{
			var hp = player.GetComponent<HP>();
			if (hp == null) return;
			int missing = Math.Max(0, hp.Max - hp.Current);
			if (missing <= 0) return;
			// Heal 70% of missing health (rounded down)
			int healAmount = (int)Math.Floor(missing * 0.7f);
			if (healAmount <= 0) return;
			EventManager.Publish(new ModifyHpEvent { Target = player, Delta = healAmount });
		}
	}
}



