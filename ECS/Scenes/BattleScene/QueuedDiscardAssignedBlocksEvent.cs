using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Queued event that kicks off the assigned-blocks-to-discard animation for the current attack context,
    /// then waits until all flights for that context have completed.
    /// </summary>
    public class QueuedDiscardAssignedBlocksEvent : EventQueue.IQueuedEvent
    {
        public string Name { get; }
        public object Payload { get; }
        public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

        private readonly EntityManager _entityManager;
        private readonly string _contextId;

        public QueuedDiscardAssignedBlocksEvent(EntityManager entityManager, string contextId)
        {
            _entityManager = entityManager;
            _contextId = contextId;
            Name = "Rule.DiscardAssignedBlocks";
            Payload = contextId;
        }

        public void StartResolving()
        {
            EventManager.Publish(new DebugCommandEvent { Command = "AnimateAssignedBlocksToDiscard" });
            State = EventQueue.EventState.Waiting;
        }

        public void Update(float deltaSeconds)
        {
            if (State != EventQueue.EventState.Waiting) return;
            bool anyFlights = _entityManager.GetEntitiesWithComponent<CardToDiscardFlight>()
                .Any(e =>
                {
                    var f = e.GetComponent<CardToDiscardFlight>();
                    return f != null && f.ContextId == _contextId && !f.Completed;
                });
            if (!anyFlights)
            {
                State = EventQueue.EventState.Complete;
            }
        }
    }
}


