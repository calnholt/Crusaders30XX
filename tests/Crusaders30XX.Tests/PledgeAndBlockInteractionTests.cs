using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class PledgeAndBlockInteractionTests : IDisposable
{
    public PledgeAndBlockInteractionTests()
    {
        EventManager.Clear();
        StateSingleton.IsActive = false;
        StateSingleton.PreventClicking = false;
        StateSingleton.IsTutorialActive = false;
        StateSingleton.IsPledgeEnabled = true;
    }

    public void Dispose()
    {
        EventManager.Clear();
        StateSingleton.IsActive = false;
        StateSingleton.PreventClicking = false;
        StateSingleton.IsTutorialActive = false;
        StateSingleton.IsPledgeEnabled = true;
    }

    [Theory]
    [InlineData("sealed", "Sealed cards cannot be pledged!")]
    [InlineData("weapon", "Can't pledge weapons!")]
    [InlineData("block", "Can't pledge block cards!")]
    [InlineData("relic", "Can't pledge relics!")]
    [InlineData("token", "Can't pledge token cards!")]
    public void Invalid_pledge_attempt_preserves_card_specific_message(
        string invalidKind,
        string expectedMessage)
    {
        var entityManager = BuildActionHand(out var deck, out _);
        var card = AddCard(entityManager, deck, CreateDefinition(invalidKind));
        if (invalidKind == "sealed")
        {
            entityManager.AddComponent(card, new Sealed());
        }
        _ = new PledgeManagementSystem(entityManager);
        var messages = new List<string>();
        EventManager.Subscribe<CantPlayCardMessage>(evt => messages.Add(evt.Message));

        EventManager.Publish(new PledgeCardRequested { Card = card });

        Assert.Equal([expectedMessage], messages);
        Assert.False(card.HasComponent<Pledge>());
    }

    [Fact]
    public void Pledge_attempt_preserves_once_per_action_phase_message()
    {
        var entityManager = BuildActionHand(out var deck, out _);
        var card = AddCard(entityManager, deck, new CardBase());
        PledgeAvailabilityService.SetPledgedThisActionPhase(entityManager, true);
        _ = new PledgeManagementSystem(entityManager);
        string message = null;
        EventManager.Subscribe<CantPlayCardMessage>(evt => message = evt.Message);

        EventManager.Publish(new PledgeCardRequested { Card = card });

        Assert.Equal("You can only pledge one card per action phase!", message);
    }

    [Fact]
    public void Pledge_attempt_preserves_existing_pledge_message()
    {
        var entityManager = BuildActionHand(out var deck, out _);
        var candidate = AddCard(entityManager, deck, new CardBase());
        var pledged = AddCard(entityManager, deck, new CardBase());
        entityManager.AddComponent(pledged, new Pledge());
        _ = new PledgeManagementSystem(entityManager);
        string message = null;
        EventManager.Subscribe<CantPlayCardMessage>(evt => message = evt.Message);

        EventManager.Publish(new PledgeCardRequested { Card = candidate });

        Assert.Equal("You already have a pledged card in hand!", message);
    }

    [Fact]
    public void Removing_a_pledged_card_does_not_restore_the_same_phase_opportunity()
    {
        var entityManager = BuildActionHand(out var deck, out var deckEntity);
        var first = AddCard(entityManager, deck, new CardBase());
        AddCard(entityManager, deck, new CardBase());
        _ = new PledgeManagementSystem(entityManager);

        EventManager.Publish(new PledgeCardRequested { Card = first });
        EventManager.Publish(new CardMoved
        {
            Card = first,
            Deck = deckEntity,
            From = CardZoneType.Hand,
            To = CardZoneType.DiscardPile,
        });

        var result = PledgeAvailabilityService.Evaluate(entityManager);
        Assert.False(result.IsAvailable);
        Assert.Equal(PledgeAvailabilityFailure.AlreadyPledgedThisActionPhase, result.Failure);
    }

    [Fact]
    public void Hand_block_assignment_uses_typed_request_and_ignores_IsClicked_polling()
    {
        var entityManager = new EntityManager();
        var phaseEntity = entityManager.CreateEntity("PhaseState");
        entityManager.AddComponent(phaseEntity, new PhaseState
        {
            Main = MainPhase.EnemyTurn,
            Sub = SubPhase.Block,
        });
        var deckEntity = entityManager.CreateEntity("Deck");
        var deck = new Deck();
        entityManager.AddComponent(deckEntity, deck);
        var card = AddCard(entityManager, deck, new CardBase { Block = 3 });
        entityManager.AddComponent(card, new UIElement { IsInteractable = true, IsClicked = true });
        entityManager.AddComponent(card, new Transform { Position = new Vector2(100, 200) });
        var enemy = entityManager.CreateEntity("Enemy");
        entityManager.AddComponent(enemy, new AttackIntent
        {
            Planned = [new PlannedAttack { ContextId = "attack-1" }],
        });
        var system = new HandBlockInteractionSystem(entityManager);
        int moves = 0;
        int assignments = 0;
        EventManager.Subscribe<CardMoveRequested>(evt =>
        {
            Assert.Same(card, evt.Card);
            moves++;
        });
        EventManager.Subscribe<BlockAssignmentAdded>(evt =>
        {
            Assert.Same(card, evt.Card);
            assignments++;
        });

        system.Update(new GameTime());
        Assert.Equal(0, moves);
        Assert.Equal(0, assignments);

        EventManager.Publish(new AssignCardAsBlockRequested { Card = card });

        Assert.Equal(1, moves);
        Assert.Equal(1, assignments);
    }

    private static EntityManager BuildActionHand(out Deck deck, out Entity deckEntity)
    {
        var entityManager = new EntityManager();
        var phaseEntity = entityManager.CreateEntity("PhaseState");
        entityManager.AddComponent(phaseEntity, new PhaseState
        {
            Main = MainPhase.PlayerTurn,
            Sub = SubPhase.Action,
        });
        deckEntity = entityManager.CreateEntity("Deck");
        deck = new Deck();
        entityManager.AddComponent(deckEntity, deck);
        return entityManager;
    }

    private static Entity AddCard(EntityManager entityManager, Deck deck, CardBase definition)
    {
        var card = entityManager.CreateEntity("Card");
        entityManager.AddComponent(card, new CardData { Card = definition });
        deck.Hand.Add(card);
        return card;
    }

    private static CardBase CreateDefinition(string invalidKind)
    {
        return invalidKind switch
        {
            "weapon" => new CardBase { IsWeapon = true },
            "block" => new CardBase { Type = CardType.Block },
            "relic" => new CardBase { Type = CardType.Relic },
            "token" => new CardBase { IsToken = true },
            _ => new CardBase(),
        };
    }
}
