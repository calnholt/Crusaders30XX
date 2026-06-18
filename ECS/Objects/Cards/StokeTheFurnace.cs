using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class StokeTheFurnace : CardBase
    {
        private int CourageCost = 2;
        private int VigorGained = 1;
        private int MaxRepeats = 3;
        private int MaxRepeatsUpgrade = 1;

        public StokeTheFurnace()
        {
            CardId = "stoke_the_furnace";
            Rarity = Rarity.Common;
            Name = "Stoke the Furnace";
            Target = "Player";
            Text = $"Lose {CourageCost} courage, gain {VigorGained} vigor. Repeat up to {GetMaxRepeats(IsUpgraded)} times if possible.";
            Animation = "Attack";
            Type = CardType.Attack;
            Damage = 2;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                for (int i = 0; i < GetMaxRepeats(IsUpgraded); i++)
                {
                    var courageCmp = player?.GetComponent<Courage>();
                    int courage = courageCmp?.Amount ?? 0;
                    if (courage < CourageCost) break;

                    EventManager.Publish(new ModifyCourageRequestEvent { Delta = -CourageCost, Type = ModifyCourageType.Spent });
                    EventManager.Publish(new ApplyPassiveEvent
                    {
                        Target = player,
                        Type = AppliedPassiveType.Vigor,
                        Delta = VigorGained
                    });
                }
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });
            };

            OnUpgrade = (entityManager, card) =>
            {
                Text = $"Lose {CourageCost} courage, gain {VigorGained} vigor. Repeat up to {GetMaxRepeats(IsUpgraded)} times if possible.";
            };
        }
        private int GetMaxRepeats(bool isUpgraded)
        {
            return isUpgraded ? MaxRepeats + MaxRepeatsUpgrade : MaxRepeats;
        }
    }
}
