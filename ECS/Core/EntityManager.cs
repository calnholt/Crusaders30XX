using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Core
{
    /// <summary>
    /// Manages the lifecycle of entities and their components.
    /// Handles entity creation, destruction, and component queries.
    /// </summary>
    public class EntityManager
    {
        private readonly Dictionary<int, Entity> _entities = new();
        private readonly Dictionary<Type, List<Entity>> _componentToEntities = new();
        private int _nextEntityId = 1;
        
        /// <summary>
        /// Creates a new entity and returns it
        /// </summary>
        public Entity CreateEntity(string name = "Entity")
        {
            var entity = new Entity(_nextEntityId++)
            {
                Name = name
            };
            
            _entities[entity.Id] = entity;
            // Auto-tag entity with the current scene for lifecycle cleanup
            try
            {
                var sceneEntity = GetEntitiesWithComponent<SceneState>().FirstOrDefault();
                var currentScene = sceneEntity?.GetComponent<SceneState>()?.Current ?? SceneId.None;
                AddComponent(entity, new OwnedByScene { Scene = currentScene });
            }
            catch { /* best-effort; ignore during early bootstrap */ }
            return entity;
        }
        
        /// <summary>
        /// Destroys an entity and removes all its components
        /// </summary>
        public void DestroyEntity(int entityId)
        {
            if (!_entities.TryGetValue(entityId, out var entity))
                return;
                
            // Remove entity from all component mappings
            foreach (var componentType in entity.GetComponentTypes())
            {
                if (_componentToEntities.TryGetValue(componentType, out var entities))
                {
                    entities.Remove(entity);
                }
            }
            
            _entities.Remove(entityId);
        }

        public void DestroyEntity(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return;
            if (int.TryParse(identifier, out var intId))
            {
                DestroyEntity(intId);
                return;
            }
            var entity = GetEntity(identifier);
            if (entity == null) return;
            DestroyEntity(entity.Id);
        }
        
        /// <summary>
        /// Adds a component to an entity
        /// </summary>
        public void AddComponent<T>(Entity entity, T component) where T : class, IComponent
        {
            component.Owner = entity;
            entity.AddComponent(component);
            
            var componentType = typeof(T);
            if (!_componentToEntities.ContainsKey(componentType))
            {
                _componentToEntities[componentType] = new List<Entity>();
            }
            
            if (!_componentToEntities[componentType].Contains(entity))
            {
                _componentToEntities[componentType].Add(entity);
            }
        }
        
        /// <summary>
        /// Removes a component from an entity
        /// </summary>
        public void RemoveComponent<T>(Entity entity) where T : class, IComponent
        {
            var componentType = typeof(T);
            entity.RemoveComponent<T>();
            
            if (_componentToEntities.TryGetValue(componentType, out var entities))
            {
                entities.Remove(entity);
            }
        }
        
        /// <summary>
        /// Gets all entities that have a specific component
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : class, IComponent
        {
            var componentType = typeof(T);
            return _componentToEntities.TryGetValue(componentType, out var entities) 
                ? entities.Where(e => e.IsActive) 
                : Enumerable.Empty<Entity>();
        }
        
        /// <summary>
        /// Gets all entities that have all of the specified components
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWithComponents(params Type[] componentTypes)
        {
            if (componentTypes.Length == 0)
                return Enumerable.Empty<Entity>();
                
            // Start with entities that have the first component
            var result = GetEntitiesWithComponentType(componentTypes[0]);
            
            // Intersect with entities that have the remaining components
            for (int i = 1; i < componentTypes.Length; i++)
            {
                var entitiesWithComponent = GetEntitiesWithComponentType(componentTypes[i]);
                result = result.Intersect(entitiesWithComponent);
            }
            
            return result.Where(e => e.IsActive);
        }
        
        private IEnumerable<Entity> GetEntitiesWithComponentType(Type componentType)
        {
            return _componentToEntities.TryGetValue(componentType, out var entities) 
                ? entities 
                : Enumerable.Empty<Entity>();
        }
        
        /// <summary>
        /// Gets all active entities
        /// </summary>
        public IEnumerable<Entity> GetAllEntities()
        {
            return _entities.Values.Where(e => e.IsActive);
        }
        
        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        public Entity GetEntity(int entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        /// <summary>
        /// Gets the first active entity by its Name (exact match).
        /// Returns null if not found.
        /// </summary>
        public Entity GetEntity(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _entities.Values.FirstOrDefault(e => e.IsActive && string.Equals(e.Name, name, StringComparison.Ordinal));
        }
        
        /// <summary>
        /// Clears all entities and components
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
            _componentToEntities.Clear();
            _nextEntityId = 1;
        }
    }
} 