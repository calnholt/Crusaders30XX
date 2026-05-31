using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Bulwark : CardBase
    {
        private int BlockBonus = 1;

        public Bulwark()
        {
            CardId = "bulwark";
            Name = "Bulwark";
            Target = "Enemy";
            Text = $"This card gains +{BlockBonus} block for the duration of the quest.";
            Block = 3;
            Damage = 2;
            IsFreeAction = true;
            Animation = "Attack";

            OnCreate = (entityManager, card) =>
            {
                var cleanup = new QuestScopedCardModificationCleanup
                {
                    Owner = card,
                    ModificationReason = "Bulwark",
                    UseBlock = true
                };
                cleanup.Initialize(entityManager, card);
                entityManager.AddComponent(card, cleanup);
            };

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
                BlockValueService.ApplyDelta(card, BlockBonus, "Bulwark");
            };
        }
    }
}
