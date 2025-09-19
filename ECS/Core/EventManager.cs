using System;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Core
{
    /// <summary>
    /// Manages event-driven communication between systems
    /// </summary>
    public static class EventManager
    {
        private static readonly Dictionary<Type, List<Delegate>> _eventHandlers = new();
        
        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Delegate>();
            }
            _eventHandlers[eventType].Add(handler);
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType].Remove(handler);
            }
        }
        
        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        public static void Publish<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            if (_eventHandlers.ContainsKey(eventType))
            {
                foreach (var handler in _eventHandlers[eventType].ToList())
                {
                    try
                    {
                        ((Action<T>)handler)(eventData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in event handler: {ex}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Clear all event handlers (useful for cleanup)
        /// </summary>
        public static void Clear()
        {
            _eventHandlers.Clear();
        }
        
        /// <summary>
        /// Get the number of active event handlers for debugging
        /// </summary>
        public static int GetEventHandlerCount()
        {
            return _eventHandlers.Values.Sum(handlers => handlers.Count);
        }
    }
} 