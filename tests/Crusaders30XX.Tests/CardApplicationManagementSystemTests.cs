using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class CardApplicationManagementSystemTests
{
	[Theory]
	[InlineData(CardApplicationTarget.HandAndDrawPile, true, true, false, false)]
	[InlineData(CardApplicationTarget.TopXCards, false, true, false, false)]
	[InlineData(CardApplicationTarget.DrawPile, false, true, false, false)]
	[InlineData(CardApplicationTarget.DrawPileAndDiscard, false, true, true, false)]
	[InlineData(CardApplicationTarget.Hand, true, false, false, false)]
	[InlineData(CardApplicationTarget.Deck, true, true, true, true)]
	public void Target_applies_only_to_cards_in_selected_zones(
		CardApplicationTarget target,
		bool appliesToHand,
		bool appliesToDrawPile,
		bool appliesToDiscardPile,
		bool appliesToExhaustPile)
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var deck = CreateDeck(entityManager);
			var handCard = AddCard(entityManager, deck, deck.Hand, new Tempest());
			var drawCard = AddCard(entityManager, deck, deck.DrawPile, new Tempest());
			var discardCard = AddCard(entityManager, deck, deck.DiscardPile, new Tempest());
			var exhaustCard = AddCard(entityManager, deck, deck.ExhaustPile, new Tempest());
			_ = new CardApplicationManagementSystem(entityManager);

			EventManager.Publish(new ApplyCardApplicationEvent
			{
				Amount = 10,
				Type = CardApplicationType.Frozen,
				Target = target,
			});

			Assert.Equal(appliesToHand, handCard.HasComponent<Frozen>());
			Assert.Equal(appliesToDrawPile, drawCard.HasComponent<Frozen>());
			Assert.Equal(appliesToDiscardPile, discardCard.HasComponent<Frozen>());
			Assert.Equal(appliesToExhaustPile, exhaustCard.HasComponent<Frozen>());
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Application_type_adds_the_corresponding_component()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var deck = CreateDeck(entityManager);
			var card = AddCard(entityManager, deck, deck.DrawPile, new Tempest());
			_ = new CardApplicationManagementSystem(entityManager);

			Apply(CardApplicationType.Frozen);
			Apply(CardApplicationType.Brittle);

			Assert.True(card.HasComponent<Frozen>());
			Assert.True(card.HasComponent<Brittle>());
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Application_skips_ineligible_and_already_applied_cards()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var deck = CreateDeck(entityManager);
			var eligibleCard = AddCard(entityManager, deck, deck.Hand, new Tempest());
			var alreadyFrozenCard = AddCard(entityManager, deck, deck.Hand, new Tempest());
			var pledgedCard = AddCard(entityManager, deck, deck.Hand, new Tempest());
			var weaponCard = AddCard(entityManager, deck, deck.Hand, new Dagger());
			entityManager.AddComponent(alreadyFrozenCard, new Frozen());
			entityManager.AddComponent(pledgedCard, new Pledge());
			_ = new CardApplicationManagementSystem(entityManager);

			EventManager.Publish(new ApplyCardApplicationEvent
			{
				Amount = 1,
				Type = CardApplicationType.Frozen,
				Target = CardApplicationTarget.Hand,
			});

			Assert.True(eligibleCard.HasComponent<Frozen>());
			Assert.True(alreadyFrozenCard.HasComponent<Frozen>());
			Assert.False(pledgedCard.HasComponent<Frozen>());
			Assert.False(weaponCard.HasComponent<Frozen>());
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Brittle_sole_blocker_publishes_mill_event()
	{
		EventManager.Clear();
		try
		{
			var entityManager = new EntityManager();
			var brittleCard = entityManager.CreateEntity("BrittleCard");
			entityManager.AddComponent(brittleCard, new Brittle());
			entityManager.AddComponent(brittleCard, new AssignedBlockCard { ContextId = "attack-1" });
			var frozenCard = entityManager.CreateEntity("FrozenCard");
			entityManager.AddComponent(frozenCard, new Frozen());
			entityManager.AddComponent(frozenCard, new AssignedBlockCard { ContextId = "attack-1" });
			var progressEntity = entityManager.CreateEntity("EnemyAttackProgress");
			entityManager.AddComponent(progressEntity, new EnemyAttackProgress
			{
				ContextId = "attack-1",
				PlayedCards = 1,
			});
			_ = new CardApplicationManagementSystem(entityManager);
			int millEvents = 0;
			EventManager.Subscribe<MillCardEvent>(_ => millEvents++);

			EventManager.Publish(new CardBlockedEvent { Card = frozenCard });
			EventManager.Publish(new CardBlockedEvent { Card = brittleCard });

			Assert.Equal(1, millEvents);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	private static void Apply(CardApplicationType type)
	{
		EventManager.Publish(new ApplyCardApplicationEvent
		{
			Amount = 1,
			Type = type,
			Target = CardApplicationTarget.DrawPile,
		});
	}

	private static Deck CreateDeck(EntityManager entityManager)
	{
		var deckEntity = entityManager.CreateEntity("Deck");
		var deck = new Deck();
		entityManager.AddComponent(deckEntity, deck);
		return deck;
	}

	private static Entity AddCard(
		EntityManager entityManager,
		Deck deck,
		System.Collections.Generic.ICollection<Entity> zone,
		CardBase definition)
	{
		var card = entityManager.CreateEntity(definition.CardId);
		entityManager.AddComponent(card, new CardData { Card = definition });
		deck.Cards.Add(card);
		zone.Add(card);
		return card;
	}
}
