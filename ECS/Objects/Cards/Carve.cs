using System;
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
            Text = $"This gains +{DamageBonus} damage for the rest of the quest. {Chance}% chance this is shuffled back into the deck.";
            Block = 3;
            Damage = 3;
            Animation = "Attack";

            OnCreate = (entityManager, card) =>
            {
                var cleanup = new QuestScopedCardModificationCleanup
                {
                    Owner = card,
                    ModificationReason = "Carve",
                    UseBlock = false
                };
                cleanup.Initialize(entityManager, card);
                entityManager.AddComponent(card, cleanup);
            };

            OnPlay = (entityManager, card) =>
            {
                EventManager.Publish(new ModifyHpRequestEvent
                {
                    Source = entityManager.GetEntity("Player"),
                    Target = entityManager.GetEntity("Enemy"),
                    Delta = -GetDerivedDamage(entityManager, card),
                    AttackCard = card,
                    DamageType = ModifyTypeEnum.Attack
                });
                AttackDamageValueService.ApplyDelta(card, +DamageBonus, "Carve");
                var random = Random.Shared.Next(0, 100);
                if (random <= Chance)
                {
                    entityManager.AddComponent(card, new MarkedForReturnToDeck { Owner = card });
                }
            };
        }
    }
}
