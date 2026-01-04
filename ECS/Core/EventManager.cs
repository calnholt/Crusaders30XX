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
        /// <summary>
        /// Wrapper class to store handler delegate with its priority
        /// </summary>
        private class PrioritizedHandler
        {
            public Delegate Handler { get; set; }
            public int Priority { get; set; }
        }

        private static readonly Dictionary<Type, List<PrioritizedHandler>> _eventHandlers = new();
        
        /// <summary>
        /// Subscribe to an event type with optional priority.
        /// Higher priority handlers execute first. Default priority is 0.
        /// </summary>
        /// <param name="handler">The handler to subscribe</param>
        /// <param name="priority">Execution priority (higher = earlier). Default: 0</param>
        public static void Subscribe<T>(Action<T> handler, int priority = 0) where T : class
        {
            var eventType = typeof(T);
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<PrioritizedHandler>();
            }
            _eventHandlers[eventType].Add(new PrioritizedHandler 
            { 
                Handler = handler, 
                Priority = priority 
            });
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType].RemoveAll(ph => ph.Handler.Equals(handler));
            }
        }
        
        /// <summary>
        /// Publish an event to all subscribers in priority order (highest first)
        /// </summary>
        public static void Publish<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            if (_eventHandlers.ContainsKey(eventType))
            {
                // Sort by priority descending (highest priority first)
                var sortedHandlers = _eventHandlers[eventType]
                    .OrderByDescending(ph => ph.Priority)
                    .ToList();

                foreach (var prioritizedHandler in sortedHandlers)
                {
                    try
                    {
                        ((Action<T>)prioritizedHandler.Handler)(eventData);
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