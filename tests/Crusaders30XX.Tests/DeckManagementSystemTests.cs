using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class DeckManagementSystemTests
{
    [Fact]
    public void DrawCard_clears_stale_filtered_from_hand_marker()
    {
        var entityManager = new EntityManager();
        var deck = new Deck();
        var card = CreateCard(entityManager);
        entityManager.AddComponent(card, new FilteredFromHand());
        deck.DrawPile.Add(card);
        var system = new DeckManagementSystem(entityManager);

        bool drawn = system.DrawCard(deck);

        Assert.True(drawn);
        Assert.Contains(card, deck.Hand);
        Assert.DoesNotContain(card, deck.DrawPile);
        Assert.False(card.HasComponent<FilteredFromHand>());
        Assert.True(HandStateLoggingService.CountsForHandLayout(card));
    }

    private static Entity CreateCard(EntityManager entityManager)
    {
        var entity = entityManager.CreateEntity("tempest");
        entityManager.AddComponent(entity, new CardData { Card = new Tempest() });
        entityManager.AddComponent(entity, new Transform { Position = Vector2.Zero });
        entityManager.AddComponent(entity, new UIElement { Bounds = new Rectangle(-1000, -1000, 1, 1) });
        return entity;
    }
}
