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

        public MustBeBlockedSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
            
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
            EventManager.Subscribe<BlockAssignmentRemoved>(OnBlockAssignmentRemoved);
		}

        private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        {
            
        }

        private void OnBlockAssignmentAdded(BlockAssignmentAdded e)
        {

        }

        private void OnBlockAssignmentRemoved(BlockAssignmentRemoved e)
        {

        }

        private void UpdateConfirmButton()
        {
            var enemy = EntityManager.GetEntity("Enemy");
            var intent = enemy?.GetComponent<AttackIntent>();
            var attackId = intent?.Planned?.FirstOrDefault()?.AttackId;
            if (!AttackDefinitionCache.TryGet(attackId, out var def)) return;
            if (def.specialEffects.Length == 0) return;
            var mustBeBlocked = def.specialEffects.Where(sp => sp.type == "MustBeBlocked").FirstOrDefault();
            if (mustBeBlocked == null) return;
            TimerScheduler.Schedule(0.2f, () =>
            {
                var assignedBlocks = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>().ToList().Count;

            });
            var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
        }

    }

}