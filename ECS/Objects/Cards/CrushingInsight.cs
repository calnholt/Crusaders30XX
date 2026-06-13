using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class CrushingInsight : CardBase
    {
        private int DrawAmount = 2;

        public CrushingInsight()
        {
            CardId = "crushing_insight";
            Name = "Crushing Insight";
            Target = "Enemy";
            Text = $"Draw {DrawAmount} cards.";
            Cost = ["Black", "Any"];
            Animation = "Attack";
            Damage = 4;
            Block = 3;
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
                EventManager.Publish(new RequestDrawCardsEvent { Count = DrawAmount });
            };
        }
    }
}
