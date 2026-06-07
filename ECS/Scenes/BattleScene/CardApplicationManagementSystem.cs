using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class CardApplicationManagementSystem : Core.System
	{
		private sealed record ApplicationDefinition(
			Func<Entity, bool> IsApplied,
			Action<EntityManager, Entity> Apply);

		private static readonly IReadOnlyDictionary<CardApplicationType, ApplicationDefinition> ApplicationDefinitions =
			new Dictionary<CardApplicationType, ApplicationDefinition>
			{
				[CardApplicationType.Frozen] = new(
					card => card.HasComponent<Frozen>(),
					(entityManager, card) => entityManager.AddComponent(card, new Frozen())),
				[CardApplicationType.Brittle] = new(
					card => card.HasComponent<Brittle>(),
					(entityManager, card) => entityManager.AddComponent(card, new Brittle())),
			};

		public CardApplicationManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ApplyCardApplicationEvent>(OnApplyCardApplication);
			EventManager.Subscribe<CardBlockedEvent>(OnCardBlocked);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnApplyCardApplication(ApplyCardApplicationEvent evt)
		{
			if (evt.Amount <= 0) return;

			var definition = GetDefinition(evt.Type);
			var cards = ResolveCandidates(evt.Target)
				.Where(card => !definition.IsApplied(card))
				.Distinct()
				.OrderBy(_ => Random.Shared.Next())
				.Take(evt.Amount)
				.ToList();

			if (cards.Count == 0)
			{
				LoggingService.Append(
					"CardApplicationManagementSystem.Apply",
					new JsonObject
					{
						["message"] = "no eligible cards",
						["applicationType"] = evt.Type.ToString(),
						["target"] = evt.Target.ToString(),
					});
				return;
			}

			foreach (var card in cards)
			{
				definition.Apply(EntityManager, card);
				RunScopedStateService.SyncCardRestrictionsFromComponents(card);
				LoggingService.Append(
					"CardApplicationManagementSystem.Apply.card",
					new JsonObject
					{
						["applicationType"] = evt.Type.ToString(),
						["target"] = evt.Target.ToString(),
						["cardId"] = card.GetComponent<CardData>()?.Card?.CardId ?? "unknown",
					});
			}
		}

		private ApplicationDefinition GetDefinition(CardApplicationType type)
		{
			if (ApplicationDefinitions.TryGetValue(type, out var definition)) return definition;
			throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported card application type.");
		}

		private IEnumerable<Entity> ResolveCandidates(CardApplicationTarget target)
		{
			var deck = EntityManager.GetEntitiesWithComponent<Deck>()
				.FirstOrDefault()
				?.GetComponent<Deck>();
			if (deck == null) return Enumerable.Empty<Entity>();

			return target switch
			{
				CardApplicationTarget.HandAndDrawPile => GetHandCards()
					.Concat(GetNonWeaponCards(deck.DrawPile)),
				CardApplicationTarget.TopXCards => GetNonWeaponCards(deck.DrawPile),
				CardApplicationTarget.DrawPile => GetNonWeaponCards(deck.DrawPile),
				CardApplicationTarget.DrawPileAndDiscard => GetNonWeaponCards(deck.DrawPile)
					.Concat(GetNonWeaponCards(deck.DiscardPile)),
				CardApplicationTarget.Hand => GetHandCards(),
				CardApplicationTarget.Deck => GetNonWeaponCards(deck.Cards),
				_ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported card application target."),
			};
		}

		private IEnumerable<Entity> GetHandCards()
		{
			return GetComponentHelper.GetHandOfCards(EntityManager)
				?? Enumerable.Empty<Entity>();
		}

		private static IEnumerable<Entity> GetNonWeaponCards(IEnumerable<Entity> cards)
		{
			return cards?.Where(card =>
				card != null &&
				(card.GetComponent<CardData>()?.Card?.IsWeapon ?? false) == false)
				?? Enumerable.Empty<Entity>();
		}

		private void OnCardBlocked(CardBlockedEvent evt)
		{
			if (evt.Card?.GetComponent<Brittle>() == null) return;

			var contextId = evt.Card.GetComponent<AssignedBlockCard>()?.ContextId
				?? GetComponentHelper.GetContextId(EntityManager);
			if (string.IsNullOrEmpty(contextId)) return;

			var progress = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>()
				.FirstOrDefault(entity =>
					entity.GetComponent<EnemyAttackProgress>()?.ContextId == contextId)
				?.GetComponent<EnemyAttackProgress>();
			if (progress == null || progress.PlayedCards != 1) return;

			EventManager.Publish(new MillCardEvent());
		}
	}
}
