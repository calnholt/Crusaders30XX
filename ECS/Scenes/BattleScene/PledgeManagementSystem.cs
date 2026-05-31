using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Objects.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages pledging during the Action phase (Arsenal zone mechanic).
    /// Players may pledge one eligible hand card per action phase via right-click, space, or gamepad X.
    /// </summary>
    public class PledgeManagementSystem : Core.System
    {
        private bool _pledgedThisActionPhase;

        private MouseState _prevMouseState;
        private KeyboardState _prevKeyboardState;
        private GamePadState _prevGamePadState;

        public PledgeManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            _prevMouseState = Mouse.GetState();
            _prevKeyboardState = Keyboard.GetState();
            _prevGamePadState = GamePad.GetState(PlayerIndex.One);

            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
            EventManager.Subscribe<PledgeCardRequested>(OnPledgeCardRequested);
            EventManager.Subscribe<StartBattleRequested>(_ => ClearAllPledges());
            EventManager.Subscribe<CardMoved>(OnCardMoved);
            EventManager.Subscribe<DiscardAllCardsEvent>(OnDiscardAllCards, priority: 10);
            EventManager.Subscribe<RedrawHandEvent>(OnRedrawHand, priority: 10);
        }

        public static bool PledgedThisActionPhase { get; private set; }

        public static bool CanPledgeMore(EntityManager entityManager)
        {
            if (!StateSingleton.IsPledgeEnabled) return false;
            if (PledgedThisActionPhase) return false;

            var deck = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null) return false;
            if (deck.Hand.Any(c => c.GetComponent<Pledge>() != null)) return false;

            return deck.Hand.Any(c => IsEligibleForPledge(c));
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            if (phase == null || phase.Sub != SubPhase.Action) return;
            if (!StateSingleton.IsPledgeEnabled) return;
            if (StateSingleton.PreventClicking || StateSingleton.IsTutorialActive) return;

            var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null) return;

            var hoveredCard = GetHoveredHandCard(deck);
            if (hoveredCard == null)
            {
                AdvanceInputState();
                return;
            }

            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            var gp = GamePad.GetState(PlayerIndex.One);
            var caps = GamePad.GetCapabilities(PlayerIndex.One);

            bool rightClickEdge = mouseState.RightButton == ButtonState.Pressed
                && _prevMouseState.RightButton == ButtonState.Released;
            bool spaceEdge = keyboardState.IsKeyDown(Keys.Space) && !_prevKeyboardState.IsKeyDown(Keys.Space);
            bool gamepadXEdge = caps.IsConnected
                && gp.Buttons.X == ButtonState.Pressed
                && _prevGamePadState.Buttons.X == ButtonState.Released;

            if (rightClickEdge || spaceEdge || gamepadXEdge)
            {
                TryPledge(hoveredCard);
            }

            AdvanceInputState();
        }

        private void AdvanceInputState()
        {
            _prevMouseState = Mouse.GetState();
            _prevKeyboardState = Keyboard.GetState();
            _prevGamePadState = GamePad.GetState(PlayerIndex.One);
        }

        private Entity GetHoveredHandCard(Deck deck)
        {
            return deck.Hand
                .Where(c =>
                {
                    var ui = c.GetComponent<UIElement>();
                    return ui != null && ui.IsHovered;
                })
                .OrderByDescending(c => c.GetComponent<Transform>()?.ZOrder ?? 0)
                .FirstOrDefault();
        }

        private void OnPledgeCardRequested(PledgeCardRequested evt)
        {
            if (evt?.Card == null) return;
            TryPledge(evt.Card);
        }

        private void OnCardMoved(CardMoved evt)
        {
            if (evt?.Card == null) return;
            if (evt.From != CardZoneType.Hand) return;
            if (evt.To == CardZoneType.Hand || evt.To == CardZoneType.HandStaged) return;
            RemovePledgeFromCard(evt.Card);
        }

        private void OnDiscardAllCards(DiscardAllCardsEvent evt)
        {
            var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null) return;
            foreach (var card in deck.Hand.ToList())
                RemovePledgeFromCard(card);
        }

        private void OnRedrawHand(RedrawHandEvent evt)
        {
            var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null) return;
            foreach (var card in deck.Hand.ToList())
                RemovePledgeFromCard(card);
        }

        private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current != SubPhase.Action) return;

            _pledgedThisActionPhase = false;
            PledgedThisActionPhase = false;

            // Unlock pledges from prior turns. Previous is rarely set on ChangeBattlePhaseEvent,
            // so we unlock at Action start (Shadow still sees !CanPlay at PlayerEnd entry).
            foreach (var card in EntityManager.GetEntitiesWithComponent<Pledge>())
            {
                var pledge = card.GetComponent<Pledge>();
                if (pledge != null)
                    pledge.CanPlay = true;
            }
            LogPledgeHandSnapshot("ActionPhaseUnlock", null);
        }

        public void TryPledge(Entity card)
        {
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            if (phase == null || phase.Sub != SubPhase.Action) return;

            if (!StateSingleton.IsPledgeEnabled) return;

            var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null || !deck.Hand.Contains(card)) return;

            if (!IsEligibleForPledge(card, true)) return;

            if (_pledgedThisActionPhase)
            {
                EventManager.Publish(new CantPlayCardMessage { Message = "You can only pledge one card per action phase!" });
                return;
            }

            if (deck.Hand.Any(c => c.GetComponent<Pledge>() != null))
            {
                EventManager.Publish(new CantPlayCardMessage { Message = "You already have a pledged card in hand!" });
                return;
            }

            AddPledgeToCard(card);
            _pledgedThisActionPhase = true;
            PledgedThisActionPhase = true;
        }

        private void AddPledgeToCard(Entity card)
        {
            if (card == null) return;
            if (card.GetComponent<Pledge>() != null) return;

            EntityManager.AddComponent(card, new Pledge { Owner = card, CanPlay = false });
            EventManager.Publish(new PledgeAddedEvent { Card = card });
            var cardData = card.GetComponent<CardData>();
            cardData?.Card?.OnPledged?.Invoke(EntityManager, card);
            LoggingService.Append("PledgeManagementSystem.AddPledgeToCard", new System.Text.Json.Nodes.JsonObject
            {
                ["entityId"] = card.Id,
                ["cardId"] = card.GetComponent<CardData>()?.Card.CardId ?? "unknown",
                ["card"] = HandStateLoggingService.BuildCardSnapshot(card)
            });
            LogPledgeHandSnapshot("AddPledgeToCard", card);
        }

        public static bool IsEligibleForPledge(Entity card, bool showErrorMessage = false)
        {
            if (card == null) return false;

            var cardData = card.GetComponent<CardData>();
            if (cardData == null) return false;

            if (card.GetComponent<Pledge>() != null) return false;

            if (card.GetComponent<Sealed>() != null)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Sealed cards cannot be pledged!" });
                return false;
            }

            if (cardData.Card.IsWeapon)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge weapons!" });
                return false;
            }

            if (cardData.Card.Type == CardType.Block)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge block cards!" });
                return false;
            }

            if (cardData.Card.Type == CardType.Relic)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge relics!" });
                return false;
            }

            if (cardData.Card.IsToken)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge token cards!" });
                return false;
            }

            return true;
        }

        private void RemovePledgeFromCard(Entity card)
        {
            if (card == null || card.GetComponent<Pledge>() == null) return;

            EntityManager.RemoveComponent<Pledge>(card);
            _pledgedThisActionPhase = false;
            PledgedThisActionPhase = false;
            LoggingService.Append("PledgeManagementSystem.RemovePledgeFromCard", new System.Text.Json.Nodes.JsonObject
            {
                ["entityId"] = card.Id,
                ["cardId"] = card.GetComponent<CardData>()?.Card.CardId ?? "unknown",
                ["card"] = HandStateLoggingService.BuildCardSnapshot(card)
            });
            LogPledgeHandSnapshot("RemovePledgeFromCard", card);
        }

        private void ClearAllPledges()
        {
            _pledgedThisActionPhase = false;
            PledgedThisActionPhase = false;

            foreach (var card in EntityManager.GetEntitiesWithComponent<Pledge>().ToList())
                RemovePledgeFromCard(card);
        }

        private void LogPledgeHandSnapshot(string reason, Entity card)
        {
            var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck == null) return;

            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>()?.Sub;
            var snapshot = HandStateLoggingService.BuildHandSnapshot(deck, reason, phase);
            snapshot["pledgedEntityId"] = card?.Id ?? -1;
            snapshot["pledgedCardId"] = card?.GetComponent<CardData>()?.Card?.CardId ?? "none";
            LoggingService.Append("PledgeManagementSystem.HandSnapshot", snapshot);
        }
    }
}
