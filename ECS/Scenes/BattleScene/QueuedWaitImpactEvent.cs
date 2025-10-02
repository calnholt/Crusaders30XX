using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Queued event that waits until the enemy attack animation signals impact (EnemyAttackImpactNow) for a context.
    /// </summary>
    public class QueuedWaitImpactEvent : EventQueue.IQueuedEvent
    {
        public string Name { get; }
        public object Payload { get; }
        public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

        private readonly string _contextId;
        private System.Action<EnemyAttackImpactNow> _handler;

        public QueuedWaitImpactEvent(string contextId)
        {
            _contextId = contextId;
            Name = "Rule.WaitImpact";
            Payload = contextId;
        }

        public void StartResolving()
        {
            State = EventQueue.EventState.Waiting;
            _handler = OnImpact;
            EventManager.Subscribe(_handler);
        }

        public void Update(float deltaSeconds) { }

        private void OnImpact(EnemyAttackImpactNow e)
        {
            if (e == null || string.IsNullOrEmpty(e.ContextId)) return;
            if (e.ContextId != _contextId) return;
            EventManager.Unsubscribe(_handler);
            State = EventQueue.EventState.Complete;
        }
    }
}


