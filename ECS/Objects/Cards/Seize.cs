using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Seize : CardBase
    {
        public Seize()
        {
            CardId = "seize";
            Name = "Seize";
            Target = "Enemy";
            Damage = 6;
            Block = 3;
            Text = "If you have lost courage during this action phase, this gains +{10} damage.";
            Animation = "Attack";
            Type = "Attack";

            OnPlay = (entityManager, card) =>
            {
                var damage = GetDerivedDamage(entityManager, card);
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -damage, 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var battleStateInfo = entityManager.GetEntitiesWithComponent<BattleStateInfo>().FirstOrDefault().GetComponent<BattleStateInfo>();
                battleStateInfo.PhaseTracking.TryGetValue(TrackingTypeEnum.CourageLost.ToString(), out var courageLost);
                return courageLost > 0 ? ValuesParse[0] : 0;
            };


        }
    }
}