using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class RelentlessStrike : CardBase
    {
        private const string ModificationReason = "RelentlessStrike";
        private int BattleDamageBonus = 4;

        public RelentlessStrike()
        {
            CardId = "relentless_strike";
            Name = "Relentless Strike";
            Target = "Enemy";
            Text = "The first time you play this each battle, it goes to the bottom of your deck. It gains +4 damage for the rest of the battle.";
            Animation = "Attack";
            Damage = 9;
            Block = 3;
            Cost = ["White", "Any"];

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                bool isFirstPlay = card.GetComponent<RelentlessStrikeBattleState>() == null;

                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });

                if (isFirstPlay)
                {
                    entityManager.AddComponent(card, new RelentlessStrikeBattleState { Owner = card });
                    entityManager.AddComponent(card, new MarkedForBottomOfDrawPile { Owner = card });
                    AttackDamageValueService.ApplyDelta(card, BattleDamageBonus, ModificationReason);
                }
            };
        }
    }
}
