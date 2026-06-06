using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Services
{
	public static class EnemyPhaseResetService
	{
		public static bool TryResetForNextPhase(EntityManager entityManager, Entity enemy, Random random = null)
		{
			var enemyComponent = enemy?.GetComponent<Enemy>();
			var enemyBase = enemyComponent?.EnemyBase;
			if (entityManager == null || enemyBase == null || enemyBase.CurrentPhase >= enemyBase.Phases)
			{
				return false;
			}

			EventQueue.Clear();
			enemyBase.CurrentPhase++;

			var phaseState = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			int turnNumber = phaseState?.TurnNumber ?? 1;
			var arsenal = enemy.GetComponent<EnemyArsenal>();
			if (arsenal != null)
			{
				arsenal.AttackIds = enemyBase.GetAttackIds(entityManager, turnNumber).ToList();
			}

			enemy.GetComponent<AttackIntent>()?.Planned.Clear();
			enemy.GetComponent<NextTurnAttackIntent>()?.Planned.Clear();
			foreach (var progress in entityManager.GetEntitiesWithComponent<EnemyAttackProgress>().ToList())
			{
				entityManager.DestroyEntity(progress.Id);
			}

			FullyHeal(entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault());
			FullyHeal(enemy);
			var enemyHp = enemy.GetComponent<HP>();
			if (enemyHp != null)
			{
				enemyComponent.MaxHealth = enemyHp.Max;
				enemyComponent.CurrentHealth = enemyHp.Current;
				enemyBase.MaxHealth = enemyHp.Max;
				enemyBase.CurrentHealth = enemyHp.Current;
			}

			ResetDeck(entityManager, random ?? Random.Shared);
			ClearTurnPassives(entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault());
			ClearTurnPassives(enemy);
			ClearInteractionState(entityManager);

			var actionPoints = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault()?.GetComponent<ActionPoints>();
			if (actionPoints != null) actionPoints.Current = 0;

			return true;
		}

		private static void FullyHeal(Entity entity)
		{
			var hp = entity?.GetComponent<HP>();
			if (hp != null) hp.Current = hp.Max;
		}

		private static void ResetDeck(EntityManager entityManager, Random random)
		{
			var deck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
			if (deck == null) return;

			var exhausted = new HashSet<Entity>(deck.ExhaustPile);
			var cards = deck.DrawPile
				.Concat(deck.Hand)
				.Concat(deck.DiscardPile)
				.Where(card => card != null
					&& !exhausted.Contains(card)
					&& card.GetComponent<CardData>()?.Card?.IsWeapon != true)
				.Distinct()
				.ToList();

			for (int i = cards.Count - 1; i > 0; i--)
			{
				int j = random.Next(i + 1);
				(cards[i], cards[j]) = (cards[j], cards[i]);
			}

			deck.DrawPile.Clear();
			deck.DrawPile.AddRange(cards);
			deck.Hand.Clear();
			deck.DiscardPile.Clear();

			foreach (var card in cards)
			{
				ResetCardPresentation(card);
			}
		}

		private static void ResetCardPresentation(Entity card)
		{
			var ui = card.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.IsInteractable = false;
				ui.IsHovered = false;
				ui.IsClicked = false;
				ui.EventType = UIElementEventType.None;
				ui.Bounds = new Microsoft.Xna.Framework.Rectangle(-1000, -1000, 1, 1);
			}

			var transform = card.GetComponent<Transform>();
			if (transform != null)
			{
				transform.Position = Microsoft.Xna.Framework.Vector2.Zero;
				transform.Rotation = 0f;
				transform.Scale = Microsoft.Xna.Framework.Vector2.One;
			}
		}

		private static void ClearTurnPassives(Entity owner)
		{
			var passives = owner?.GetComponent<AppliedPassives>()?.Passives;
			if (passives == null) return;

			foreach (var passive in AppliedPassivesManagementSystem.GetTurnPassives()
				.Concat(AppliedPassivesManagementSystem.GetTurnPassivesToDecrement()))
			{
				passives.Remove(passive);
			}
		}

		private static void ClearInteractionState(EntityManager entityManager)
		{
			foreach (var assigned in entityManager.GetEntitiesWithComponent<AssignedBlockCard>().ToList())
			{
				entityManager.RemoveComponent<AssignedBlockCard>(assigned);
				var equipmentZone = assigned.GetComponent<EquipmentZone>();
				if (equipmentZone != null) equipmentZone.Zone = EquipmentZoneType.Default;
			}

			foreach (var card in entityManager.GetEntitiesWithComponent<CardData>().ToList())
			{
				RemoveIfPresent<Pledge>(entityManager, card);
				RemoveIfPresent<PledgePreview>(entityManager, card);
				RemoveIfPresent<SelectedForPayment>(entityManager, card);
				RemoveIfPresent<MarkedForSpecificDiscard>(entityManager, card);
				RemoveIfPresent<MarkedForReturnToDeck>(entityManager, card);
				RemoveIfPresent<MarkedForBottomOfDrawPile>(entityManager, card);
				RemoveIfPresent<MarkedForExhaust>(entityManager, card);
				RemoveIfPresent<MarkedForEndOfTurnDiscard>(entityManager, card);
				RemoveIfPresent<AnimatingHandToDiscard>(entityManager, card);
				RemoveIfPresent<AnimatingHandToZone>(entityManager, card);
				RemoveIfPresent<AnimatingHandToDrawPile>(entityManager, card);
				RemoveIfPresent<CardToDiscardFlight>(entityManager, card);
				RemoveIfPresent<FilteredFromHand>(entityManager, card);
				RemoveIfPresent<Intimidated>(entityManager, card);
				RemoveIfPresent<CannotBlockThisAttack>(entityManager, card);
			}

			var payCostState = entityManager.GetEntitiesWithComponent<PayCostOverlayState>()
				.FirstOrDefault()?.GetComponent<PayCostOverlayState>();
			if (payCostState != null)
			{
				payCostState.IsOpen = false;
				payCostState.CardToPlay = null;
				payCostState.SelectedCards.Clear();
				payCostState.ConsumedCostByCardId.Clear();
			}

			var ambushState = entityManager.GetEntitiesWithComponent<AmbushState>()
				.FirstOrDefault()?.GetComponent<AmbushState>();
			if (ambushState != null)
			{
				ambushState.IsActive = false;
				ambushState.IntroActive = false;
				ambushState.TimerRemainingSeconds = 0f;
				ambushState.FiredAutoConfirm = false;
				ambushState.ContextId = string.Empty;
			}

			var paymentCache = entityManager.GetEntitiesWithComponent<LastPaymentCache>()
				.FirstOrDefault()?.GetComponent<LastPaymentCache>();
			if (paymentCache != null)
			{
				paymentCache.CardPlayed = null;
				paymentCache.PaymentCards.Clear();
				paymentCache.HasData = false;
			}
		}

		private static void RemoveIfPresent<T>(EntityManager entityManager, Entity entity)
			where T : class, IComponent
		{
			if (entity.HasComponent<T>()) entityManager.RemoveComponent<T>(entity);
		}
	}
}
