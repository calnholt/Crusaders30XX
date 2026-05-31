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
        private int DamageBonus = 1;
        private int Chance = 50;
        public Carve()
        {
            CardId = "carve";
            Name = "Carve";
            Target = "Enemy";
            Text = $"{Chance}% chance this card gains +{DamageBonus} damage for the rest of the run.";
            Block = 3;
            Damage = 2;
            Animation = "Attack";

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = entityManager.GetEntity("Player"), 
                    Target = entityManager.GetEntity("Enemy"), 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
                var random = Random.Shared.Next(0, 100);
                if (random <= Chance)
                {
                    AttackDamageValueService.ApplyDelta(card, +DamageBonus, "Carve");
                }   
            };
        }
    }
}