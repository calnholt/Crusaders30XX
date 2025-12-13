using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class NarrowGate : CardBase
    {
        public NarrowGate()
        {
            CardId = "narrow_gate";
            Name = "Narrow Gate";
            Target = "Enemy";
            Text = "Gain {6} aegis, and gain {1} penance.";
            Cost = ["White", "Any", "Any"];
            Animation = "Attack";
            Type = "Attack";
            Damage = 25;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = +ValuesParse[0] });
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Penance, Delta = +ValuesParse[1] });
            };
        }
    }
}

