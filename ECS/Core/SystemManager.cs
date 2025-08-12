using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Core
{
    /// <summary>
    /// Manages all systems in the ECS architecture.
    /// Handles system registration, ordering, and updates.
    /// </summary>
    public class SystemManager
    {
        private readonly List<System> _systems = new();
        private readonly EntityManager _entityManager;
        
        public SystemManager(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }
        
        /// <summary>
        /// Adds a system to the manager
        /// </summary>
        public void AddSystem(System system)
        {
            if (!_systems.Contains(system))
            {
                _systems.Add(system);
            }
        }
        
        /// <summary>
        /// Removes a system from the manager
        /// </summary>
        public void RemoveSystem(System system)
        {
            _systems.Remove(system);
        }
        
        /// <summary>
        /// Gets a system of the specified type
        /// </summary>
        public T GetSystem<T>() where T : System
        {
            return _systems.OfType<T>().FirstOrDefault();
        }
        
        /// <summary>
        /// Gets all systems of the specified type
        /// </summary>
        public IEnumerable<T> GetSystems<T>() where T : System
        {
            return _systems.OfType<T>();
        }
        
        /// <summary>
        /// Gets all systems.
        /// </summary>
        public IEnumerable<System> GetAllSystems()
        {
            return _systems;
        }
        
        /// <summary>
        /// Updates all systems
        /// </summary>
        public void Update(GameTime gameTime)
        {
            foreach (var system in _systems)
            {
                system.Update(gameTime);
            }
        }
        
        /// <summary>
        /// Enables or disables all systems
        /// </summary>
        public void SetAllSystemsActive(bool active)
        {
            foreach (var system in _systems)
            {
                system.SetActive(active);
            }
        }
        
        /// <summary>
        /// Clears all systems
        /// </summary>
        public void Clear()
        {
            _systems.Clear();
        }
        
        /// <summary>
        /// Gets the number of registered systems
        /// </summary>
        public int SystemCount => _systems.Count;
    }
} 