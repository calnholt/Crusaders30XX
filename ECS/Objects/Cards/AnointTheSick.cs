using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class AnointTheSick : CardBase
    {
        private int Heal = 4;
        public AnointTheSick()
        {
            CardId = "anoint_the_sick";
            Name = "Anoint the Sick";
            Target = "Player";
            Text = $"Heal {Heal} HP.";
            IsFreeAction = true;
            Animation = "Buff";
            Type = CardType.Prayer;
            Block = 2;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = player, 
                    Delta = +Heal, 
                    DamageType = ModifyTypeEnum.Heal 
                });
            };
        }

        
    }
}
