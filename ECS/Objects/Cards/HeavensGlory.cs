using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class HeavensGlory : CardBase
    {
        public HeavensGlory()
        {
            CardId = "heavens_glory";
            Name = "Heaven's Glory";
            Target = "Enemy";
            Text = "The enemy gains {1} inferno and {1} burn.";
            IsFreeAction = true;
            Animation = "Attack";
            Type = CardType.Prayer;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var enemy = entityManager.GetEntity(Target);
                EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Inferno, Delta = ValuesParse[0] });
                EventManager.Publish(new ApplyPassiveEvent { Target = enemy, Type = AppliedPassiveType.Burn, Delta = ValuesParse[1] });
            };
        }
    }
}

