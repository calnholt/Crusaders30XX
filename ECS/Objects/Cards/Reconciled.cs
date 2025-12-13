using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Reconciled : CardBase
    {
        public Reconciled()
        {
            CardId = "reconciled";
            Name = "Reconciled";
            Target = "Enemy";
            Text = "If you have no penance, this attack gains +{15} damage.";
            Cost = ["Red", "Red"];
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
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var passives = player.GetComponent<AppliedPassives>().Passives;
                passives.TryGetValue(AppliedPassiveType.Penance, out var penance);
                return penance == 0 ? ValuesParse[0] : 0;
            };
        }
    }
}

