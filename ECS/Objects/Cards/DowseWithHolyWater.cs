using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class DowseWithHolyWater : CardBase
    {
        public DowseWithHolyWater()
        {
            CardId = "dowse_with_holy_water";
            Name = "Dowse with Holy Water";
            Target = "Player";
            Text = "Gain {5} aggression. If you have {4}+ courage, gain {12} aggression instead.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var courage = player.GetComponent<Courage>().Amount;
                var delta = courage >= ValuesParse[1] ? ValuesParse[2] : ValuesParse[0];
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = delta });
            };
        }
    }
}

