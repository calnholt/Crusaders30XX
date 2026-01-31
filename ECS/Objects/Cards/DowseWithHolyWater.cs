using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class DowseWithHolyWater : CardBase
    {
        private int Aggression = 1;
        private int CourageThreshold = 5;
        private int AggressionBonus = 4;
        public DowseWithHolyWater()
        {
            CardId = "dowse_with_holy_water";
            Name = "Douse with Holy Water";
            Target = "Player";
            Text = $"Gain {Aggression} aggression. If you have {CourageThreshold}+ courage, gain {AggressionBonus} aggression instead.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var courage = player.GetComponent<Courage>().Amount;
                var delta = courage >= CourageThreshold ? AggressionBonus : Aggression;
                EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = delta });
            };
        }
    }
}

