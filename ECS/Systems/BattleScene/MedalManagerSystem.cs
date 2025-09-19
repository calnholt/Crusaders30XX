using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
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
		[DebugEditable(DisplayName = "Activation Delay (s)", Step = 0.05f, Min = 0f, Max = 2f)]
		public float ActivationDelaySeconds { get; set; } = 0.3f;
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
			var medalEntity = EntityManager.GetEntitiesWithComponent<EquippedMedal>()
				.FirstOrDefault(e => e.GetComponent<EquippedMedal>()?.EquippedOwner == player && e.GetComponent<EquippedMedal>()?.MedalId == medal.id);

			if (!CanTriggerMedal(player, medal)) return;

			EventQueueBridge.EnqueueTriggerAction(() =>
			{
				EventManager.Publish(new MedalTriggered { MedalEntity = medalEntity, MedalId = medal.id });
				ApplyMedalEffect(player, medal);
			}, ActivationDelaySeconds);
		}

		private bool CanTriggerMedal(Entity player, MedalDefinition medal)
		{
			switch (medal.id)
			{
				case "st_luke":
					return GetStLukeHealAmount(player) > 0;
				case "st_michael":
					return true;
				default:
					return false;
			}
		}

		private void ApplyMedalEffect(Entity player, MedalDefinition medal)
		{
			switch (medal.id)
			{
				case "st_luke":
					{
						int healAmount = GetStLukeHealAmount(player);
						if (healAmount > 0)
						{
							EventManager.Publish(new ModifyHpEvent { Source = player, Target = player, Delta = healAmount });
						}
						break;
					}
				case "st_michael":
					EventManager.Publish(new ModifyCourageEvent { Delta = 3 });
					break;
				default:
					break;
			}
		}

		private int GetStLukeHealAmount(Entity player)
		{
			var hp = player.GetComponent<HP>();
			if (hp == null) return 0;
			int missing = Math.Max(0, hp.Max - hp.Current);
			if (missing <= 0) return 0;
			int healAmount = (int)Math.Floor(missing * 0.7f);
			return Math.Max(0, healAmount);
		}


	}
}



