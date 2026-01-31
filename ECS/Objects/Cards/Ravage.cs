using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Ravage : CardBase
    {
        private int MillAmount = 4;
        public Ravage()
        {
            CardId = "ravage";
            Name = "Ravage";
            Target = "Enemy";
            Text = $"As an additional cost, mill {MillAmount} cards.";
            Cost = ["Any"];
            Animation = "Attack";
            Damage = 8;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                for (int j = 0; j < MillAmount; j++)
                {
                    EventManager.Publish(new MillCardEvent { });
                }
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            CanPlay = (entityManager, card) =>
            {
                var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                var show = deck == null || deck.DrawPile.Count < MillAmount;
                if (show)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {MillAmount} cards in deck!" });
                }
                return !show;
            };
        }
    }
}

