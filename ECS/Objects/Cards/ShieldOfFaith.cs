using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class ShieldOfFaith : CardBase
    {
        public ShieldOfFaith()
        {
            CardId = "shield_of_faith";
            Name = "Shield of Faith";
            Target = "Player";
            Cost = ["Any"];
            Text = "Gain {9} aegis.";
            Animation = "Buff";
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aegis, Delta = +ValuesParse[0] });
            };
        }
    }
}

