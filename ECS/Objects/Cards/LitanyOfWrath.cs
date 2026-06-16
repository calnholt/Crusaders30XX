using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class LitanyOfWrath : CardBase
    {
        private int AggressionGained = 3;
        public LitanyOfWrath()
        {
            CardId = "litany_of_wrath";
            Rarity = Rarity.Starter;
            Name = "Litany of Wrath";
            Target = "Player";
            Text = $"Gain {AggressionGained} aggression.";
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
                    Type = AppliedPassiveType.Aggression,
                    Delta = AggressionGained
                });
            };
        }
    }
}
