using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using System.Linq;
using Xunit;

namespace Crusaders30XX.Tests;

public class DeckManagementSystemTests
{
    [Fact]
    public void DrawCard_clears_stale_filtered_from_hand_marker()
    {
        EventManager.Clear();
        var entityManager = new EntityManager();
        var deck = new Deck();
        var card = CreateCard(entityManager);
        entityManager.AddComponent(card, new FilteredFromHand());
        entityManager.AddComponent(card, new HotKey { Button = FaceButton.B, Position = HotKeyPosition.Top });
        deck.DrawPile.Add(card);
        var system = new DeckManagementSystem(entityManager);

        bool drawn = system.DrawCard(deck);

        Assert.True(drawn);
        Assert.Contains(card, deck.Hand);
        Assert.DoesNotContain(card, deck.DrawPile);
        Assert.False(card.HasComponent<FilteredFromHand>());
        Assert.False(card.HasComponent<HotKey>());
        Assert.True(HandStateLoggingService.CountsForHandLayout(card));
    }

    [Fact]
    public void RemoveRandomCardEvent_removes_only_starter_cards_from_loadout()
    {
        EventManager.Clear();
        SaveCache.DeleteSaveFilesIfPresent();
        SaveCache.StartNewRun();

        var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
        loadout.cardIds.Clear();
        loadout.cardIds.Add("smite|Red");
        loadout.cardIds.Add("smite|White");
        loadout.cardIds.Add("fervor|Red");
        SaveCache.SaveLoadout(loadout);

        var save = SaveCache.GetAll();
        save.starterCardKeys.Clear();
        save.starterCardKeys.Add("smite|Red");
        save.starterCardKeys.Add("smite|White");

        var entityManager = new EntityManager();
        _ = new DeckManagementSystem(entityManager);
        RunDeckService.EnsureRunDeck(entityManager);

        var beforeCount = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds.Count;
        Assert.Equal(3, beforeCount);

        EventManager.Publish(new RemoveRandomCardEvent { Amount = 1 });

        var after = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
        Assert.Equal(2, after.cardIds.Count);
        Assert.Contains("fervor|Red", after.cardIds);
        Assert.Equal(1, after.cardIds.Count(k => SaveCache.IsStarterCardKey(k)));
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
