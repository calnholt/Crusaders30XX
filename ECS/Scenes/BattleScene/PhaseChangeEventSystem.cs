using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    public class PhaseChangeEventSystem : Core.System
    {
        private bool _waitingForAnimation = false;
        private string _lastSeenContextId = null;
        private int _lastTurn = -1;
        private bool _firstBlockProcessed = false;

        public PhaseChangeEventSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
            {
                if (evt.Current == SubPhase.Block)
                {
                    // Check turn number to determine if this is the first block of the turn
                    var phaseState = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
                    if (phaseState != null)
                    {
                        if (phaseState.TurnNumber != _lastTurn)
                        {
                            _lastTurn = phaseState.TurnNumber;
                            _firstBlockProcessed = false;
                        }
                    }

                    if (!_firstBlockProcessed)
                    {
                        _firstBlockProcessed = true;
                        _waitingForAnimation = true;
                        _lastSeenContextId = null;
                    }
                    else
                    {
                        // Subsequent block in same turn; do not wait for animation
                        _waitingForAnimation = false;
                        // Do not trigger immediately here; let UpdateEntity handle it to avoid race condition with EnemyAttackDisplaySystem hiding the banner
                    }
                }
            });

            EventManager.Subscribe<BattlePhaseAnimationCompleteEvent>(evt =>
            {
                _waitingForAnimation = false;
                CheckAndTriggerNextAttack();
            });
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<AttackIntent>();
        }

        protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
        {
            CheckAndTriggerNextAttack();
        }

        private void CheckAndTriggerNextAttack()
        {
            var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
            if (enemy == null) return;

            var intent = enemy.GetComponent<AttackIntent>();
            if (intent == null || intent.Planned.Count == 0) return;

            var currentContextId = intent.Planned[0].ContextId;

            if (currentContextId != _lastSeenContextId)
            {
                if (!_waitingForAnimation)
                {
                    EventManager.Publish(new TriggerEnemyAttackDisplayEvent { ContextId = currentContextId });
                    _lastSeenContextId = currentContextId;
                }
                // Else: Do nothing (wait for animation complete)
            }
        }
    }
}

