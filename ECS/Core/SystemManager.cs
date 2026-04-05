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
        private readonly List<System> _lateSystems = new();
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
        /// Adds a system that runs after all normal systems have updated.
        /// Use this for systems that must see final Position values (e.g. parallax).
        /// </summary>
        public void AddLateSystem(System system)
        {
            if (!_lateSystems.Contains(system))
            {
                _lateSystems.Add(system);
            }
        }

        /// <summary>
        /// Removes a system from the manager
        /// </summary>
        public void RemoveSystem(System system)
        {
            _systems.Remove(system);
            _lateSystems.Remove(system);
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
            // Iterate over a snapshot to avoid modification during enumeration
            var snapshot = _systems.ToArray();
            foreach (var system in snapshot)
            {
                system.Update(gameTime);
            }
        }

        /// <summary>
        /// Updates late systems (runs after all normal systems).
        /// </summary>
        public void LateUpdate(GameTime gameTime)
        {
            var snapshot = _lateSystems.ToArray();
            foreach (var system in snapshot)
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
            _lateSystems.Clear();
        }
        
        /// <summary>
        /// Gets the number of registered systems
        /// </summary>
        public int SystemCount => _systems.Count;
    }
} 