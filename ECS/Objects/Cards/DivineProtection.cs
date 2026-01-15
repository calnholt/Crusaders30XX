using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class DivineProtection : CardBase
    {
        private int AegisGained = 4;
        public DivineProtection()
        {
            CardId = "divine_protection";
            Name = "Divine Protection";
            Target = "Player";
            Text = $"Gain {AegisGained} aegis.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Player"), Type = AppliedPassiveType.Aegis, Delta = AegisGained });
            };
        }
    }
}