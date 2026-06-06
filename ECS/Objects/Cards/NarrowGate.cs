using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class NarrowGate : CardBase
    {
        private int AegisGained = 6;
        private int ScarsGained = 2;
        public NarrowGate()
        {
            CardId = "narrow_gate";
            Name = "Narrow Gate";
            Target = "Enemy";
            Text = $"Gain {AegisGained} aegis, and gain {ScarsGained} scars.";
            Cost = ["White", "Any", "Any"];
            Animation = "Attack";
            Damage = 13;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    AttackCard = card,
 
                    DamageType = ModifyTypeEnum.Attack 
                });
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = +AegisGained });
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Scar, Delta = +ScarsGained });
            };
        }
    }
}

