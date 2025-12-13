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
            Text = "Attacks {4} times.";
            Cost = ["Black"];
            Animation = "Attack";
            Type = "Attack";
            Damage = 5;
            Block = 3;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                var time = 0.5f;
                StateSingleton.PreventClicking = true;
                var numOfHits = ValuesParse[0];
                TimerScheduler.Schedule(time * numOfHits, () => {
                    StateSingleton.PreventClicking = false;
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

