using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    public class TutorialManager : Core.System
    {
        private readonly Queue<TutorialDefinition> _tutorialQueue = new Queue<TutorialDefinition>();
        private TutorialDefinition _activeTutorial = null;
        private bool _tutorialActive = false;
        private int _lastProcessedTurn = -1;
        private SubPhase _lastProcessedPhase = SubPhase.StartBattle;

        public TutorialDefinition ActiveTutorial => _activeTutorial;
        public bool IsTutorialActive => _tutorialActive;
        public int QueueCount => _tutorialQueue.Count;

        public TutorialManager(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<AdvanceTutorialEvent>(OnAdvanceTutorial);
            EventManager.Subscribe<LoadSceneEvent>(_ => Reset());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            base.Update(gameTime);

            // If we have queued tutorials but none active, start the next one
            if (!_tutorialActive && _tutorialQueue.Count > 0)
            {
                StartNextTutorial();
            }
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            var phaseState = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            if (phaseState == null) return;

            int turnNumber = phaseState.TurnNumber;
            SubPhase currentPhase = evt.Current;

            // Avoid reprocessing the same phase/turn combination
            if (turnNumber == _lastProcessedTurn && currentPhase == _lastProcessedPhase)
                return;

            _lastProcessedTurn = turnNumber;
            _lastProcessedPhase = currentPhase;

            Console.WriteLine($"[TutorialManager] OnChangeBattlePhase: {currentPhase} {turnNumber}");

            // Queue tutorials based on phase and turn number
            if (currentPhase == SubPhase.Block && turnNumber == 1)
            {
                QueueTutorialsForFirstBlockPhase();
            }
            else if (currentPhase == SubPhase.Action && turnNumber == 1)
            {
                QueueTutorialsForFirstActionPhase();
            }
            else if (currentPhase == SubPhase.Block && turnNumber == 2)
            {
                QueueTutorialsForSecondBlockPhase();
            }

            if (currentPhase == SubPhase.Action)
            {
                TryQueueTutorial("cost");
            }
            if (currentPhase == SubPhase.Pledge)
            {
                TryQueueTutorial("pledge");
            }
            if (currentPhase == SubPhase.Block)
            {
                TryQueueTutorial("medal");
                TryQueueTutorial("equipment");
                TryQueueTutorial("tribulation");
            }
        }

        private void QueueTutorialsForFirstBlockPhase()
        {
            TryQueueTutorial("how_to_win");
            TryQueueTutorial("block_phase_overview");
            TryQueueTutorial("card_block_value");
            TryQueueTutorial("dungeon_overview");
        }

        private void QueueTutorialsForFirstActionPhase()
        {
            TryQueueTutorial("action_phase_overview");
            TryQueueTutorial("action_points");
            TryQueueTutorial("card_damage");
            TryQueueTutorial("weapon");
        }

        private void QueueTutorialsForSecondBlockPhase()
        {
            TryQueueTutorial("card_colors");
            TryQueueTutorial("courage");
            TryQueueTutorial("temperance");
            TryQueueTutorial("threat");
        }

        private void TryQueueTutorial(string key)
        {
            // Skip if already seen
            if (SaveCache.HasSeenTutorial(key))
                return;

            // Try to get tutorial definition
            if (!TutorialDefinitionCache.TryGet(key, out var tutorial) || tutorial == null)
            {
                Console.WriteLine($"[TutorialManager] Tutorial not found: {key}");
                return;
            }

            // Check conditions
            if (!CheckCondition(tutorial.condition))
                return;

            // Queue the tutorial
            _tutorialQueue.Enqueue(tutorial);
            Console.WriteLine($"[TutorialManager] Queued tutorial: {key}");
        }

        private bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            switch (condition)
            {
                case "has_cost_card":
                    return HasCostCardInHand();
                case "has_equipment":
                    return HasEquipment();
                case "has_medal":
                    return HasMedal();
                case "has_tribulation":
                    return HasTribulation();
                case "can_pledge":
                    return CanPledge();
                case "is_dungeon":
                    return IsDungeon();
                default:
                    return true;
            }
        }

        private bool IsDungeon()
        {
            var qeEntity = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            return qeEntity != null && qeEntity.GetComponent<DungeonLoadout>() != null;
        }

        private bool CanPledge()
        {
            // Check if a card is already pledged
            var pledgedCards = EntityManager.GetEntitiesWithComponent<Pledge>().ToList();
            if (pledgedCards.Count > 0)
                return false;

            // Check if there are eligible cards in hand
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null || deck.Hand == null)
                return false;

            return deck.Hand.Any(card => PledgeManagementSystem.IsEligibleForPledge(card));
        }

        private bool HasCostCardInHand()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null || deck.Hand == null)
                return false;

            foreach (var card in deck.Hand)
            {
                var cardData = card.GetComponent<CardData>();
                if (cardData == null) continue;

                var cardObj = CardFactory.Create(cardData.Card.CardId);
                if (cardObj != null && cardObj.Cost != null && cardObj.Cost.Count > 0)
                    return true;
            }

            return false;
        }

        private bool HasEquipment()
        {
            var equipmentEntity = EntityManager.GetEntitiesWithComponent<EquippedEquipment>().FirstOrDefault();
            return equipmentEntity != null;
        }

        private bool HasMedal()
        {
            var medalEntity = EntityManager.GetEntitiesWithComponent<EquippedMedal>().FirstOrDefault();
            return medalEntity != null;
        }

        private bool HasTribulation()
        {
            var tribulationEntity = EntityManager.GetEntitiesWithComponent<Tribulation>().FirstOrDefault();
            return tribulationEntity != null;
        }

        private void StartNextTutorial()
        {
            if (_tutorialQueue.Count == 0)
            {
                _tutorialActive = false;
                _activeTutorial = null;
                EventManager.Publish(new AllTutorialsCompletedEvent());
                return;
            }

            _activeTutorial = _tutorialQueue.Dequeue();
            _tutorialActive = true;

            // Mark as seen immediately so it won't be queued again
            SaveCache.MarkTutorialSeen(_activeTutorial.key);

            Console.WriteLine($"[TutorialManager] Starting tutorial: {_activeTutorial.key}");
            EventManager.Publish(new TutorialStartedEvent { Tutorial = _activeTutorial });
        }

        private void OnAdvanceTutorial(AdvanceTutorialEvent evt)
        {
            if (!_tutorialActive || _activeTutorial == null)
                return;

            Console.WriteLine($"[TutorialManager] Completed tutorial: {_activeTutorial.key}");
            EventManager.Publish(new TutorialCompletedEvent { Tutorial = _activeTutorial });

            _activeTutorial = null;
            _tutorialActive = false;

            // Start next tutorial if available
            if (_tutorialQueue.Count > 0)
            {
                StartNextTutorial();
            }
            else
            {
                EventManager.Publish(new AllTutorialsCompletedEvent());
            }
        }

        private void Reset()
        {
            _tutorialQueue.Clear();
            _activeTutorial = null;
            _tutorialActive = false;
            _lastProcessedTurn = -1;
            _lastProcessedPhase = SubPhase.StartBattle;
        }

        /// <summary>
        /// Resolves target entity bounds for the active tutorial.
        /// Returns a list of rectangles for multi-target tutorials.
        /// </summary>
        public List<Rectangle> ResolveTargetBounds()
        {
            var bounds = new List<Rectangle>();
            if (_activeTutorial == null)
                return bounds;

            var targetIds = new List<string>();
            if (_activeTutorial.targetIds != null && _activeTutorial.targetIds.Count > 0)
            {
                targetIds.AddRange(_activeTutorial.targetIds);
            }
            else if (!string.IsNullOrEmpty(_activeTutorial.targetId))
            {
                targetIds.Add(_activeTutorial.targetId);
            }

            foreach (var targetId in targetIds)
            {
                var rect = ResolveSingleTargetBounds(_activeTutorial.targetType, targetId);
                if (rect.Width > 0 && rect.Height > 0)
                {
                    bounds.Add(rect);
                }
            }

            return bounds;
        }

        private Rectangle ResolveSingleTargetBounds(string targetType, string targetId)
        {
            switch (targetType)
            {
                case "entity_name":
                    return GetEntityBounds(targetId);
                case "component_hand":
                    return GetFirstHandCardBounds();
                case "component_any":
                    return GetFirstComponentBounds(targetId);
                case "ui_region":
                    return GetUIRegionBounds(targetId);
                case "card_with_cost":
                    return GetCardWithCostBounds();
                case "equipment":
                    return GetEquipmentBounds();
                case "medal":
                    return GetMedalBounds();
                case "tribulation":
                    return GetTribulationBounds();
                default:
                    return Rectangle.Empty;
            }
        }

        private Rectangle GetEntityBounds(string entityName)
        {
            var entity = EntityManager.GetEntity(entityName);
            if (entity == null)
                return Rectangle.Empty;

            var ui = entity.GetComponent<UIElement>();
            if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
                return ui.Bounds;

            var transform = entity.GetComponent<Transform>();
            if (transform != null)
            {
                // Fallback to transform position with default size
                return new Rectangle((int)transform.Position.X - 50, (int)transform.Position.Y - 50, 100, 100);
            }

            return Rectangle.Empty;
        }

        private Rectangle GetFirstHandCardBounds()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null || deck.Hand == null || deck.Hand.Count == 0)
                return Rectangle.Empty;

            var firstCard = deck.Hand.FirstOrDefault();
            if (firstCard == null)
                return Rectangle.Empty;

            var ui = firstCard.GetComponent<UIElement>();
            if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
                return new Rectangle(ui.Bounds.X, ui.Bounds.Y, (ui.Bounds.Width - 20) * deck.Hand.Count, ui.Bounds.Height);

            return Rectangle.Empty;
        }

        private Rectangle GetFirstComponentBounds(string componentTypeName)
        {
            // Map component type names to actual component types
            Entity entity = null;
            switch (componentTypeName)
            {
                case "EnemyDisplay":
                    entity = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
                    break;
                case "CardData":
                    var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var deck = deckEntity?.GetComponent<Deck>();
                    entity = deck?.Hand?.FirstOrDefault();
                    break;
                default:
                    return Rectangle.Empty;
            }

            if (entity == null)
                return Rectangle.Empty;

            var ui = entity.GetComponent<UIElement>();
            if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
                return ui.Bounds;

            var transform = entity.GetComponent<Transform>();
            if (transform != null)
            {
                return new Rectangle((int)transform.Position.X - 50, (int)transform.Position.Y - 50, 100, 100);
            }

            return Rectangle.Empty;
        }

        private Rectangle GetEquipmentBounds()
        {
            var equipmentEntity = EntityManager.GetEntitiesWithComponent<EquippedEquipment>().FirstOrDefault();
            if (equipmentEntity == null)
            {
                Console.WriteLine($"[TutorialManager] Equipment entity not found");
                return Rectangle.Empty;
            }

            var ui = equipmentEntity.GetComponent<UIElement>();
            if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
            {
                return new Rectangle(ui.Bounds.X, ui.Bounds.Y, ui.Bounds.Width, ui.Bounds.Height);
            }

            Console.WriteLine($"[TutorialManager] Equipment bounds not found");
            return Rectangle.Empty;
        }

        private Rectangle GetMedalBounds()
        {
            var medalEntity = EntityManager.GetEntitiesWithComponent<EquippedMedal>().FirstOrDefault();
            if (medalEntity == null)
            {
                Console.WriteLine($"[TutorialManager] Medal entity not found");
                return Rectangle.Empty;
            }
            var ui = medalEntity.GetComponent<UIElement>();
            if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
            {
                return new Rectangle(ui.Bounds.X, ui.Bounds.Y, ui.Bounds.Width, ui.Bounds.Height);
            }

            Console.WriteLine($"[TutorialManager] Medal bounds not found");
            return Rectangle.Empty;
        }
        private Rectangle GetTribulationBounds()
        {
            var tribulationEntity = EntityManager.GetEntitiesWithComponent<Tribulation>().FirstOrDefault();
            if (tribulationEntity == null)
            {
                return Rectangle.Empty;
            }
            var ui = tribulationEntity.GetComponent<UIElement>();
            if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
            {
                return new Rectangle(ui.Bounds.X, ui.Bounds.Y, ui.Bounds.Width, ui.Bounds.Height);
            }
            Console.WriteLine($"[TutorialManager] Tribulation bounds not found");
            return Rectangle.Empty;
        }
        private Rectangle GetUIRegionBounds(string regionId)
        {
            // Predefined UI regions
            switch (regionId)
            {
                case "player_hand":
                    // Return bounds encompassing the hand area at bottom of screen
                    return new Rectangle(Game1.VirtualWidth / 4, Game1.VirtualHeight - 200, Game1.VirtualWidth / 2, 180);

                case "enemy_attack_display":
                    // Return bounds for the enemy attack display area
                    return new Rectangle(Game1.VirtualWidth / 4, Game1.VirtualHeight / 4, Game1.VirtualWidth / 2, 150);

                default:
                    return Rectangle.Empty;
            }
        }

        private Rectangle GetCardWithCostBounds()
        {
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null || deck.Hand == null || deck.Hand.Count == 0)
                return Rectangle.Empty;

            foreach (var card in deck.Hand)
            {
                var cardData = card.GetComponent<CardData>();
                if (cardData == null) continue;

                if (cardData.Card.Cost != null && cardData.Card.Cost.Count > 0)
                {
                    var ui = card.GetComponent<UIElement>();
                    if (ui != null && ui.Bounds.Width > 0 && ui.Bounds.Height > 0)
                    {
                        return new Rectangle(ui.Bounds.X, ui.Bounds.Y, ui.Bounds.Width, ui.Bounds.Height);
                    }

                }
            }
            return Rectangle.Empty;
        }
    }
}

