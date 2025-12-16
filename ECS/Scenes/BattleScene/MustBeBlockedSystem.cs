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
        private bool mustBeBlocked = false;
        private int mustBeBlockedThreshold = 0;
        private AttackDefinition mustBeBlockedAttackDefinition;

        public MustBeBlockedSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
            
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EventManager.Subscribe<AmbushTimerExpired>(OnAmbushTimerExpired);
            Console.WriteLine($"[MustBeBlockedSystem] MustBeBlockedSystem initialized");
		}

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            if (evt.Current != SubPhase.PreBlock) return;
            var ui = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack").GetComponent<UIElement>();
            ui.IsInteractable = true;
            Console.WriteLine($"[MustBeBlockedSystem] OnShowConfirmButtonEvent: evt={evt}");
            blockCount = 0;
            mustBeBlocked = false;
            mustBeBlockedThreshold = 0;
            var enemy = EntityManager.GetEntity("Enemy");
            var intent = enemy?.GetComponent<AttackIntent>();
            mustBeBlockedAttackDefinition = intent?.Planned?.FirstOrDefault()?.AttackDefinition;
            var attackId = intent?.Planned?.FirstOrDefault()?.AttackId;
            if (!AttackDefinitionCache.TryGet(attackId, out var def)) return;
            if (def.specialEffects.Length == 0) return;
            var mustBeBlockedDef = def.specialEffects.Where(sp => sp.type == "MustBeBlocked").FirstOrDefault();
            if (mustBeBlockedDef == null) return;
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;
            var deck = deckEntity.GetComponent<Deck>();
            if (deck == null) return;
            // TODO: determine if equipment can be used to block
            if (deck.Hand.Count < mustBeBlockedThreshold)
            {
                Console.WriteLine($"[MustBeBlockedSystem] OnChangeBattlePhaseEvent: deck.Hand.Count < mustBeBlockedThreshold");
                mustBeBlocked = false;
                mustBeBlockedThreshold = 0;
                return;
            }
            mustBeBlocked = true;
            mustBeBlockedThreshold = mustBeBlockedDef.amount;
            mustBeBlockedAttackDefinition.isTextConditionFulfilled = false;
            Console.WriteLine($"[MustBeBlockedSystem] OnShowConfirmButtonEvent: mustBeBlocked={mustBeBlocked}, mustBeBlockedThreshold={mustBeBlockedThreshold}");
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) 
        {
            if (!mustBeBlocked) return;
            var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
            if (confirmBtn == null) return;
            var ui = confirmBtn.GetComponent<UIElement>();
            if (ui == null) return;
            var progress = EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>().FirstOrDefault();
            if (progress == null) return;
            var progressComponent = progress.GetComponent<EnemyAttackProgress>();
            if (progressComponent == null) return;
            var blockCount = progressComponent.PlayedCards;
            var isFullfilled = mustBeBlocked && blockCount >= mustBeBlockedThreshold;
            ui.IsHidden = !isFullfilled;
            ui.IsInteractable = isFullfilled;
            mustBeBlockedAttackDefinition.isTextConditionFulfilled = isFullfilled;
        }
        private void OnAmbushTimerExpired(AmbushTimerExpired evt)
        {
            try
            {
                if (!mustBeBlocked || mustBeBlockedThreshold <= 0)
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
                if (deck == null || deck.Hand == null || deck.Hand.Count == 0)
                {
                    return;
                }

                // Build eligible list mirroring HandBlockInteractionSystem rules
                var eligible = new List<Entity>();
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