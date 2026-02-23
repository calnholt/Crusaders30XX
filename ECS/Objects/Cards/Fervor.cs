using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Fervor : CardBase
    {
        private int CourageThreshold = 5;
        private int DamageBonus = 3;
        public Fervor()
        {
            CardId = "fervor";
            Name = "Fervor";
            Target = "Enemy";
            Cost = ["Red"];
            Animation = "Attack";
            Damage = 6;
            Block = 2;
            Type = CardType.Attack;
            Text = $"If you have {CourageThreshold} or more courage, this attack gains +{DamageBonus} damage.";

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
                var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var courageCmp = player?.GetComponent<Courage>();
                int courage = courageCmp?.Amount ?? 0;
                return courage >= CourageThreshold ? DamageBonus : 0;
            };

        }
    }
}
