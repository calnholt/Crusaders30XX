using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Core
{
    /// <summary>
    /// The main ECS world that manages entities and systems.
    /// This is the primary interface for interacting with the ECS system.
    /// </summary>
    public class World
    {
        public EntityManager EntityManager { get; }
        public SystemManager SystemManager { get; }
        
        public World()
        {
            EntityManager = new EntityManager();
            SystemManager = new SystemManager(EntityManager);
        }
        
        /// <summary>
        /// Updates the entire ECS world
        /// </summary>
        public void Update(GameTime gameTime)
        {
            SystemManager.Update(gameTime);
        }
        
        /// <summary>
        /// Creates a new entity
        /// </summary>
        public Entity CreateEntity(string name = "Entity")
        {
            return EntityManager.CreateEntity(name);
        }
        
        /// <summary>
        /// Destroys an entity
        /// </summary>
        public void DestroyEntity(int entityId)
        {
            EntityManager.DestroyEntity(entityId);
        }
        
        /// <summary>
        /// Adds a component to an entity
        /// </summary>
        public void AddComponent<T>(Entity entity, T component) where T : class, IComponent
        {
            EntityManager.AddComponent(entity, component);
        }
        
        /// <summary>
        /// Removes a component from an entity
        /// </summary>
        public void RemoveComponent<T>(Entity entity) where T : class, IComponent
        {
            EntityManager.RemoveComponent<T>(entity);
        }
        
        /// <summary>
        /// Gets entities with a specific component
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWithComponent<T>() where T : class, IComponent
        {
            return EntityManager.GetEntitiesWithComponent<T>();
        }
        
        /// <summary>
        /// Adds a system to the world
        /// </summary>
        public void AddSystem(System system)
        {
            SystemManager.AddSystem(system);
        }
        
        /// <summary>
        /// Removes a system from the world
        /// </summary>
        public void RemoveSystem(System system)
        {
            SystemManager.RemoveSystem(system);
        }
        
        /// <summary>
        /// Gets a system of the specified type
        /// </summary>
        public T GetSystem<T>() where T : System
        {
            return SystemManager.GetSystem<T>();
        }
        
        /// <summary>
        /// Clears all entities and systems
        /// </summary>
        public void Clear()
        {
            EntityManager.Clear();
            SystemManager.Clear();
        }
    }
} 