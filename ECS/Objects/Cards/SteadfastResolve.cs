using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class SteadfastResolve : CardBase
    {
        private int VigorGained = 1;

        public SteadfastResolve()
        {
            CardId = "steadfast_resolve";
            Rarity = Rarity.Common;
            Name = "Steadfast Resolve";
            Target = "Player";
            Text = $"Gain {VigorGained} vigor.";
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 3;
            IsFreeAction = false;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = player,
                    Type = AppliedPassiveType.Vigor,
                    Delta = VigorGained
                });
            };
        }
    }
}
