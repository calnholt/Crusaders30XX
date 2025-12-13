using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Dagger : CardBase
    {
        public Dagger()
        {
            CardId = "dagger";
            Name = "Dagger";
            Target = "Enemy";
            Text = "As an additional cost, lose {2} courage.";
            IsFreeAction = true;
            Animation = "Attack";
            Type = "Attack";
            Damage = 10;
            IsWeapon = true;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyCourageEvent { Delta = -ValuesParse[0] });
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -Damage, 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            CanPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var courageCmp = player?.GetComponent<Courage>();
                int courage = courageCmp?.Amount ?? 0;
                if (courage < ValuesParse[0])
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {ValuesParse[0]} courage!" });
                    return false;
                }
                return true;
            };
        }
    }
}

