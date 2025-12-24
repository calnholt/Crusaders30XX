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
            Text = "Gain {1} courage. Deal X damage, where X is {2} times your courage";
            Animation = "Attack";
            Damage = 0;
            Block = 2;
            IsFreeAction = true;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                var damage = GetDerivedDamage(entityManager, card);
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
                return (courage + ValuesParse[0]) * ValuesParse[1];
            };
        }
    }
}