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
            // Simplified handler: only handle subsequent blocks in same turn
            EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
            {
                if (evt.Current == SubPhase.Block)
                {
                    var phaseState = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
                    if (phaseState != null && phaseState.TurnNumber != _lastTurn)
                    {
                        // New turn detected - reset state (CheckAndTriggerNextAttack will handle first block)
                        _lastTurn = phaseState.TurnNumber;
                        _firstBlockProcessed = false;
                    }

                // For subsequent blocks in same turn, clear waiting flag and reset context so attack display triggers
                if (_firstBlockProcessed)
                {
                    _waitingForAnimation = false;
                    _lastSeenContextId = null; // Reset so new attack context is detected
                }
                }
            });

            EventManager.Subscribe<BattlePhaseAnimationCompleteEvent>(evt =>
            {
                // Mark first block as processed when Block phase animation completes
                if (evt.SubPhase == SubPhase.Block)
                {
                    _firstBlockProcessed = true;
                }
                _waitingForAnimation = false;
                CheckAndTriggerNextAttack();
            });
            EventManager.Subscribe<DeleteCachesEvent>(_ => {
                _waitingForAnimation = false;
                _lastSeenContextId = null;
                _lastTurn = -1;
                _firstBlockProcessed = false;
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

            var phaseState = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            if (phaseState == null) return;

            // Detect new turn
            if (phaseState.TurnNumber != _lastTurn)
            {
                _lastTurn = phaseState.TurnNumber;
                _firstBlockProcessed = false;
            }

            // If we're in Block phase and haven't processed first block yet, wait for animation
            if (phaseState.Sub == SubPhase.Block && !_firstBlockProcessed)
            {
                _waitingForAnimation = true;
                _lastSeenContextId = null;
                return; // Don't trigger - wait for BattlePhaseAnimationCompleteEvent
            }

            if (currentContextId != _lastSeenContextId && phaseState.Sub == SubPhase.Block)
            {
                if (!_waitingForAnimation)
                {
                    EventManager.Publish(new TriggerEnemyAttackDisplayEvent { ContextId = currentContextId });
                    _lastSeenContextId = currentContextId;
                }
            }
        }
    }
}

