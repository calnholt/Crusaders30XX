using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
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
            EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
            EventManager.Subscribe<BlockAssignmentRemoved>(OnBlockAssignmentRemoved);
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
            UpdateConfirmButton();
        }

        private void OnBlockAssignmentAdded(BlockAssignmentAdded e)
        {
            if (!mustBeBlocked) return;
            Console.WriteLine($"[MustBeBlockedSystem] OnBlockAssignmentAdded");
            blockCount++;
            UpdateConfirmButton();
        }

        private void OnBlockAssignmentRemoved(BlockAssignmentRemoved e)
        {
            if (!mustBeBlocked) return;
            Console.WriteLine($"[MustBeBlockedSystem] OnBlockAssignmentRemoved");
            blockCount--;
            UpdateConfirmButton();
        }

        private void UpdateConfirmButton()
        {
            if (!mustBeBlocked) return;
            var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
            Console.WriteLine($"[MustBeBlockedSystem] UpdateConfirmButton: confirmBtn={confirmBtn}");
            if (confirmBtn == null) return;
            var ui = confirmBtn.GetComponent<UIElement>();
            Console.WriteLine($"[MustBeBlockedSystem] UpdateConfirmButton: ui={ui}");
            if (ui == null) return;
            var isFullfilled = mustBeBlocked && blockCount >= mustBeBlockedThreshold;
            ui.IsInteractable = isFullfilled;
            mustBeBlockedAttackDefinition.isTextConditionFulfilled = isFullfilled;
            Console.WriteLine($"[MustBeBlockedSystem] UpdateConfirmButton: isFullfilled={isFullfilled}");
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return new List<Entity> { EntityManager.GetEntity("UIButton_ConfirmEnemyAttack") };
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            
        }
    }

}