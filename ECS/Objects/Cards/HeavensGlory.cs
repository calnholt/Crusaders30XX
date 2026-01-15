using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class HeavensGlory : CardBase
    {
        private int InfernoAmount = 1;
        private int BurnAmount = 1;
        public HeavensGlory()
        {
            CardId = "heavens_glory";
            Name = "Heaven's Glory";
            Target = "Enemy";
            Text = $"The enemy gains {InfernoAmount} inferno and {BurnAmount} burn.";
            IsFreeAction = true;
            Animation = "Attack";
            Type = CardType.Prayer;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var enemy = entityManager.GetEntity(Target);
                EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Inferno, Delta = InfernoAmount });
                EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Burn, Delta = BurnAmount });
            };
        }
    }
}

