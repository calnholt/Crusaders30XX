using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;



namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Burn : CardBase
    {
        public Burn()
        {
            CardId = "burn";
            Name = "Burn";
            Target = "Enemy";
            Text = "Apply {4} burn to the enemy. If you have {3}+ courage, gain {1} action point and lose {1} courage.";
            Block = 2;
            Type = CardType.Prayer;
            Animation = "Attack";

            OnPlay = (entityManager, card) =>
            {
                var courage = entityManager.GetEntity("Player").GetComponent<Courage>().Amount;
                EventManager.Publish(new ApplyPassiveEvent { Target = entityManager.GetEntity("Enemy"), Type = AppliedPassiveType.Burn, Delta = ValuesParse[0] });
                if (courage >= ValuesParse[1])
                {
                    EventManager.Publish(new ModifyActionPointsEvent { Delta = ValuesParse[2] });
                    EventManager.Publish(new ModifyCourageEvent { Delta = -ValuesParse[3] });
                }
            };
        }
    }
}