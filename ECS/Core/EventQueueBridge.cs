using System;

namespace Crusaders30XX.ECS.Core
{
    /// <summary>
    /// Generic bridge helpers to interoperate between EventQueue and EventManager without game-specific logic.
    /// </summary>
    public static class EventQueueBridge
    {
        /// <summary>
        /// A queued event that immediately publishes a bus event (via EventManager) when it starts, then completes.
        /// </summary>
        public class QueuedPublish<T> : EventQueue.IQueuedEvent where T : class
        {
            public string Name { get; }
            public object Payload { get; }
            public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

            private readonly T _eventData;

            public QueuedPublish(string name, T eventData)
            {
                Name = name;
                Payload = eventData;
                _eventData = eventData;
            }

            public void StartResolving()
            {
                EventManager.Publish(_eventData);
                State = EventQueue.EventState.Complete;
            }

            public void Update(float deltaSeconds) { }
        }

        /// <summary>
        /// A queued event that waits until a bus event of type T is published, then completes.
        /// </summary>
        public class WaitForEvent<T> : EventQueue.IQueuedEvent where T : class
        {
            public string Name { get; }
            public object Payload { get; }
            public EventQueue.EventState State { get; set; } = EventQueue.EventState.Pending;

            private Action<T> _handler;

            public WaitForEvent(string name, object payload = null)
            {
                Name = name;
                Payload = payload;
            }

            public void StartResolving()
            {
                State = EventQueue.EventState.Waiting;
                _handler = OnEvent;
                EventManager.Subscribe(_handler);
            }

            public void Update(float deltaSeconds) { }

            private void OnEvent(T _)
            {
                // Complete and detach
                State = EventQueue.EventState.Complete;
                EventManager.Unsubscribe(_handler);
            }
        }
    }
}


