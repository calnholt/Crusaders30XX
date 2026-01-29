using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class SearingRebuke : CardBase
    {
        public int BurnAmount = 2;
        public SearingRebuke()
        {
            CardId = "searing_rebuke";
            Name = "Searing Rebuke";
            Text = $"Apply {BurnAmount} burn to the enemy.";
            Block = 4;
            Animation = "Block";
            Type = CardType.Block;

            OnBlock = (entityManager, card) =>
            {
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Burn, Delta = BurnAmount });
            };
        }
    }
}
