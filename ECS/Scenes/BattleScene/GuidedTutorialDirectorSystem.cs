using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public sealed class GuidedTutorialDirectorSystem : Core.System
	{
		public GuidedTutorialDirectorSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
			EventManager.Subscribe<CardPlayedEvent>(OnCardPlayed);
			EventManager.Subscribe<PledgeAddedEvent>(OnPledged);
		}

		protected override IEnumerable<Entity> GetRelevantEntities() => Array.Empty<Entity>();
		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
		{
			var state = GuidedTutorialService.GetState(EntityManager);
			if (state == null) return;
			state.RequiredPhase = evt.Current;

			if (evt.Current == SubPhase.EnemyStart)
			{
				var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
				int turn = phase?.TurnNumber ?? state.Turn;
				if (evt.Previous is SubPhase.PlayerEnd or SubPhase.Action or SubPhase.PlayerStart)
					turn++;
				GuidedTutorialService.BeginNextTurn(EntityManager, turn);
			}
			else if (evt.Current == SubPhase.EnemyAttack)
			{
				foreach (var card in EntityManager.GetEntitiesWithComponent<AssignedBlockCard>())
				{
					string id = card.GetComponent<CardData>()?.Card?.CardId;
					if (!string.IsNullOrEmpty(id))
						state.BlockedCardIdsThisTurn.Add(id);
				}
				state.ConfirmedAttackCountThisTurn++;
			}
		}

		private void OnCardPlayed(CardPlayedEvent evt)
		{
			var state = GuidedTutorialService.GetState(EntityManager);
			string id = evt?.Card?.GetComponent<CardData>()?.Card?.CardId;
			if (state == null || string.IsNullOrEmpty(id)) return;
			state.PlayedCardIds.Add(id);
			state.ActionRequirementsComplete = GuidedTutorialDefinitions.AreActionRequirementsComplete(state);
			GuidedTutorialService.RefreshValidPlays(state);
		}

		private void OnPledged(PledgeAddedEvent evt)
		{
			var state = GuidedTutorialService.GetState(EntityManager);
			string id = evt?.Card?.GetComponent<CardData>()?.Card?.CardId;
			if (state == null || string.IsNullOrEmpty(id)) return;
			state.PledgedCardIds.Add(id);
			state.ActionRequirementsComplete = GuidedTutorialDefinitions.AreActionRequirementsComplete(state);
			GuidedTutorialService.RefreshValidPlays(state);
		}
	}
}
