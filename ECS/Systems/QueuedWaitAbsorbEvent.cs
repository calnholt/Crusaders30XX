using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Queued event that waits for the enemy absorb animation to complete (EnemyAbsorbComplete).
    /// It publishes a bus event to start the absorb tween (optional), then completes when the notification arrives.
    /// </summary>
    public class QueuedWaitAbsorbEvent : EventQueue.IQueuedEvent
    {
        public string Name { get; }
        public object Payload { get; }
        public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

        private readonly string _contextId;
        private System.Action<EnemyAbsorbComplete> _handler;

        public QueuedWaitAbsorbEvent(string contextId)
        {
            _contextId = contextId;
            Name = "Rule.WaitAbsorb";
            Payload = contextId;
        }

        public void StartResolving()
        {
            State = EventQueue.EventState.Waiting;
            _handler = OnAbsorbComplete;
            EventManager.Subscribe(_handler);
        }

        public void Update(float deltaSeconds) { }

        private void OnAbsorbComplete(EnemyAbsorbComplete e)
        {
            if (e == null || string.IsNullOrEmpty(e.ContextId)) return;
            if (e.ContextId != _contextId) return;
            EventManager.Unsubscribe(_handler);
            State = EventQueue.EventState.Complete;
        }
    }
}


