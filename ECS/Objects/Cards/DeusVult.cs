using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class DeusVult : CardBase
    {
        public DeusVult()
        {
            Name = "Deus Vult";
            CardId = "deus_vult";
            Text = "Deal X damage, where X is {4} times your courage. Lose all courage.";
            Animation = "Attack";
            Type = "Attack";
            Damage = 0;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                var damage = GetDerivedDamage(entityManager, card);
                EventManager.Publish(new SetCourageEvent { Amount = 0 });
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -damage, 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var courage = player.GetComponent<Courage>().Amount;
                return courage * ValuesParse[0];
            };
        }
    }
}