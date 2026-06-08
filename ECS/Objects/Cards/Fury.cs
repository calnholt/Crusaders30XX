
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Fury : CardBase
    {
        public Fury()
        {
            CardId = "fury";
            Name = "Fury";
            Target = "Player";
            Text = "Gain 1 aggression, then double your aggression.";
            IsFreeAction = true;
            Animation = "Buff";
            Block = 3;
            Type = CardType.Prayer;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = 1 });
                player.GetComponent<AppliedPassives>().Passives.TryGetValue(AppliedPassiveType.Aggression, out var amount);
                if (amount > 0)
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = amount });
                }
            };
        }
    }
}
