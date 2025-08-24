using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Queued event that tells the enemy display to begin its attack animation for a given context.
    /// Completes immediately after publishing.
    /// </summary>
    public class QueuedStartEnemyAttackAnimation : EventQueue.IQueuedEvent
    {
        public string Name { get; }
        public object Payload { get; }
        public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

        private readonly string _contextId;

        public QueuedStartEnemyAttackAnimation(string contextId)
        {
            _contextId = contextId;
            Name = "Rule.StartEnemyAttackAnimation";
            Payload = contextId;
        }

        public void StartResolving()
        {
            EventManager.Publish(new StartEnemyAttackAnimation { ContextId = _contextId });
            State = EventQueue.EventState.Complete;
        }

        public void Update(float deltaSeconds) { }
    }
}


