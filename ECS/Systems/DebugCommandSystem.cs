using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System that listens to DebugCommandEvent and triggers corresponding actions
    /// </summary>
    public class DebugCommandSystem : Core.System
    {
        public DebugCommandSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<DebugCommandEvent>(OnDebugCommand);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            // Not tied to specific entities
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
        }

        private void OnDebugCommand(DebugCommandEvent evt)
        {
            switch (evt.Command)
            {
                case "DrawCard":
                    EventManager.Publish(new RequestDrawCardsEvent { Count = 1 });
                    break;
            }
        }
    }
}

