using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Whirlwind : CardBase
    {
        public Whirlwind()
        {
            CardId = "whirlwind";
            Name = "Whirlwind";
            Target = "Enemy";
            Text = "Attacks {2} times.";
            Cost = ["Black"];
            Animation = "Attack";
            Damage = 8;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                var time = 0.5f;
                var numOfHits = ValuesParse[0];
                EventManager.Publish(new EndTurnDisplayEvent { ShowButton = false });
                TimerScheduler.Schedule(time * numOfHits, () => {
                    EventManager.Publish(new EndTurnDisplayEvent { ShowButton = true });
                });
                for (int j = 0; j < numOfHits; j++)
                {
                    TimerScheduler.Schedule(time + (j * time), () => {
                        EventManager.Publish(new ModifyHpRequestEvent { 
                            Source = player, 
                            Target = enemy, 
                            Delta = -GetDerivedDamage(entityManager, card), 
                            DamageType = ModifyTypeEnum.Attack 
                        });
                    });
                }
            };
        }
    }
}

