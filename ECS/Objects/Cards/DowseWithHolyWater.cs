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
        private int CourageOnPledgeUpgrade = 1;
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
                if (!IsUpgraded)
                {
                    var delta = courage >= CourageThreshold ? AggressionBonus : Aggression;
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = delta });
                }
                else
                {
                    if (courage >= CourageThreshold)
                    {
                        EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Aggression, Delta = AggressionBonus });
                    }
                }
            };

            OnPledged = (entityManager, card) =>
            {
                if (IsUpgraded)
                {
                    EventManager.Publish(new ModifyCourageRequestEvent { Delta = CourageOnPledgeUpgrade, Type = ModifyCourageType.Gain });
                }
            };

            OnUpgrade = (entityManager, card) =>
            {
                Text = $"When this is pledged, gain {CourageOnPledgeUpgrade} courage. Gain {Aggression} aggression. If you have {CourageThreshold}+ courage, gain {AggressionBonus} aggression instead.";
            };
        }
        
    }
}

