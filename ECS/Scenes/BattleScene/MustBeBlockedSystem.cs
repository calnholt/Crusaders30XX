using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	public class MustBeBlockedSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
        private int blockCount = 0;
        
        private enum BlockRequirementType { None, AtLeast, Exactly }
        private BlockRequirementType requirementType = BlockRequirementType.None;
        private int mustBeBlockedThreshold = 0;
        private string mustBeBlockedContextId;
        private AttackDefinition mustBeBlockedAttackDefinition;
        
        // Cache previous state to prevent flickering UI updates
        private bool? previousFulfilledState = null;
        private int previousPlayedCardsCount = -1;

        public MustBeBlockedSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
            
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EventManager.Subscribe<AmbushTimerExpired>(OnAmbushTimerExpired);
            Console.WriteLine($"[MustBeBlockedSystem] MustBeBlockedSystem initialized");
		}

        private bool IsAtLeastRequirementMet(int blockCount)
        {
            return blockCount >= mustBeBlockedThreshold;
        }

        private bool IsExactlyRequirementMet(int blockCount)
        {
            return blockCount == mustBeBlockedThreshold;
        }

        private bool IsRequirementFulfilled(int blockCount)
        {
            return requirementType switch
            {
                BlockRequirementType.AtLeast => IsAtLeastRequirementMet(blockCount),
                BlockRequirementType.Exactly => IsExactlyRequirementMet(blockCount),
                BlockRequirementType.None => false,
                _ => false
            };
        }

        private void InitializeBlockRequirement()
        {
            // Reset state
            requirementType = BlockRequirementType.None;
            mustBeBlockedThreshold = 0;
            mustBeBlockedContextId = null;
            mustBeBlockedAttackDefinition = null;
            previousFulfilledState = null;
            previousPlayedCardsCount = -1;

            var enemy = EntityManager.GetEntity("Enemy");
            var intent = enemy?.GetComponent<AttackIntent>();
            var plannedAttack = intent?.Planned?.FirstOrDefault();
            mustBeBlockedContextId = plannedAttack?.ContextId;
            mustBeBlockedAttackDefinition = plannedAttack?.AttackDefinition;
            var attackId = plannedAttack?.AttackId;
            
            if (!AttackDefinitionCache.TryGet(attackId, out var def)) return;
            if (def.specialEffects.Length == 0) return;

            // Check for MustBeBlockedExactly first, then MustBeBlocked
            var exactlyDef = def.specialEffects.FirstOrDefault(sp => sp.type == "MustBeBlockedExactly");
            var atLeastDef = def.specialEffects.FirstOrDefault(sp => sp.type == "MustBeBlocked");

            var selectedDef = exactlyDef ?? atLeastDef;
            if (selectedDef == null) return;

            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;

            int threshold = selectedDef.amount;
            
            // TODO: determine if equipment can be used to block
            if (deck.Hand.Count < threshold)
            {
                Console.WriteLine($"[MustBeBlockedSystem] InitializeBlockRequirement: deck.Hand.Count < threshold");
                return;
            }

            // Set the requirement type and threshold
            if (exactlyDef != null)
            {
                requirementType = BlockRequirementType.Exactly;
                Console.WriteLine($"[MustBeBlockedSystem] InitializeBlockRequirement: MustBeBlockedExactly={threshold}");
            }
            else
            {
                requirementType = BlockRequirementType.AtLeast;
                Console.WriteLine($"[MustBeBlockedSystem] InitializeBlockRequirement: MustBeBlocked (at least)={threshold}");
            }
            
            mustBeBlockedThreshold = threshold;
            mustBeBlockedAttackDefinition.isTextConditionFulfilled = false;
        }

        private List<Entity> GetEligibleBlockCards(Deck deck)
        {
            var eligible = new List<Entity>();
            
            if (deck == null || deck.Hand == null || deck.Hand.Count == 0)
            {
                return eligible;
            }

            foreach (var card in deck.Hand)
            {
                var data = card.GetComponent<CardData>();
                if (data == null)
                {
                    continue;
                }

                string id = data.Card.CardId ?? string.Empty;

                // Skip invalid or special types
                if (string.IsNullOrEmpty(id) || data.Card == null)
                {
                    continue;
                }
                if (data.Card.IsWeapon || data.Card.IsToken)
                {
                    continue;
                }

                // Skip intimidated cards
                if (card.GetComponent<Intimidated>() != null)
                {
                    continue;
                }

                // If it's a block card with extra cost, ensure it can be paid
                if (data.Card.Type == CardType.Block && !data.Card.CanPlay(EntityManager, card))
                {
                    continue;
                }

                eligible.Add(card);
            }

            return eligible;
        }

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current != SubPhase.PreBlock) return;
            var ui = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack").GetComponent<UIElement>();
            ui.IsInteractable = true;
            ui.IsHidden = false;
            Console.WriteLine($"[MustBeBlockedSystem] OnShowConfirmButtonEvent: evt={evt}");
            blockCount = 0;
            
            // Initialize the block requirement (checks for both MustBeBlocked and MustBeBlockedExactly)
            InitializeBlockRequirement();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) 
        {
            if (requirementType == BlockRequirementType.None) return;
            if (string.IsNullOrEmpty(mustBeBlockedContextId)) return;
            
            var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
            if (confirmBtn == null) return;
            var ui = confirmBtn.GetComponent<UIElement>();
            if (ui == null) return;
            var progress = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>()
                .FirstOrDefault(e => e.GetComponent<EnemyAttackProgress>()?.ContextId == mustBeBlockedContextId);
            if (progress == null) return;
            var progressComponent = progress.GetComponent<EnemyAttackProgress>();
            if (progressComponent == null) return;
            
            var blockCount = progressComponent.PlayedCards;
            
            // Only process if the card count has actually changed (prevents redundant evaluations)
            if (blockCount == previousPlayedCardsCount)
            {
                return;
            }
            
            previousPlayedCardsCount = blockCount;
            var isFullfilled = IsRequirementFulfilled(blockCount);
            
            // Only update UI if the fulfillment state has changed (prevents flickering)
            if (previousFulfilledState.HasValue && previousFulfilledState.Value == isFullfilled)
            {
                return;
            }
            
            previousFulfilledState = isFullfilled;
            ui.IsHidden = !isFullfilled;
            ui.IsInteractable = isFullfilled;
            
            if (mustBeBlockedAttackDefinition != null)
            {
                mustBeBlockedAttackDefinition.isTextConditionFulfilled = isFullfilled;
            }
        }
        private void OnAmbushTimerExpired(AmbushTimerExpired evt)
        {
            try
            {
                if (requirementType == BlockRequirementType.None || mustBeBlockedThreshold <= 0)
                {
                    return;
                }

                // Need current planned ambush attack and matching context
                var enemy = EntityManager.GetEntity("Enemy");
                var intent = enemy?.GetComponent<AttackIntent>();
                var pa = intent?.Planned?.FirstOrDefault();
                if (pa == null || !pa.IsAmbush || string.IsNullOrEmpty(pa.ContextId))
                {
                    return;
                }
                if (!string.Equals(pa.ContextId, evt.ContextId, StringComparison.Ordinal))
                {
                    return;
                }

                // Already satisfied?
                int needed = mustBeBlockedThreshold - blockCount;
                if (needed <= 0)
                {
                    return;
                }

                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null)
                {
                    return;
                }

                // Get eligible cards using extracted helper
                var eligible = GetEligibleBlockCards(deck);
                if (eligible.Count == 0)
                {
                    Console.WriteLine("[MustBeBlockedSystem] OnAmbushTimerExpired: no eligible cards to auto-assign");
                    return;
                }

                int toAssign = Math.Min(needed, eligible.Count);
                // Randomize order and take up to needed
                var rng = Random.Shared;
                var randomized = eligible
                    .OrderBy(_ => rng.Next())
                    .Take(toAssign)
                    .ToList();

                Console.WriteLine($"[MustBeBlockedSystem] OnAmbushTimerExpired: auto-assigning {randomized.Count} cards (needed={needed}, currentCount={blockCount}, threshold={mustBeBlockedThreshold})");

                foreach (var card in randomized)
                {
                    var t = card.GetComponent<Transform>();
                    if (deckEntity != null && t != null)
                    {
                        var startPos = t.Position;
                        EventManager.Publish(new CardMoveRequested
                        {
                            Card = card,
                            Deck = deckEntity,
                            Destination = CardZoneType.AssignedBlock,
                            ContextId = pa.ContextId,
                            Reason = "AssignBlockAutoAmbush"
                        });
                        var abc = card.GetComponent<AssignedBlockCard>();
                        if (abc != null)
                        {
                            abc.ReturnTargetPos = startPos;
                        }
                    }

                    var data = card.GetComponent<CardData>();
                    if (data == null)
                    {
                        continue;
                    }

                    int blockVal = BlockValueService.GetTotalBlockValue(card);
                    string color = data.Color.ToString();
                    EventManager.Publish(new BlockAssignmentAdded
                    {
                        ContextId = pa.ContextId,
                        Card = card,
                        Color = color,
                        DeltaBlock = blockVal
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MustBeBlockedSystem] OnAmbushTimerExpired exception: {ex}");
            }
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return new List<Entity> { EntityManager.GetEntity("UIButton_ConfirmEnemyAttack") };
        }

    }

}