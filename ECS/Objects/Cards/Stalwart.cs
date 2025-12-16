using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Stalwart : CardBase
    {
        public Stalwart()
        {
            CardId = "stalwart";
            Name = "Stalwart";
            Text = "As an additional cost when using this card to block, lose {2} courage.";
            Type = CardType.Block;
            Block = 7;
            
            OnBlock = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyCourageEvent { Delta = -ValuesParse[0] });
            };

            CanPlay = (entityManager, card) =>
            {
                var phase = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
                if (phase.Sub == SubPhase.Block)
                {
                    var player = entityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    int courage = player?.GetComponent<Courage>()?.Amount ?? 0;
                    if (courage < ValuesParse[0])
                    {
                        EventManager.Publish(new CantPlayCardMessage { Message = $"Requires {ValuesParse[0]} courage!" });
                        return false;
                    }
                    return true;
                }
                EventManager.Publish(new CantPlayCardMessage { Message = $"Can only pay during block phase!" });
                return false;
            };
        }
    }
}

