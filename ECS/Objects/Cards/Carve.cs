using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class Carve : CardBase
    {
        public Carve()
        {
            CardId = "carve";
            Name = "Carve";
            Target = "Enemy";
            Text = "This gains +{5} damage for the rest of the quest. {50}% chance this is shuffled back into the deck.";
            Block = 2;
            Damage = 7;
            Type = "Attack";
            Animation = "Attack";

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
                AttackDamageValueService.ApplyDelta(card, +ValuesParse[0], "Carve");
                var random = Random.Shared.Next(0, 100);
                if (random <= ValuesParse[1])
                {
                    entityManager.AddComponent(card, new MarkedForReturnToDeck { Owner = card });
                }   
            };
        }
    }
}