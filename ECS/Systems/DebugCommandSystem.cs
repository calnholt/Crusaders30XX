using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System that listens to DebugCommandEvent and triggers corresponding actions
    /// </summary>
    [Crusaders30XX.Diagnostics.DebugTab("Commands")]
    public class DebugCommandSystem : Core.System
    {
        public DebugCommandSystem(EntityManager entityManager) : base(entityManager)
        {
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            // Not tied to specific entities
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
        }

        // Debug actions discoverable by the DebugMenuSystem
        [DebugAction("Draw Card")]
        public void Debug_DrawCard()
        {
            EventManager.Publish(new RequestDrawCardsEvent { Count = 1 });
        }

        [DebugAction("Redraw Hand")]
        public void Debug_RedrawHand()
        {
            EventManager.Publish(new RedrawHandEvent { DrawCount = 4 });
        }

        [DebugAction("+ Courage")]
        public void Debug_IncreaseCourage()
        {
            EventManager.Publish(new ModifyCourageEvent { Delta = 1 });
        }

        [DebugAction("- Courage")]
        public void Debug_DecreaseCourage()
        {
            EventManager.Publish(new ModifyCourageEvent { Delta = -1 });
        }
    }
}

