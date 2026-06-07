using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages pledging during the Action phase (Arsenal zone mechanic).
    /// Players may pledge one eligible hand card per action phase via right-click, space, or gamepad X.
    /// </summary>
    public class PledgeManagementSystem : Core.System
    {
        public PledgeManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
            EventManager.Subscribe<PledgeCardRequested>(OnPledgeCardRequested);
            EventManager.Subscribe<StartBattleRequested>(_ => ClearAllPledges());
            EventManager.Subscribe<EnemyPhaseResetEvent>(_ => ClearAllPledges());
            EventManager.Subscribe<CardMoved>(OnCardMoved);
            EventManager.Subscribe<DiscardAllCardsEvent>(OnDiscardAllCards, priority: 10);
            EventManager.Subscribe<RedrawHandEvent>(OnRedrawHand, priority: 10);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

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

            PledgeAvailabilityService.SetPledgedThisActionPhase(EntityManager, false);

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
            if (StateSingleton.PreventClicking || StateSingleton.IsTutorialActive) return;
            if (BattleInputGate.IsBattleInputFrozen(EntityManager)) return;

            var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault()?.GetComponent<Deck>();
            if (deck?.Hand == null || !deck.Hand.Contains(card)) return;

            var cardEligibility = PledgeAvailabilityService.EvaluateCard(card);
            if (!cardEligibility.IsEligible)
            {
                if (!string.IsNullOrEmpty(cardEligibility.RejectionMessage))
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = cardEligibility.RejectionMessage });
                }
                return;
            }

            var availability = PledgeAvailabilityService.Evaluate(EntityManager);
            if (!availability.IsAvailable)
            {
                if (availability.Failure == PledgeAvailabilityFailure.AlreadyPledgedThisActionPhase)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = "You can only pledge one card per action phase!" });
                }
                else if (availability.Failure == PledgeAvailabilityFailure.CardAlreadyPledged)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = "You already have a pledged card in hand!" });
                }
                return;
            }

            AddPledgeToCard(card);
            PledgeAvailabilityService.SetPledgedThisActionPhase(EntityManager, true);
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

        private void RemovePledgeFromCard(Entity card)
        {
            if (card == null || card.GetComponent<Pledge>() == null) return;

            EntityManager.RemoveComponent<Pledge>(card);
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
            PledgeAvailabilityService.SetPledgedThisActionPhase(EntityManager, false);

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
