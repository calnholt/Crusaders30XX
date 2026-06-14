using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Hammer : CardBase
    {
        private int VigorGained = 1;

        public Hammer()
        {
            CardId = "hammer";
            Name = "Hammer";
            Target = "Enemy";
            Text = $"Gain {VigorGained} vigor.";
            Cost = ["Any", "Any", "Any"];
            Animation = "Attack";
            Damage = 6;
            IsWeapon = true;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.Vigor,
                    Delta = VigorGained
                });
            };
        }
    }
}
