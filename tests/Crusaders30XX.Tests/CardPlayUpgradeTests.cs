using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public sealed class CardPlayUpgradeTests : IDisposable
{
	public CardPlayUpgradeTests()
	{
		EventManager.Clear();
		EventQueue.Clear();
		StateSingleton.IsActive = false;
	}

	public void Dispose()
	{
		EventManager.Clear();
		EventQueue.Clear();
		StateSingleton.IsActive = false;
	}

	[Fact]
	public void Unlocked_pledged_fervor_upgrade_accepts_any_color_for_its_cost()
	{
		var entityManager = BuildActionBattle(1, "fervor", out var deck);
		var fervor = AddCard(entityManager, deck, "fervor", CardData.CardColor.Red, isUpgraded: true);
		entityManager.AddComponent(fervor, new Pledge { CanPlay = true });
		AddCard(entityManager, deck, "smite", CardData.CardColor.White);
		AddCard(entityManager, deck, "smite", CardData.CardColor.Black);
		_ = new CardPlaySystem(entityManager);
		OpenPayCostOverlayEvent openedOverlay = null;
		var messages = new List<string>();
		EventManager.Subscribe<OpenPayCostOverlayEvent>(evt => openedOverlay = evt);
		EventManager.Subscribe<CantPlayCardMessage>(evt => messages.Add(evt.Message));

		EventManager.Publish(new PlayCardRequested { Card = fervor });

		Assert.NotNull(openedOverlay);
		Assert.Same(fervor, openedOverlay.CardToPlay);
		Assert.Equal(["Any"], openedOverlay.RequiredCosts);
		Assert.Empty(messages);
	}

	[Fact]
	public void Base_fervor_still_requires_a_red_card_for_its_cost()
	{
		var entityManager = BuildActionBattle(1, "fervor", out var deck);
		var fervor = AddCard(entityManager, deck, "fervor", CardData.CardColor.Red);
		AddCard(entityManager, deck, "smite", CardData.CardColor.White);
		AddCard(entityManager, deck, "smite", CardData.CardColor.Black);
		_ = new CardPlaySystem(entityManager);
		OpenPayCostOverlayEvent openedOverlay = null;
		string message = null;
		EventManager.Subscribe<OpenPayCostOverlayEvent>(evt => openedOverlay = evt);
		EventManager.Subscribe<CantPlayCardMessage>(evt => message = evt.Message);

		EventManager.Publish(new PlayCardRequested { Card = fervor });

		Assert.Null(openedOverlay);
		Assert.Equal("Can't pay card's cost!", message);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, true)]
	public void Burn_upgrade_is_free_to_play_without_action_points(bool isUpgraded, bool expectedPlayed)
	{
		var entityManager = BuildActionBattle(0, "burn", out var deck);
		var burn = AddCard(entityManager, deck, "burn", CardData.CardColor.Red, isUpgraded);
		_ = new CardPlaySystem(entityManager);
		int burnApplications = 0;
		string message = null;
		EventManager.Subscribe<ApplyPassiveEvent>(evt =>
		{
			if (evt.Type == AppliedPassiveType.Burn)
				burnApplications++;
		});
		EventManager.Subscribe<CantPlayCardMessage>(evt => message = evt.Message);

		EventManager.Publish(new PlayCardRequested { Card = burn });

		Assert.Equal(expectedPlayed ? 1 : 0, burnApplications);
		Assert.Equal(expectedPlayed ? null : "Not enough action points!", message);
	}

	[Fact]
	public void Upgraded_play_effect_uses_the_initialized_card_instance()
	{
		var entityManager = BuildActionBattle(0, "rally_the_faithful", out var deck);
		var rally = AddCard(
			entityManager,
			deck,
			"rally_the_faithful",
			CardData.CardColor.White,
			isUpgraded: true);
		_ = new CardPlaySystem(entityManager);
		var courageRequests = new List<ModifyCourageRequestEvent>();
		EventManager.Subscribe<ModifyCourageRequestEvent>(courageRequests.Add);

		EventManager.Publish(new PlayCardRequested { Card = rally });

		Assert.Equal(1, Assert.Single(courageRequests).Delta);
	}

	private static EntityManager BuildActionBattle(
		int actionPoints,
		string playableCardId,
		out Deck deck)
	{
		var entityManager = new EntityManager();
		var phaseEntity = entityManager.CreateEntity("PhaseState");
		entityManager.AddComponent(phaseEntity, new PhaseState
		{
			Main = MainPhase.PlayerTurn,
			Sub = SubPhase.Action,
		});

		var deckEntity = entityManager.CreateEntity("Deck");
		deck = new Deck();
		entityManager.AddComponent(deckEntity, deck);

		var player = entityManager.CreateEntity("Player");
		entityManager.AddComponent(player, new Player { DeckEntity = deckEntity });
		entityManager.AddComponent(player, new ActionPoints { Current = actionPoints });
		entityManager.AddComponent(player, new Courage());

		var enemy = entityManager.CreateEntity("Enemy");
		entityManager.AddComponent(enemy, new Enemy());

		var tutorial = entityManager.CreateEntity("GuidedTutorial");
		entityManager.AddComponent(tutorial, new GuidedTutorial { Section = 1 });

		return entityManager;
	}

	private static Entity AddCard(
		EntityManager entityManager,
		Deck deck,
		string cardId,
		CardData.CardColor color,
		bool isUpgraded = false)
	{
		var card = EntityFactory.CreateCardFromDefinition(
			entityManager,
			cardId,
			color,
			isUpgraded: isUpgraded);
		Assert.NotNull(card);
		deck.Cards.Add(card);
		deck.Hand.Add(card);
		return card;
	}
}
