using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Reconciled : CardBase
    {
        private int DamageBonus = 5;
        public Reconciled()
        {
            CardId = "reconciled";
            Name = "Reconciled";
            Target = "Enemy";
            Text = $"If you have no scars or penance, this attack gains +{DamageBonus} damage.";
            Cost = ["Red", "Red"];
            Animation = "Attack";
            Damage = 9;
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
                passives.TryGetValue(AppliedPassiveType.Scar, out var scar);
                passives.TryGetValue(AppliedPassiveType.Penance, out var penance);
                return scar == 0 && penance == 0 ? DamageBonus : 0;
            };
        }
    }
}

