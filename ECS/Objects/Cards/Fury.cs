
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
            Text = "Double your aggression.";
            IsFreeAction = true;
            Animation = "Buff";
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                player.GetComponent<AppliedPassives>().Passives.TryGetValue(AppliedPassiveType.Aggression, out var amount);
                if (amount > 0)
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = amount });
                }
            };
        }
    }
}
