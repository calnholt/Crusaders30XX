using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Mace : CardBase
    {
        private int PowerGained = 1;
        public Mace()
        {
            CardId = "mace";
            Name = "Mace";
            Target = "Enemy";
            Text = $"Gain {PowerGained} power.";
            Cost = ["Red"];
            Animation = "Attack";
            Damage = 4;
            IsWeapon = true;
            IsFreeAction = true;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Power, Delta = +PowerGained });
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };
        }
    }
}

