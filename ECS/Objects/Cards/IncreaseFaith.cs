using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class IncreaseFaith : CardBase
    {
        private int PowerGained = 1;
        public IncreaseFaith()
        {
            CardId = "increase_faith";
            Name = "Increase Faith";
            Target = "Player";
            Text = $"Gain {PowerGained} power.";
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent 
                { 
                    Target = player, 
                    Type = AppliedPassiveType.Power, 
                    Delta = PowerGained 
                });
            };
        }
    }
}
