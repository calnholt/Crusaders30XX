using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Clears per-battle Relentless Strike damage bonus at the start of each queued encounter.
    /// </summary>
    public class RelentlessStrikeBattleResetSystem : Core.System
    {
        private const string RelentlessStrikeReason = "RelentlessStrike";

        public RelentlessStrikeBattleResetSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<StartBattleRequested>(OnStartBattle);
        }

        private void OnStartBattle(StartBattleRequested evt)
        {
            foreach (var card in EntityManager.GetEntitiesWithComponent<RunDeckCard>().ToList())
            {
                AttackDamageValueService.RemoveModification(card, RelentlessStrikeReason);
                if (card.GetComponent<RelentlessStrikeBattleState>() != null)
                {
                    EntityManager.RemoveComponent<RelentlessStrikeBattleState>(card);
                }
            }
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }
    }
}
