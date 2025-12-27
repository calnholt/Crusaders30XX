using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public class MedalBase
    {
        public string MedalId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Tooltip { get; set; } = "";
        
        // Track what events this medal is subscribed to for cleanup
        private List<Action<object>> _activeSubscriptions = new();
        
        /// <summary>
        /// Called when the medal is acquired - sets up event subscriptions
        /// </summary>
        public virtual void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            // Override in derived classes to subscribe to events
        }
        
        /// <summary>
        /// Called when the medal is removed - cleans up subscriptions
        /// </summary>
        public virtual void Cleanup()
        {
            // Subscriptions are tracked and cleaned up by derived classes
        }
        
        /// <summary>
        /// Helper to subscribe to an event and track it for cleanup
        /// </summary>
        protected void SubscribeToEvent<T>(Action<T> handler) where T : class
        {
            EventManager.Subscribe(handler);
            // Store for unsubscribing later (need to cast back)
            _activeSubscriptions.Add(obj => handler(obj as T));
        }
        
        /// <summary>
        /// Helper to unsubscribe from a tracked event
        /// </summary>
        protected void UnsubscribeFromEvent<T>(Action<T> handler) where T : class
        {
            EventManager.Unsubscribe(handler);
        }
        
        /// <summary>
        /// Optional: Called when medal is first acquired (before Initialize)
        /// </summary>
        public Action<EntityManager, Entity> OnAcquire { get; protected set; }
        
        /// <summary>
        /// Optional: Called when medal is removed (after Cleanup)
        /// </summary>
        public Action<EntityManager, Entity> OnRemove { get; protected set; }
    }
}

namespace Crusaders30XX.ECS.Objects.Medals
{
    /// <summary>
    /// Gain 1 courage whenever you play a card
    /// </summary>
    public class BerserkerMedal : MedalBase
    {
        private EntityManager _entityManager;
        private Entity _medalEntity;
        private Action<PlayCardRequested> _playCardHandler;

        public BerserkerMedal()
        {
            MedalId = "medal_berserker";
            Name = "Berserker's Medal";
            Description = "Gain 1 courage whenever you play a card.";
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            _entityManager = entityManager;
            _medalEntity = medalEntity;
            
            // Create handler and subscribe
            _playCardHandler = OnCardPlayed;
            EventManager.Subscribe(_playCardHandler);
        }

        private void OnCardPlayed(PlayCardRequested evt)
        {
            // Publish medal triggered event for UI
            EventManager.Publish(new MedalTriggered 
            { 
                MedalEntity = _medalEntity, 
                MedalId = MedalId 
            });
            
            // Grant courage
            EventManager.Publish(new ModifyCourageEvent { Delta = 1 });
        }

        public override void Cleanup()
        {
            if (_playCardHandler != null)
            {
                EventManager.Unsubscribe(_playCardHandler);
            }
        }
    }

}