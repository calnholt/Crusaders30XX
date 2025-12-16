using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Vindicate : CardBase
    {
        public Vindicate()
        {
            CardId = "vindicate";
            Name = "Vindicate";
            Target = "Enemy";
            Text = "If you have {5} or more courage, this attack gains +{20} damage and lose all courage.";
            Cost = ["Red", "Any"];
            Animation = "Attack";
            Damage = 25;
            Block = 3;

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
                EventManager.Publish(new SetCourageEvent { Amount = 0 });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var courage = player.GetComponent<Courage>().Amount;
                return courage >= ValuesParse[0] ? ValuesParse[1] : 0;
            };
        }
    }
}

