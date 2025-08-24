using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Queued event that publishes ResolveAttack for a given context.
    /// Completes immediately after publishing.
    /// </summary>
    public class QueuedResolveAttackEvent : EventQueue.IQueuedEvent
    {
        public string Name { get; }
        public object Payload { get; }
        public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

        private readonly string _contextId;

        public QueuedResolveAttackEvent(string contextId)
        {
            _contextId = contextId;
            Name = "Rule.ResolveAttack";
            Payload = contextId;
        }

        public void StartResolving()
        {
            if (!string.IsNullOrEmpty(_contextId))
            {
                EventManager.Publish(new ResolveAttack { ContextId = _contextId });
            }
            State = EventQueue.EventState.Complete;
        }

        public void Update(float deltaSeconds) { }
    }
}


