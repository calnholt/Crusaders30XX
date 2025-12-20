using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class ArkOfTheCovenant : CardBase
    {
        public ArkOfTheCovenant()
        {
            CardId = "ark_of_the_covenant";
            Name = "Ark of the Covenant";
            Target = "Player";
            Text = "When this card is discarded to pay for a card cost, heal {3} HP. This card is discarded from your hand at the end of your action phase.";
            Animation = "Buff";
            Type = CardType.Relic;
            Block = 1;

            OnCreate = (entityManager, card) => 
            {
                entityManager.AddComponent(card, new MarkedForEndOfTurnDiscard());
            };

            OnDiscardedForCost = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = player, 
                    Delta = +ValuesParse[0], 
                    DamageType = ModifyTypeEnum.Heal 
                });
            };
        }
    }
}
