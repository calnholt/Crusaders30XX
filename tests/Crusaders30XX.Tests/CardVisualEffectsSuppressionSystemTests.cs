using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Input;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public sealed class CardVisualEffectsSuppressionSystemTests
{
    [Fact]
    public void Adds_suppression_to_hovered_hand_card_when_gamepad_left_trigger_is_held()
    {
        var entityManager = BuildBattleHand(2, out var cards, out _);
        cards[0].GetComponent<UIElement>().IsHovered = true;
        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 0.5f));
        var system = new CardVisualEffectsSuppressionSystem(entityManager);

        system.Update(FrameTime());

        Assert.NotNull(cards[0].GetComponent<SuppressCardVisualEffects>());
        Assert.Null(cards[1].GetComponent<SuppressCardVisualEffects>());
    }

    [Theory]
    [InlineData(PlayerInputDevice.KeyboardMouse, 1f)]
    [InlineData(PlayerInputDevice.Gamepad, 0.1f)]
    public void Does_not_add_suppression_without_gamepad_left_trigger(PlayerInputDevice device, float leftTrigger)
    {
        var entityManager = BuildBattleHand(1, out var cards, out _);
        cards[0].GetComponent<UIElement>().IsHovered = true;
        SetInputFrame(entityManager, Frame(device, leftTrigger));
        var system = new CardVisualEffectsSuppressionSystem(entityManager);

        system.Update(FrameTime());

        Assert.Null(cards[0].GetComponent<SuppressCardVisualEffects>());
    }

    [Fact]
    public void Does_not_recreate_component_while_same_card_remains_suppressed()
    {
        var entityManager = BuildBattleHand(1, out var cards, out _);
        cards[0].GetComponent<UIElement>().IsHovered = true;
        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 1f));
        var system = new CardVisualEffectsSuppressionSystem(entityManager);

        system.Update(FrameTime());
        var firstComponent = cards[0].GetComponent<SuppressCardVisualEffects>();
        system.Update(FrameTime());

        Assert.Same(firstComponent, cards[0].GetComponent<SuppressCardVisualEffects>());
    }

    [Fact]
    public void Moves_suppression_when_hover_changes()
    {
        var entityManager = BuildBattleHand(2, out var cards, out _);
        cards[0].GetComponent<UIElement>().IsHovered = true;
        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 1f));
        var system = new CardVisualEffectsSuppressionSystem(entityManager);

        system.Update(FrameTime());
        cards[0].GetComponent<UIElement>().IsHovered = false;
        cards[1].GetComponent<UIElement>().IsHovered = true;
        system.Update(FrameTime());

        Assert.Null(cards[0].GetComponent<SuppressCardVisualEffects>());
        Assert.NotNull(cards[1].GetComponent<SuppressCardVisualEffects>());
    }

    [Fact]
    public void Clears_stale_suppression_from_non_hovered_cards_while_trigger_is_held()
    {
        var entityManager = BuildBattleHand(2, out var cards, out _);
        entityManager.AddComponent(cards[0], new SuppressCardVisualEffects());
        cards[1].GetComponent<UIElement>().IsHovered = true;
        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 1f));
        var system = new CardVisualEffectsSuppressionSystem(entityManager);

        system.Update(FrameTime());

        Assert.Null(cards[0].GetComponent<SuppressCardVisualEffects>());
        Assert.NotNull(cards[1].GetComponent<SuppressCardVisualEffects>());
    }

    [Fact]
    public void Removes_suppression_when_trigger_releases_hover_clears_or_card_leaves_hand()
    {
        var entityManager = BuildBattleHand(1, out var cards, out var deck);
        cards[0].GetComponent<UIElement>().IsHovered = true;
        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 1f));
        var system = new CardVisualEffectsSuppressionSystem(entityManager);

        system.Update(FrameTime());
        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 0f));
        system.Update(FrameTime());
        Assert.Null(cards[0].GetComponent<SuppressCardVisualEffects>());

        SetInputFrame(entityManager, Frame(PlayerInputDevice.Gamepad, leftTrigger: 1f));
        system.Update(FrameTime());
        cards[0].GetComponent<UIElement>().IsHovered = false;
        system.Update(FrameTime());
        Assert.Null(cards[0].GetComponent<SuppressCardVisualEffects>());

        cards[0].GetComponent<UIElement>().IsHovered = true;
        system.Update(FrameTime());
        deck.Hand.Remove(cards[0]);
        system.Update(FrameTime());
        Assert.Null(cards[0].GetComponent<SuppressCardVisualEffects>());
    }

    private static EntityManager BuildBattleHand(int count, out List<Entity> cards, out Deck deck)
    {
        var entityManager = new EntityManager();
        var scene = entityManager.CreateEntity("Scene");
        entityManager.AddComponent(scene, new SceneState { Current = SceneId.Battle });
        var input = entityManager.CreateEntity("PlayerInput");
        entityManager.AddComponent(input, new PlayerInputState());
        var deckEntity = entityManager.CreateEntity("Deck");
        deck = new Deck();
        entityManager.AddComponent(deckEntity, deck);
        cards = new List<Entity>();

        for (int i = 0; i < count; i++)
        {
            var card = entityManager.CreateEntity($"Card_{i}");
            entityManager.AddComponent(card, new CardData());
            entityManager.AddComponent(card, new UIElement { IsInteractable = true });
            deck.Hand.Add(card);
            cards.Add(card);
        }

        return entityManager;
    }

    private static void SetInputFrame(EntityManager entityManager, PlayerInputFrame frame)
    {
        entityManager
            .GetEntitiesWithComponent<PlayerInputState>()
            .Single()
            .GetComponent<PlayerInputState>()
            .Frame = frame;
    }

    private static PlayerInputFrame Frame(PlayerInputDevice device, float leftTrigger)
    {
        return new PlayerInputFrame(
            1,
            true,
            device == PlayerInputDevice.Gamepad,
            device,
            device,
            GamepadGlyphStyle.Xbox,
            Vector2.Zero,
            Vector2.Zero,
            0f,
            Vector2.Zero,
            Vector2.Zero,
            leftTrigger,
            0f,
            PlayerButtonMask.None,
            PlayerButtonMask.None,
            PlayerButtonMask.None);
    }

    private static GameTime FrameTime()
    {
        return new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1d / 60d));
    }
}
