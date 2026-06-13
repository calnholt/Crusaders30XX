using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Events;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Services
{
	public static class BattleInputGate
	{
		public static bool IsBattleInputFrozen(EntityManager entityManager)
		{
			var phase = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			return (phase != null && phase.DefeatPresentationActive)
				|| StateSingleton.IsActive;
		}

		public static bool TryAllowTutorialAction(
			EntityManager entityManager,
			TutorialAction action,
			Entity card = null)
		{
			bool allowed = IsTutorialActionAllowed(entityManager, action, card);

			if (!allowed)
			{
				EventManager.Publish(new CantPlayCardMessage
				{
					Message = "That action is not available at this tutorial step."
				});
			}
			return allowed;
		}

		public static bool IsTutorialActionAllowed(
			EntityManager entityManager,
			TutorialAction action,
			Entity card = null)
		{
			var state = GuidedTutorialService.GetState(entityManager);
			if (state == null) return true;

			string cardId = card?.GetComponent<CardData>()?.Card?.CardId ?? string.Empty;
			return action switch
			{
				TutorialAction.AssignBlock => CanAssignBlock(entityManager, state, cardId),
				TutorialAction.ConfirmBlocks => CanConfirmBlocks(entityManager, state),
				TutorialAction.PlayCard => state.ValidPlayCardIds.Contains(cardId),
				TutorialAction.PledgeCard => string.Equals(
					GuidedTutorialDefinitions.GetTurn(state.Battle, state.Turn).RequiredPledgeCardId,
					cardId,
					System.StringComparison.OrdinalIgnoreCase),
				TutorialAction.PayCost => CanPayCost(entityManager, card),
				TutorialAction.EndTurn => GuidedTutorialDefinitions.AreActionRequirementsComplete(state),
				_ => false,
			};
		}

		private static bool CanPayCost(EntityManager entityManager, Entity card)
		{
			if (card == null || card.HasComponent<Pledge>()) return false;
			var overlay = entityManager.GetEntitiesWithComponent<PayCostOverlayState>()
				.FirstOrDefault()?.GetComponent<PayCostOverlayState>();
			if (overlay == null || !overlay.IsOpen || overlay.RequiredCosts.Count == 0) return false;
			return overlay.RequiredCosts.Any(cost =>
				CardColorQualificationService.IsEligibleForCost(card, cost));
		}

		private static bool CanAssignBlock(EntityManager entityManager, GuidedTutorial state, string cardId)
		{
			var rule = GetCurrentBlockRule(state);
			if (rule == null || rule.MustNotBlock || string.IsNullOrEmpty(cardId)) return false;
			if (!rule.AllowedCardIds.Contains(cardId)) return false;

			var current = GetCurrentAssignedCardIds(entityManager);
			if (current.Count >= rule.RequiredCount) return false;
			if (current.Contains(cardId)) return false;
			if (state.BlockedCardIdsThisTurn.Contains(cardId)) return false;
			return true;
		}

		private static bool CanConfirmBlocks(EntityManager entityManager, GuidedTutorial state)
		{
			var rule = GetCurrentBlockRule(state);
			if (rule == null) return false;
			var assigned = GetCurrentAssignedCardIds(entityManager);
			if (rule.MustNotBlock) return assigned.Count == 0;
			if (assigned.Count != rule.RequiredCount) return false;
			if (assigned.Any(id => !rule.AllowedCardIds.Contains(id))) return false;
			if (rule.MustUseEveryAllowedCard && !rule.AllowedCardIds.All(assigned.Contains)) return false;

			if (state.Battle == TutorialBattle.SandCorpse && state.Turn >= 2)
			{
				var combined = state.BlockedCardIdsThisTurn.Concat(assigned).ToList();
				if (combined.Count != combined.Distinct().Count()) return false;
				if (state.ConfirmedAttackCountThisTurn == 1)
				{
					var turn = GuidedTutorialDefinitions.GetTurn(state.Battle, state.Turn);
					var allowedAcrossAttacks = turn.BlockRules.SelectMany(blockRule => blockRule.AllowedCardIds).Distinct();
					if (combined.Count != 2 || combined.Any(id => !allowedAcrossAttacks.Contains(id))) return false;
				}
			}

			return true;
		}

		private static TutorialBlockRule GetCurrentBlockRule(GuidedTutorial state)
		{
			var rules = GuidedTutorialDefinitions.GetTurn(state.Battle, state.Turn).BlockRules;
			return state.ConfirmedAttackCountThisTurn >= 0 && state.ConfirmedAttackCountThisTurn < rules.Count
				? rules[state.ConfirmedAttackCountThisTurn]
				: null;
		}

		private static List<string> GetCurrentAssignedCardIds(EntityManager entityManager) =>
			entityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Select(entity => entity.GetComponent<CardData>()?.Card?.CardId)
				.Where(id => !string.IsNullOrEmpty(id))
				.ToList();
	}
}
