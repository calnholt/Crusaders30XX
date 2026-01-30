using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages the Pledge subphase (Arsenal zone mechanic).
    /// At end of turn, if there's no pledged card and eligible cards in hand,
    /// allows player to optionally pledge one card by clicking it.
    /// </summary>
    public class PledgeManagementSystem : Core.System
    {
        private bool _awaitingPledgeSelection = false;

        public PledgeManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnPhaseChanged);
            EventManager.Subscribe<SkipPledgeRequested>(OnSkipPledge);
            
            // Clear pledges at start of battle
            EventManager.Subscribe<StartBattleRequested>(_ => ClearAllPledges());
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            if (!_awaitingPledgeSelection) return;
            
            // Check for card clicks in hand during pledge phase
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;
            
            foreach (var card in deck.Hand)
            {
                var ui = card.GetComponent<UIElement>();
                if (ui == null || !ui.IsClicked) continue;
                
                ui.IsClicked = false; // Always consume the click first

                // Check if this is an eligible card
                if (!IsEligibleForPledge(card, true)) continue;
                
                // Card was clicked - pledge it
                Console.WriteLine($"[PledgeManagementSystem] Card clicked to pledge: {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");
                AddPledgeToCard(card);
                _awaitingPledgeSelection = false;
                ProceedToEnemyStart();
                return;
            }
        }

        private void OnPhaseChanged(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current != SubPhase.Pledge)
            {
                _awaitingPledgeSelection = false;
                return;
            }

            Console.WriteLine("[PledgeManagementSystem] Entered Pledge subphase");

            // Check if a card is already pledged
            var pledgedCards = EntityManager.GetEntitiesWithComponent<Pledge>().ToList();
            if (pledgedCards.Count > 0)
            {
                Console.WriteLine("[PledgeManagementSystem] Card already pledged, skipping to EnemyStart");
                ProceedToEnemyStart();
                return;
            }

            // Get eligible cards in hand
            var eligibleCards = GetEligiblePledgeCards();
            if (eligibleCards.Count == 0)
            {
                Console.WriteLine("[PledgeManagementSystem] No eligible cards to pledge, skipping to EnemyStart");
                ProceedToEnemyStart();
                return;
            }

            Console.WriteLine($"[PledgeManagementSystem] {eligibleCards.Count} eligible card(s), awaiting selection");
            _awaitingPledgeSelection = true;
            
            // Update interactability - only eligible cards should be clickable
            UpdateCardInteractability();
        }

        private void OnSkipPledge(SkipPledgeRequested evt)
        {
            Console.WriteLine("[PledgeManagementSystem] Pledge skipped by player");
            _awaitingPledgeSelection = false;
            RestoreCardInteractability();
            ProceedToEnemyStart();
        }

        private void AddPledgeToCard(Entity card)
        {
            if (card == null) return;
            if (card.GetComponent<Pledge>() != null) return; // Already pledged
            
            EntityManager.AddComponent(card, new Pledge { Owner = card });
            EventManager.Publish(new PledgeAddedEvent { Card = card });
            RestoreCardInteractability();
            Console.WriteLine($"[PledgeManagementSystem] Added Pledge component to {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");
        }

        private void ProceedToEnemyStart()
        {
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.EnemyStart",
                new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart }
            ));
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.PreBlock",
                new ChangeBattlePhaseEvent { Current = SubPhase.PreBlock }
            ));
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.Block",
                new ChangeBattlePhaseEvent { Current = SubPhase.Block }
            ));
        }

        public static bool IsEligibleForPledge(Entity card, bool showErrorMessage = false)
        {
            if (card == null) return false;
            
            var cardData = card.GetComponent<CardData>();
            if (cardData == null) return false;

            // Skip if already pledged
            if (card.GetComponent<Pledge>() != null) return false;

            // Skip sealed cards - they cannot be pledged
            if (card.GetComponent<Sealed>() != null)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Sealed cards cannot be pledged!" });
                return false;
            }

            // Skip weapons
            if (cardData.Card.IsWeapon)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge weapons!" });
                return false;
            };

            // Skip block cards - they can't be played from pledge
            if (cardData.Card.Type == CardType.Block)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge block cards!" });
                return false;
            };

            // Skip relics - they can't be played normally
            if (cardData.Card.Type == CardType.Relic)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge relics!" });
                return false;
            };

            // Skip token cards
            if (cardData.Card.IsToken)
            {
                if (showErrorMessage) EventManager.Publish(new CantPlayCardMessage { Message = "Can't pledge token cards!" });
                return false;
            };

            return true;
        }

        private System.Collections.Generic.List<Entity> GetEligiblePledgeCards()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return new System.Collections.Generic.List<Entity>();

            var eligible = new System.Collections.Generic.List<Entity>();
            foreach (var card in deck.Hand)
            {
                if (IsEligibleForPledge(card))
                {
                    eligible.Add(card);
                }
            }

            return eligible;
        }

        private void UpdateCardInteractability()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            foreach (var card in deck.Hand)
            {
                var ui = card.GetComponent<UIElement>();
                if (ui == null) continue;
                
                // Allow interaction with all cards so ineligible clicks can show error messages
                ui.IsInteractable = true;
            }
        }

        private void RestoreCardInteractability()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            foreach (var card in deck.Hand)
            {
                var ui = card.GetComponent<UIElement>();
                if (ui != null)
                {
                    ui.IsInteractable = true;
                }
            }
        }

        private void ClearAllPledges()
        {
            _awaitingPledgeSelection = false;
            var pledgedCards = EntityManager.GetEntitiesWithComponent<Pledge>().ToList();
            foreach (var card in pledgedCards)
            {
                EntityManager.RemoveComponent<Pledge>(card);
                Console.WriteLine($"[PledgeManagementSystem] Cleared pledge from {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");
            }
        }
    }
}
