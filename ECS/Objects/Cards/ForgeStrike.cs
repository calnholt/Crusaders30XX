using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class ForgeStrike : CardBase
    {
        public ForgeStrike()
        {
            CardId = "forge_strike";
            Rarity = Rarity.Starter;
            Name = "Forge Strike";
            Target = "Enemy";
            Animation = "Attack";
            Damage = 7;
            Cost = ["Any", "Any"];
            Block = 2;
            IsFreeAction = true;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = entityManager.GetEntity("Player"),
                    Target = entityManager.GetEntity("Enemy"),
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });
            };
        }
    }
}
