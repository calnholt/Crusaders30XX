using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class TemperTheBlade : CardBase
    {
        private int SharpenAmount = 5;

        public TemperTheBlade()
        {
            CardId = "temper_the_blade";
            Name = "Temper the Blade";
            Target = "Player";
            Text = $"Sharpen {SharpenAmount}.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.Sharpen,
                    Delta = SharpenAmount
                });
            };
        }
    }
}
