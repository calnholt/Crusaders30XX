using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class CrimsonRite : CardBase
    {
        public CrimsonRite()
        {
            CardId = "crimson_rite";
            Name = "Crimson Rite";
            Target = "Enemy";
            Cost = ["Black", "Any"];
            Animation = "Attack";
            Damage = 5;
            Block = 3;
            Text = "Heal X HP where X is the damage dealt from this attack.";

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                int damage = GetDerivedDamage(entityManager, card);

                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = player,
                    Target = enemy,
                    Delta = -damage,
                    AttackCard = card,

                    DamageType = ModifyTypeEnum.Attack
                });

                Action<ModifyHpEvent> healHandler = null;
                healHandler = (evt) =>
                {
                    if (evt.Target == enemy && evt.Source == player && evt.DamageType == ModifyTypeEnum.Attack)
                    {
                        EventManager.Unsubscribe(healHandler);
                        int healedAmount = Math.Abs(evt.Delta);
                        if (healedAmount > 0)
                        {
                            EventManager.Publish(new ModifyHpRequestEvent
                            {
                                Source = enemy,
                                Target = player,
                                Delta = healedAmount,
                                DamageType = ModifyTypeEnum.Heal
                            });
                        }
                    }
                };
                EventManager.Subscribe(healHandler);
            };
        }
    }
}
