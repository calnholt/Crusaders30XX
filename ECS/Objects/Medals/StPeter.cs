using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using static Crusaders30XX.ECS.Components.CardData;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public class StPeter : MedalBase
    {
        public StPeter()
        {
            Id = "st_peter";
            Name = "St. Peter the Apostle";
            Text = "Each time you block with six black cards this quest, draw a card.";
            MaxCount = 6;
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            EventManager.Subscribe<CardMoveRequested>(OnCardMoveRequested);
            EntityManager = entityManager;
            MedalEntity = medalEntity;
        }

        private void OnCardMoveRequested(CardMoveRequested evt)
        {
            if (Activated) return;
            if ((evt.Destination == CardZoneType.DiscardPile || evt.Destination == CardZoneType.ExhaustPile) && evt.Card.GetComponent<CardData>()?.Color == CardColor.Black)
            {
                CurrentCount++;
                if (CurrentCount >= MaxCount)
                {
                    EmitActivateEvent();
                }
            }
        }

        public override void Activate()
        {
            Console.WriteLine($"[StPeter] Activate: Drawing 1 card");
            CurrentCount = 0;
            Activated = true;
            EventManager.Publish(new RequestDrawCardsEvent { Count = 1 });
        }
    }
}