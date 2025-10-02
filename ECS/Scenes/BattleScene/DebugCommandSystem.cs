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

        [DebugAction("Set Battlefield: Desert")]
        public void Debug_SetBattlefield_Desert()
        {
            EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Desert });
        }

        [DebugAction("Set Battlefield: Forest")]
        public void Debug_SetBattlefield_Forest()
        {
            EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Forest });
        }

        [DebugAction("Set Battlefield: Cathedral")]
        public void Debug_SetBattlefield_Cathedral()
        {
            EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Cathedral });
        }

        [DebugActionInt("Player: Deal Damage", Step = 1, Min = 1, Max = 999, Default = 5)]
        public void Debug_PlayerDealDamage(int amount)
        {
            EventManager.Publish(new ModifyHpRequestEvent { Source = EntityManager.GetEntity("Player"), Target = EntityManager.GetEntity("Enemy"), Delta = -System.Math.Abs(amount) });
        }
        [DebugActionInt("Gain AP", Step = 1, Min = 1, Max = 999, Default = 5)]
        public void Debug_GainAp(int amount)
        {
            EventManager.Publish(new ModifyActionPointsEvent { Delta = System.Math.Abs(amount) });
        }
        [DebugActionInt("Apply Burn (enemy)", Step = 1, Min = 1, Max = 999, Default = 3)]
        public void Debug_ApplyBurn(int amount)
        {
            EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Burn, Delta = amount });
        }
        [DebugActionInt("Apply Burn (player)", Step = 1, Min = 1, Max = 999, Default = 3)]
        public void Debug_ApplyBurnPlayer(int amount)
        {
            EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Player"), Type = AppliedPassiveType.Burn, Delta = amount });
        }
        [DebugAction("Activate Temperance")]
        public void Debug_ActivateTemperance()
        {
            EventManager.Publish(new TriggerTemperance { Owner = EntityManager.GetEntity("Player"), AbilityId = "radiance" });
        }
        [DebugAction("Apply Stun")]
        public void Debug_ApplyStun()
        {
            EventManager.Publish(new ApplyPassiveEvent { Target = EntityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Stun, Delta = 1 });
        }
    }
}

